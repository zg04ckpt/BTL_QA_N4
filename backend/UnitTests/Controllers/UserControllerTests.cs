using BusinessLogicLayer.Interfaces;
using DataAccessLayer.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebAPI.Controllers;
using Xunit;

namespace UnitTests.Controllers;

public class UserControllerTests
{
    private static UserController CreateController(IUserService svc)
    {
        var c = new UserController(svc)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return c;
    }

    /// <summary>
    /// TC-UC-REG-001 — ModelState không hợp lệ => 400 ModelState.
    /// </summary>
    [Fact]
    public async Task Register_InvalidModelState_TC_UC_REG_001()
    {
        var mock = new Mock<IUserService>();
        var controller = CreateController(mock.Object);
        controller.ModelState.AddModelError("Email", "Email không hợp lệ");

        // ModelState lỗi trước khi gọi service
        var badDto = new UserDTO
        {
            Email = "khong-phai-email",
            Password = "MatKhau123!",
            Role = "customer",
            Address = new AddressDto { City = "HN", Detail = "d", Ward = "w", District = "q", Lat = 1, Lon = 2 }
        };
        var result = await controller.Register(badDto);

        // 400 và service không được gọi
        Assert.IsType<BadRequestObjectResult>(result);
        mock.Verify(s => s.RegisterUserAsync(It.IsAny<UserDTO>()), Times.Never);
    }

    /// <summary>
    /// TC-UC-REG-002 — đăng ký thành công => 200 + body có trường Message.
    /// </summary>
    [Fact]
    public async Task Register_Success_TC_UC_REG_002()
    {
        var mock = new Mock<IUserService>();
        mock.Setup(s => s.RegisterUserAsync(It.IsAny<UserDTO>()))
            .ReturnsAsync((true, "User registered successfully"));
        var controller = CreateController(mock.Object);

        // Đăng ký với email chưa tồn tại, DTO đầy đủ
        var dto = new UserDTO
        {
            Email = "dangky@ok.vn",
            Password = "MatKhau123!",
            Name = "Đăng Ký OK",
            PhoneNumber = "0909123456",
            Role = "customer",
            Address = new AddressDto
            {
                City = "Hà Nội",
                District = "Cầu Giấy",
                Ward = "Dịch Vọng",
                Detail = "Ngõ 12",
                Lat = 21.02,
                Lon = 105.80
            }
        };
        var result = await controller.Register(dto);

        // 200 + body có trường Message = "User registered successfully"
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var message = ok.Value!.GetType().GetProperty("Message")?.GetValue(ok.Value) as string;
        Assert.Equal("User registered successfully", message);
    }

