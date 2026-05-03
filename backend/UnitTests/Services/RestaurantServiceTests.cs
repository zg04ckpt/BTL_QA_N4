// =============================================================================
//  RestaurantServiceTests.cs
//  Unit tests cho RestaurantService – kiểm thử 6 nhóm nghiệp vụ:
//    1. AddRestaurantAsync         – Tạo nhà hàng mới
//    2. GetRestaurantsByCategoryAsync – Lấy danh sách theo danh mục
//    3. GetRestaurantsByUserAsync   – Lấy danh sách theo chủ sở hữu
//    4. SearchRestaurantsAsync      – Tìm kiếm theo từ khoá
//    5. GetRestaurantByIdAsync      – Lấy chi tiết 1 nhà hàng
//    6. UpdateRestaurantAsync       – Cập nhật thông tin nhà hàng
//    7. DeleteRestaurantAsync       – Xóa nhà hàng
//    8. GetRestaurantsByAddressAsync – Lọc theo địa chỉ (city/district/ward)
//    9. GetRestaurantsAsync         – Lấy danh sách với nhiều bộ lọc kết hợp
//
//  Hạ tầng test:
//    - SQLite in-memory: DB tạm trong bộ nhớ, cô lập hoàn toàn với DB thật
//    - Transaction + Rollback: mỗi test mở transaction, rollback sau khi xong
//      → đảm bảo mỗi test chạy trên DB sạch
//    - TestFirebaseService: stub ghi nhận cuộc gọi Firebase mà không gửi thật
//    - Mock<IAddressService>: giả lập AddressService cho các test cần kiểm soát đầu ra
//    - Fake repositories (3 loại): giả lập lỗi ở tầng repository
//
//  ⚠ LƯU Ý QUAN TRỌNG – UNIQUE constraint trên Restaurants.AddressId:
//    Schema DB áp dụng quan hệ 1-1 giữa Restaurant và Address.
//    Mỗi Restaurant PHẢI có AddressId riêng biệt.
//    Các test cần seed N nhà hàng → phải tạo N bản ghi Address với Id khác nhau.
//    Không dùng chung AddressId=1 cho nhiều Restaurant → sẽ ném UNIQUE constraint failed.
// =============================================================================

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

// =============================================================================
//  NullReturningRestaurantRepository
//  Giả lập tình huống: repository tạo nhà hàng thành công nhưng trả về null
//  (ví dụ: DB bị lỗi silent failure không ném exception).
//  Dùng cho: TC-RSS-010
// =============================================================================
public class NullReturningRestaurantRepository : RestaurantRepository
{
    public NullReturningRestaurantRepository(ApplicationDbContext context) : base(context) { }

    // Override để luôn trả null thay vì object Restaurant thật
    public override Task<Restaurant?> AddRestaurantAsync(Restaurant restaurant)
        => Task.FromResult<Restaurant?>(null); // Trả null giả lập DB lỗi không ném exception
}

// =============================================================================
//  ThrowOnUpdateRestaurantRepository
//  Giả lập tình huống: repository ném lỗi khi cập nhật nhà hàng.
//  Mục đích: kiểm tra service không gọi Firebase khi DB lỗi trước.
//  Dùng cho: TC-RSS-047
// =============================================================================
public class ThrowOnUpdateRestaurantRepository : RestaurantRepository
{
    // Exception được cấu hình để ném khi UpdateRestaurantAsync được gọi
    private readonly Exception _throwOnUpdate;

    public ThrowOnUpdateRestaurantRepository(ApplicationDbContext context, Exception throwOnUpdate)
        : base(context)
    {
        _throwOnUpdate = throwOnUpdate;
    }

    // Override: bỏ qua logic thật, ném lỗi ngay lập tức
    public override Task UpdateRestaurantAsync(Restaurant restaurant)
        => throw _throwOnUpdate;
}

// =============================================================================
//  ThrowOnDeleteRestaurantRepository
//  Giả lập tình huống: repository ném lỗi khi xóa nhà hàng.
//  Mục đích: kiểm tra service lan truyền exception đúng loại.
//  Dùng cho: TC-RSS-052
// =============================================================================
public class ThrowOnDeleteRestaurantRepository : RestaurantRepository
{
    // Exception được cấu hình để ném khi DeleteRestaurantAsync được gọi
    private readonly Exception _throwOnDelete;

    public ThrowOnDeleteRestaurantRepository(ApplicationDbContext context, Exception throwOnDelete)
        : base(context)
    {
        _throwOnDelete = throwOnDelete;
    }

    // Override: bỏ qua logic thật, ném lỗi ngay lập tức
    public override Task DeleteRestaurantAsync(int id)
        => throw _throwOnDelete;
}

// =============================================================================
//  RestaurantServiceTests – class chứa toàn bộ unit test cho RestaurantService
// =============================================================================
public class RestaurantServiceTests
{
    // -------------------------------------------------------------------------
    //  HELPER: DefaultAddressDto
    //  Địa chỉ mẫu chuẩn dùng xuyên suốt trong các DTO tạo/cập nhật nhà hàng.
    //  Giá trị này phản ánh địa chỉ thực tế tại Hà Nội để dữ liệu test có nghĩa.
    // -------------------------------------------------------------------------
    private static AddressDto DefaultAddressDto() => new AddressDto
    {
        City = "Hà Nội",
        District = "Cầu Giấy",
        Ward = "Dịch Vọng",
        Detail = "Số 12 phố Duy Tân",
        Lon = 105.7827,  // Kinh độ khu vực Cầu Giấy, Hà Nội
        Lat = 21.0285    // Vĩ độ khu vực Cầu Giấy, Hà Nội
    };

    // -------------------------------------------------------------------------
    //  HELPER: SeedCategoryUserAddressAsync
    //  Seed 3 entity phụ thuộc tối thiểu để AddRestaurantAsync hoạt động:
    //    Category (nhà hàng cần thuộc danh mục)
    //    Address  (nhà hàng cần có địa chỉ)
    //    User     (nhà hàng cần chủ sở hữu)
    //  Lưu ý: KHÔNG seed Restaurant – test sẽ tự seed sau.
    // -------------------------------------------------------------------------
    private static async Task SeedCategoryUserAddressAsync(ApplicationDbContext ctx,
        int cateId = 1, int userId = 100, int addressId = 1,
        string city = "Hà Nội")
    {
        ctx.Categories.Add(new Category { Id = cateId, Name = "Món Việt" });
        // Address với Id tùy chỉnh – quan trọng vì AddressId phải UNIQUE trong Restaurants
        ctx.Addresses.Add(new Address { Id = addressId, City = city, District = "Quận Test", Ward = "Phường Test", Detail = "Số 1 đường Test" });
        ctx.Users.Add(new User
        {
            Id = userId, Email = $"owner{userId}@test.vn", Password = "123",
            Role = "restaurant_owner", Name = $"Chủ nhà hàng {userId}", Status = 1
        });
        await ctx.SaveChangesAsync(); // Lưu cả 3 entity cùng lúc
    }

