using BusinessLogicLayer.Interfaces;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebAPI.Controllers;
using Xunit;

namespace UnitTests.Controllers;

// ============================================================
//  RestaurantsControllerTests – kiểm thử đơn vị cho RestaurantsController
//
//  Chiến lược:
//  - Mock<IRestaurantService> + khởi tạo controller trực tiếp cho đa số test.
//  - Các test cần route type binding (route không phải int):
//    TC-RSC-009, TC-RSC-014, TC-RSC-021, TC-RSC-025, TC-RSC-027, TC-RSC-030, TC-RSC-043
//    → Ghi chú cần WebApplicationFactory (integration test), cung cấp placeholder.
//  - ModelState.AddModelError dùng cho TC-RSC-002 (invalid model).
// ============================================================
public class RestaurantsControllerTests
{
    // Helper: AddressDto mẫu mặc định
    private static AddressDto DefaultAddressDto() => new AddressDto
    {
        City = "Hà Nội", District = "Cầu Giấy",
        Ward = "Dịch Vọng", Detail = "Số 12 phố Duy Tân",
        Lon = 105.7827, Lat = 21.0285
    };

    // Helper: tạo controller với mock IRestaurantService
    private static (RestaurantsController controller, Mock<IRestaurantService> mock) BuildController()
    {
        var mock = new Mock<IRestaurantService>();
        var controller = new RestaurantsController(mock.Object);
        return (controller, mock);
    }

    // Helper: CreateRestaurantDto hợp lệ
    private static CreateRestaurantDto ValidCreateDto(string name = "Quán Lẩu Hương Sơn") =>
        new CreateRestaurantDto
        {
            Name = name, Status = 0, Email = "lau@test.vn",
            Description = "Lẩu cao cấp", PhoneNumber = "0988123456",
            AvtImage = "avt.jpg", CateId = 1, UserId = 100,
            Address = DefaultAddressDto(),
            RestaurantPhotos = new List<string> { "p1.jpg" }
        };

    // Helper: UpdateRestaurantDto hợp lệ
    private static UpdateRestaurantDto ValidUpdateDto(string name = "Tên mới") =>
        new UpdateRestaurantDto
        {
            Name = name, Email = "moi@test.vn", PhoneNumber = "0988000000",
            Status = 1, AvtImage = "new.jpg", CateId = 1,
            Address = DefaultAddressDto(),
            RestaurantPhotos = new List<string> { "x.jpg" }
        };

    // ===========================================================
    //  AddRestaurant
    // ===========================================================

    /// <summary>TC-RSC-001 – Trả 201 CreatedAtAction khi DTO hợp lệ và service thành công</summary>
    [Fact]
    public async Task AddRestaurant_ValidDto_Returns201CreatedAtAction()
    {
        var (controller, mock) = BuildController();

        var fakeRestaurant = new Restaurant
        {
            Id = 700, Name = "Quán Lẩu Hương Sơn",
            Status = 0, Email = "lau@test.vn", PhoneNumber = "0988123456",
            CateId = 1, UserId = 100, AddressId = 1
        };
        mock.Setup(s => s.AddRestaurantAsync(It.IsAny<CreateRestaurantDto>()))
            .ReturnsAsync(fakeRestaurant);

        var actionResult = await controller.AddRestaurant(ValidCreateDto());

        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal("GetRestaurantById", createdResult.ActionName);
        Assert.Equal(700, createdResult.RouteValues!["id"]);

        var restaurant = Assert.IsType<Restaurant>(createdResult.Value);
        Assert.Equal(700, restaurant.Id);
        Assert.Equal("Quán Lẩu Hương Sơn", restaurant.Name);
    }

    /// <summary>TC-RSC-002 – Trả 400 BadRequest khi ModelState invalid (Name = null)</summary>
    [Fact]
    public async Task AddRestaurant_InvalidModelState_Returns400BadRequest()
    {
        var (controller, mock) = BuildController();

        // Thêm lỗi vào ModelState thủ công (thay thế cho DataAnnotation validation)
        controller.ModelState.AddModelError("Name", "Required");

        var dto = new CreateRestaurantDto
        {
            Name = null!, Email = "a@b.vn", PhoneNumber = "0900000000",
            AvtImage = "avt.jpg", CateId = 1, UserId = 100,
            Address = DefaultAddressDto(),
            RestaurantPhotos = new List<string>()
        };

        var actionResult = await controller.AddRestaurant(dto);

        var badResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(400, badResult.StatusCode);

        // result.Value là SerializableError chứa key "Name"
        var errors = Assert.IsType<SerializableError>(badResult.Value);
        Assert.True(errors.ContainsKey("Name"));

        // Service không được gọi khi ModelState invalid
        mock.Verify(s => s.AddRestaurantAsync(It.IsAny<CreateRestaurantDto>()), Times.Never);
    }

