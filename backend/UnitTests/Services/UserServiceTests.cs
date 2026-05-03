using BusinessLogicLayer.Services;
using DataAccessLayer.Context;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UnitTests.Infrastructure;
using Xunit;

namespace UnitTests.Services;

/// <summary>
/// Unit test UserService — SQLite seed + transaction rollback (mẫu), không chỉnh production.
/// TC-USV-REG-004 / UU-009 / DU-003 cần mock repository → Skip (không đổi DI trong mã nguồn).
/// </summary>
public class UserServiceTests
{
    private static IConfiguration JwtConfiguration =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "KhoaBiMatChoJwtPhaiDuDaiDeKyToken123456789",
                ["Jwt:Subject"] = "NguoiDungHeThong",
                ["Jwt:Issuer"] = "DoAnBackend",
                ["Jwt:Audience"] = "DoAnClient"
            })
            .Build();

    private static UserService CreateUserService(ApplicationDbContext context)
    {
        var userRepository = new UserRepository(context);
        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);
        return new UserService(userRepository, addressService, JwtConfiguration);
    }

    private static AddressDto SampleAddress(string city = "Hà Nội") =>
        new()
        {
            City = city,
            District = "Cầu Giấy",
            Ward = "Dịch Vọng",
            Detail = "Ngõ 12, đường Xuân Thủy",
            Lat = 21.0285,
            Lon = 105.8041
        };

    private static async Task<Address> SeedAddressAsync(AddressRepository ar, AddressDto dto)
    {
        return await ar.AddAddressAsync(new Address
        {
            City = dto.City,
            District = dto.District ?? "",
            Ward = dto.Ward ?? "",
            Detail = dto.Detail ?? "",
            Lon = dto.Lon ?? 0,
            Lat = dto.Lat ?? 0
        });
    }

    /// <summary>
    /// TC-USV-REG-001
    /// Đăng ký customer + địa chỉ hợp lệ => Success, User + Address trong DB.
    /// </summary>
    [Fact]
    public async Task RegisterUserAsync_CustomerValid_TC_USV_REG_001()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await context.Database.BeginTransactionAsync();
        try
        {
            var svc = CreateUserService(context);

            // Gọi đăng ký với email/phone chưa tồn tại, role = customer
            var dto = new UserDTO
            {
                Email = "tranvanhung@example.vn",
                Password = "MatKhau123!",
                Name = "Trần Văn Hùng",
                PhoneNumber = "0912345678",
                Role = "customer",
                Address = SampleAddress()
            };
            var result = await svc.RegisterUserAsync(dto);

            // Kiểm tra kết quả trả về + User trong DB + Address liên kết
            // SPEC: Email/Name/PhoneNumber cũng phải khớp request
            Assert.True(result.Success);
            Assert.Equal("User registered successfully", result.Message);
            var u = await context.Users.SingleAsync();
            Assert.Equal("customer", u.Role);
            Assert.Equal(1, u.Status);
            Assert.Equal("tranvanhung@example.vn", u.Email);
            Assert.Equal("Trần Văn Hùng", u.Name);
            Assert.Equal("0912345678", u.PhoneNumber);
            Assert.NotNull(u.AddressId);
            var addr = await context.Addresses.SingleAsync(a => a.Id == u.AddressId);
            Assert.Equal("Hà Nội", addr.City);
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-USV-REG-002
    /// Đăng ký admin => Role admin.
    /// </summary>
    [Fact]
    public async Task RegisterUserAsync_AdminRole_TC_USV_REG_002()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await context.Database.BeginTransactionAsync();
        try
        {
            var svc = CreateUserService(context);

            // Gọi đăng ký với role = admin, email mới
            var result = await svc.RegisterUserAsync(new UserDTO
            {
                Email = "quantri@hethong.vn",
                Password = "MatKhauAdmin2024!",
                Name = "Lê Thị Quản Trị",
                PhoneNumber = "0908111222",
                Role = "admin",
                Address = new AddressDto
                {
                    City = "Đà Nẵng",
                    District = "Hải Châu",
                    Ward = "Thạch Thang",
                    Detail = "Số 20 đường Lê Duẩn",
                    Lat = 16.0544,
                    Lon = 108.2022
                }
            });

            // User.Role phải là admin
            Assert.True(result.Success);
            Assert.Equal("admin", (await context.Users.SingleAsync()).Role);
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-USV-REG-003
    /// Email đã tồn tại => Success false, không tạo user mới.
    /// </summary>
    [Fact]
    public async Task RegisterUserAsync_DuplicateEmail_TC_USV_REG_003()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await context.Database.BeginTransactionAsync();
        try
        {
            var ur = new UserRepository(context);
            var ar = new AddressRepository(context);
            var addr = await SeedAddressAsync(ar, SampleAddress());
            await ur.CreateAsync(new User
            {
                Email = "nguyenthianh@gmail.com",
                Password = "old",
                Role = "customer",
                Name = "Người Cũ",
                PhoneNumber = "0909000111",
                Status = 1,
                AddressId = addr.Id
            });
            var countBefore = await context.Users.CountAsync();

            var svc = CreateUserService(context);

            // Đăng ký lại với email đã tồn tại
            var result = await svc.RegisterUserAsync(new UserDTO
            {
                Email = "nguyenthianh@gmail.com",
                Password = "MatKhau123!",
                Name = "Nguyễn Thị Ánh",
                PhoneNumber = "0912333444",
                Role = "customer",
                Address = SampleAddress("Hà Nội")
            });

            // Từ chối, DB không tăng user, user cũ không bị ghi đè
            Assert.False(result.Success);
            Assert.Equal("Email already exists", result.Message);
            Assert.Equal(countBefore, await context.Users.CountAsync());
            Assert.Equal("Người Cũ", (await context.Users.FirstAsync(u => u.Email == "nguyenthianh@gmail.com")).Name);
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-USV-REG-004 — mock CreateAsync trả null; không đổi repository trong production.
    /// </summary>
    [Fact(Skip = "TC-USV-REG-004: cần mock UserRepository.CreateAsync — không chỉnh mã nguồn.")]
    public Task RegisterUserAsync_CreateReturnsNull_TC_USV_REG_004() => Task.CompletedTask;

    /// <summary>
    /// TC-USV-LOGIN-001
    /// Đăng nhập đúng => JWT, Login successfully.
    /// </summary>
    [Fact]
    public async Task LoginAsync_ValidCredentials_TC_USV_LOGIN_001()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var userRepository = new UserRepository(context);
            var addressRepository = new AddressRepository(context);
            var addressService = new AddressService(addressRepository);
            var userService = new UserService(userRepository, addressService, JwtConfiguration);

            // Seed user với đúng email/password/Status=1
            await userRepository.CreateAsync(new User
            {
                Email = "nguoidung@test.vn",
                Password = "matkhau123",
                Role = "customer",
                Name = "Nguyen Van B",
                PhoneNumber = "0909123456",
                Status = 1
            });

            // Ghi lại số user trước login để kiểm tra DB không bị thay đổi
            var userCountBeforeLogin = await context.Users.CountAsync();

            // Đăng nhập đúng thông tin
            var result = await userService.LoginAsync(new LoginDTO
            {
                Email = "nguoidung@test.vn",
                Password = "matkhau123"
            });

            // Message thành công, Token JWT hợp lệ (dạng xxx.yyy.zzz), DB không đổi
            Assert.Equal("Login successfully", result.Message);
            Assert.False(string.IsNullOrWhiteSpace(result.Token));
            Assert.Contains(".", result.Token);
            Assert.Equal(userCountBeforeLogin, await context.Users.CountAsync());
            Assert.False(context.ChangeTracker.HasChanges());
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        // Mở context mới xác nhận rollback thật sự — user không còn tồn tại
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var rolledBackUser = await verifyContext.Users.FirstOrDefaultAsync(u => u.Email == "nguoidung@test.vn");
        Assert.Null(rolledBackUser);
    }

    /// <summary>
    /// TC-USV-LOGIN-002
    /// Sai mật khẩu hoặc email => không token.
    /// </summary>
    [Fact]
    public async Task LoginAsync_WrongPassword_TC_USV_LOGIN_002()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await context.Database.BeginTransactionAsync();
        try
        {
            await new UserRepository(context).CreateAsync(new User
            {
                Email = "nguoidung@test.vn",
                Password = "matkhau123",
                Role = "customer",
                Name = "A",
                Status = 1
            });
            var svc = CreateUserService(context);

            // Đăng nhập sai mật khẩu
            var r = await svc.LoginAsync(new LoginDTO { Email = "nguoidung@test.vn", Password = "saimatkhau" });

            // Token null, message sai thông tin đăng nhập
            Assert.Null(r.Token);
            Assert.Equal("Email or Password is incorrect", r.Message);
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-USV-LOGIN-003
    /// Tài khoản khóa => không token.
    /// </summary>
    [Fact]
    public async Task LoginAsync_AccountLocked_TC_USV_LOGIN_003()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await context.Database.BeginTransactionAsync();
        try
        {
            await new UserRepository(context).CreateAsync(new User
            {
                Email = "bikhoa@example.vn",
                Password = "dungmatkhau",
                Role = "customer",
                Name = "B",
                Status = 0
            });
            var svc = CreateUserService(context);

            // Đăng nhập đúng mật khẩu nhưng Status=0
            var r = await svc.LoginAsync(new LoginDTO { Email = "bikhoa@example.vn", Password = "dungmatkhau" });

            // Token null, message chứa "locked"
            Assert.Null(r.Token);
            Assert.Contains("locked", r.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-USV-GAU-001
    /// Nhiều user => GetAllUsers trả đủ.
    /// </summary>
    [Fact]
    public async Task GetAllUsersAsync_MultipleUsers_TC_USV_GAU_001()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await context.Database.BeginTransactionAsync();
        try
        {
            var ur = new UserRepository(context);
            var ar = new AddressRepository(context);
            var a1 = await SeedAddressAsync(ar, SampleAddress());
            var a2 = await SeedAddressAsync(ar, SampleAddress("Huế"));
            await ur.CreateAsync(new User
            {
                Email = "phamvanan@mail.vn",
                Name = "Phạm Văn An",
                PhoneNumber = "0911112222",
                Role = "customer",
                Status = 1,
                Password = "p",
                AddressId = a1.Id
            });
            await ur.CreateAsync(new User
            {
                Email = "hoangthibich@mail.vn",
                Name = "Hoàng Thị Bích",
                PhoneNumber = "0922223333",
                Role = "customer",
                Status = 1,
                Password = "p",
                AddressId = a2.Id
            });
            var svc = CreateUserService(context);

            // Lấy toàn bộ danh sách user
            var list = (await svc.GetAllUsersAsync()).ToList();

            // Count ≥ 2, có đủ email đã seed
            Assert.True(list.Count >= 2);
            Assert.Contains(list, u => u.Email == "phamvanan@mail.vn");
            Assert.Contains(list, u => u.Email == "hoangthibich@mail.vn");
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-USV-GAU-002
    /// Không có user => rỗng.
    /// </summary>
    [Fact]
    public async Task GetAllUsersAsync_Empty_TC_USV_GAU_002()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var svc = CreateUserService(context);

        // DB rỗng → GetAllUsersAsync trả empty
        Assert.Empty(await svc.GetAllUsersAsync());
    }

    /// <summary>
    /// TC-USV-GAS-001
    /// Admin summaries có đủ field + địa chỉ.
    /// </summary>
    [Fact]
    public async Task GetAllUserSummariesForAdminAsync_WithAddress_TC_USV_GAS_001()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await context.Database.BeginTransactionAsync();
        try
        {
            var ur = new UserRepository(context);
            var ar = new AddressRepository(context);
            var addr = await SeedAddressAsync(ar, new AddressDto
            {
                City = "Hà Nội",
                District = "Ba Đình",
                Ward = "Điện Biên",
                Detail = "Số 5 phố Điện Biên Phủ",
                Lat = 21.03,
                Lon = 105.82
            });
            await ur.CreateAsync(new User
            {
                Email = "dominhchau@gmail.com",
                Name = "Đỗ Minh Châu",
                PhoneNumber = "0933444555",
                Role = "customer",
                Status = 1,
                Password = "p",
                AvtImage = "/images/chau.png",
                AddressId = addr.Id
            });
            var svc = CreateUserService(context);

            // Lấy tóm tắt admin — 1 user, có đầy đủ địa chỉ
            var row = (await svc.GetAllUserSummariesForAdminAsync()).Single();

            // SPEC: Name, Address not null, và các field địa chỉ (District, Ward) khớp seed
            Assert.Equal("Đỗ Minh Châu", row.Name);
            Assert.NotNull(row.Address);
            Assert.Equal("Ba Đình", row.Address!.District);
            Assert.Equal("Điện Biên", row.Address.Ward);
            Assert.Equal("Số 5 phố Điện Biên Phủ", row.Address.Detail);
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    // /// <summary>
    // /// TC-USV-GAS-002
    // /// User không địa chỉ => Address null trên summary.
    // /// </summary>
    // [Fact]
    // public async Task GetAllUserSummariesForAdminAsync_NoAddress_TC_USV_GAS_002()
    // {
    //     await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
    //     await using var context = SqliteMemoryDb.CreateContext(connection);
    //     await using var tx = await context.Database.BeginTransactionAsync();
    //     try
    //     {
    //         await new UserRepository(context).CreateAsync(new User
    //         {
    //             Email = "vuthilan@mail.vn",
    //             Name = "Vũ Thị Lan",
    //             Role = "customer",
    //             Status = 1,
    //             Password = "p",
    //             AddressId = null
    //         });
    //         var svc = CreateUserService(context);

    //         var row = (await svc.GetAllUserSummariesForAdminAsync()).Single();

    //         Assert.Null(row.Address);
    //     }
    //     finally
    //     {
    //         await tx.RollbackAsync();
    //     }
    // }

    /// <summary>
    /// TC-USV-GUI-001
    /// User có địa chỉ => UserDTO đầy đủ.
    /// </summary>
    [Fact]
    public async Task GetUserByIdAsync_WithAddress_TC_USV_GUI_001()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await context.Database.BeginTransactionAsync();
        try
        {
            var ur = new UserRepository(context);
            var ar = new AddressRepository(context);
            var addr = await SeedAddressAsync(ar, new AddressDto
            {
                City = "Huế",
                District = "Thành phố Huế",
                Ward = "Phú Hậu",
                Detail = "Số 88 phố Huế",
                Lat = 16.4637,
                Lon = 107.5909
            });
            await ur.CreateAsync(new User
            {
                Email = "user1@test.vn",
                Name = "Nguyễn Văn Một",
                PhoneNumber = "0909123456",
                Role = "customer",
                Status = 1,
                Password = "p",
                AddressId = addr.Id
            });
            var id = (await context.Users.FirstAsync()).Id;
            var svc = CreateUserService(context);

            var dto = await svc.GetUserByIdAsync(id);

            Assert.NotNull(dto);
            Assert.Equal("Số 88 phố Huế", dto!.Address!.Detail);
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-USV-GUI-002
    /// User không địa chỉ => Address null.
    /// </summary>
    [Fact]
    public async Task GetUserByIdAsync_NoAddress_TC_USV_GUI_002()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await context.Database.BeginTransactionAsync();
        try
        {
            await new UserRepository(context).CreateAsync(new User
            {
                Email = "khongdiachi@example.vn",
                Name = "Trần Thị Hai",
                PhoneNumber = "0912000333",
                Role = "customer",
                Status = 1,
                Password = "p",
                AddressId = null
            });
            var id = (await context.Users.FirstAsync()).Id;
            var svc = CreateUserService(context);

            var dto = await svc.GetUserByIdAsync(id);

            Assert.NotNull(dto);
            Assert.Null(dto!.Address);
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-USV-GUI-003
    /// Id không tồn tại => null.
    /// </summary>
    [Fact]
    public async Task GetUserByIdAsync_NotFound_TC_USV_GUI_003()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var svc = CreateUserService(context);

        Assert.Null(await svc.GetUserByIdAsync(9999));
    }

    /// <summary>
    /// TC-USV-UU-001
    /// Đổi Status => Success.
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_ChangeStatus_TC_USV_UU_001()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await context.Database.BeginTransactionAsync();
        try
        {
            await new UserRepository(context).CreateAsync(new User
            {
                Email = "user10@test.vn",
                Name = "Người Mười",
                PhoneNumber = "0900101010",
                Role = "customer",
                Status = 1,
                Password = "p"
            });
            var id = (await context.Users.FirstAsync()).Id;
            var svc = CreateUserService(context);

            // Gửi chỉ Status=0, các field khác null
            var result = await svc.UpdateUserAsync(id, new UserUpdateDTO { Status = 0 });

            // Status đổi thành 0 trong DB
            Assert.True(result.Success);
            Assert.Equal(0, (await context.Users.AsNoTracking().FirstAsync(u => u.Id == id)).Status);
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-USV-UU-002
    /// Chỉ đổi Name.
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_NameOnly_TC_USV_UU_002()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await context.Database.BeginTransactionAsync();
        try
        {
            await new UserRepository(context).CreateAsync(new User
            {
                Email = "buivan@mail.vn",
                Name = "Bùi Văn Cũ",
                PhoneNumber = "0909111222",
                Role = "customer",
                Status = 1,
                Password = "p"
            });
            var id = (await context.Users.FirstAsync()).Id;
            var svc = CreateUserService(context);

            // Gửi chỉ Name mới
            var result = await svc.UpdateUserAsync(id, new UserUpdateDTO { Name = "Bùi Văn Mới" });

            // Name đổi, Email + PhoneNumber giữ nguyên
            Assert.True(result.Success);
            var u = await context.Users.AsNoTracking().FirstAsync(x => x.Id == id);
            Assert.Equal("Bùi Văn Mới", u.Name);
            Assert.Equal("buivan@mail.vn", u.Email);
            Assert.Equal("0909111222", u.PhoneNumber);
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-USV-UU-003
    /// Chỉ đổi PhoneNumber.
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_PhoneOnly_TC_USV_UU_003()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await context.Database.BeginTransactionAsync();
        try
        {
            await new UserRepository(context).CreateAsync(new User
            {
                Email = "user12@test.vn",
                Name = "Lê Văn Mười Hai",
                PhoneNumber = "0909000111",
                Role = "customer",
                Status = 1,
                Password = "p"
            });
            var id = (await context.Users.FirstAsync()).Id;
            var svc = CreateUserService(context);

            // Gửi chỉ PhoneNumber mới
            var result = await svc.UpdateUserAsync(id, new UserUpdateDTO { PhoneNumber = "0987654321" });

            // PhoneNumber đổi, Email giữ nguyên
            Assert.True(result.Success);
            var u = await context.Users.AsNoTracking().FirstAsync(x => x.Id == id);
            Assert.Equal("0987654321", u.PhoneNumber);
            Assert.Equal("user12@test.vn", u.Email);
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-USV-UU-004
    /// Chỉ đổi AvtImage.
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_AvtOnly_TC_USV_UU_004()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await context.Database.BeginTransactionAsync();
        try
        {
            await new UserRepository(context).CreateAsync(new User
            {
                Email = "user13@test.vn",
                Name = "Ảnh Cũ",
                PhoneNumber = "0909131313",
                Role = "customer",
                Status = 1,
                Password = "p",
                AvtImage = "/images/old.png"
            });
            var id = (await context.Users.FirstAsync()).Id;
            var svc = CreateUserService(context);

            // Gửi chỉ AvtImage mới
            var result = await svc.UpdateUserAsync(id,
                new UserUpdateDTO { AvtImage = "/images/avatar_moi.png" });

            // AvtImage cập nhật đúng
            Assert.True(result.Success);
            Assert.Equal("/images/avatar_moi.png",
                (await context.Users.AsNoTracking().FirstAsync(x => x.Id == id)).AvtImage);
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-USV-UU-005
    /// Merge địa chỉ — chỉ đổi District.
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_AddressMergeDistrict_TC_USV_UU_005()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await context.Database.BeginTransactionAsync();
        try
        {
            var ur = new UserRepository(context);
            var ar = new AddressRepository(context);
            var addr = await SeedAddressAsync(ar, new AddressDto
            {
                City = "Hà Nội",
                District = "Hoàn Kiếm",
                Ward = "Hàng Bài",
                Detail = "Số 1 phố Hàng Bài",
                Lat = 21.0280,
                Lon = 105.8500
            });
            await ur.CreateAsync(new User
            {
                Email = "user14@test.vn",
                Role = "customer",
                Status = 1,
                Password = "p",
                Name = "U14",
                AddressId = addr.Id
            });
            var id = (await context.Users.FirstAsync()).Id;
            var svc = CreateUserService(context);

            // Gửi chỉ Address.District — merge partial
            var result = await svc.UpdateUserAsync(id,
                new UserUpdateDTO
                {
                    Address = new AddressDto { District = "Ba Đình" }
                });

            // District đổi, Ward giữ nguyên (merge)
            Assert.True(result.Success);
            var a = await context.Addresses.AsNoTracking().FirstAsync();
            Assert.Equal("Ba Đình", a.District);
            Assert.Equal("Hàng Bài", a.Ward);
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-USV-UU-006
    /// Đổi Lat/Lon địa chỉ.
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_LatLon_TC_USV_UU_006()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await context.Database.BeginTransactionAsync();
        try
        {
            var ur = new UserRepository(context);
            var ar = new AddressRepository(context);
            var addr = await SeedAddressAsync(ar, SampleAddress());
            await ur.CreateAsync(new User
            {
                Email = "user15@test.vn",
                Role = "customer",
                Status = 1,
                Password = "p",
                Name = "U15",
                AddressId = addr.Id
            });
            var id = (await context.Users.FirstAsync()).Id;
            var svc = CreateUserService(context);

            // Gửi tọa độ Lon/Lat mới
            var result = await svc.UpdateUserAsync(id,
                new UserUpdateDTO { Address = new AddressDto { Lon = 105.85, Lat = 21.03 } });

            // Lon/Lat trong bảng addresses đổi đúng
            Assert.True(result.Success);
            var a = await context.Addresses.AsNoTracking().FirstAsync();
            Assert.Equal(105.85, a.Lon);
            Assert.Equal(21.03, a.Lat);
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-USV-UU-007
    /// User chưa có địa chỉ — tạo Address mới.
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_CreateAddress_TC_USV_UU_007()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await context.Database.BeginTransactionAsync();
        try
        {
            await new UserRepository(context).CreateAsync(new User
            {
                Email = "user16@test.vn",
                Role = "customer",
                Status = 1,
                Password = "p",
                Name = "U16",
                AddressId = null
            });
            var id = (await context.Users.FirstAsync()).Id;
            var svc = CreateUserService(context);

            // User chưa có địa chỉ — gửi Address đầy đủ
            var result = await svc.UpdateUserAsync(id,
                new UserUpdateDTO
                {
                    Address = new AddressDto
                    {
                        City = "TP.HCM",
                        District = "Quận 1",
                        Ward = "Bến Nghé",
                        Detail = "Số 45 Nguyễn Huệ",
                        Lon = 106.7044,
                        Lat = 10.7769
                    }
                });

            // AddressId được gán — Address mới đã được tạo và liên kết
            Assert.True(result.Success);
            var u = await context.Users.AsNoTracking().FirstAsync(x => x.Id == id);
            Assert.NotNull(u.AddressId);
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-USV-UU-008
    /// User không tồn tại.
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_UserNotFound_TC_USV_UU_008()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var svc = CreateUserService(context);

        // Cập nhật user không tồn tại
        var result = await svc.UpdateUserAsync(777,
            new UserUpdateDTO { Name = "Tên Giả", PhoneNumber = "0999888777" });

        // Từ chối với message not found
        Assert.False(result.Success);
        Assert.Equal("User not found", result.Message);
    }

    /// <summary>
    /// TC-USV-UU-009 — mock UpdateAsync null.
    /// </summary>
    [Fact(Skip = "TC-USV-UU-009: cần mock UserRepository.UpdateAsync — không chỉnh mã nguồn.")]
    public Task UpdateUserAsync_UpdateReturnsNull_TC_USV_UU_009() => Task.CompletedTask;

    /// <summary>
    /// TC-USV-DU-001
    /// Xóa user tồn tại.
    /// </summary>
    [Fact]
    public async Task DeleteUserAsync_Exists_TC_USV_DU_001()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await context.Database.BeginTransactionAsync();
        try
        {
            await new UserRepository(context).CreateAsync(new User
            {
                Email = "canxoa@test.vn",
                Name = "Người Bị Xóa",
                Role = "customer",
                Status = 1,
                Password = "p"
            });
            var id = (await context.Users.FirstAsync()).Id;
            var svc = CreateUserService(context);

            // Xóa user tồn tại
            var result = await svc.DeleteUserAsync(id);

            // Success, DB không còn user nào
            // SPEC: cũng cần kiểm tra Message = "User deleted successfully"
            Assert.True(result.Success);
            Assert.Equal("User deleted successfully", result.Message);
            Assert.Equal(0, await context.Users.CountAsync());
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-USV-DU-002
    /// Id không tồn tại.
    /// </summary>
    [Fact]
    public async Task DeleteUserAsync_NotFound_TC_USV_DU_002()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var svc = CreateUserService(context);

        // Xóa user không tồn tại
        var result = await svc.DeleteUserAsync(888);

        // Từ chối với message not found
        Assert.False(result.Success);
        Assert.Equal("User not found", result.Message);
    }

    /// <summary>
    /// TC-USV-DU-003 — mock Delete false.
    /// </summary>
    [Fact(Skip = "TC-USV-DU-003: cần mock UserRepository.DeleteAsync — không chỉnh mã nguồn.")]
    public Task DeleteUserAsync_DeleteReturnsFalse_TC_USV_DU_003() => Task.CompletedTask;
}