    // -------------------------------------------------------------------------
    //  HELPER: MakeCreateDto
    //  Tạo CreateRestaurantDto với các giá trị mặc định hợp lệ.
    //  Dùng khi cần test logic service mà không quan tâm đến giá trị cụ thể.
    //  Tham số photos cho phép tùy chỉnh danh sách ảnh (null = danh sách rỗng).
    // -------------------------------------------------------------------------
    private static CreateRestaurantDto MakeCreateDto(
        string name = "Phở Thìn Bờ Hồ",
        int cateId = 1, int userId = 100,
        List<string>? photos = null) => new CreateRestaurantDto
        {
            Name = name,
            Status = 0,                       // Service sẽ bỏ qua giá trị này và set = 0
            Email = "test@test.vn",
            Description = "Mô tả",
            PhoneNumber = "0241234567",
            AvtImage = "avt.jpg",
            CateId = cateId,                  // FK đến Categories
            UserId = userId,                  // FK đến Users (chủ nhà hàng)
            Address = DefaultAddressDto(),    // Địa chỉ mẫu Hà Nội
            RestaurantPhotos = photos ?? new List<string>()  // Danh sách URL ảnh
        };

    // -------------------------------------------------------------------------
    //  HELPER: MakeUpdateDto
    //  Tạo UpdateRestaurantDto với các giá trị mặc định hợp lệ cho update.
    //  Tham số name/status/cateId/photos cho phép tùy chỉnh giá trị cần test.
    // -------------------------------------------------------------------------
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
            Address = DefaultAddressDto(),    // Address mặc định – có thể override trong test cụ thể
            RestaurantPhotos = photos ?? new List<string>()
        };

    // -------------------------------------------------------------------------
    //  HELPER: BuildService
    //  Khởi tạo RestaurantService với SQLite context và các dependency.
    //  Mặc định dùng AddressService thật và TestFirebaseService (stub).
    //  Các test cần kiểm soát Firebase/Address có thể truyền mock vào.
    // -------------------------------------------------------------------------
    private static RestaurantService BuildService(
        ApplicationDbContext ctx,
        IAddressService? addressService = null,    // null = dùng AddressService thật
        FirebaseService? firebaseService = null)   // null = dùng TestFirebaseService stub
    {
        var restaurantRepo = new RestaurantRepository(ctx); // Repository thật với SQLite
        var addrSvc = addressService ?? new AddressService(new AddressRepository(ctx));
        var fbSvc = firebaseService ?? new TestFirebaseService(); // Stub: không gửi notification thật
        return new RestaurantService(restaurantRepo, addrSvc, fbSvc);
    }

    // =========================================================================
    //  NHÓM 1: AddRestaurantAsync
    //  Kiểm tra toàn bộ luồng tạo nhà hàng mới từ CreateRestaurantDto.
    //  Service sẽ: gọi AddressService → tạo Address → tạo Restaurant → lưu ảnh.
    // =========================================================================

    /// <summary>
    /// TC-RSS-001 – Tạo nhà hàng thành công với dữ liệu đầu vào hợp lệ.
    /// Kiểm tra: kết quả trả về không null, Id > 0, các field được ánh xạ đúng,
    /// và DB tăng thêm 1 bản ghi Restaurant.
    /// </summary>
    [Fact]
    public async Task AddRestaurantAsync_ValidDto_CreatesRestaurant()
    {
        // Tạo kết nối SQLite in-memory và DbContext riêng cho test này
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context); // Service với AddressService thật + TestFirebaseService stub

        // Mở transaction để có thể rollback sau khi test
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            // === BASE DATA: Seed Category + Address + User (các entity phụ thuộc) ===
            await SeedCategoryUserAddressAsync(context);
            // Sau bước này: DB có 1 Category (Id=1), 1 Address (Id=1), 1 User (Id=100)

            // === TEST DATA: DTO từ client ===
            var dto = MakeCreateDto("Phở Thìn Bờ Hồ", photos: new List<string> { "p1.jpg", "p2.jpg" });

            // === THỰC THI (Act) ===
            // === GỌI SERVICE ===
            var result = await service.AddRestaurantAsync(dto);

            // === KIỂM TRA KẾT QUẢ ===
            Assert.NotNull(result);                         // Service phải trả về đối tượng
            Assert.True(result!.Id > 0);                   // DB tự sinh Id > 0
            Assert.Equal("Phở Thìn Bờ Hồ", result.Name);  // Tên được lưu nguyên vẹn
            Assert.Equal("test@test.vn", result.Email);    // Email từ DTO
            Assert.Equal("0241234567", result.PhoneNumber); // SĐT từ DTO

            // === KIỂM TRA DB (CheckDB) ===
            Assert.Equal(1, await context.Restaurants.CountAsync()); // Đúng 1 nhà hàng được tạo
        }
        finally { await transaction.RollbackAsync(); } // Rollback: DB về trạng thái trống
    }

    /// <summary>
    /// TC-RSS-002 – Service luôn ép Status = 0 khi tạo mới, bất kể DTO gửi giá trị nào.
    /// Kiểm tra: nhà hàng mới luôn ở trạng thái "chờ duyệt" (0), không để client tự đặt Status.
    /// </summary>
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
            dto.Status = 99;  // Client cố tình gửi Status = 99 – phải bị service bỏ qua

            var result = await service.AddRestaurantAsync(dto);

            // Kết quả trả về phải có Status = 0 (không phải 99)
            Assert.Equal(0, result!.Status);

            // CheckDB: xác nhận DB cũng lưu Status = 0
            var dbRecord = await context.Restaurants.FindAsync(result.Id);
            Assert.Equal(0, dbRecord!.Status); // DB không được ghi giá trị từ DTO
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-003 – Nhà hàng mới tạo phải có AverageScore = 0 và TotalReviews = 0.
    /// Kiểm tra: service khởi tạo đúng các field thống kê (chưa có review nào).
    /// </summary>
    [Fact]
    public async Task AddRestaurantAsync_NewRestaurant_InitialScoresAreZero()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            await SeedCategoryUserAddressAsync(context);
            // === THỰC THI (Act) ===
            var result = await service.AddRestaurantAsync(MakeCreateDto("Quán Cơm Tấm"));

            // === KIỂM TRA (Assert) ===

            Assert.Equal(0, result!.AverageScore);  // Điểm trung bình = 0 (chưa có review)
            Assert.Equal(0, result.TotalReviews);   // Tổng số review = 0 (nhà hàng mới)
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-004 – Service tạo Address qua AddressService và gán AddressId đúng vào Restaurant.
    /// Kiểm tra: Address được persist vào DB, AddressId trong Restaurant trỏ đến Address đó.
    /// </summary>
    [Fact]
    public async Task AddRestaurantAsync_CreatesAddress_AndLinksAddressIdToRestaurant()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context); // AddressService thật → ghi Address vào SQLite

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            // Base data: chỉ cần Category + User (không seed Address vì service sẽ tự tạo)
            context.Categories.Add(new Category { Id = 1, Name = "Mon Viet" });
            context.Users.Add(new User { Id = 100, Email = "o@test.vn", Password = "123", Role = "restaurant_owner", Status = 1 });
            await context.SaveChangesAsync();

            // DTO với địa chỉ Đà Nẵng – service sẽ gọi AddressService.AddAddressAsync để lưu
            var dto = MakeCreateDto("Quán Hải Sản");
            dto.Address = new AddressDto
            {
                City = "Đà Nẵng", District = "Sơn Trà", Ward = "Phước Mỹ",
                Detail = "Số 5 đường Võ Nguyên Giáp", Lon = 108.245, Lat = 16.060
            };

            var result = await service.AddRestaurantAsync(dto);

            // CheckDB: đúng 1 Address được tạo bởi service
            Assert.Equal(1, await context.Addresses.CountAsync());
            var dbAddr = await context.Addresses.FirstAsync();
            Assert.Equal("Đà Nẵng", dbAddr.City);                       // City khớp
            Assert.Equal("Số 5 đường Võ Nguyên Giáp", dbAddr.Detail);  // Detail khớp

            // Restaurant.AddressId phải trỏ đến Address vừa tạo
            Assert.Equal(dbAddr.Id, result!.AddressId);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-005 – Service lưu đủ và đúng 3 ảnh vào RestaurantPhotos.
    /// Kiểm tra: URL ảnh được ánh xạ từ DTO vào entity RestaurantPhoto.
    /// </summary>
    [Fact]
    public async Task AddRestaurantAsync_WithPhotos_SavesAllPhotos()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            await SeedCategoryUserAddressAsync(context);

            // DTO với 3 URL ảnh
            var dto = MakeCreateDto("Quán Nướng",
                photos: new List<string> { "img1.jpg", "img2.jpg", "img3.jpg" });

            // === THỰC THI (Act) ===

            var result = await service.AddRestaurantAsync(dto);

            // Phải tạo đúng 3 RestaurantPhoto
            // === KIỂM TRA (Assert) ===
            Assert.Equal(3, result!.RestaurantPhotos!.Count);
            var urls = result.RestaurantPhotos.Select(p => p.ImageUrl).ToHashSet();
            Assert.Contains("img1.jpg", urls); // Ảnh 1 được lưu
            Assert.Contains("img2.jpg", urls); // Ảnh 2 được lưu
            Assert.Contains("img3.jpg", urls); // Ảnh 3 được lưu
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-006 – Tạo nhà hàng thành công khi danh sách ảnh rỗng.
    /// Kiểm tra: RestaurantPhotos = [] (không null, không có phần tử).
    /// </summary>
    [Fact]
    public async Task AddRestaurantAsync_EmptyPhotos_CreatesRestaurantWithNoPhotos()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            await SeedCategoryUserAddressAsync(context);
            // DTO với photos = [] (list rỗng)
            // === THỰC THI (Act) ===
            var result = await service.AddRestaurantAsync(MakeCreateDto("Quán Chay", photos: new List<string>()));

            // === KIỂM TRA (Assert) ===

            Assert.NotNull(result);                       // Restaurant được tạo
            Assert.NotNull(result!.RestaurantPhotos);     // Photos không null (là list rỗng)
            Assert.Empty(result.RestaurantPhotos!);       // Không có ảnh nào
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-007 – URL AvtImage được lưu chính xác (URL đầy đủ, không bị encode hay truncate).
    /// Kiểm tra: service không thay đổi URL ảnh đại diện nhà hàng.
    /// </summary>
    [Fact]
    public async Task AddRestaurantAsync_AvtImageSaved_Correctly()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            await SeedCategoryUserAddressAsync(context);
            var dto = MakeCreateDto("Quán Lẩu");
            dto.AvtImage = "https://cdn.example.com/avt-nhahang.png"; // URL đầy đủ CDN

            // === THỰC THI (Act) ===

            var result = await service.AddRestaurantAsync(dto);

            // URL AvtImage phải được lưu nguyên vẹn, không thay đổi
            // === KIỂM TRA (Assert) ===
            Assert.Equal("https://cdn.example.com/avt-nhahang.png", result!.AvtImage);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-008 – Ném DbUpdateException khi UserId không tồn tại trong DB (FK violation).
    /// Kiểm tra: SQLite với FK enforcement từ chối tạo Restaurant khi User không có.
    /// </summary>
    [Fact]
    public async Task AddRestaurantAsync_NonexistentUserId_ThrowsDbUpdateException()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            await context.SaveChangesAsync();
            // Cố tình không seed User 999 → FK violation

            var dto = MakeCreateDto();
            dto.UserId = 999; // UserId không tồn tại → SaveChangesAsync sẽ ném lỗi

            // === KIỂM TRA (Assert) ===
            // Kỳ vọng: ném DbUpdateException (FOREIGN KEY constraint failed)
            // === THỰC THI (Act) ===
            await Assert.ThrowsAsync<DbUpdateException>(() => service.AddRestaurantAsync(dto));
        }
        finally { await transaction.RollbackAsync(); }

        // Verify qua context sạch: không có Restaurant nào được tạo
        await using var v = SqliteMemoryDb.CreateContext(connection);
        // === KIỂM TRA (Assert) ===
        Assert.Equal(0, await v.Restaurants.CountAsync()); // DB vẫn trống
    }

    /// <summary>
    /// TC-RSS-009 – AddressService ném exception → service lan truyền, không tạo Restaurant.
    /// Kiểm tra: khi phụ thuộc AddressService lỗi, toàn bộ request thất bại (không partial save).
    /// Dùng Mock để kiểm soát hành vi của AddressService.
    /// </summary>
    [Fact]
    public async Task AddRestaurantAsync_AddressServiceThrows_PropagatesException()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        // Mock IAddressService: bất kỳ lời gọi AddAddressAsync nào đều ném lỗi
        var mockAddrSvc = new Mock<IAddressService>();
        mockAddrSvc.Setup(a => a.AddAddressAsync(It.IsAny<AddressDto>()))
                   .ThrowsAsync(new Exception("address error")); // Giả lập lỗi AddressService

        // Service dùng mock AddressService (lỗi) + TestFirebaseService stub
        var service = new RestaurantService(new RestaurantRepository(context), mockAddrSvc.Object, new TestFirebaseService());

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            await SeedCategoryUserAddressAsync(context);
            var dto = MakeCreateDto("Test Addr");

            // === THỰC THI (Act) ===
            // Service phải ném exception từ AddressService, không tự xử lý
            var ex = await Assert.ThrowsAsync<Exception>(() => service.AddRestaurantAsync(dto));

            // === KIỂM TRA (Assert) ===
            Assert.Equal("address error", ex.Message); // Message không bị thay đổi

            // CheckDB: không có Restaurant nào được tạo (do lỗi trước khi AddRestaurantAsync)
            Assert.Equal(0, await context.Restaurants.CountAsync());
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-010 – Service trả null khi repository.AddRestaurantAsync trả null.
    /// Kiểm tra: service không ném exception mà trả null – caller cần kiểm tra giá trị trả về.
    /// Dùng NullReturningRestaurantRepository để giả lập.
    /// </summary>
    [Fact]
    public async Task AddRestaurantAsync_RepositoryReturnsNull_ServiceReturnsNull()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        // Mock AddressService trả Address giả (để service vượt qua bước tạo Address)
        var mockAddrSvc = new Mock<IAddressService>();
        mockAddrSvc.Setup(a => a.AddAddressAsync(It.IsAny<AddressDto>()))
                   .ReturnsAsync(new Address { Id = 5, City = "Hà Nội" }); // Address giả

        // Repo trả null khi AddRestaurantAsync được gọi
        var nullRepo = new NullReturningRestaurantRepository(context);
        var service = new RestaurantService(nullRepo, mockAddrSvc.Object, new TestFirebaseService());

        var dto = MakeCreateDto("Test Null");
        // === THỰC THI (Act) ===
        var result = await service.AddRestaurantAsync(dto);

        // === KIỂM TRA (Assert) ===

        Assert.Null(result); // Service phải trả null khi repo trả null
    }

    /// <summary>
    /// TC-RSS-011 – Email sai định dạng và SĐT không hợp lệ vẫn được lưu (không có validate tại service).
    /// Ghi nhận hành vi hiện hành: validation chỉ ở Controller (ModelState), không ở Service.
    /// ⚠ Test này sẽ fail nếu service bổ sung validate Email/PhoneNumber.
    /// </summary>
    [Fact]
    public async Task AddRestaurantAsync_InvalidEmailPhone_SavedWithoutValidation()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            await SeedCategoryUserAddressAsync(context);
            var dto = MakeCreateDto("Test Format");
            dto.Email = "khongphaiemail"; // Email không có @
            dto.PhoneNumber = "abc";      // SĐT không phải số

            var result = await service.AddRestaurantAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("khongphaiemail", result!.Email);  // Được lưu nguyên không validate
            Assert.Equal("abc", result.PhoneNumber);        // Được lưu nguyên không validate
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-012 – Lưu đúng tên tiếng Việt có dấu đầy đủ, emoji, và mô tả dài.
    /// Kiểm tra: SQLite in-memory xử lý đúng Unicode (tiếng Việt + emoji UTF-16).
    /// </summary>
    [Fact]
    public async Task AddRestaurantAsync_VietnameseNameWithEmoji_SavedCorrectly()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            await SeedCategoryUserAddressAsync(context);
            var dto = MakeCreateDto("Bún Bò Huế Cô Ba 🍜"); // Tên có emoji Unicode
            dto.Description = "Quán nhỏ nằm trong ngõ, không gian ấm cúng – đậm đà hương vị Huế"; // Mô tả tiếng Việt đầy đủ

            // === THỰC THI (Act) ===

            var result = await service.AddRestaurantAsync(dto);

            // Tên và mô tả phải được lưu nguyên (không mất dấu, không mất emoji)
            // === KIỂM TRA (Assert) ===
            Assert.Equal("Bún Bò Huế Cô Ba 🍜", result!.Name);
            Assert.Equal("Quán nhỏ nằm trong ngõ, không gian ấm cúng – đậm đà hương vị Huế", result.Description);
        }
        finally { await transaction.RollbackAsync(); }
    }

    // =========================================================================
    //  NHÓM 2: GetRestaurantsByCategoryAsync
    //  Lấy danh sách nhà hàng theo danh mục (CateId).
    //  ⚠ KNOWN ISSUE: các test này seed nhiều Restaurant với cùng AddressId=1
    //    → vi phạm UNIQUE constraint trên Restaurants.AddressId → test FAILED.
    //    Cách fix: mỗi Restaurant cần một Address riêng biệt.
    // =========================================================================

    /// <summary>
    /// TC-RSS-013 – Trả đúng 3 nhà hàng thuộc danh mục 1, bỏ qua 2 nhà hàng danh mục 2.
    /// ⚠ FAIL: 5 restaurant dùng cùng AddressId=1 → UNIQUE constraint failed.
    /// Fix: tạo 5 Address riêng biệt (Id=1..5).
    /// </summary>
    [Fact]
    public async Task GetRestaurantsByCategoryAsync_ReturnsOnlyMatchingCategory()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            // Base data: 2 danh mục + 1 user + 1 địa chỉ (lưu ý: tất cả 5 restaurant dùng AddressId=1 → lỗi UNIQUE)
            context.Categories.AddRange(
                new Category { Id = 1, Name = "Món Việt" },  // Danh mục cần lọc
                new Category { Id = 2, Name = "Món Hàn" });  // Danh mục sẽ bị loại trừ
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();

            // 3 nhà hàng danh mục 1 (Món Việt) + 2 nhà hàng danh mục 2 (Món Hàn)
            // ⚠ Tất cả dùng AddressId=1 → từ restaurant thứ 2 sẽ bị UNIQUE constraint
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, Name = "Phở Thìn", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, Name = "Bún Chả", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, Name = "Cơm Tấm", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 4, Name = "Kimbap", CateId = 2, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 5, Name = "BBQ Hàn", CateId = 2, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync(); // ← SaveChangesAsync sẽ ném lỗi UNIQUE ở đây

            // === THỰC THI (Act) ===
            // Gọi service: chỉ lấy nhà hàng thuộc danh mục 1
            var result = (await service.GetRestaurantsByCategoryAsync(1)).ToList();

            // === KIỂM TRA (Assert) ===
            // Kỳ vọng: 3 nhà hàng Món Việt
            Assert.Equal(3, result.Count);
            var ids = result.Select(r => r.Id).ToHashSet();
            Assert.Contains(1, ids); // Phở Thìn phải có
            Assert.Contains(2, ids); // Bún Chả phải có
            Assert.Contains(3, ids); // Cơm Tấm phải có
            var names = result.Select(r => r.Name).ToHashSet();
            Assert.Contains("Phở Thìn", names);
            Assert.Contains("Bún Chả", names);
            Assert.Contains("Cơm Tấm", names);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-014 – Trả danh sách rỗng khi danh mục tồn tại nhưng chưa có nhà hàng nào.
    /// Kiểm tra: không ném exception, trả IEnumerable rỗng.
    /// </summary>
    [Fact]
    public async Task GetRestaurantsByCategoryAsync_EmptyCategory_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            // Seed Category 3 (Món Nhật) nhưng không có Restaurant nào thuộc nó
            context.Categories.Add(new Category { Id = 3, Name = "Món Nhật" });
            await context.SaveChangesAsync();

            // Gọi service với CateId của danh mục rỗng
            // === THỰC THI (Act) ===
            var result = await service.GetRestaurantsByCategoryAsync(3);

            // === KIỂM TRA (Assert) ===

            Assert.NotNull(result);      // Không trả null
            Assert.False(result.Any()); // Danh sách rỗng (không có nhà hàng)
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-015 – Trả rỗng khi categoryId không tồn tại trong DB.
    /// Kiểm tra: không ném exception dù query với id hoàn toàn không có.
    /// </summary>
    [Fact]
    public async Task GetRestaurantsByCategoryAsync_NonexistentCategory_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        // Không cần transaction vì không thay đổi DB (chỉ đọc)
        // categoryId=999 không có trong DB → phải trả rỗng an toàn
        // === THỰC THI (Act) ===
        var result = await service.GetRestaurantsByCategoryAsync(999);

        // === KIỂM TRA (Assert) ===

        Assert.NotNull(result);
        Assert.False(result.Any());
    }

    /// <summary>
    /// TC-RSS-016 – Ánh xạ đúng tất cả field từ DB sang RestaurantDto.
    /// Kiểm tra: Id, Name, Email, PhoneNumber, AverageScore đều được ánh xạ chính xác.
    /// </summary>
    [Fact]
    public async Task GetRestaurantsByCategoryAsync_SingleRestaurant_MapsFieldsCorrectly()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            context.Categories.Add(new Category { Id = 1, Name = "Món Việt" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();

            // Seed nhà hàng với đầy đủ field để kiểm tra ánh xạ
            context.Restaurants.Add(new Restaurant
            {
                Id = 10, Name = "Quán Lẩu Thái",
                Email = "lau@test.vn", PhoneNumber = "0987654321",
                AverageScore = 4.5f, TotalReviews = 20,   // Điểm và số review cố định để Assert
                CateId = 1, UserId = 100, AddressId = 1, Status = 1
            });
            await context.SaveChangesAsync();

            var result = (await service.GetRestaurantsByCategoryAsync(1)).ToList();

            Assert.Single(result);
            Assert.Equal(10, result[0].Id);                        // Id ánh xạ đúng
            Assert.Equal("Quán Lẩu Thái", result[0].Name);        // Name ánh xạ đúng
            Assert.Equal("lau@test.vn", result[0].Email);          // Email ánh xạ đúng
            Assert.Equal("0987654321", result[0].PhoneNumber);     // PhoneNumber ánh xạ đúng
            Assert.Equal(4.5, (double)result[0].AverageScore!.Value, 1); // AverageScore (float→double) với precision 1
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-017 – categoryId âm hoặc 0 trả rỗng, không ném exception.
    /// Kiểm tra: biên giá trị ≤ 0 được xử lý an toàn (Id âm không tồn tại trong DB).
    /// </summary>
    [Fact]
    public async Task GetRestaurantsByCategoryAsync_NegativeOrZeroId_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            // Seed 1 nhà hàng hợp lệ để đảm bảo DB không trống
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            var r1 = await service.GetRestaurantsByCategoryAsync(-1); // Id âm → rỗng
            var r2 = await service.GetRestaurantsByCategoryAsync(0);  // Id = 0 → rỗng

            // === KIỂM TRA (Assert) ===

            Assert.NotNull(r1); Assert.False(r1.Any()); // Không null, không có phần tử
            Assert.NotNull(r2); Assert.False(r2.Any()); // Không null, không có phần tử
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-018 – GetRestaurantsByCategoryAsync trả tất cả nhà hàng bất kể Status.
    /// Kiểm tra: hàm này KHÔNG lọc theo Status (khác với API thực tế có thể lọc chỉ active).
    /// ⚠ FAIL: 3 restaurant dùng cùng AddressId=1 → UNIQUE constraint failed.
    /// </summary>
    [Fact]
    public async Task GetRestaurantsByCategoryAsync_AllStatuses_ReturnsAll()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();

            // 3 nhà hàng với Status khác nhau: 0, 1, -1 (tất cả phải được trả về)
            // ⚠ Cả 3 dùng AddressId=1 → UNIQUE violation từ bản ghi thứ 2
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 0 },  // Chờ duyệt
                new Restaurant { Id = 2, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },  // Đang hoạt động
                new Restaurant { Id = 3, CateId = 1, UserId = 100, AddressId = 1, Status = -1 }); // Bị tắt
            await context.SaveChangesAsync();

            // === THỰC THI (Act) ===

            var result = (await service.GetRestaurantsByCategoryAsync(1)).ToList();

            // === KIỂM TRA (Assert) ===
            // Kỳ vọng: 3 nhà hàng (không lọc theo Status)
            // === KIỂM TRA (Assert) ===
            Assert.Equal(3, result.Count);
        }
        finally { await transaction.RollbackAsync(); }
    }

    // =========================================================================
    //  NHÓM 3: GetRestaurantsByUserAsync
    //  Lấy danh sách nhà hàng theo chủ sở hữu (UserId).
    //  ⚠ KNOWN ISSUE: các test seed nhiều Restaurant với cùng AddressId=1
    //    → vi phạm UNIQUE constraint → test FAILED (TC-RSS-019, TC-RSS-023).
    // =========================================================================

    /// <summary>
    /// TC-RSS-019 – Trả đúng 2 nhà hàng của user 100, không lẫn nhà hàng của user 101.
    /// ⚠ FAIL: 3 restaurant dùng AddressId=1 → UNIQUE constraint failed.
    /// </summary>
    [Fact]
    public async Task GetRestaurantsByUserAsync_ReturnsOnlyUserRestaurants()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            // 2 user: user 100 sẽ có 2 nhà hàng, user 101 sẽ có 1 nhà hàng
            context.Users.AddRange(
                new User { Id = 100, Email = "u100@x.vn", Password = "1", Role = "r", Status = 1 },
                new User { Id = 101, Email = "u101@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();

            // ⚠ 3 restaurant cùng AddressId=1 → UNIQUE violation từ bản ghi thứ 2
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, Name = "Quán A", CateId = 1, UserId = 100, AddressId = 1, Status = 1 }, // Của user 100
                new Restaurant { Id = 2, Name = "Quán B", CateId = 1, UserId = 100, AddressId = 1, Status = 1 }, // Của user 100
                new Restaurant { Id = 3, Name = "Quán C", CateId = 1, UserId = 101, AddressId = 1, Status = 1 }); // Của user 101 (phải bị loại)
            await context.SaveChangesAsync();

            // === THỰC THI (Act) ===
            // Gọi service: chỉ lấy nhà hàng của user 100
            // === THỰC THI (Act) ===
            var result = (await service.GetRestaurantsByUserAsync(100)).ToList();

            // === KIỂM TRA (Assert) ===

            Assert.Equal(2, result.Count); // Đúng 2 nhà hàng của user 100
            var ids = result.Select(r => r.Id).ToHashSet();
            Assert.Contains(1, ids);          // Quán A phải có
            Assert.Contains(2, ids);          // Quán B phải có
            Assert.DoesNotContain(3, ids);    // Quán C (user 101) phải bị loại
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-020 – Trả rỗng khi user tồn tại nhưng chưa có nhà hàng nào.
    /// Kiểm tra: không ném exception với user "trắng" (chưa mở quán).
    /// </summary>
    [Fact]
    public async Task GetRestaurantsByUserAsync_UserWithNoRestaurants_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            // Seed user 102 nhưng không tạo nhà hàng nào cho họ
            context.Users.Add(new User { Id = 102, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();

            // === THỰC THI (Act) ===

            var result = await service.GetRestaurantsByUserAsync(102);

            // === KIỂM TRA (Assert) ===

            Assert.NotNull(result);      // Không trả null
            Assert.False(result.Any()); // Danh sách rỗng
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-021 – Trả rỗng khi userId không tồn tại trong DB.
    /// Kiểm tra: query với userId không có → an toàn, không ném exception.
    /// </summary>
    [Fact]
    public async Task GetRestaurantsByUserAsync_NonexistentUser_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        // userId=9999 không có trong DB trống → phải trả rỗng
        // === THỰC THI (Act) ===
        var result = await service.GetRestaurantsByUserAsync(9999);

        // === KIỂM TRA (Assert) ===

        Assert.NotNull(result);
        Assert.False(result.Any());
    }

    /// <summary>
    /// TC-RSS-022 – userId âm hoặc 0 trả rỗng, không ném exception.
    /// Kiểm tra: biên giá trị ≤ 0 được xử lý an toàn.
    /// </summary>
    [Fact]
    public async Task GetRestaurantsByUserAsync_NegativeOrZeroUserId_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        var r1 = await service.GetRestaurantsByUserAsync(-5); // Id âm → rỗng
        var r2 = await service.GetRestaurantsByUserAsync(0);  // Id = 0 → rỗng

        Assert.False(r1.Any()); // Không có nhà hàng với UserId = -5
        Assert.False(r2.Any()); // Không có nhà hàng với UserId = 0
    }

    /// <summary>
    /// TC-RSS-023 – GetRestaurantsByUserAsync trả tất cả nhà hàng bất kể Status.
    /// ⚠ FAIL: 3 restaurant cùng AddressId=1 → UNIQUE constraint failed.
    /// </summary>
    [Fact]
    public async Task GetRestaurantsByUserAsync_IncludesAllStatuses()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();

            // 3 nhà hàng cùng user 100 với Status 0, 1, -1 (không lọc)
            // ⚠ Cùng AddressId=1 → UNIQUE violation từ bản ghi thứ 2
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 0 },  // Chờ duyệt
                new Restaurant { Id = 2, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },  // Hoạt động
                new Restaurant { Id = 3, CateId = 1, UserId = 100, AddressId = 1, Status = -1 }); // Bị tắt
            await context.SaveChangesAsync();

            var result = (await service.GetRestaurantsByUserAsync(100)).ToList();

            // === KIỂM TRA (Assert) ===
            // Kỳ vọng: 3 nhà hàng (bao gồm cả chờ duyệt và bị tắt)
            Assert.Equal(3, result.Count);
        }
        finally { await transaction.RollbackAsync(); }
    }

    // =========================================================================
    //  NHÓM 4: SearchRestaurantsAsync
    //  Tìm kiếm nhà hàng theo từ khoá (Contains trong Name hoặc Address.City).
    //  Sử dụng SQL LIKE với wildcard %searchTerm%.
    //  ⚠ SQLite LIKE mặc định chỉ hỗ trợ ASCII – tiếng Việt có dấu có thể không khớp.
    // =========================================================================

    /// <summary>
    /// TC-RSS-024 – Tìm kiếm theo tên với từ khoá ASCII (Contains).
    /// Kiểm tra: trả 2 nhà hàng có "Phở" trong tên, bỏ qua nhà hàng không khớp.
    /// </summary>
    [Fact]
    public async Task SearchRestaurantsAsync_ByName_ReturnsMatchingRestaurants()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            // 3 nhà hàng: 2 có "Phở" trong tên, 1 không có
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, Name = "Phở Bát Đàn", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, Name = "Bún Chả Hàng Mành", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, Name = "Phở Gia Truyền", CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            // Tìm kiếm với từ khoá "Phở"
            // === THỰC THI (Act) ===
            var result = (await service.SearchRestaurantsAsync("Phở")).ToList();

            // Chỉ 2 nhà hàng có "Phở" trong tên được trả về
            // === KIỂM TRA (Assert) ===
            Assert.Equal(2, result.Count);
            var names = result.Select(r => r.Name).ToHashSet();
            Assert.Contains("Phở Bát Đàn", names);    // Khớp tên
            Assert.Contains("Phở Gia Truyền", names); // Khớp tên
            // "Bún Chả Hàng Mành" không được trả về (không chứa "Phở")
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-025 – Từ khoá không khớp với bất kỳ nhà hàng nào → trả danh sách rỗng.
    /// Kiểm tra: không ném exception khi không có kết quả.
    /// </summary>
    [Fact]
    public async Task SearchRestaurantsAsync_NoMatch_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, Name = "Phở Bát Đàn", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, Name = "Bún Chả Hàng Mành", CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            // === THỰC THI (Act) ===

            var result = await service.SearchRestaurantsAsync("KhôngTồnTại");

            // === KIỂM TRA (Assert) ===

            Assert.NotNull(result);
            Assert.False(result.Any());
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-026 – Chuỗi rỗng ("") → trả toàn bộ nhà hàng (không có điều kiện lọc).
    /// Kiểm tra: LIKE '%' khớp tất cả bản ghi.
    /// </summary>
    [Fact]
    public async Task SearchRestaurantsAsync_EmptyString_ReturnsAllRestaurants()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, Name = "R1", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, Name = "R2", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, Name = "R3", CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            // === THỰC THI (Act) ===

            var result = (await service.SearchRestaurantsAsync("")).ToList();

            // === KIỂM TRA (Assert) ===

            Assert.Equal(3, result.Count);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-027 – searchTerm = null → kỳ vọng ném ArgumentNullException.
    /// ⚠ FAIL: production code KHÔNG có null-guard trong SearchRestaurantsAsync.
    ///   EF Core xử lý null trong LIKE → trả kết quả thay vì ném exception.
    ///   Cách fix: thêm null-guard ở đầu SearchRestaurantsAsync, hoặc sửa test để
    ///   kỳ vọng kết quả trả về (không throw) thay vì exception.
    /// </summary>
    [Fact]
    public async Task SearchRestaurantsAsync_NullSearchTerm_ThrowsArgumentNullException()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant { Id = 1, Name = "Phở", CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            // Kỳ vọng ArgumentNullException – nhưng hiện tại production code không throw
            // (EF Core chuyển null thành SQL không có điều kiện LIKE)
            // === THỰC THI (Act) ===
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => service.SearchRestaurantsAsync(null!));
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-028 – Tìm kiếm tiếng Việt có dấu khớp chính xác (SQLite hỗ trợ Unicode đủ cho LIKE).
    /// Kiểm tra: "Sườn" có dấu nặng vẫn được tìm thấy trong tên nhà hàng.
    /// </summary>
    [Fact]
    public async Task SearchRestaurantsAsync_VietnameseAccented_ReturnsMatch()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
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

            // === THỰC THI (Act) ===

            var result = (await service.SearchRestaurantsAsync("Sườn")).ToList();

            // === KIỂM TRA (Assert) ===

            Assert.Single(result);
            Assert.Equal("Cơm Tấm Sườn Bì Chả", result[0].Name);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-029 – searchTerm có khoảng trắng đầu/cuối KHÔNG tự trim → trả rỗng.
    /// Kiểm tra: service không tự trim khoảng trắng → " Phở " không khớp "Phở Bát Đàn".
    /// Ghi nhận hành vi hiện hành: nếu cần trim, thêm .Trim() vào production code.
    /// </summary>
    [Fact]
    public async Task SearchRestaurantsAsync_LeadingTrailingSpace_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
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

            // === THỰC THI (Act) ===

            var result = await service.SearchRestaurantsAsync(" Phở ");

            // === KIỂM TRA (Assert) ===

            Assert.NotNull(result);
            Assert.False(result.Any());  // Hành vi hiện hành: không trim
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RSS-030 – Tìm kiếm theo City của Address (không chỉ theo Name nhà hàng).
    /// Kiểm tra: "Đà" khớp với City = "Đà Nẵng" → trả nhà hàng tại Đà Nẵng.
    /// SearchRestaurantsAsync tìm kiếm trong cả Address.City lẫn Restaurant.Name.
    /// </summary>
    [Fact]
    public async Task SearchRestaurantsAsync_ByAddressCity_ReturnsMatch()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
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

            // === THỰC THI (Act) ===

            var result = (await service.SearchRestaurantsAsync("Đà")).ToList();

            // === KIỂM TRA (Assert) ===

            Assert.Single(result);
            Assert.Equal("Quán Ăn Miền Trung", result[0].Name);
        }
        finally { await transaction.RollbackAsync(); }
    }

    // =========================================================================
    //  NHÓM 5: GetRestaurantByIdAsync
    //  Lấy chi tiết 1 nhà hàng theo Id, bao gồm Address, Category, RestaurantPhotos.
    // =========================================================================

    /// <summary>
    /// TC-RSS-031 – Trả RestaurantDetailDto với đầy đủ tất cả field khi nhà hàng tồn tại.
    /// Kiểm tra: Id, Name, Email, Description, PhoneNumber, Status, AvtImage,
    ///   Category, AverageScore, TotalReviews, UserId, Address.City, Address.District.
    /// </summary>
    [Fact]
    public async Task GetRestaurantByIdAsync_ExistingRestaurant_ReturnsFullDetail()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = BuildService(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
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

            // === THỰC THI (Act) ===

            var result = await service.GetRestaurantByIdAsync(500);

            // === KIỂM TRA (Assert) ===

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

        // === THỰC THI (Act) ===

        var result = await service.GetRestaurantByIdAsync(999);

        // === KIỂM TRA (Assert) ===

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
            // === CHUẨN BỊ (Arrange) ===
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
            // === CHUẨN BỊ (Arrange) ===
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

            // === THỰC THI (Act) ===

            var result = await service.GetRestaurantByIdAsync(502);

            // === KIỂM TRA (Assert) ===

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
            // === CHUẨN BỊ (Arrange) ===
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant
            {
                Id = 503, CateId = 1, UserId = 100, AddressId = 1, Status = 1
            });
            await context.SaveChangesAsync();

            // === THỰC THI (Act) ===

            var result = await service.GetRestaurantByIdAsync(503);

            // === KIỂM TRA (Assert) ===

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

        // === KIỂM TRA (Assert) ===

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
            // === CHUẨN BỊ (Arrange) ===
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

            // === THỰC THI (Act) ===

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
            // === CHUẨN BỊ (Arrange) ===
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

            // === THỰC THI (Act) ===

            await service.UpdateRestaurantAsync(600, MakeUpdateDto("Tên mới", status: 1, cateId: 2));

            // CheckDB
            var dbR = await context.Restaurants.FindAsync(600);
            // === KIỂM TRA (Assert) ===
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

        // === THỰC THI (Act) ===

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateRestaurantAsync(9999, MakeUpdateDto()));

        // === KIỂM TRA (Assert) ===

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
            // === CHUẨN BỊ (Arrange) ===
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
            // === CHUẨN BỊ (Arrange) ===
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

            // === THỰC THI (Act) ===

            await service.UpdateRestaurantAsync(602, updateDto);

            var dbAddr = context.Restaurants.Include(r => r.Address).First(r => r.Id == 602).Address;
            // === KIỂM TRA (Assert) ===
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
            // === CHUẨN BỊ (Arrange) ===
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
            // === THỰC THI (Act) ===
            await service.UpdateRestaurantAsync(603, updateDto);

            var dbR = context.Restaurants.Include(r => r.RestaurantPhotos).First(r => r.Id == 603);
            // === KIỂM TRA (Assert) ===
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
            // === CHUẨN BỊ (Arrange) ===
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

            // === THỰC THI (Act) ===

            await service.UpdateRestaurantAsync(604, updateDto);

            // CheckDB: không còn ảnh nào của restaurant 604
            var hasPhotos = await context.RestaurantPhotos.AnyAsync(p => p.RestaurantId == 604);
            // === KIỂM TRA (Assert) ===
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
            // === CHUẨN BỊ (Arrange) ===
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
            // === THỰC THI (Act) ===
            await service.UpdateRestaurantAsync(605, updateDto);

            // === KIỂM TRA (Assert) ===

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
            // === CHUẨN BỊ (Arrange) ===
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

            // === THỰC THI (Act) ===

            var ex = await Assert.ThrowsAsync<Exception>(
                () => service.UpdateRestaurantAsync(606, MakeUpdateDto("Tên mới")));

            // === KIỂM TRA (Assert) ===

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
            // === CHUẨN BỊ (Arrange) ===
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
            // === THỰC THI (Act) ===
            await Assert.ThrowsAsync<NullReferenceException>(
                () => service.UpdateRestaurantAsync(607, updateDto));

            // === KIỂM TRA (Assert) ===

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

        // === THỰC THI (Act) ===

        var ex = await Assert.ThrowsAsync<DbUpdateException>(
            () => service.UpdateRestaurantAsync(608, MakeUpdateDto()));

        // === KIỂM TRA (Assert) ===

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
            // === CHUẨN BỊ (Arrange) ===
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
        // === THỰC THI (Act) ===
        await service.DeleteRestaurantAsync(9999);

        // === KIỂM TRA (Assert) ===

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
            // === CHUẨN BỊ (Arrange) ===
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
            // === CHUẨN BỊ (Arrange) ===
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

            // === THỰC THI (Act) ===

            await service.DeleteRestaurantAsync(701);

            // CheckDB: nhà hàng và ảnh, order cascade bị xóa
            // === KIỂM TRA (Assert) ===
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

        // === THỰC THI (Act) ===

        var ex = await Assert.ThrowsAsync<DbUpdateException>(
            () => service.DeleteRestaurantAsync(700));

        // === KIỂM TRA (Assert) ===

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
            // === CHUẨN BỊ (Arrange) ===
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
            // === CHUẨN BỊ (Arrange) ===
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

            // === THỰC THI (Act) ===

            var result = (await service.GetRestaurantsByAddressAsync(null, null, null, null)).ToList();

            // === KIỂM TRA (Assert) ===

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
            // === CHUẨN BỊ (Arrange) ===
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

            // === THỰC THI (Act) ===

            var result = (await service.GetRestaurantsByAddressAsync("Hà Nội", null, null, null)).ToList();

            // === KIỂM TRA (Assert) ===

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
            // === CHUẨN BỊ (Arrange) ===
            // Giả lập 2 địa chỉ ở Hà Nội nhưng khác District
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            context.Addresses.AddRange(
                new Address { Id = 1, City = "Hà Nội", District = "Ba Đình" },
                new Address { Id = 2, City = "Hà Nội", District = "Cầu Giấy" });
            await context.SaveChangesAsync();
            
            // 3 nhà hàng ở Ba Đình, 2 nhà hàng ở Cầu Giấy
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 4, CateId = 1, UserId = 100, AddressId = 2, Status = 1 },
                new Restaurant { Id = 5, CateId = 1, UserId = 100, AddressId = 2, Status = 1 });
            await context.SaveChangesAsync();

            // === THỰC THI (Act) ===
            // Lọc các nhà hàng có District = "Ba Đình" (city null)
            var result = (await service.GetRestaurantsByAddressAsync(null, "Ba Đình", null, null)).ToList();

            // === KIỂM TRA (Assert) ===
            // Phải trả về chính xác 3 nhà hàng ở Ba Đình
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
            // === CHUẨN BỊ (Arrange) ===
            // Khởi tạo 1 nhà hàng ở Hà Nội
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            // === THỰC THI (Act) ===
            // Tìm kiếm ở Cần Thơ
            var result = await service.GetRestaurantsByAddressAsync("Cần Thơ", null, null, null);

            // === KIỂM TRA (Assert) ===
            // Trả về danh sách rỗng, không văng exception
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
            // === CHUẨN BỊ (Arrange) ===
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            // Tạo 2 địa chỉ: 1 có dấu, 1 không dấu để kiểm tra Unicode
            context.Addresses.AddRange(
                new Address { Id = 1, City = "Huế" },
                new Address { Id = 2, City = "Hue" });
            await context.SaveChangesAsync();
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, CateId = 1, UserId = 100, AddressId = 2, Status = 1 });
            await context.SaveChangesAsync();

            // === THỰC THI (Act) ===
            // Tìm theo "Huế" (có dấu)
            var result = (await service.GetRestaurantsByAddressAsync("Huế", null, null, null)).ToList();

            // === KIỂM TRA (Assert) ===
            // Chỉ trả về 1 nhà hàng ở "Huế", bỏ qua "Hue"
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
            // === CHUẨN BỊ (Arrange) ===
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            context.Restaurants.Add(new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            // === THỰC THI (Act) ===
            // Tìm theo chữ thường. Do SQLite mặc định phân biệt hoa thường với tiếng Việt
            var result = await service.GetRestaurantsByAddressAsync("hà nội", null, null, null);

            // === KIỂM TRA (Assert) ===
            // Kết quả rỗng (Ghi nhận hành vi hiện hành của hệ thống DB)
            Assert.NotNull(result);
            Assert.False(result.Any());
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
            // === CHUẨN BỊ (Arrange) ===
            // Seed 5 nhà hàng chuẩn
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

            // === THỰC THI (Act) ===
            // Gọi hàm get toàn diện nhưng để null hết các tham số lọc
            var result = (await service.GetRestaurantsAsync(null, null, null, null, null, null)).ToList();

            // === KIỂM TRA (Assert) ===
            // Trả về đầy đủ 5 nhà hàng
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
            // === CHUẨN BỊ (Arrange) ===
            context.Categories.AddRange(
                new Category { Id = 1, Name = "Món Việt" },
                new Category { Id = 2, Name = "Món Hàn" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            
            // 2 nhà hàng Món Việt, 3 nhà hàng Món Hàn
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, CateId = 2, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 4, CateId = 2, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 5, CateId = 2, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            // === THỰC THI (Act) ===
            // Chỉ lọc theo CateId = 1
            var result = (await service.GetRestaurantsAsync(1, null, null, null, null, null)).ToList();

            // === KIỂM TRA (Assert) ===
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
            // === CHUẨN BỊ (Arrange) ===
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.AddRange(
                new User { Id = 100, Email = "u100@x.vn", Password = "1", Role = "r", Status = 1 },
                new User { Id = 101, Email = "u101@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            
            // User 100 sở hữu 2 quán, User 101 sở hữu 1 quán
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, CateId = 1, UserId = 101, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            // === THỰC THI (Act) ===
            // Lọc UserId = 100
            var result = (await service.GetRestaurantsAsync(null, 100, null, null, null, null)).ToList();

            // === KIỂM TRA (Assert) ===
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
            // === CHUẨN BỊ (Arrange) ===
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            
            // 2 quán tên "Phở", 1 quán "Bún Chả"
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, Name = "Phở Thìn", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, Name = "Bún Chả", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, Name = "Phở Gia Truyền", CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            // === THỰC THI (Act) ===
            // Lọc tên có chữ "Phở"
            var result = (await service.GetRestaurantsAsync(null, null, "Phở", null, null, null)).ToList();

            // === KIỂM TRA (Assert) ===
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
            // === CHUẨN BỊ (Arrange) ===
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
            
            // Tạo dữ liệu phức tạp nhiều giao điểm
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, Name = "Phở Thìn", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, Name = "Phở Khác", CateId = 1, UserId = 101, AddressId = 1, Status = 1 },
                new Restaurant { Id = 3, Name = "Mỳ Quảng", CateId = 2, UserId = 100, AddressId = 2, Status = 1 });
            await context.SaveChangesAsync();

            // === THỰC THI (Act) ===
            // categoryId=1, userId=100, searchTerm="Phở", city="Hà Nội" → Dùng cơ chế AND để lọc
            var result = (await service.GetRestaurantsAsync(1, 100, "Phở", "Hà Nội", null, null)).ToList();

            // === KIỂM TRA (Assert) ===
            // Giao điểm chỉ có duy nhất nhà hàng Id = 1
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
            // === CHUẨN BỊ (Arrange) ===
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

            // === THỰC THI (Act) ===
            // Tìm các nhà hàng ở "Cần Thơ" trong category 1
            var result = await service.GetRestaurantsAsync(1, null, null, "Cần Thơ", null, null);

            // === KIỂM TRA (Assert) ===
            // Không tìm thấy nên mảng rỗng
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
            // === CHUẨN BỊ (Arrange) ===
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            context.Categories.Add(new Category { Id = 99, Name = "Orphan" });
            await context.SaveChangesAsync();
            
            // Giả lập một nhà hàng liên kết tới Category 99
            context.Restaurants.Add(new Restaurant
            {
                Id = 1, Name = "Quán Lạ", CateId = 99, UserId = 100, AddressId = 1, Status = 1
            });
            await context.SaveChangesAsync();
            
            // Xóa cứng danh mục 99 (Orphan mapping)
            context.Categories.Remove(context.Categories.Find(99)!);
            await context.SaveChangesAsync();

            // === THỰC THI (Act) ===
            var result = (await service.GetRestaurantsAsync(null, null, null, null, null, null)).ToList();

            // === KIỂM TRA (Assert) ===
            // Vẫn trả về thông tin nhà hàng nhưng thuộc tính Category sẽ là null thay vì crash code
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
            // === CHUẨN BỊ (Arrange) ===
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, Name = "R1", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, Name = "R2", CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();
            
            // R1 có 2 ảnh phụ
            context.RestaurantPhotos.AddRange(
                new RestaurantPhoto { Id = 1, RestaurantId = 1, ImageUrl = "a.jpg" },
                new RestaurantPhoto { Id = 2, RestaurantId = 1, ImageUrl = "b.jpg" });
            // R2 không có ảnh
            await context.SaveChangesAsync();

            // === THỰC THI (Act) ===
            var result = (await service.GetRestaurantsAsync(null, null, null, null, null, null)).ToList();

            // === KIỂM TRA (Assert) ===
            Assert.Equal(2, result.Count);
            var r1 = result.First(r => r.Id == 1);
            var r2 = result.First(r => r.Id == 2);

            // R1 phải có 2 ảnh trong mảng
            Assert.Equal(2, r1.RestaurantPhotos!.Count);
            Assert.Contains("a.jpg", r1.RestaurantPhotos);
            Assert.Contains("b.jpg", r1.RestaurantPhotos);

            // R2 phải khởi tạo mảng rỗng, không được null
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
            // === CHUẨN BỊ (Arrange) ===
            // Cấu hình đầy đủ data tham chiếu cho nhà hàng
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

            // === THỰC THI (Act) ===
            var result = (await service.GetRestaurantsAsync(null, null, null, null, null, null)).ToList();

            // === KIỂM TRA (Assert) ===
            // Đảm bảo không trường thông tin nào bị mất mát trong quá trình map từ Entity sang Dto
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
            // === CHUẨN BỊ (Arrange) ===
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User { Id = 100, Email = "u@x.vn", Password = "1", Role = "r", Status = 1 });
            await context.SaveChangesAsync();
            
            // Tạo nhà hàng "Phở Gánh" (có chứa từ Gánh) và "Phở Thìn"
            context.Restaurants.AddRange(
                new Restaurant { Id = 1, Name = "Phở Gánh", CateId = 1, UserId = 100, AddressId = 1, Status = 1 },
                new Restaurant { Id = 2, Name = "Phở Thìn", CateId = 1, UserId = 100, AddressId = 1, Status = 1 });
            await context.SaveChangesAsync();

            // === THỰC THI (Act) ===
            // Truy vấn từ khóa tiếng việt "Gánh"
            var result = (await service.GetRestaurantsAsync(null, null, "Gánh", null, null, null)).ToList();

            // === KIỂM TRA (Assert) ===
            // Chỉ trả về đúng 1 kết quả
            Assert.Single(result);
            Assert.Equal("Phở Gánh", result[0].Name);
        }
        finally { await transaction.RollbackAsync(); }
    }
}