    /// <summary>
    /// TC-UC-REG-003 — email trùng => 400.
    /// </summary>
    [Fact]
    public async Task Register_EmailExists_TC_UC_REG_003()
    {
        var mock = new Mock<IUserService>();
        mock.Setup(s => s.RegisterUserAsync(It.IsAny<UserDTO>()))
            .ReturnsAsync((false, "Email already exists"));
        var controller = CreateController(mock.Object);

        var result = await controller.Register(new UserDTO
        {
            Email = "trung@mail.vn",
            Password = "MatKhau123!",
            Name = "Người Mới",
            PhoneNumber = "0911222333",
            Role = "customer",
            Address = new AddressDto { City = "HN", District = "d", Ward = "w", Detail = "x", Lat = 1, Lon = 2 }
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// TC-UC-REG-004 — đặc tả kỳ vọng 400 khi trùng số điện thoại.
    /// GAP-SPEC: UserService.RegisterUserAsync không kiểm tra trùng PhoneNumber → service trả (true, success).
    /// Test mock hành vi thực của service (trả true) rồi assert kỳ vọng spec (400).
    /// Test sẽ FAIL vì controller trả 200 thay vì 400 — phản ánh đúng gap giữa đặc tả và code.
    /// </summary>
    [Fact]
    public async Task Register_DuplicatePhoneBaseline_TC_UC_REG_004()
    {
        var mock = new Mock<IUserService>();
        // Mock phản ánh hành vi thực của UserService: không chặn SĐT trùng → trả success.
        mock.Setup(s => s.RegisterUserAsync(It.IsAny<UserDTO>()))
            .ReturnsAsync((true, "User registered successfully"));
        var controller = CreateController(mock.Object);

        var result = await controller.Register(new UserDTO
        {
            Email = "user_b@mail.vn",
            PhoneNumber = "0911222333",
            Password = "MatKhau123!",
            Name = "Mới",
            Role = "customer",
            Address = new AddressDto { City = "HN", District = "d", Ward = "w", Detail = "x", Lat = 1, Lon = 2 }
        });

        // Spec kỳ vọng 400 khi SĐT trùng — nhưng code trả 200 → test FAIL = bug được expose.
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// TC-UC-LOGIN-001 — loginDto null => 400 Invalid login data.
    /// </summary>
    [Fact]
    public async Task Login_NullBody_TC_UC_LOGIN_001()
    {
        var mock = new Mock<IUserService>();
        var controller = CreateController(mock.Object);

        var result = await controller.Login(null!);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        mock.Verify(s => s.LoginAsync(It.IsAny<LoginDTO>()), Times.Never);
    }

    /// <summary>
    /// TC-UC-LOGIN-002 — đăng nhập thành công => 200 + body có trường Token không rỗng.
    /// </summary>
    [Fact]
    public async Task Login_Success_TC_UC_LOGIN_002()
    {
        var mock = new Mock<IUserService>();
        mock.Setup(s => s.LoginAsync(It.IsAny<LoginDTO>()))
            .ReturnsAsync(new LoginResult { Message = "Login successfully", Token = "a.b.c" });
        var controller = CreateController(mock.Object);

        var result = await controller.Login(new LoginDTO { Email = "nguoidung@test.vn", Password = "matkhau123" });

        // Controller trả Ok(new { Token }) khi Message == "Login successfully"
        // Token phải không rỗng và khớp giá trị mock
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var token = ok.Value!.GetType().GetProperty("Token")?.GetValue(ok.Value) as string;
        Assert.False(string.IsNullOrWhiteSpace(token), "Body phải có trường Token không rỗng.");
        Assert.Equal("a.b.c", token);
    }

    /// <summary>
    /// TC-UC-LOGIN-003 — sai mật khẩu => 401.
    /// </summary>
    [Fact]
    public async Task Login_WrongPassword_TC_UC_LOGIN_003()
    {
        var mock = new Mock<IUserService>();
        mock.Setup(s => s.LoginAsync(It.IsAny<LoginDTO>()))
            .ReturnsAsync(new LoginResult { Message = "Email or Password is incorrect", Token = null! });
        var controller = CreateController(mock.Object);

        var result = await controller.Login(new LoginDTO { Email = "nguoidung@test.vn", Password = "saimatkhau" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    /// <summary>
    /// TC-UC-LOGIN-004 — tài khoản khóa => 401.
    /// </summary>
    [Fact]
    public async Task Login_LockedAccount_TC_UC_LOGIN_004()
    {
        var mock = new Mock<IUserService>();
        mock.Setup(s => s.LoginAsync(It.IsAny<LoginDTO>()))
            .ReturnsAsync(new LoginResult
            {
                Message = "Account is locked. Please contact the administrator.",
                Token = null!
            });
        var controller = CreateController(mock.Object);

        var result = await controller.Login(new LoginDTO { Email = "bikhoa@example.vn", Password = "dungmatkhau" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    /// <summary>
    /// TC-UC-GAU-001 — GET danh sách => 200 array.
    /// </summary>
    [Fact]
    public async Task GetAllUsers_Ok_TC_UC_GAU_001()
    {
        var mock = new Mock<IUserService>();
        mock.Setup(s => s.GetAllUserSummariesForAdminAsync())
            .ReturnsAsync(new List<AdminUserSummaryDto>());
        var controller = CreateController(mock.Object);

        var result = await controller.GetAllUsers();

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// TC-UC-GUI-001 — user tồn tại => 200 UserDTO.
    /// </summary>
    [Fact]
    public async Task GetUserById_Found_TC_UC_GUI_001()
    {
        var mock = new Mock<IUserService>();
        mock.Setup(s => s.GetUserByIdAsync(42))
            .ReturnsAsync(new UserDTO
            {
                Id = 42,
                Email = "user42@test.vn",
                Name = "Người 42",
                PhoneNumber = "0909424242",
                Role = "customer",
                Status = 1
            });
        var controller = CreateController(mock.Object);

        var result = await controller.GetUserById(42);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// TC-UC-GUI-002 — không tồn tại => 204.
    /// </summary>
    [Fact]
    public async Task GetUserById_NotFound_TC_UC_GUI_002()
    {
        var mock = new Mock<IUserService>();
        mock.Setup(s => s.GetUserByIdAsync(99999)).ReturnsAsync((UserDTO?)null);
        var controller = CreateController(mock.Object);

        var result = await controller.GetUserById(99999);

        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>
    /// TC-UC-UU-001 — ModelState invalid => 400.
    /// </summary>
    [Fact]
    public async Task UpdateUser_InvalidModelState_TC_UC_UU_001()
    {
        var mock = new Mock<IUserService>();
        var controller = CreateController(mock.Object);
        controller.ModelState.AddModelError("Name", "Error");

        var result = await controller.UpdateUser(10, new UserUpdateDTO { Name = "x" });

        Assert.IsType<BadRequestObjectResult>(result);
        mock.Verify(s => s.UpdateUserAsync(It.IsAny<int>(), It.IsAny<UserUpdateDTO>()), Times.Never);
    }

    /// <summary>
    /// TC-UC-UU-002 — cập nhật thành công => 200.
    /// </summary>
    [Fact]
    public async Task UpdateUser_Success_TC_UC_UU_002()
    {
        var mock = new Mock<IUserService>();
        mock.Setup(s => s.UpdateUserAsync(20, It.IsAny<UserUpdateDTO>()))
            .ReturnsAsync((true, "User updated successfully"));
        var controller = CreateController(mock.Object);

        var result = await controller.UpdateUser(20, new UserUpdateDTO { Name = "Tên Sau Cập Nhật" });

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// TC-UC-UU-003 — user không tồn tại => 400.
    /// </summary>
    [Fact]
    public async Task UpdateUser_NotFound_TC_UC_UU_003()
    {
        var mock = new Mock<IUserService>();
        mock.Setup(s => s.UpdateUserAsync(777, It.IsAny<UserUpdateDTO>()))
            .ReturnsAsync((false, "User not found"));
        var controller = CreateController(mock.Object);

        var result = await controller.UpdateUser(777, new UserUpdateDTO { Name = "X" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// TC-UC-UU-004 — baseline đặc tả trùng email (mock service fail).
    /// </summary>
    [Fact]
    public async Task UpdateUser_DuplicateEmailBaseline_TC_UC_UU_004()
    {
        var mock = new Mock<IUserService>();
        mock.Setup(s => s.UpdateUserAsync(31, It.IsAny<UserUpdateDTO>()))
            .ReturnsAsync((false, "Email already exists"));
        var controller = CreateController(mock.Object);

        var result = await controller.UpdateUser(31, new UserUpdateDTO { Name = "Y" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// TC-UC-UU-005 — baseline trùng SĐT.
    /// </summary>
    [Fact]
    public async Task UpdateUser_DuplicatePhoneBaseline_TC_UC_UU_005()
    {
        var mock = new Mock<IUserService>();
        mock.Setup(s => s.UpdateUserAsync(40, It.IsAny<UserUpdateDTO>()))
            .ReturnsAsync((false, "Phone already exists"));
        var controller = CreateController(mock.Object);

        var result = await controller.UpdateUser(40, new UserUpdateDTO { PhoneNumber = "0911000222" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// TC-UC-DU-001 — xóa thành công => 200.
    /// </summary>
    [Fact]
    public async Task DeleteUser_Success_TC_UC_DU_001()
    {
        var mock = new Mock<IUserService>();
        mock.Setup(s => s.DeleteUserAsync(55)).ReturnsAsync((true, "User deleted successfully"));
        var controller = CreateController(mock.Object);

        var result = await controller.DeleteUser(55);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// TC-UC-DU-002 — không tìm thấy => 404.
    /// </summary>
    [Fact]
    public async Task DeleteUser_NotFound_TC_UC_DU_002()
    {
        var mock = new Mock<IUserService>();
        mock.Setup(s => s.DeleteUserAsync(66666)).ReturnsAsync((false, "User not found"));
        var controller = CreateController(mock.Object);

        var result = await controller.DeleteUser(66666);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
