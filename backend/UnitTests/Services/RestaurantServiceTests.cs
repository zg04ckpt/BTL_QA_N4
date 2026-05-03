using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.Services;
using DataAccessLayer;
using DataAccessLayer.Context;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using UnitTests.Infrastructure;
using Xunit;

namespace UnitTests.Services;

// ============================================================
//  Fake repositories – dùng để giả lập lỗi từ repository (TC-RSS-010, TC-RSS-047, TC-RSS-052)
// ============================================================

/// <summary>
/// RestaurantRepository giả: AddRestaurantAsync trả null (TC-RSS-010)
/// </summary>
public class NullReturningRestaurantRepository : RestaurantRepository
{
    public NullReturningRestaurantRepository(ApplicationDbContext context) : base(context) { }

    public override Task<Restaurant?> AddRestaurantAsync(Restaurant restaurant)
        => Task.FromResult<Restaurant?>(null);
}

/// <summary>
/// RestaurantRepository giả: UpdateRestaurantAsync ném DbUpdateException (TC-RSS-047)
/// </summary>
public class ThrowOnUpdateRestaurantRepository : RestaurantRepository
{
    private readonly Exception _throwOnUpdate;

    public ThrowOnUpdateRestaurantRepository(ApplicationDbContext context, Exception throwOnUpdate)
        : base(context)
    {
        _throwOnUpdate = throwOnUpdate;
    }

    public override Task UpdateRestaurantAsync(Restaurant restaurant)
        => throw _throwOnUpdate;
}

/// <summary>
/// RestaurantRepository giả: DeleteRestaurantAsync ném DbUpdateException (TC-RSS-052)
/// </summary>
public class ThrowOnDeleteRestaurantRepository : RestaurantRepository
{
    private readonly Exception _throwOnDelete;

    public ThrowOnDeleteRestaurantRepository(ApplicationDbContext context, Exception throwOnDelete)
        : base(context)
    {
        _throwOnDelete = throwOnDelete;
    }

    public override Task DeleteRestaurantAsync(int id)
        => throw _throwOnDelete;
}

// ============================================================
//  RestaurantServiceTests – kiểm thử đơn vị cho RestaurantService
//  Hạ tầng: SQLite in-memory + transaction rollback
//  + TestFirebaseService (stub thay thế FirebaseService thật)
//  + Mock<IAddressService> cho các trường hợp cần cô lập AddressService
// ============================================================
public class RestaurantServiceTests
{
    // ----------------------------------------------------------
    //  AddressDto mẫu mặc định (dùng xuyên suốt)
    // ----------------------------------------------------------
    private static AddressDto DefaultAddressDto() => new AddressDto
    {
        City = "Hà Nội",
        District = "Cầu Giấy",
        Ward = "Dịch Vọng",
        Detail = "Số 12 phố Duy Tân",
        Lon = 105.7827,
        Lat = 21.0285
    };

    // ----------------------------------------------------------
    //  Helper: seed Category, Address, User (không seed restaurant)
    // ----------------------------------------------------------
    private static async Task SeedCategoryUserAddressAsync(ApplicationDbContext ctx,
        int cateId = 1, int userId = 100, int addressId = 1,
        string city = "Hà Nội")
    {
        ctx.Categories.Add(new Category { Id = cateId, Name = "Món Việt" });
        ctx.Addresses.Add(new Address { Id = addressId, City = city, District = "Quận Test", Ward = "Phường Test", Detail = "Số 1 đường Test" });
        ctx.Users.Add(new User
        {
            Id = userId, Email = $"owner{userId}@test.vn", Password = "123",
            Role = "restaurant_owner", Name = $"Chủ nhà hàng {userId}", Status = 1
        });
        await ctx.SaveChangesAsync();
    }

    // Helper: tạo CreateRestaurantDto với AddressDto mặc định
    private static CreateRestaurantDto MakeCreateDto(
        string name = "Phở Thìn Bờ Hồ",
        int cateId = 1, int userId = 100,
        List<string>? photos = null) => new CreateRestaurantDto
        {
            Name = name,
            Status = 0,
            Email = "test@test.vn",
            Description = "Mô tả",
            PhoneNumber = "0241234567",
            AvtImage = "avt.jpg",
            CateId = cateId,
            UserId = userId,
            Address = DefaultAddressDto(),
            RestaurantPhotos = photos ?? new List<string>()
        };

    // Helper: tạo UpdateRestaurantDto cơ bản
    private static UpdateRestaurantDto MakeUpdateDto(
        string name = "Tên mới", int status = 1, int cateId = 1,
        List<string>? photos = null) => new UpdateRestaurantDto
        {
            Name = name,
            Email = "moi@test.vn",
            Description = "Mô tả mới",
            PhoneNumber = "0988111222",
            Status = status,
            AvtImage = "new_avt.jpg",
            CateId = cateId,
            Address = DefaultAddressDto(),
            RestaurantPhotos = photos ?? new List<string>()
        };

    // Helper: tạo RestaurantService thật với SQLite (không có Firebase calls)
    private static RestaurantService BuildService(
        ApplicationDbContext ctx,
        IAddressService? addressService = null,
        FirebaseService? firebaseService = null)
    {
        var restaurantRepo = new RestaurantRepository(ctx);
        var addrSvc = addressService ?? new AddressService(new AddressRepository(ctx));
        var fbSvc = firebaseService ?? new TestFirebaseService();
        return new RestaurantService(restaurantRepo, addrSvc, fbSvc);
    }

    // ===========================================================
    //  AddRestaurantAsync
    // ===========================================================

    /// <summary>TC-RSS-001 – Tạo nhà hàng thành công với dữ liệu hợp lệ</summary>
    [Fact]
    public async Task AddRestaurantAsync_ValidDto_CreatesRestaurant()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedCategoryUserAddressAsync(context);

            var dto = MakeCreateDto("Phở Thìn Bờ Hồ", photos: new List<string> { "p1.jpg", "p2.jpg" });

            var result = await service.AddRestaurantAsync(dto);

            Assert.NotNull(result);
            Assert.True(result!.Id > 0);
            Assert.Equal("Phở Thìn Bờ Hồ", result.Name);
            Assert.Equal("test@test.vn", result.Email);
            Assert.Equal("0241234567", result.PhoneNumber);

            // CheckDB: nhà hàng tồn tại
            Assert.Equal(1, await context.Restaurants.CountAsync());
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-002 – Service luôn ép Status = 0, dù DTO gửi Status = 99</summary>
    [Fact]
    public async Task AddRestaurantAsync_StatusInDtoIgnored_AlwaysZero()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedCategoryUserAddressAsync(context);

            var dto = MakeCreateDto("Quán Bún Cá");
            dto.Status = 99;  // Phải bị bỏ qua

            var result = await service.AddRestaurantAsync(dto);

            Assert.Equal(0, result!.Status);

            // CheckDB
            var dbRecord = await context.Restaurants.FindAsync(result.Id);
            Assert.Equal(0, dbRecord!.Status);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-003 – AverageScore = 0 và TotalReviews = 0 khi mới tạo</summary>
    [Fact]
    public async Task AddRestaurantAsync_NewRestaurant_InitialScoresAreZero()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedCategoryUserAddressAsync(context);
            var result = await service.AddRestaurantAsync(MakeCreateDto("Quán Cơm Tấm"));

