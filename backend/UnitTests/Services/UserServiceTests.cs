using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UnitTests.Infrastructure;
using Xunit;

namespace UnitTests.Services;

public class UserServiceTests
{
    /// <summary>
    /// TC-USR-003
    /// Hàm test cho trường hợp người dùng đăng nhập thành công
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task LoginAsync_LoginSuccess()
    {
        
        // Bước 1: Khởi tạo SQLite in-memory và tạo context để seed dữ liệu test.
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        // Bước 2: Chuẩn bị repository/service thật và cấu hình JWT tối thiểu để tạo token.
        var userRepository = new UserRepository(context);
        var addressRepository = new AddressRepository(context);
        var addressService = new AddressService(addressRepository);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "KhoaBiMatChoJwtPhaiDuDaiDeKyToken123456789",
                ["Jwt:Subject"] = "NguoiDungHeThong",
                ["Jwt:Issuer"] = "DoAnBackend",
                ["Jwt:Audience"] = "DoAnClient"
            })
            .Build();

        var userService = new UserService(userRepository, addressService, configuration);

        // Bước 3: Mở transaction và dùng try-catch để luôn rollback dữ liệu test.
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Bước 4: Seed người dùng bằng dữ liệu tiếng Việt bên trong transaction.
            await userRepository.CreateAsync(new User
            {
                Email = "nguoidung@test.vn",
                Password = "matkhau123",
                Role = "customer",
                Name = "Nguyen Van B",
                PhoneNumber = "0909123456",
                Status = 1
            });

            // Bước 5: Lưu trạng thái DB trước khi login để kiểm tra Login không làm thay đổi dữ liệu.
            var userCountBeforeLogin = await context.Users.CountAsync();

            // Bước 6: Gọi hàm LoginAsync với email/mật khẩu hợp lệ.
            var loginDto = new LoginDTO
            {
                Email = "nguoidung@test.vn",
                Password = "matkhau123"
            };
            var result = await userService.LoginAsync(loginDto);

            // Bước 7: Xác nhận đăng nhập thành công và có token trả về.
            Assert.Equal("Login successfully", result.Message);
            Assert.False(string.IsNullOrWhiteSpace(result.Token));

            // Bước 8: Kiểm tra DB không thay đổi sau khi login (vì login chỉ đọc dữ liệu trong cùng transaction).
            var userCountAfterLogin = await context.Users.CountAsync();
            Assert.Equal(userCountBeforeLogin, userCountAfterLogin);
            Assert.False(context.ChangeTracker.HasChanges());
        }
        catch
        {
            // Nếu test lỗi ở bất kỳ bước nào thì ném lại để xUnit báo fail.
            throw;
        }
        finally
        {
            // Bước 9: Rollback để bảo đảm dữ liệu test luôn được hoàn tác.
            await transaction.RollbackAsync();
        }

        // Bước 10: Mở context mới để xác nhận user test đã được rollback và không còn tồn tại.
        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var rolledBackUser = await verifyContext.Users.FirstOrDefaultAsync(u => u.Email == "nguoidung@test.vn");
        Assert.Null(rolledBackUser);
    }
}