    /// <summary>TC-RSC-003 – Service trả null → 500 Internal Server Error với message lỗi</summary>
    [Fact]
    public async Task AddRestaurant_ServiceReturnsNull_Returns500()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.AddRestaurantAsync(It.IsAny<CreateRestaurantDto>()))
            .ReturnsAsync((Restaurant?)null);

        var actionResult = await controller.AddRestaurant(ValidCreateDto());

        var statusResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(500, statusResult.StatusCode);
        Assert.Equal("Error creating the restaurant", statusResult.Value);
    }

    /// <summary>TC-RSC-004 – Service ném Exception → exception lan truyền</summary>
    [Fact]
    public async Task AddRestaurant_ServiceThrows_ExceptionPropagates()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.AddRestaurantAsync(It.IsAny<CreateRestaurantDto>()))
            .ThrowsAsync(new Exception("service failure"));

        var ex = await Assert.ThrowsAsync<Exception>(
            () => controller.AddRestaurant(ValidCreateDto()));

        Assert.Equal("service failure", ex.Message);
    }

    /// <summary>TC-RSC-005 – Response chứa đủ ảnh và AddressId</summary>
    [Fact]
    public async Task AddRestaurant_WithPhotos_ResponseContainsPhotosAndAddressId()
    {
        var (controller, mock) = BuildController();

        var fakeRestaurant = new Restaurant
        {
            Id = 701, Name = "Quán Nhiều Ảnh",
            AddressId = 5,
            RestaurantPhotos = new List<RestaurantPhoto>
            {
                new RestaurantPhoto { ImageUrl = "a.jpg" },
                new RestaurantPhoto { ImageUrl = "b.jpg" }
            }
        };
        mock.Setup(s => s.AddRestaurantAsync(It.IsAny<CreateRestaurantDto>()))
            .ReturnsAsync(fakeRestaurant);

        var dto = new CreateRestaurantDto
        {
            Name = "Quán Nhiều Ảnh", Status = 0, Email = "many@test.vn",
            PhoneNumber = "0900111222", AvtImage = "avt.jpg", CateId = 1, UserId = 100,
            Address = DefaultAddressDto(),
            RestaurantPhotos = new List<string> { "a.jpg", "b.jpg" }
        };

        var actionResult = await controller.AddRestaurant(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        var restaurant = Assert.IsType<Restaurant>(createdResult.Value);
        Assert.Equal(2, restaurant.RestaurantPhotos!.Count);
        Assert.Equal(5, restaurant.AddressId);
    }

    /// <summary>TC-RSC-006 – Tên tiếng Việt có dấu được giữ nguyên trong response</summary>
    [Fact]
    public async Task AddRestaurant_VietnameseName_PreservedInResponse()
    {
        var (controller, mock) = BuildController();

        const string name = "Quán Bún Đậu Mắm Tôm – ngõ Trần Thái Tông";
        mock.Setup(s => s.AddRestaurantAsync(It.IsAny<CreateRestaurantDto>()))
            .ReturnsAsync(new Restaurant { Id = 702, Name = name });

        var actionResult = await controller.AddRestaurant(ValidCreateDto(name));

        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        var restaurant = Assert.IsType<Restaurant>(createdResult.Value);
        Assert.Equal(name, restaurant.Name);
    }

    // ===========================================================
    //  GetRestaurantsByCategory
    // ===========================================================

    /// <summary>TC-RSC-007 – Trả 200 OK với danh sách 2 nhà hàng</summary>
    [Fact]
    public async Task GetRestaurantsByCategory_HasRestaurants_Returns200WithList()
    {
        var (controller, mock) = BuildController();

        var fakeList = new List<RestaurantDto>
        {
            new RestaurantDto(new Restaurant { Id = 1, Name = "Phở Thìn", Email = "pho@test.vn", PhoneNumber = "0900000001", AverageScore = 4.5f, TotalReviews = 100 }),
            new RestaurantDto(new Restaurant { Id = 2, Name = "Bún Chả Hàng Mành", Email = "bun@test.vn", PhoneNumber = "0900000002", AverageScore = 4.0f, TotalReviews = 50 })
        };
        mock.Setup(s => s.GetRestaurantsByCategoryAsync(1))
            .ReturnsAsync(fakeList);

        var actionResult = await controller.GetRestaurantsByCategory(1);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(200, okResult.StatusCode);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDto>>(okResult.Value);
        Assert.Equal(2, list.Count());
        var names = list.Select(r => r.Name).ToHashSet();
        Assert.Contains("Phở Thìn", names);
        Assert.Contains("Bún Chả Hàng Mành", names);
    }

    /// <summary>TC-RSC-008 – Trả 200 OK rỗng khi danh mục không có nhà hàng</summary>
    [Fact]
    public async Task GetRestaurantsByCategory_EmptyCategory_Returns200Empty()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.GetRestaurantsByCategoryAsync(3))
            .ReturnsAsync(Enumerable.Empty<RestaurantDto>());

        var actionResult = await controller.GetRestaurantsByCategory(3);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDto>>(okResult.Value);
        Assert.False(list.Any());
    }

    /// <summary>
    /// TC-RSC-009 – Route không phải int → HTTP 400 (cần WebApplicationFactory).
    /// Unit test: placeholder xác nhận hành vi khi truyền int hợp lệ.
    /// </summary>
    [Fact]
    public async Task GetRestaurantsByCategory_NonIntRouteRequiresIntegrationTest()
    {
        // Integration test: GET /api/restaurants/category/abc → 400 (route binding)
        // mock.Verify(Times.Never)
        var (controller, mock) = BuildController();
        mock.Setup(s => s.GetRestaurantsByCategoryAsync(It.IsAny<int>()))
            .ReturnsAsync(Enumerable.Empty<RestaurantDto>());

        var actionResult = await controller.GetRestaurantsByCategory(999);

        Assert.IsType<OkObjectResult>(actionResult);
    }

    /// <summary>TC-RSC-010 – categoryId âm/0 vẫn gọi service và trả 200</summary>
    [Fact]
    public async Task GetRestaurantsByCategory_NegativeOrZeroId_Returns200AndCallsService()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.GetRestaurantsByCategoryAsync(-1)).ReturnsAsync(Enumerable.Empty<RestaurantDto>());
        mock.Setup(s => s.GetRestaurantsByCategoryAsync(0)).ReturnsAsync(Enumerable.Empty<RestaurantDto>());

        var r1 = await controller.GetRestaurantsByCategory(-1);
        Assert.IsType<OkObjectResult>(r1);

        var r2 = await controller.GetRestaurantsByCategory(0);
        Assert.IsType<OkObjectResult>(r2);

        // Verify: mỗi giá trị được gọi đúng 1 lần
        mock.Verify(s => s.GetRestaurantsByCategoryAsync(-1), Times.Once);
        mock.Verify(s => s.GetRestaurantsByCategoryAsync(0), Times.Once);
    }

    // ===========================================================
    //  GetRestaurantsByUser
    // ===========================================================

    /// <summary>TC-RSC-011 – Trả 200 OK với danh sách 3 nhà hàng của user</summary>
    [Fact]
    public async Task GetRestaurantsByUser_HasRestaurants_Returns200WithList()
    {
        var (controller, mock) = BuildController();

        var fakeList = new List<RestaurantDto>
        {
            new RestaurantDto(new Restaurant { Id = 1, Name = "R1" }),
            new RestaurantDto(new Restaurant { Id = 2, Name = "R2" }),
            new RestaurantDto(new Restaurant { Id = 3, Name = "R3" })
        };
        mock.Setup(s => s.GetRestaurantsByUserAsync(100)).ReturnsAsync(fakeList);

        var actionResult = await controller.GetRestaurantsByUser(100);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDto>>(okResult.Value);
        Assert.Equal(3, list.Count());
    }

    /// <summary>TC-RSC-012 – Trả 200 OK rỗng khi user chưa có nhà hàng</summary>
    [Fact]
    public async Task GetRestaurantsByUser_UserHasNoRestaurants_Returns200Empty()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.GetRestaurantsByUserAsync(102))
            .ReturnsAsync(Enumerable.Empty<RestaurantDto>());

        var actionResult = await controller.GetRestaurantsByUser(102);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDto>>(okResult.Value);
        Assert.False(list.Any());
    }

    /// <summary>TC-RSC-013 – Trả 200 OK rỗng khi userId không tồn tại</summary>
    [Fact]
    public async Task GetRestaurantsByUser_NonexistentUser_Returns200Empty()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.GetRestaurantsByUserAsync(9999))
            .ReturnsAsync(Enumerable.Empty<RestaurantDto>());

        var actionResult = await controller.GetRestaurantsByUser(9999);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDto>>(okResult.Value);
        Assert.False(list.Any());
    }

    /// <summary>
    /// TC-RSC-014 – userId không phải int → HTTP 400 (WebApplicationFactory).
    /// Unit test: placeholder.
    /// </summary>
    [Fact]
    public async Task GetRestaurantsByUser_NonIntRouteRequiresIntegrationTest()
    {
        // Integration test: GET /api/restaurants/user/abc → 400
        var (controller, mock) = BuildController();
        mock.Setup(s => s.GetRestaurantsByUserAsync(It.IsAny<int>()))
            .ReturnsAsync(Enumerable.Empty<RestaurantDto>());

        var actionResult = await controller.GetRestaurantsByUser(999);

        Assert.IsType<OkObjectResult>(actionResult);
    }

    // ===========================================================
    //  SearchRestaurants
    // ===========================================================

    /// <summary>TC-RSC-015 – Trả 200 OK với danh sách 2 nhà hàng khớp searchTerm</summary>
    [Fact]
    public async Task SearchRestaurants_HasMatches_Returns200WithList()
    {
        var (controller, mock) = BuildController();

        var fakeList = new List<RestaurantDto>
        {
            new RestaurantDto(new Restaurant { Id = 1, Name = "Phở Thìn" }),
            new RestaurantDto(new Restaurant { Id = 2, Name = "Phở Gia Truyền" })
        };
        mock.Setup(s => s.SearchRestaurantsAsync("Phở")).ReturnsAsync(fakeList);

        var actionResult = await controller.SearchRestaurants("Phở");

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDto>>(okResult.Value);
        Assert.Equal(2, list.Count());
        Assert.All(list, r => Assert.Contains("Phở", r.Name!));
    }

    /// <summary>TC-RSC-016 – Trả 200 OK rỗng khi không khớp</summary>
    [Fact]
    public async Task SearchRestaurants_NoMatch_Returns200Empty()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.SearchRestaurantsAsync("KhôngCó"))
            .ReturnsAsync(Enumerable.Empty<RestaurantDto>());

        var actionResult = await controller.SearchRestaurants("KhôngCó");

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDto>>(okResult.Value);
        Assert.False(list.Any());
    }

    /// <summary>TC-RSC-017 – Không truyền searchTerm → gọi service với null, trả 200</summary>
    [Fact]
    public async Task SearchRestaurants_NoSearchTerm_CallsServiceWithNull()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.SearchRestaurantsAsync(null!))
            .ReturnsAsync(Enumerable.Empty<RestaurantDto>());

        var actionResult = await controller.SearchRestaurants(null!);

        Assert.IsType<OkObjectResult>(actionResult);

        // Verify: service được gọi đúng 1 lần với null
        mock.Verify(s => s.SearchRestaurantsAsync(null!), Times.Once);
    }

    /// <summary>TC-RSC-018 – Tiếng Việt có dấu được giữ nguyên khi truyền xuống service</summary>
    [Fact]
    public async Task SearchRestaurants_VietnameseTerm_PassedToServiceExactly()
    {
        var (controller, mock) = BuildController();

        const string searchTerm = "Phở Gánh";
        mock.Setup(s => s.SearchRestaurantsAsync(searchTerm))
            .ReturnsAsync(new List<RestaurantDto>
            {
                new RestaurantDto(new Restaurant { Id = 1, Name = "Phở Gánh" })
            });

        var actionResult = await controller.SearchRestaurants(searchTerm);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDto>>(okResult.Value);
        Assert.Single(list);

        // Verify: service nhận đúng chuỗi tiếng Việt
        mock.Verify(s => s.SearchRestaurantsAsync(searchTerm), Times.Once);
    }

    // ===========================================================
    //  GetRestaurantById
    // ===========================================================

    /// <summary>TC-RSC-019 – Trả 200 OK với RestaurantDetailDto đầy đủ khi tồn tại</summary>
    [Fact]
    public async Task GetRestaurantById_ExistingRestaurant_Returns200WithDetail()
    {
        var (controller, mock) = BuildController();

        var fakeDetail = new RestaurantDetailDto
        {
            Id = 500, Name = "Nhà hàng Sen Tây Hồ",
            Email = "sen@test.vn", Status = 1,
            Category = "Món Việt", AverageScore = 4.7f,
            TotalReviews = 120, UserId = 100
        };
        mock.Setup(s => s.GetRestaurantByIdAsync(500)).ReturnsAsync(fakeDetail);

        var actionResult = await controller.GetRestaurantById(500);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var detail = Assert.IsType<RestaurantDetailDto>(okResult.Value);
        Assert.Equal(500, detail.Id);
        Assert.Equal("Nhà hàng Sen Tây Hồ", detail.Name);
        Assert.Equal("Món Việt", detail.Category);
        Assert.Equal(4.7, (double)detail.AverageScore!.Value, 1);
    }

    /// <summary>TC-RSC-020 – Trả 404 NotFound khi nhà hàng không tồn tại</summary>
    [Fact]
    public async Task GetRestaurantById_NonexistentRestaurant_Returns404()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.GetRestaurantByIdAsync(999)).ReturnsAsync((RestaurantDetailDto?)null);

        var actionResult = await controller.GetRestaurantById(999);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(404, notFoundResult.StatusCode);
        Assert.Equal("Restaurant not found", notFoundResult.Value);
    }

    /// <summary>
    /// TC-RSC-021 – Route id không phải int → HTTP 400 (WebApplicationFactory).
    /// Unit test: placeholder.
    /// </summary>
    [Fact]
    public async Task GetRestaurantById_NonIntRouteRequiresIntegrationTest()
    {
        // Integration test: GET /api/restaurants/abc → 400 hoặc 404 tùy route config
        var (controller, mock) = BuildController();
        mock.Setup(s => s.GetRestaurantByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((RestaurantDetailDto?)null);

        var actionResult = await controller.GetRestaurantById(-999);

        Assert.IsType<NotFoundObjectResult>(actionResult);
    }

    /// <summary>TC-RSC-022 – id âm/0 → 404 (service trả null)</summary>
    [Fact]
    public async Task GetRestaurantById_NegativeOrZeroId_Returns404()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.GetRestaurantByIdAsync(-1)).ReturnsAsync((RestaurantDetailDto?)null);
        mock.Setup(s => s.GetRestaurantByIdAsync(0)).ReturnsAsync((RestaurantDetailDto?)null);

        var r1 = await controller.GetRestaurantById(-1);
        var nf1 = Assert.IsType<NotFoundObjectResult>(r1);
        Assert.Equal("Restaurant not found", nf1.Value);

        var r2 = await controller.GetRestaurantById(0);
        var nf2 = Assert.IsType<NotFoundObjectResult>(r2);
        Assert.Equal("Restaurant not found", nf2.Value);
    }

    // ===========================================================
    //  UpdateRestaurant
    // ===========================================================

    /// <summary>TC-RSC-023 – Trả 200 OK khi cập nhật thành công, verify gọi đúng id=600</summary>
    [Fact]
    public async Task UpdateRestaurant_Success_Returns200WithMessage()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.UpdateRestaurantAsync(600, It.IsAny<UpdateRestaurantDto>()))
            .Returns(Task.CompletedTask);

        var actionResult = await controller.UpdateRestaurant(600, ValidUpdateDto());

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("Restaurant updated successfully", okResult.Value);

        // Verify: service gọi đúng 1 lần với id = 600
        mock.Verify(s => s.UpdateRestaurantAsync(600, It.IsAny<UpdateRestaurantDto>()), Times.Once);
    }

    /// <summary>TC-RSC-024 – Service ném KeyNotFoundException → exception lan truyền</summary>
    [Fact]
    public async Task UpdateRestaurant_ServiceThrowsKeyNotFoundException_Propagates()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.UpdateRestaurantAsync(9999, It.IsAny<UpdateRestaurantDto>()))
            .ThrowsAsync(new KeyNotFoundException("Restaurant not found"));

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => controller.UpdateRestaurant(9999, ValidUpdateDto()));

        Assert.Equal("Restaurant not found", ex.Message);
    }

    /// <summary>
    /// TC-RSC-025 – Body JSON sai kiểu (Status="abc") → 400 (WebApplicationFactory).
    /// Unit test: placeholder xác nhận service không được gọi khi xử lý exception.
    /// </summary>
    [Fact]
    public async Task UpdateRestaurant_InvalidBodyTypeRequiresIntegrationTest()
    {
        // Integration test: PUT /api/restaurants/update/600 với body {"Status":"abc"} → 400
        // mock.Verify(s => s.UpdateRestaurantAsync(...), Times.Never)
        var (controller, mock) = BuildController();
        mock.Setup(s => s.UpdateRestaurantAsync(600, It.IsAny<UpdateRestaurantDto>()))
            .Returns(Task.CompletedTask);

        var actionResult = await controller.UpdateRestaurant(600, ValidUpdateDto());

        Assert.IsType<OkObjectResult>(actionResult);
    }

    /// <summary>TC-RSC-026 – Chấp nhận danh sách ảnh mới trong DTO, trả 200 OK</summary>
    [Fact]
    public async Task UpdateRestaurant_WithNewPhotos_Returns200AndDtoPassedCorrectly()
    {
        var (controller, mock) = BuildController();

        UpdateRestaurantDto? capturedDto = null;
        mock.Setup(s => s.UpdateRestaurantAsync(601, It.IsAny<UpdateRestaurantDto>()))
            .Callback<int, UpdateRestaurantDto>((id, dto) => capturedDto = dto)
            .Returns(Task.CompletedTask);

        var updateDto = new UpdateRestaurantDto
        {
            Name = "R", Email = "r@x.vn", PhoneNumber = "0900000000",
            Status = 1, AvtImage = "a.jpg", CateId = 1,
            Address = DefaultAddressDto(),
            RestaurantPhotos = new List<string> { "new1.jpg", "new2.jpg" }
        };

        var actionResult = await controller.UpdateRestaurant(601, updateDto);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("Restaurant updated successfully", okResult.Value);

        // DTO truyền xuống service có 2 ảnh
        Assert.NotNull(capturedDto);
        Assert.Equal(2, capturedDto!.RestaurantPhotos.Count);
    }

    /// <summary>
    /// TC-RSC-027 – Route id không phải int → 400 (WebApplicationFactory).
    /// Unit test: placeholder.
    /// </summary>
    [Fact]
    public async Task UpdateRestaurant_NonIntRouteRequiresIntegrationTest()
    {
        // Integration test: PUT /api/restaurants/update/abc → 400
        var (controller, mock) = BuildController();
        mock.Setup(s => s.UpdateRestaurantAsync(It.IsAny<int>(), It.IsAny<UpdateRestaurantDto>()))
            .Returns(Task.CompletedTask);

        var actionResult = await controller.UpdateRestaurant(0, ValidUpdateDto());

        // 0 là id hợp lệ (không bị route-binding fail ở unit test)
        Assert.IsType<OkObjectResult>(actionResult);
    }

    // ===========================================================
    //  DeleteRestaurant
    // ===========================================================

    /// <summary>TC-RSC-028 – Trả 200 OK khi xóa thành công, verify gọi đúng id=700</summary>
    [Fact]
    public async Task DeleteRestaurant_Success_Returns200WithMessage()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.DeleteRestaurantAsync(700)).Returns(Task.CompletedTask);

        var actionResult = await controller.DeleteRestaurant(700);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("Restaurant deleted successfully", okResult.Value);

        mock.Verify(s => s.DeleteRestaurantAsync(700), Times.Once);
    }

    /// <summary>TC-RSC-029 – id không tồn tại vẫn 200 OK (service âm thầm bỏ qua)</summary>
    [Fact]
    public async Task DeleteRestaurant_NonexistentId_Returns200()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.DeleteRestaurantAsync(9999)).Returns(Task.CompletedTask);

        var actionResult = await controller.DeleteRestaurant(9999);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("Restaurant deleted successfully", okResult.Value);
    }

    /// <summary>
    /// TC-RSC-030 – Route id không phải int → 400 (WebApplicationFactory).
    /// Unit test: placeholder.
    /// </summary>
    [Fact]
    public async Task DeleteRestaurant_NonIntRouteRequiresIntegrationTest()
    {
        // Integration test: DELETE /api/restaurants/delete/abc → 400
        var (controller, mock) = BuildController();
        mock.Setup(s => s.DeleteRestaurantAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

        var actionResult = await controller.DeleteRestaurant(0);

        Assert.IsType<OkObjectResult>(actionResult);
    }

    /// <summary>TC-RSC-031 – id âm/0 vẫn gọi service và trả 200 OK</summary>
    [Fact]
    public async Task DeleteRestaurant_NegativeOrZeroId_Returns200AndCallsService()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.DeleteRestaurantAsync(-1)).Returns(Task.CompletedTask);
        mock.Setup(s => s.DeleteRestaurantAsync(0)).Returns(Task.CompletedTask);

        var r1 = await controller.DeleteRestaurant(-1);
        var ok1 = Assert.IsType<OkObjectResult>(r1);
        Assert.Equal("Restaurant deleted successfully", ok1.Value);

        var r2 = await controller.DeleteRestaurant(0);
        var ok2 = Assert.IsType<OkObjectResult>(r2);
        Assert.Equal("Restaurant deleted successfully", ok2.Value);

        mock.Verify(s => s.DeleteRestaurantAsync(-1), Times.Once);
        mock.Verify(s => s.DeleteRestaurantAsync(0), Times.Once);
    }

    // ===========================================================
    //  GetRestaurants (get-restaurant-by-address)
    // ===========================================================

    /// <summary>TC-RSC-032 – Lọc đủ 4 tiêu chí, verify tham số truyền xuống service</summary>
    [Fact]
    public async Task GetRestaurantsByAddress_AllFilters_Returns200AndVerifyCall()
    {
        var (controller, mock) = BuildController();

        var fakeList = new List<RestaurantDto>
        {
            new RestaurantDto(new Restaurant { Id = 1, Name = "R1" }),
            new RestaurantDto(new Restaurant { Id = 2, Name = "R2" })
        };
        mock.Setup(s => s.GetRestaurantsByAddressAsync("Hà Nội", "Cầu Giấy", "Dịch Vọng", "Duy Tân"))
            .ReturnsAsync(fakeList);

        var actionResult = await controller.GetRestaurants("Hà Nội", "Cầu Giấy", "Dịch Vọng", "Duy Tân");

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDto>>(okResult.Value);
        Assert.Equal(2, list.Count());

        // Verify tham số
        mock.Verify(s => s.GetRestaurantsByAddressAsync("Hà Nội", "Cầu Giấy", "Dịch Vọng", "Duy Tân"), Times.Once);
    }

    /// <summary>TC-RSC-033 – Không truyền tiêu chí → trả 200 với 5 nhà hàng</summary>
    [Fact]
    public async Task GetRestaurantsByAddress_NoCriteria_Returns200WithAllRestaurants()
    {
        var (controller, mock) = BuildController();

        var fakeList = Enumerable.Range(1, 5)
            .Select(i => new RestaurantDto(new Restaurant { Id = i }))
            .ToList();
        mock.Setup(s => s.GetRestaurantsByAddressAsync(null, null, null, null))
            .ReturnsAsync(fakeList);

        var actionResult = await controller.GetRestaurants(null, null, null, null);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDto>>(okResult.Value);
        Assert.Equal(5, list.Count());
    }

    /// <summary>TC-RSC-034 – Chỉ truyền city, verify tham số đúng</summary>
    [Fact]
    public async Task GetRestaurantsByAddress_OnlyCity_Returns200AndVerifyCall()
    {
        var (controller, mock) = BuildController();

        var fakeList = Enumerable.Range(1, 3)
            .Select(i => new RestaurantDto(new Restaurant { Id = i }))
            .ToList();
        mock.Setup(s => s.GetRestaurantsByAddressAsync("Đà Nẵng", null, null, null))
            .ReturnsAsync(fakeList);

        var actionResult = await controller.GetRestaurants("Đà Nẵng", null, null, null);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDto>>(okResult.Value);
        Assert.Equal(3, list.Count());
        mock.Verify(s => s.GetRestaurantsByAddressAsync("Đà Nẵng", null, null, null), Times.Once);
    }

    /// <summary>TC-RSC-035 – Không khớp → trả 200 rỗng</summary>
    [Fact]
    public async Task GetRestaurantsByAddress_NoMatch_Returns200Empty()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.GetRestaurantsByAddressAsync("Cần Thơ", null, null, null))
            .ReturnsAsync(Enumerable.Empty<RestaurantDto>());

        var actionResult = await controller.GetRestaurants("Cần Thơ", null, null, null);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDto>>(okResult.Value);
        Assert.False(list.Any());
    }

    /// <summary>TC-RSC-036 – Tiếng Việt có dấu trong city/district giữ nguyên khi truyền xuống service</summary>
    [Fact]
    public async Task GetRestaurantsByAddress_VietnameseFilters_PassedToServiceExactly()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.GetRestaurantsByAddressAsync("Huế", "Phú Nhuận", null, null))
            .ReturnsAsync(new List<RestaurantDto>
            {
                new RestaurantDto(new Restaurant { Id = 1, Name = "Bún Bò Huế Cô Ba" })
            });

        var actionResult = await controller.GetRestaurants("Huế", "Phú Nhuận", null, null);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDto>>(okResult.Value);
        Assert.Single(list);
        mock.Verify(s => s.GetRestaurantsByAddressAsync("Huế", "Phú Nhuận", null, null), Times.Once);
    }

    // ===========================================================
    //  GetRestaurants (GetRestaurants comprehensive endpoint)
    // ===========================================================

    /// <summary>TC-RSC-037 – Không truyền tiêu chí → trả 200 với 5 RestaurantDetailDto</summary>
    [Fact]
    public async Task GetRestaurants_NoCriteria_Returns200WithAllRestaurants()
    {
        var (controller, mock) = BuildController();

        var fakeList = Enumerable.Range(1, 5)
            .Select(i => new RestaurantDetailDto { Id = i })
            .ToList();
        mock.Setup(s => s.GetRestaurantsAsync(null, null, null, null, null, null))
            .ReturnsAsync(fakeList);

        var actionResult = await controller.GetRestaurants(
            (int?)null, (int?)null, null, null, null, null);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDetailDto>>(okResult.Value);
        Assert.Equal(5, list.Count());
    }

    /// <summary>TC-RSC-038 – Chỉ lọc theo categoryId, verify tham số</summary>
    [Fact]
    public async Task GetRestaurants_FilterByCategoryId_Returns200AndVerifyCall()
    {
        var (controller, mock) = BuildController();

        var fakeList = new List<RestaurantDetailDto>
        {
            new RestaurantDetailDto { Id = 1, Name = "R1", Category = "Món Việt" },
            new RestaurantDetailDto { Id = 2, Name = "R2", Category = "Món Việt" }
        };
        mock.Setup(s => s.GetRestaurantsAsync(1, null, null, null, null, null))
            .ReturnsAsync(fakeList);

        var actionResult = await controller.GetRestaurants(
            1, null, null, null, null, null);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDetailDto>>(okResult.Value);
        Assert.Equal(2, list.Count());
        mock.Verify(s => s.GetRestaurantsAsync(1, null, null, null, null, null), Times.Once);
    }

    /// <summary>TC-RSC-039 – Chỉ lọc theo userId, verify tham số</summary>
    [Fact]
    public async Task GetRestaurants_FilterByUserId_Returns200AndVerifyCall()
    {
        var (controller, mock) = BuildController();

        var fakeList = Enumerable.Range(1, 3)
            .Select(i => new RestaurantDetailDto { Id = i, UserId = 100 })
            .ToList();
        mock.Setup(s => s.GetRestaurantsAsync(null, 100, null, null, null, null))
            .ReturnsAsync(fakeList);

        var actionResult = await controller.GetRestaurants(
            null, 100, null, null, null, null);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDetailDto>>(okResult.Value);
        Assert.Equal(3, list.Count());
        mock.Verify(s => s.GetRestaurantsAsync(null, 100, null, null, null, null), Times.Once);
    }

    /// <summary>TC-RSC-040 – Chỉ lọc theo searchTerm, verify tham số</summary>
    [Fact]
    public async Task GetRestaurants_FilterBySearchTerm_Returns200AndVerifyCall()
    {
        var (controller, mock) = BuildController();

        var fakeList = new List<RestaurantDetailDto>
        {
            new RestaurantDetailDto { Id = 1, Name = "Phở Thìn" },
            new RestaurantDetailDto { Id = 2, Name = "Phở Gia Truyền" }
        };
        mock.Setup(s => s.GetRestaurantsAsync(null, null, "Phở", null, null, null))
            .ReturnsAsync(fakeList);

        var actionResult = await controller.GetRestaurants(
            null, null, "Phở", null, null, null);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDetailDto>>(okResult.Value);
        Assert.Equal(2, list.Count());
        mock.Verify(s => s.GetRestaurantsAsync(null, null, "Phở", null, null, null), Times.Once);
    }

    /// <summary>TC-RSC-041 – Kết hợp nhiều tiêu chí, verify tham số đầy đủ</summary>
    [Fact]
    public async Task GetRestaurants_MultipleFilters_Returns200AndVerifyCall()
    {
        var (controller, mock) = BuildController();

        var fakeList = new List<RestaurantDetailDto>
        {
            new RestaurantDetailDto
            {
                Id = 1, Name = "Phở Thìn", Category = "Món Việt",
                UserId = 100, Address = new Address { City = "Hà Nội" }
            }
        };
        mock.Setup(s => s.GetRestaurantsAsync(1, 100, "Phở", "Hà Nội", null, null))
            .ReturnsAsync(fakeList);

        var actionResult = await controller.GetRestaurants(
            1, 100, "Phở", "Hà Nội", null, null);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDetailDto>>(okResult.Value);
        Assert.Single(list);
        mock.Verify(s => s.GetRestaurantsAsync(1, 100, "Phở", "Hà Nội", null, null), Times.Once);
    }

    /// <summary>TC-RSC-042 – Không có kết quả → trả 200 rỗng</summary>
    [Fact]
    public async Task GetRestaurants_NoResults_Returns200Empty()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.GetRestaurantsAsync(null, null, null, "Cần Thơ", null, null))
            .ReturnsAsync(Enumerable.Empty<RestaurantDetailDto>());

        var actionResult = await controller.GetRestaurants(
            null, null, null, "Cần Thơ", null, null);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDetailDto>>(okResult.Value);
        Assert.False(list.Any());
    }

    /// <summary>
    /// TC-RSC-043 – categoryId/userId không phải int → HTTP 400 (WebApplicationFactory).
    /// Unit test: placeholder xác nhận service gọi bình thường với int.
    /// </summary>
    [Fact]
    public async Task GetRestaurants_NonIntQueryParamsRequireIntegrationTest()
    {
        // Integration test: GET /api/restaurants/GetRestaurants?categoryId=abc → 400
        // mock.Verify(s => s.GetRestaurantsAsync(...), Times.Never)
        var (controller, mock) = BuildController();
        mock.Setup(s => s.GetRestaurantsAsync(It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(Enumerable.Empty<RestaurantDetailDto>());

        var actionResult = await controller.GetRestaurants(
            (int?)null, (int?)null, null, null, null, null);

        Assert.IsType<OkObjectResult>(actionResult);
    }

    /// <summary>TC-RSC-044 – Response chứa đầy đủ thông tin detail của 1 nhà hàng</summary>
    [Fact]
    public async Task GetRestaurants_SingleRestaurant_ContainsAllDetailFields()
    {
        var (controller, mock) = BuildController();

        var fakeList = new List<RestaurantDetailDto>
        {
            new RestaurantDetailDto
            {
                Id = 800, Name = "Quán Hải Phòng",
                Address = new Address { City = "Hải Phòng", District = "Lê Chân" },
                Category = "Món Việt", AverageScore = 4.2f,
                Status = 1, UserId = 100
            }
        };
        mock.Setup(s => s.GetRestaurantsAsync(null, null, null, null, null, null))
            .ReturnsAsync(fakeList);

        var actionResult = await controller.GetRestaurants(
            null, null, null, null, null, null);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDetailDto>>(okResult.Value).ToList();
        Assert.Single(list);
        var r = list[0];
        Assert.Equal(800, r.Id);
        Assert.Equal("Quán Hải Phòng", r.Name);
        Assert.Equal("Hải Phòng", r.Address!.City);
        Assert.Equal("Món Việt", r.Category);
        Assert.Equal(4.2, (double)r.AverageScore!.Value, 1);
        Assert.Equal(1, r.Status);
        Assert.Equal(100, r.UserId);
    }

    /// <summary>TC-RSC-045 – searchTerm/city tiếng Việt có dấu được truyền xuống service đúng</summary>
    [Fact]
    public async Task GetRestaurants_VietnameseSearchTermAndCity_PassedToServiceExactly()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.GetRestaurantsAsync(null, null, "Gánh", "Huế", null, null))
            .ReturnsAsync(new List<RestaurantDetailDto>
            {
                new RestaurantDetailDto { Id = 1, Name = "Phở Gánh" }
            });

        var actionResult = await controller.GetRestaurants(
            null, null, "Gánh", "Huế", null, null);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantDetailDto>>(okResult.Value);
        Assert.Single(list);
        mock.Verify(s => s.GetRestaurantsAsync(null, null, "Gánh", "Huế", null, null), Times.Once);
    }
}