            Assert.Equal(0, result!.AverageScore);
            Assert.Equal(0, result.TotalReviews);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-004 – Tạo Address qua AddressService và gán đúng AddressId cho Restaurant</summary>
    [Fact]
    public async Task AddRestaurantAsync_CreatesAddress_AndLinksAddressIdToRestaurant()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Mon Viet" });
            context.Users.Add(new User { Id = 100, Email = "o@test.vn", Password = "123", Role = "restaurant_owner", Status = 1 });
            await context.SaveChangesAsync();

            var dto = MakeCreateDto("Quán Hải Sản");
            dto.Address = new AddressDto
            {
                City = "Đà Nẵng", District = "Sơn Trà", Ward = "Phước Mỹ",
                Detail = "Số 5 đường Võ Nguyên Giáp", Lon = 108.245, Lat = 16.060
            };

            var result = await service.AddRestaurantAsync(dto);

            // CheckDB: 1 Address được tạo
            Assert.Equal(1, await context.Addresses.CountAsync());
            var dbAddr = await context.Addresses.FirstAsync();
            Assert.Equal("Đà Nẵng", dbAddr.City);
            Assert.Equal("Số 5 đường Võ Nguyên Giáp", dbAddr.Detail);

            // AddressId của restaurant khớp
            Assert.Equal(dbAddr.Id, result!.AddressId);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-005 – Lưu đủ số lượng ảnh (3 ảnh)</summary>
    [Fact]
    public async Task AddRestaurantAsync_WithPhotos_SavesAllPhotos()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedCategoryUserAddressAsync(context);

            var dto = MakeCreateDto("Quán Nướng",
                photos: new List<string> { "img1.jpg", "img2.jpg", "img3.jpg" });

            var result = await service.AddRestaurantAsync(dto);

            Assert.Equal(3, result!.RestaurantPhotos!.Count);
            var urls = result.RestaurantPhotos.Select(p => p.ImageUrl).ToHashSet();
            Assert.Contains("img1.jpg", urls);
            Assert.Contains("img2.jpg", urls);
            Assert.Contains("img3.jpg", urls);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-006 – Tạo được khi danh sách ảnh rỗng</summary>
    [Fact]
    public async Task AddRestaurantAsync_EmptyPhotos_CreatesRestaurantWithNoPhotos()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedCategoryUserAddressAsync(context);
            var result = await service.AddRestaurantAsync(MakeCreateDto("Quán Chay", photos: new List<string>()));

            Assert.NotNull(result);
            Assert.NotNull(result!.RestaurantPhotos);
            Assert.Empty(result.RestaurantPhotos!);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-007 – Lưu đúng AvtImage URL</summary>
    [Fact]
    public async Task AddRestaurantAsync_AvtImageSaved_Correctly()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedCategoryUserAddressAsync(context);
            var dto = MakeCreateDto("Quán Lẩu");
            dto.AvtImage = "https://cdn.example.com/avt-nhahang.png";

            var result = await service.AddRestaurantAsync(dto);

            Assert.Equal("https://cdn.example.com/avt-nhahang.png", result!.AvtImage);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-008 – Ném DbUpdateException khi CateId/UserId không tồn tại</summary>
    [Fact]
    public async Task AddRestaurantAsync_NonexistentUserId_ThrowsDbUpdateException()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            await context.SaveChangesAsync();
            // Không seed User 999

            var dto = MakeCreateDto();
            dto.UserId = 999;

            await Assert.ThrowsAsync<DbUpdateException>(() => service.AddRestaurantAsync(dto));

            // CheckDB: không có restaurant
        }
        finally { await transaction.RollbackAsync(); }

        await using var v = SqliteMemoryDb.CreateContext(connection);
        Assert.Equal(0, await v.Restaurants.CountAsync());
    }

    /// <summary>TC-RSS-009 – AddressService ném exception → service dừng và lan truyền lỗi</summary>
    [Fact]
    public async Task AddRestaurantAsync_AddressServiceThrows_PropagatesException()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        // Mock IAddressService để ném lỗi
        var mockAddrSvc = new Mock<IAddressService>();
        mockAddrSvc.Setup(a => a.AddAddressAsync(It.IsAny<AddressDto>()))
                   .ThrowsAsync(new Exception("address error"));

        var service = new RestaurantService(new RestaurantRepository(context), mockAddrSvc.Object, new TestFirebaseService());

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedCategoryUserAddressAsync(context);
            var dto = MakeCreateDto("Test Addr");

            var ex = await Assert.ThrowsAsync<Exception>(() => service.AddRestaurantAsync(dto));
            Assert.Equal("address error", ex.Message);

            // CheckDB: không có restaurant
            Assert.Equal(0, await context.Restaurants.CountAsync());
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-010 – Service trả null khi repository.AddRestaurantAsync trả null</summary>
    [Fact]
    public async Task AddRestaurantAsync_RepositoryReturnsNull_ServiceReturnsNull()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        // Mock IAddressService trả address giả
        var mockAddrSvc = new Mock<IAddressService>();
        mockAddrSvc.Setup(a => a.AddAddressAsync(It.IsAny<AddressDto>()))
                   .ReturnsAsync(new Address { Id = 5, City = "Hà Nội" });

        var nullRepo = new NullReturningRestaurantRepository(context);
        var service = new RestaurantService(nullRepo, mockAddrSvc.Object, new TestFirebaseService());

        var dto = MakeCreateDto("Test Null");

        var result = await service.AddRestaurantAsync(dto);

        Assert.Null(result);
    }

    /// <summary>TC-RSS-011 – Email/SĐT sai định dạng vẫn được lưu (không validate tại service)</summary>
    [Fact]
    public async Task AddRestaurantAsync_InvalidEmailPhone_SavedWithoutValidation()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedCategoryUserAddressAsync(context);
            var dto = MakeCreateDto("Test Format");
            dto.Email = "khongphaiemail";
            dto.PhoneNumber = "abc";

            var result = await service.AddRestaurantAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("khongphaiemail", result!.Email);
            Assert.Equal("abc", result.PhoneNumber);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-012 – Lưu đúng tên tiếng Việt có dấu và emoji</summary>
    [Fact]
    public async Task AddRestaurantAsync_VietnameseNameWithEmoji_SavedCorrectly()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedCategoryUserAddressAsync(context);
            var dto = MakeCreateDto("Bún Bò Huế Cô Ba 🍜");
            dto.Description = "Quán nhỏ nằm trong ngõ, không gian ấm cúng – đậm đà hương vị Huế";

            var result = await service.AddRestaurantAsync(dto);

            Assert.Equal("Bún Bò Huế Cô Ba 🍜", result!.Name);
            Assert.Equal("Quán nhỏ nằm trong ngõ, không gian ấm cúng – đậm đà hương vị Huế", result.Description);
        }
        finally { await transaction.RollbackAsync(); }
    }

    // ===========================================================
    //  GetRestaurantsByCategoryAsync
    // ===========================================================

    /// <summary>TC-RSS-013 – Trả đúng 3 nhà hàng thuộc danh mục 1, loại bỏ danh mục khác</summary>
    [Fact]
    public async Task GetRestaurantsByCategoryAsync_ReturnsOnlyMatchingCategory()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.AddRange(
                new Category { Id = 1, Name = "Món Việt" },
                new Category { Id = 2, Name = "Món Hàn" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();

            context.Restaurants.AddRange(
                new Restaurant { Id = 1, Name = "Phở Thìn", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, Name = "Bún Chả", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, Name = "Cơm Tấm", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 4, Name = "Kimbap", CateId = 2, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 5, Name = "BBQ Hàn", CateId = 2, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            var result = (await service.GetRestaurantsByCategoryAsync(1)).ToList();

            Assert.Equal(3, result.Count);
            var ids = result.Select(r => r.Id).ToHashSet();
            Assert.Contains(1, ids);
            Assert.Contains(2, ids);
            Assert.Contains(3, ids);
            var names = result.Select(r => r.Name).ToHashSet();
            Assert.Contains("Phở Thìn", names);
            Assert.Contains("Bún Chả", names);
            Assert.Contains("Cơm Tấm", names);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-014 – Trả rỗng khi danh mục chưa có nhà hàng nào</summary>
    [Fact]
    public async Task GetRestaurantsByCategoryAsync_EmptyCategory_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 3, Name = "Món Nhật" });
            await context.SaveChangesAsync();

            var result = await service.GetRestaurantsByCategoryAsync(3);

            Assert.NotNull(result);
            Assert.False(result.Any());
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-015 – Trả rỗng, không ném exception khi categoryId không tồn tại</summary>
    [Fact]
    public async Task GetRestaurantsByCategoryAsync_NonexistentCategory_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        var result = await service.GetRestaurantsByCategoryAsync(999);

        Assert.NotNull(result);
        Assert.False(result.Any());
    }

    /// <summary>TC-RSS-016 – Ánh xạ đúng các field của RestaurantDto</summary>
    [Fact]
    public async Task GetRestaurantsByCategoryAsync_SingleRestaurant_MapsFieldsCorrectly()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Món Việt" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();

            context.Restaurants.Add(new Restaurant
            {
                Id = 10, Name = "Quán Lẩu Thái",
                Email = "lau@test.vn", PhoneNumber = "0987654321",
                AverageScore = 4.5f, TotalReviews = 20,
                CateId = 1, UserId = 100, AddressId = 1, Status = 1
            });
            await context.SaveChangesAsync();

            var result = (await service.GetRestaurantsByCategoryAsync(1)).ToList();

            Assert.Single(result);
            Assert.Equal(10, result[0].Id);
            Assert.Equal("Quán Lẩu Thái", result[0].Name);
            Assert.Equal("lau@test.vn", result[0].Email);
            Assert.Equal("0987654321", result[0].PhoneNumber);
            Assert.Equal(4.5, (double)result[0].AverageScore!.Value, 1);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-017 – categoryId âm/0 trả rỗng</summary>
    [Fact]
    public async Task GetRestaurantsByCategoryAsync_NegativeOrZeroId_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            var r1 = await service.GetRestaurantsByCategoryAsync(-1);
            var r2 = await service.GetRestaurantsByCategoryAsync(0);

            Assert.NotNull(r1); Assert.False(r1.Any());
            Assert.NotNull(r2); Assert.False(r2.Any());
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-018 – Trả mọi nhà hàng bất kể Status (không lọc)</summary>
    [Fact]
    public async Task GetRestaurantsByCategoryAsync_AllStatuses_ReturnsAll()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 0 },
                new Restaurant { Id = 2, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, CateId = 1, UserId = 100, AddressId = 1, Status = -1 });
            await context.SaveChangesAsync();

            var result = (await service.GetRestaurantsByCategoryAsync(1)).ToList();

            Assert.Equal(3, result.Count);
        }
        finally { await transaction.RollbackAsync(); }
    }

    // ===========================================================
    //  GetRestaurantsByUserAsync
    // ===========================================================

    /// <summary>TC-RSS-019 – Trả đúng các nhà hàng thuộc user, không lẫn user khác</summary>
    [Fact]
    public async Task GetRestaurantsByUserAsync_ReturnsOnlyUserRestaurants()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.AddRange(
                new User { Id = 100, Email = "u100@x.vn", Password = "1", Role = "r", Status = 1 },
                new User { Id = 101, Email = "u101@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, Name = "Quán A", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, Name = "Quán B", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, Name = "Quán C", CateId = 1, UserId = 101, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            var result = (await service.GetRestaurantsByUserAsync(100)).ToList();

            Assert.Equal(2, result.Count);
            var ids = result.Select(r => r.Id).ToHashSet();
            Assert.Contains(1, ids);
            Assert.Contains(2, ids);
            Assert.DoesNotContain(3, ids);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-020 – Trả rỗng khi user chưa có nhà hàng</summary>
    [Fact]
    public async Task GetRestaurantsByUserAsync_UserWithNoRestaurants_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Users.Add(new User { Id = 102, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();

            var result = await service.GetRestaurantsByUserAsync(102);

            Assert.NotNull(result);
            Assert.False(result.Any());
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-021 – Trả rỗng khi userId không tồn tại</summary>
    [Fact]
    public async Task GetRestaurantsByUserAsync_NonexistentUser_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        var result = await service.GetRestaurantsByUserAsync(9999);

        Assert.NotNull(result);
        Assert.False(result.Any());
    }

    /// <summary>TC-RSS-022 – userId âm/0 trả rỗng, không ném exception</summary>
    [Fact]
    public async Task GetRestaurantsByUserAsync_NegativeOrZeroUserId_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        var r1 = await service.GetRestaurantsByUserAsync(-5);
        var r2 = await service.GetRestaurantsByUserAsync(0);

        Assert.False(r1.Any());
        Assert.False(r2.Any());
    }

    /// <summary>TC-RSS-023 – Bao gồm nhà hàng ở mọi trạng thái (không lọc theo Status)</summary>
    [Fact]
    public async Task GetRestaurantsByUserAsync_IncludesAllStatuses()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 0 },
                new Restaurant { Id = 2, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, CateId = 1, UserId = 100, AddressId = 1, Status = -1 });
            await context.SaveChangesAsync();

            var result = (await service.GetRestaurantsByUserAsync(100)).ToList();

            Assert.Equal(3, result.Count);
        }
        finally { await transaction.RollbackAsync(); }
    }

    // ===========================================================
    //  SearchRestaurantsAsync
    // ===========================================================

    /// <summary>TC-RSS-024 – Khớp theo tên (Contains)</summary>
    [Fact]
    public async Task SearchRestaurantsAsync_ByName_ReturnsMatchingRestaurants()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, Name = "Phở Bát Đàn", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, Name = "Bún Chả Hàng Mành", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, Name = "Phở Gia Truyền", CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            var result = (await service.SearchRestaurantsAsync("Phở")).ToList();

            Assert.Equal(2, result.Count);
            var names = result.Select(r => r.Name).ToHashSet();
            Assert.Contains("Phở Bát Đàn", names);
            Assert.Contains("Phở Gia Truyền", names);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-025 – Không khớp → trả rỗng</summary>
    [Fact]
    public async Task SearchRestaurantsAsync_NoMatch_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, Name = "Phở Bát Đàn", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, Name = "Bún Chả Hàng Mành", CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            var result = await service.SearchRestaurantsAsync("KhôngTồnTại");

            Assert.NotNull(result);
            Assert.False(result.Any());
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-026 – Chuỗi rỗng → trả toàn bộ nhà hàng</summary>
    [Fact]
    public async Task SearchRestaurantsAsync_EmptyString_ReturnsAllRestaurants()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, Name = "R1", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, Name = "R2", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, Name = "R3", CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            var result = (await service.SearchRestaurantsAsync("")).ToList();

            Assert.Equal(3, result.Count);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-027 – searchTerm = null → ném ArgumentNullException</summary>
    [Fact]
    public async Task SearchRestaurantsAsync_NullSearchTerm_ThrowsArgumentNullException()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant { Id = 1, Name = "Phở", CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            // SQLite LIKE với null sẽ ném ArgumentNullException từ EF Core
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => service.SearchRestaurantsAsync(null!));
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-028 – Khớp tiếng Việt có dấu đầy đủ</summary>
    [Fact]
    public async Task SearchRestaurantsAsync_VietnameseAccented_ReturnsMatch()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant
            {
                Id = 1, Name = "Cơm Tấm Sườn Bì Chả",
                CateId = 1, UserId = 100, AddressId = 1, Status = 1
            });
            await context.SaveChangesAsync();

            var result = (await service.SearchRestaurantsAsync("Sườn")).ToList();

            Assert.Single(result);
            Assert.Equal("Cơm Tấm Sườn Bì Chả", result[0].Name);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-029 – searchTerm có khoảng trắng đầu/cuối không tự trim → trả rỗng (ghi nhận hành vi)</summary>
    [Fact]
    public async Task SearchRestaurantsAsync_LeadingTrailingSpace_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant
            {
                Id = 1, Name = "Phở Bát Đàn",
                CateId = 1, UserId = 100, AddressId = 1, Status = 1
            });
            await context.SaveChangesAsync();

            var result = await service.SearchRestaurantsAsync(" Phở ");

            Assert.NotNull(result);
            Assert.False(result.Any());  // Hành vi hiện hành: không trim
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-030 – Khớp theo City của Address (searchTerm = "Đà")</summary>
    [Fact]
    public async Task SearchRestaurantsAsync_ByAddressCity_ReturnsMatch()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.AddRange(
                new Address { Id = 1, City = "Đà Nẵng" },
                new Address { Id = 2, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, Name = "Quán Ăn Miền Trung", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, Name = "Quán Miền Bắc", CateId = 1, UserId = 100, AddressId = 2, Status = 1 });
            await context.SaveChangesAsync();

            var result = (await service.SearchRestaurantsAsync("Đà")).ToList();

            Assert.Single(result);
            Assert.Equal("Quán Ăn Miền Trung", result[0].Name);
        }
        finally { await transaction.RollbackAsync(); }
    }

    // ===========================================================
    //  GetRestaurantByIdAsync
    // ===========================================================

    /// <summary>TC-RSS-031 – Trả RestaurantDetailDto đầy đủ tất cả field</summary>
    [Fact]
    public async Task GetRestaurantByIdAsync_ExistingRestaurant_ReturnsFullDetail()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Món Việt" });
            context.Addresses.Add(new Address
            {
                Id = 1, City = "Hà Nội", District = "Tây Hồ",
                Ward = "Quảng An", Detail = "Số 6 đường Hồ Tây",
                Lon = 105.82, Lat = 21.06
            });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant
            {
                Id = 500, Name = "Nhà hàng Sen Tây Hồ",
                Email = "sen@test.vn", Description = "Buffet cao cấp",
                PhoneNumber = "0909123123", Status = 1, AvtImage = "avt.jpg",
                AverageScore = 4.7f, TotalReviews = 120,
                UserId = 100, CateId = 1, AddressId = 1
            });
            await context.SaveChangesAsync();

            var result = await service.GetRestaurantByIdAsync(500);

            Assert.NotNull(result);
            Assert.Equal(500, result!.Id);
            Assert.Equal("Nhà hàng Sen Tây Hồ", result.Name);
            Assert.Equal("sen@test.vn", result.Email);
            Assert.Equal("Buffet cao cấp", result.Description);
            Assert.Equal("0909123123", result.PhoneNumber);
            Assert.Equal(1, result.Status);
            Assert.Equal("avt.jpg", result.AvtImage);
            Assert.Equal("Món Việt", result.Category);
            Assert.Equal(4.7, (double)result.AverageScore!.Value, 1);
            Assert.Equal(120, result.TotalReviews);
            Assert.Equal(100, result.UserId);
            Assert.Equal("Hà Nội", result.Address!.City);
            Assert.Equal("Tây Hồ", result.Address.District);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-032 – Trả null khi nhà hàng không tồn tại</summary>
    [Fact]
    public async Task GetRestaurantByIdAsync_NonexistentId_ReturnsNull()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        var result = await service.GetRestaurantByIdAsync(999);

        Assert.Null(result);
    }

    /// <summary>TC-RSS-033 – Category = null khi CateId trỏ tới category không tồn tại (orphan FK)</summary>
    [Fact]
    public async Task GetRestaurantByIdAsync_OrphanCateId_CategoryIsNull()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            // Không seed Category 99 → orphan FK
            context.Categories.Add(new Category { Id = 99, Name = "Orphan" }); // thêm để SQLite không lỗi FK
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant
            {
                Id = 501, Name = "Quán Lạ", CateId = 99, UserId = 100, AddressId = 1, Status = 1
            });
            await context.SaveChangesAsync();
            // Xóa category để tạo orphan
            context.Categories.Remove(context.Categories.Find(99)!);
            await context.SaveChangesAsync();

            var result = await service.GetRestaurantByIdAsync(501);

            Assert.NotNull(result);
            Assert.Null(result!.Category);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-034 – Trả đúng danh sách ảnh theo thứ tự</summary>
    [Fact]
    public async Task GetRestaurantByIdAsync_WithPhotos_ReturnsAllPhotosInOrder()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant
            {
                Id = 502, Name = "Quán Ảnh", CateId = 1, UserId = 100, AddressId = 1, Status = 1
            });
            await context.SaveChangesAsync();
            context.RestaurantPhotos.AddRange(
                new RestaurantPhoto { Id = 1, RestaurantId = 502, ImageUrl = "a.jpg" },
                new RestaurantPhoto { Id = 2, RestaurantId = 502, ImageUrl = "b.jpg" },
                new RestaurantPhoto { Id = 3, RestaurantId = 502, ImageUrl = "c.jpg" });
            await context.SaveChangesAsync();

            var result = await service.GetRestaurantByIdAsync(502);

            Assert.Equal(3, result!.RestaurantPhotos!.Count);
            var urls = result.RestaurantPhotos.ToHashSet();
            Assert.Contains("a.jpg", urls);
            Assert.Contains("b.jpg", urls);
            Assert.Contains("c.jpg", urls);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-035 – RestaurantPhotos không null và Count = 0 khi nhà hàng không có ảnh</summary>
    [Fact]
    public async Task GetRestaurantByIdAsync_NoPhotos_ReturnsEmptyPhotoList()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant
            {
                Id = 503, CateId = 1, UserId = 100, AddressId = 1, Status = 1
            });
            await context.SaveChangesAsync();

            var result = await service.GetRestaurantByIdAsync(503);

            Assert.NotNull(result);
            Assert.NotNull(result!.RestaurantPhotos);
            Assert.Empty(result.RestaurantPhotos!);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-036 – id âm/0 trả null</summary>
    [Fact]
    public async Task GetRestaurantByIdAsync_NegativeOrZeroId_ReturnsNull()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        var r1 = await service.GetRestaurantByIdAsync(-1);
        var r2 = await service.GetRestaurantByIdAsync(0);

        Assert.Null(r1);
        Assert.Null(r2);
    }

    /// <summary>TC-RSS-037 – Ánh xạ đúng Address, AverageScore, TotalReviews</summary>
    [Fact]
    public async Task GetRestaurantByIdAsync_MapsAddressAndScoreCorrectly()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address
            {
                Id = 7, City = "Hải Phòng", District = "Lê Chân",
                Ward = "Hồ Nam", Detail = "12 Tô Hiệu", Lon = 106.68, Lat = 20.85
            });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant
            {
                Id = 504, AverageScore = 3.25f, TotalReviews = 8,
                AddressId = 7, CateId = 1, UserId = 100, Status = 1
            });
            await context.SaveChangesAsync();

            var result = await service.GetRestaurantByIdAsync(504);

            Assert.Equal("Hải Phòng", result!.Address!.City);
            Assert.Equal("Lê Chân", result.Address.District);
            Assert.Equal("Hồ Nam", result.Address.Ward);
            Assert.Equal("12 Tô Hiệu", result.Address.Detail);
            Assert.Equal(3.25, (double)result.AverageScore!.Value, 2);
            Assert.Equal(8, result.TotalReviews);
        }
        finally { await transaction.RollbackAsync(); }
    }

    // ===========================================================
    //  UpdateRestaurantAsync
    // ===========================================================

    /// <summary>TC-RSS-038 – Cập nhật thành công và gửi đúng 1 notification qua TestFirebaseService</summary>
    [Fact]
    public async Task UpdateRestaurantAsync_ValidData_UpdatesAndSendsNotification()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var firebaseSvc = new TestFirebaseService();
        var service = BuildService(context, firebaseService: firebaseSvc);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.AddRange(
                new Category { Id = 1, Name = "Cate 1" },
                new Category { Id = 2, Name = "Cate 2" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội", District = "Cầu Giấy", Ward = "Dịch Vọng", Detail = "Số 1" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant
            {
                Id = 600, Name = "Tên cũ", Email = "old@test.vn",
                Description = "Mô tả cũ", PhoneNumber = "0900000000",
                Status = 0, AvtImage = "old_avt.jpg", CateId = 1, UserId = 100, AddressId = 1
            });
            await context.SaveChangesAsync();

            await service.UpdateRestaurantAsync(600, MakeUpdateDto("Tên mới", status: 1, cateId: 2));

            // CheckDB
            var dbR = await context.Restaurants.FindAsync(600);
            Assert.Equal("Tên mới", dbR!.Name);
            Assert.Equal(1, dbR.Status);

            // Verify Firebase gọi đúng 1 lần
            Assert.Equal(1, firebaseSvc.CallCount);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-039 – Ném KeyNotFoundException khi nhà hàng không tồn tại, Firebase không được gọi</summary>
    [Fact]
    public async Task UpdateRestaurantAsync_NonexistentRestaurant_ThrowsKeyNotFoundException()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var firebaseSvc = new TestFirebaseService();
        var service = BuildService(context, firebaseService: firebaseSvc);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateRestaurantAsync(9999, MakeUpdateDto()));

        Assert.Equal("Restaurant not found", ex.Message);
        Assert.Equal(0, firebaseSvc.CallCount);
    }

    /// <summary>TC-RSS-040 – Ghi đè tất cả field thông tin của nhà hàng</summary>
    [Fact]
    public async Task UpdateRestaurantAsync_AllFields_OverwrittenCorrectly()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.AddRange(
                new Category { Id = 1, Name = "Cate 1" },
                new Category { Id = 2, Name = "Cate 2" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant
            {
                Id = 601, Name = "A", Email = "a@x", Description = "D",
                PhoneNumber = "1", Status = 0, AvtImage = "old.jpg",
                CateId = 1, UserId = 100, AddressId = 1
            });
            await context.SaveChangesAsync();

            var updateDto = new UpdateRestaurantDto
            {
                Name = "B mới", Email = "b@y", Description = "Description mới",
                PhoneNumber = "2", Status = 2, AvtImage = "new.jpg",
                CateId = 2, Address = DefaultAddressDto(), RestaurantPhotos = new List<string>()
            };

            await service.UpdateRestaurantAsync(601, updateDto);

            var dbR = context.Restaurants.Include(r => r.Address).First(r => r.Id == 601);
            Assert.Equal("B mới", dbR.Name);
            Assert.Equal("b@y", dbR.Email);
            Assert.Equal("Description mới", dbR.Description);
            Assert.Equal("2", dbR.PhoneNumber);
            Assert.Equal(2, dbR.Status);
            Assert.Equal("new.jpg", dbR.AvtImage);
            Assert.Equal(2, dbR.CateId);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-041 – Cập nhật đầy đủ Address (City, District, Ward, Detail, Lon, Lat)</summary>
    [Fact]
    public async Task UpdateRestaurantAsync_UpdatesAllAddressFields()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address
            {
                Id = 1, City = "Hà Nội", District = "Ba Đình",
                Ward = "Quán Thánh", Detail = "Số 1", Lon = 105.0, Lat = 21.0
            });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant { Id = 602, CateId = 1, UserId = 100, AddressId = 1, Status = 0 });
            await context.SaveChangesAsync();

            var updateDto = MakeUpdateDto();
            updateDto.Address = new AddressDto
            {
                City = "TP HCM", District = "Quận 1", Ward = "Bến Nghé",
                Detail = "Số 99 Lê Lợi", Lon = 106.7, Lat = 10.77
            };

            await service.UpdateRestaurantAsync(602, updateDto);

            var dbAddr = context.Restaurants.Include(r => r.Address).First(r => r.Id == 602).Address;
            Assert.Equal("TP HCM", dbAddr.City);
            Assert.Equal("Quận 1", dbAddr.District);
            Assert.Equal("Bến Nghé", dbAddr.Ward);
            Assert.Equal("Số 99 Lê Lợi", dbAddr.Detail);
            Assert.Equal(106.7, dbAddr.Lon!.Value, precision: 1);
            Assert.Equal(10.77, dbAddr.Lat!.Value, precision: 2);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-042 – Thay thế hoàn toàn danh sách ảnh cũ bằng ảnh mới</summary>
    [Fact]
    public async Task UpdateRestaurantAsync_ReplacesOldPhotosWithNew()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant { Id = 603, CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();
            context.RestaurantPhotos.AddRange(
                new RestaurantPhoto { Id = 1, RestaurantId = 603, ImageUrl = "old1.jpg" },
                new RestaurantPhoto { Id = 2, RestaurantId = 603, ImageUrl = "old2.jpg" });
            await context.SaveChangesAsync();

            var updateDto = MakeUpdateDto(photos: new List<string> { "new1.jpg", "new2.jpg", "new3.jpg" });
            await service.UpdateRestaurantAsync(603, updateDto);

            var dbR = context.Restaurants.Include(r => r.RestaurantPhotos).First(r => r.Id == 603);
            Assert.Equal(3, dbR.RestaurantPhotos!.Count);
            var urls = dbR.RestaurantPhotos.Select(p => p.ImageUrl).ToHashSet();
            Assert.Contains("new1.jpg", urls);
            Assert.Contains("new2.jpg", urls);
            Assert.Contains("new3.jpg", urls);
            Assert.DoesNotContain("old1.jpg", urls);
            Assert.DoesNotContain("old2.jpg", urls);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-043 – RestaurantPhotos = null → xóa sạch ảnh cũ</summary>
    [Fact]
    public async Task UpdateRestaurantAsync_NullPhotos_ClearsAllPhotos()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant { Id = 604, CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();
            context.RestaurantPhotos.Add(new RestaurantPhoto { Id = 1, RestaurantId = 604, ImageUrl = "old1.jpg" });
            await context.SaveChangesAsync();

            var updateDto = MakeUpdateDto();
            updateDto.RestaurantPhotos = null!;   // null → RestaurantPhotos bị xóa

            await service.UpdateRestaurantAsync(604, updateDto);

            // CheckDB: không còn ảnh nào của restaurant 604
            var hasPhotos = await context.RestaurantPhotos.AnyAsync(p => p.RestaurantId == 604);
            Assert.False(hasPhotos);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-044 – Gửi notification đúng topic/title/body qua TestFirebaseService</summary>
    [Fact]
    public async Task UpdateRestaurantAsync_SendsCorrectNotificationTopicTitleBody()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var firebaseSvc = new TestFirebaseService();
        var service = BuildService(context, firebaseService: firebaseSvc);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant
            {
                Id = 605, Name = "Quán Nem Nướng Nha Trang",
                CateId = 1, UserId = 100, AddressId = 1, Status = 1
            });
            await context.SaveChangesAsync();

            var updateDto = MakeUpdateDto("Quán Nem Nướng Nha Trang");
            await service.UpdateRestaurantAsync(605, updateDto);

            Assert.Equal(1, firebaseSvc.CallCount);
            Assert.Equal("user_605", firebaseSvc.Topic);
            Assert.Equal("Nhà hàng đã được cập nhật!", firebaseSvc.Title);
            Assert.Equal(
                "Nhà hàng Quán Nem Nướng Nha Trang vừa cập nhật thông tin mới. Hãy kiểm tra ngay!",
                firebaseSvc.Body);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-045 – Firebase ném exception → service lan truyền, nhưng DB đã cập nhật trước đó</summary>
    [Fact]
    public async Task UpdateRestaurantAsync_FirebaseThrows_PropagatesAndDbAlreadySaved()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var firebaseSvc = new TestFirebaseService
        {
            ThrowOnSend = new Exception("firebase fail")
        };
        var service = BuildService(context, firebaseService: firebaseSvc);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant
            {
                Id = 606, Name = "Tên cũ",
                CateId = 1, UserId = 100, AddressId = 1, Status = 0
            });
            await context.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<Exception>(
                () => service.UpdateRestaurantAsync(606, MakeUpdateDto("Tên mới")));

            Assert.Equal("firebase fail", ex.Message);

            // CheckDB: tên đã cập nhật vào DB trước khi Firebase được gọi
            var dbR = await context.Restaurants.FindAsync(606);
            Assert.Equal("Tên mới", dbR!.Name);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-046 – Address = null trong DTO → ném NullReferenceException, Firebase không được gọi</summary>
    [Fact]
    public async Task UpdateRestaurantAsync_NullAddressInDto_ThrowsNullReferenceException()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var firebaseSvc = new TestFirebaseService();
        var service = BuildService(context, firebaseService: firebaseSvc);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant
            {
                Id = 607, Name = "Tên cũ", CateId = 1, UserId = 100, AddressId = 1, Status = 1
            });
            await context.SaveChangesAsync();

            var updateDto = new UpdateRestaurantDto
            {
                Name = null, Email = null, Description = null,
                PhoneNumber = null, Status = 1, CateId = 1,
                Address = null,    // null → truy cập Address.City sẽ ném lỗi
                RestaurantPhotos = new List<string>()
            };

            // Ghi nhận bug tiềm tàng: code truy cập updateDto.Address.City khi Address = null
            await Assert.ThrowsAsync<NullReferenceException>(
                () => service.UpdateRestaurantAsync(607, updateDto));

            Assert.Equal(0, firebaseSvc.CallCount);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-047 – Repository.UpdateRestaurantAsync ném DbUpdateException → service lan truyền,
    /// Firebase KHÔNG được gọi (vì exception xảy ra trước)
    /// </summary>
    [Fact]
    public async Task UpdateRestaurantAsync_RepositoryThrows_FirebaseNotCalled()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        // Seed data trước (ngoài transaction) để GetRestaurantByIdAsync hoạt động
        context.Categories.Add(new Category { Id = 1, Name = "Cat" });
        context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
        context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
        await context.SaveChangesAsync();
        context.Restaurants.Add(new Restaurant { Id = 608, CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
        await context.SaveChangesAsync();

        // Dùng fake repository ném lỗi tại UpdateRestaurantAsync
        var throwingRepo = new ThrowOnUpdateRestaurantRepository(
            context,
            new DbUpdateException("write failed"));

        var firebaseSvc = new TestFirebaseService();
        var service = new RestaurantService(throwingRepo, new AddressService(new AddressRepository(context)), firebaseSvc);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(
            () => service.UpdateRestaurantAsync(608, MakeUpdateDto()));

        Assert.Equal("write failed", ex.Message);
        // Firebase phải = 0 vì exception xảy ra trước khi Firebase được gọi
        Assert.Equal(0, firebaseSvc.CallCount);
    }

    // ===========================================================
    //  DeleteRestaurantAsync
    // ===========================================================

    /// <summary>TC-RSS-048 – Xóa thành công nhà hàng tồn tại</summary>
    [Fact]
    public async Task DeleteRestaurantAsync_ExistingRestaurant_DeletesSuccessfully()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant
            {
                Id = 700, Name = "Quán Xóa", CateId = 1, UserId = 100, AddressId = 1, Status = 1
            });
            await context.SaveChangesAsync();
            var countBefore = await context.Restaurants.CountAsync();

            await service.DeleteRestaurantAsync(700);

            // CheckDB: nhà hàng không còn
            Assert.Null(await context.Restaurants.FindAsync(700));
            Assert.Equal(countBefore - 1, await context.Restaurants.CountAsync());
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-049 – Nhà hàng không tồn tại → không ném exception (âm thầm bỏ qua)</summary>
    [Fact]
    public async Task DeleteRestaurantAsync_NonexistentRestaurant_DoesNotThrow()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        var countBefore = await context.Restaurants.CountAsync();

        // Không ném exception
        await service.DeleteRestaurantAsync(9999);

        Assert.Equal(countBefore, await context.Restaurants.CountAsync());
    }

    /// <summary>TC-RSS-050 – id âm/0 không xóa bất kỳ nhà hàng nào</summary>
    [Fact]
    public async Task DeleteRestaurantAsync_NegativeOrZeroId_NoDataChanged()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            await service.DeleteRestaurantAsync(-1);
            await service.DeleteRestaurantAsync(0);

            // CheckDB: vẫn còn 2 nhà hàng
            Assert.Equal(2, await context.Restaurants.CountAsync());
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-051 – Xóa cascade: ảnh và đơn đặt bàn liên quan bị xóa theo (hoặc ghi nhận lỗi FK)
    /// </summary>
    [Fact]
    public async Task DeleteRestaurantAsync_CascadeDeletesPhotosAndOrders()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant
            {
                Id = 701, CateId = 1, UserId = 100, AddressId = 1, Status = 1
            });
            await context.SaveChangesAsync();
            context.RestaurantPhotos.Add(new RestaurantPhoto { Id = 1, RestaurantId = 701, ImageUrl = "p.jpg" });
            context.Orders.Add(new Order
            {
                Id = 1, RestaurantId = 701, UserId = 100,
                Name = "Nguyễn Văn X", PhoneNumber = "0900000000",
                Email = "x@x.vn", Status = 0, NumOfMembers = 2,
                ReservationTime = "2024-01-01 18:00"
            });
            await context.SaveChangesAsync();

            await service.DeleteRestaurantAsync(701);

            // CheckDB: nhà hàng và ảnh, order cascade bị xóa
            Assert.Null(await context.Restaurants.FindAsync(701));
            Assert.False(await context.RestaurantPhotos.AnyAsync(p => p.RestaurantId == 701));
            Assert.False(await context.Orders.AnyAsync(o => o.RestaurantId == 701));
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-052 – Repository.DeleteRestaurantAsync ném DbUpdateException → service lan truyền</summary>
    [Fact]
    public async Task DeleteRestaurantAsync_RepositoryThrows_PropagatesException()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var throwingRepo = new ThrowOnDeleteRestaurantRepository(
            context,
            new DbUpdateException("delete failed"));
        var service = new RestaurantService(
            throwingRepo,
            new AddressService(new AddressRepository(context)),
            new TestFirebaseService());

        var ex = await Assert.ThrowsAsync<DbUpdateException>(
            () => service.DeleteRestaurantAsync(700));

        Assert.Equal("delete failed", ex.Message);
    }

    // ===========================================================
    //  GetRestaurantsByAddressAsync
    // ===========================================================

    /// <summary>TC-RSS-053 – Lọc theo city + district + ward (street không áp dụng tại repo)</summary>
    [Fact]
    public async Task GetRestaurantsByAddressAsync_FilterByCityDistrictWard_ReturnsMatching()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            context.Addresses.AddRange(
                new Address { Id = 1, City = "Hà Nội", District = "Cầu Giấy", Ward = "Dịch Vọng" },
                new Address { Id = 2, City = "Hà Nội", District = "Cầu Giấy", Ward = "Mai Dịch" },
                new Address { Id = 3, City = "TP HCM", District = "Quận 1", Ward = "Bến Nghé" });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 4, CateId = 1, UserId = 100, AddressId = 2, Status = 1 },
                new Restaurant { Id = 5, CateId = 1, UserId = 100, AddressId = 3, Status = 1 });
            await context.SaveChangesAsync();

            var result = (await service.GetRestaurantsByAddressAsync("Hà Nội", "Cầu Giấy", "Dịch Vọng", "Duy Tân")).ToList();

            // street không được lọc tại repo → trả 3 nhà hàng thuộc Hà Nội/Cầu Giấy/Dịch Vọng
            Assert.Equal(3, result.Count);
            var ids = result.Select(r => r.Id).ToHashSet();
            Assert.Contains(1, ids);
            Assert.Contains(2, ids);
            Assert.Contains(3, ids);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-054 – Không truyền tiêu chí → trả toàn bộ (4 nhà hàng)</summary>
    [Fact]
    public async Task GetRestaurantsByAddressAsync_AllNullFilters_ReturnsAll()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            context.Addresses.AddRange(
                new Address { Id = 1, City = "Hà Nội" },
                new Address { Id = 2, City = "Đà Nẵng" },
                new Address { Id = 3, City = "TP HCM" },
                new Address { Id = 4, City = "Cần Thơ" });
            await context.SaveChangesAsync();
            for (int i = 1; i <= 4; i++)
                context.Restaurants.Add(new Restaurant { Id = i, CateId = 1, UserId = 100, AddressId = i, Status = 1 });
            await context.SaveChangesAsync();

            var result = (await service.GetRestaurantsByAddressAsync(null, null, null, null)).ToList();

            Assert.Equal(4, result.Count);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-055 – Chỉ lọc theo city</summary>
    [Fact]
    public async Task GetRestaurantsByAddressAsync_FilterByCityOnly_ReturnsMatching()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            context.Addresses.AddRange(
                new Address { Id = 1, City = "Hà Nội" },
                new Address { Id = 2, City = "Đà Nẵng" });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, CateId = 1, UserId = 100, AddressId = 2, Status = 1 },
                new Restaurant { Id = 4, CateId = 1, UserId = 100, AddressId = 2, Status = 1 },
                new Restaurant { Id = 5, CateId = 1, UserId = 100, AddressId = 2, Status = 1 });
            await context.SaveChangesAsync();

            var result = (await service.GetRestaurantsByAddressAsync("Hà Nội", null, null, null)).ToList();

            Assert.Equal(2, result.Count);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-056 – Chỉ lọc theo district</summary>
    [Fact]
    public async Task GetRestaurantsByAddressAsync_FilterByDistrictOnly_ReturnsMatching()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            context.Addresses.AddRange(
                new Address { Id = 1, City = "Hà Nội", District = "Ba Đình" },
                new Address { Id = 2, City = "Hà Nội", District = "Cầu Giấy" });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 4, CateId = 1, UserId = 100, AddressId = 2, Status = 1 },
                new Restaurant { Id = 5, CateId = 1, UserId = 100, AddressId = 2, Status = 1 });
            await context.SaveChangesAsync();

            var result = (await service.GetRestaurantsByAddressAsync(null, "Ba Đình", null, null)).ToList();

            Assert.Equal(3, result.Count);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-057 – Không khớp → trả rỗng</summary>
    [Fact]
    public async Task GetRestaurantsByAddressAsync_NoMatch_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            var result = await service.GetRestaurantsByAddressAsync("Cần Thơ", null, null, null);

            Assert.NotNull(result);
            Assert.False(result.Any());
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-058 – Khớp chính xác tiếng Việt có dấu (phân biệt "Huế" vs "Hue")</summary>
    [Fact]
    public async Task GetRestaurantsByAddressAsync_ExactVietnameseCityMatch()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            context.Addresses.AddRange(
                new Address { Id = 1, City = "Huế" },
                new Address { Id = 2, City = "Hue" });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, CateId = 1, UserId = 100, AddressId = 2, Status = 1 });
            await context.SaveChangesAsync();

            var result = (await service.GetRestaurantsByAddressAsync("Huế", null, null, null)).ToList();

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-059 – SQLite phân biệt hoa/thường → "hà nội" không khớp "Hà Nội" (ghi nhận hành vi)</summary>
    [Fact]
    public async Task GetRestaurantsByAddressAsync_CaseSensitive_NoMatchForLowercase()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            // SQLite mặc định phân biệt hoa/thường với tiếng Việt
            var result = await service.GetRestaurantsByAddressAsync("hà nội", null, null, null);

            Assert.NotNull(result);
            Assert.False(result.Any());  // Ghi nhận hành vi hiện hành
        }
        finally { await transaction.RollbackAsync(); }
    }

    // ===========================================================
    //  GetRestaurantsAsync
    // ===========================================================

    /// <summary>TC-RSS-060 – Không truyền tiêu chí → trả toàn bộ 5 nhà hàng</summary>
    [Fact]
    public async Task GetRestaurantsAsync_AllNullFilters_ReturnsAll()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            for (int i = 1; i <= 5; i++)
                context.Restaurants.Add(new Restaurant
                {
                    Id = i, Name = $"R{i}", CateId = 1, UserId = 100, AddressId = 1, Status = 1
                });
            await context.SaveChangesAsync();

            var result = (await service.GetRestaurantsAsync(null, null, null, null, null, null)).ToList();

            Assert.Equal(5, result.Count);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-061 – Chỉ lọc theo categoryId</summary>
    [Fact]
    public async Task GetRestaurantsAsync_FilterByCategoryId_ReturnsMatching()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.AddRange(
                new Category { Id = 1, Name = "Món Việt" },
                new Category { Id = 2, Name = "Món Hàn" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, CateId = 2, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 4, CateId = 2, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 5, CateId = 2, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            var result = (await service.GetRestaurantsAsync(1, null, null, null, null, null)).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal("Món Việt", r.Category));
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-062 – Chỉ lọc theo userId</summary>
    [Fact]
    public async Task GetRestaurantsAsync_FilterByUserId_ReturnsMatching()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.AddRange(
                new User { Id = 100, Email = "u100@x.vn", Password = "1", Role = "r", Status = 1 },
                new User { Id = 101, Email = "u101@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, CateId = 1, UserId = 101, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            var result = (await service.GetRestaurantsAsync(null, 100, null, null, null, null)).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(100, r.UserId));
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-063 – Chỉ lọc theo searchTerm</summary>
    [Fact]
    public async Task GetRestaurantsAsync_FilterBySearchTerm_ReturnsMatching()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, Name = "Phở Thìn", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, Name = "Bún Chả", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, Name = "Phở Gia Truyền", CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            var result = (await service.GetRestaurantsAsync(null, null, "Phở", null, null, null)).ToList();

            Assert.Equal(2, result.Count);
            var names = result.Select(r => r.Name).ToHashSet();
            Assert.Contains("Phở Thìn", names);
            Assert.Contains("Phở Gia Truyền", names);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-064 – Kết hợp nhiều tiêu chí (AND logic) → chỉ 1 kết quả</summary>
    [Fact]
    public async Task GetRestaurantsAsync_MultipleFilters_ReturnsIntersection()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.AddRange(
                new Category { Id = 1, Name = "Món Việt" },
                new Category { Id = 2, Name = "Món Hàn" });
            context.Addresses.AddRange(
                new Address { Id = 1, City = "Hà Nội" },
                new Address { Id = 2, City = "Đà Nẵng" });
            context.Users.AddRange(
                new User { Id = 100, Email = "u100@x.vn", Password = "1", Role = "r", Status = 1 },
                new User { Id = 101, Email = "u101@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, Name = "Phở Thìn", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, Name = "Phở Khác", CateId = 1, UserId = 101, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, Name = "Mỳ Quảng", CateId = 2, UserId = 100, AddressId = 2, Status = 1 });
            await context.SaveChangesAsync();

            // categoryId=1, userId=100, searchTerm="Phở", city="Hà Nội" → chỉ Restaurant Id=1
            var result = (await service.GetRestaurantsAsync(1, 100, "Phở", "Hà Nội", null, null)).ToList();

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
            Assert.Equal("Phở Thìn", result[0].Name);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-065 – Không có nhà hàng thỏa điều kiện → trả rỗng</summary>
    [Fact]
    public async Task GetRestaurantsAsync_NoMatchingRestaurants_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.AddRange(
                new Category { Id = 1, Name = "Cat1" },
                new Category { Id = 2, Name = "Cat2" });
            context.Addresses.AddRange(
                new Address { Id = 1, City = "Hà Nội" },
                new Address { Id = 2, City = "Đà Nẵng" });
            context.Users.AddRange(
                new User { Id = 100, Email = "u100@x.vn", Password = "1", Role = "r", Status = 1 },
                new User { Id = 101, Email = "u101@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, Name = "Phở Thìn", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, Name = "Phở Khác", CateId = 1, UserId = 101, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            var result = await service.GetRestaurantsAsync(1, null, null, "Cần Thơ", null, null);

            Assert.NotNull(result);
            Assert.False(result.Any());
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-066 – Nhà hàng với CateId orphan → Category = null, không ném exception</summary>
    [Fact]
    public async Task GetRestaurantsAsync_OrphanCateId_CategoryIsNull()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            context.Categories.Add(new Category { Id = 99, Name = "Orphan" });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant
            {
                Id = 1, Name = "Quán Lạ", CateId = 99, UserId = 100, AddressId = 1, Status = 1
            });
            await context.SaveChangesAsync();
            context.Categories.Remove(context.Categories.Find(99)!);
            await context.SaveChangesAsync();

            var result = (await service.GetRestaurantsAsync(null, null, null, null, null, null)).ToList();

            Assert.Single(result);
            Assert.Null(result[0].Category);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-067 – Ánh xạ đúng RestaurantPhotos cho từng nhà hàng (đầy đủ/rỗng)</summary>
    [Fact]
    public async Task GetRestaurantsAsync_MapsPhotosCorrectly()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, Name = "R1", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, Name = "R2", CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();
            context.RestaurantPhotos.AddRange(
                new RestaurantPhoto { Id = 1, RestaurantId = 1, ImageUrl = "a.jpg" },
                new RestaurantPhoto { Id = 2, RestaurantId = 1, ImageUrl = "b.jpg" });
            // Restaurant 2 không có ảnh
            await context.SaveChangesAsync();

            var result = (await service.GetRestaurantsAsync(null, null, null, null, null, null)).ToList();

            Assert.Equal(2, result.Count);
            var r1 = result.First(r => r.Id == 1);
            var r2 = result.First(r => r.Id == 2);

            Assert.Equal(2, r1.RestaurantPhotos!.Count);
            Assert.Contains("a.jpg", r1.RestaurantPhotos);
            Assert.Contains("b.jpg", r1.RestaurantPhotos);

            Assert.NotNull(r2.RestaurantPhotos);
            Assert.Empty(r2.RestaurantPhotos!);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-068 – Ánh xạ đúng Address, Status, AverageScore, UserId, Category</summary>
    [Fact]
    public async Task GetRestaurantsAsync_MapsAllDetailFieldsCorrectly()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Món Việt" });
            context.Addresses.Add(new Address { Id = 9, City = "Hải Phòng", District = "Lê Chân" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant
            {
                Id = 800, Name = "Quán Hải Phòng",
                Status = 1, AverageScore = 4.2f, TotalReviews = 30,
                CateId = 1, UserId = 100, AddressId = 9
            });
            await context.SaveChangesAsync();

            var result = (await service.GetRestaurantsAsync(null, null, null, null, null, null)).ToList();

            Assert.True(result.Count >= 1);
            var r = result.First(x => x.Id == 800);
            Assert.Equal("Hải Phòng", r.Address!.City);
            Assert.Equal("Lê Chân", r.Address.District);
            Assert.Equal(1, r.Status);
            Assert.Equal(4.2, (double)r.AverageScore!.Value, 1);
            Assert.Equal(30, r.TotalReviews);
            Assert.Equal(100, r.UserId);
            Assert.Equal("Món Việt", r.Category);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RSS-069 – searchTerm tiếng Việt có dấu ("Gánh") → khớp đúng</summary>
    [Fact]
    public async Task GetRestaurantsAsync_VietnameseSearchTerm_ReturnsCorrectMatch()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, Name = "Phở Gánh", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, Name = "Phở Thìn", CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            var result = (await service.GetRestaurantsAsync(null, null, "Gánh", null, null, null)).ToList();

            Assert.Single(result);
            Assert.Equal("Phở Gánh", result[0].Name);
        }
        finally { await transaction.RollbackAsync(); }
    }
}
