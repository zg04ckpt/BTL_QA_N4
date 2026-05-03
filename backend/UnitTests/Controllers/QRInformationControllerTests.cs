using BusinessLogicLayer.Interfaces;
using DataAccessLayer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Controllers;
using Xunit;

namespace UnitTests.Controllers;

public class QRInformationControllerTests
{
    /// <summary>
    /// TC-QR-CTRL-001
    /// Quet QR dung thi tra ve Ok
    /// </summary>
    [Fact]
    public async Task AddQRInformation_Valid_ReturnsOk()
    {
        var service = new FakeQrInformationService();
        var controller = new QRInformationController(service);

        var dto = new QRInformationDto
        {
            UserId = 1,
            RestaurantId = 2,
            CreateTime = 1714725000
        };

        var result = await controller.AddQRInformation(dto);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.AddCallCount);
    }

    /// <summary>
    /// TC-QR-CTRL-002
    /// Quet QR sai (du lieu khong hop le) thi van ghi nhan va tra ve Ok
    /// </summary>
    [Fact]
    public async Task AddQRInformation_InvalidInput_ReturnsOk()
    {
        var service = new FakeQrInformationService();
        var controller = new QRInformationController(service);

        var dto = new QRInformationDto
        {
            UserId = 0,
            RestaurantId = 0,
            CreateTime = 0
        };

        var result = await controller.AddQRInformation(dto);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.AddCallCount);
    }

    /// <summary>
    /// TC-QR-CTRL-009
    /// ModelState khong hop le khi quet QR thi van tra ve Ok theo hanh vi hien tai
    /// </summary>
    [Fact]
    public async Task AddQRInformation_InvalidModelState_StillReturnsOk()
    {
        var service = new FakeQrInformationService();
        var controller = new QRInformationController(service);
        controller.ModelState.AddModelError("UserId", "UserId invalid");

        var result = await controller.AddQRInformation(new QRInformationDto());

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.AddCallCount);
    }

    /// <summary>
    /// TC-QR-CTRL-010
    /// Service nem exception khi them QR thi controller day loi ra ngoai
    /// </summary>
    [Fact]
    public async Task AddQRInformation_ServiceThrows_PropagatesException()
    {
        var service = new FakeQrInformationService { ThrowOnAdd = true };
        var controller = new QRInformationController(service);

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.AddQRInformation(new QRInformationDto()));
    }

    /// <summary>
    /// TC-QR-CTRL-002
    /// Lay danh sach theo tai khoan thi tra ve Ok
    /// </summary>
    [Fact]
    public async Task GetQRInformation_ByUser_ReturnsOk()
    {
        var service = new FakeQrInformationService();
        service.Result.Add(new QRInformationDto { Id = 1, UserId = 1, RestaurantId = 2 });
        var controller = new QRInformationController(service);

        var result = await controller.GetQRInformation(1, null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<QRInformationDto>>(ok.Value);
        Assert.Single(list);
    }

    /// <summary>
    /// TC-QR-CTRL-003
    /// Lay danh sach theo nha hang thi tra ve Ok
    /// </summary>
    [Fact]
    public async Task GetQRInformation_ByRestaurant_ReturnsOk()
    {
        var service = new FakeQrInformationService();
        service.Result.Add(new QRInformationDto { Id = 2, UserId = 1, RestaurantId = 10 });
        var controller = new QRInformationController(service);

        var result = await controller.GetQRInformation(null, 10);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<QRInformationDto>>(ok.Value);
        Assert.Single(list);
    }

    /// <summary>
    /// TC-QR-CTRL-005
    /// Khong truyen dieu kien loc thi tra ve danh sach toan bo
    /// </summary>
    [Fact]
    public async Task GetQRInformation_NoFilter_ReturnsOk()
    {
        var service = new FakeQrInformationService();
        service.Result.Add(new QRInformationDto { Id = 3, UserId = 2, RestaurantId = 5 });
        var controller = new QRInformationController(service);

        var result = await controller.GetQRInformation(null, null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<QRInformationDto>>(ok.Value);
        Assert.Single(list);
    }

    /// <summary>
    /// TC-QR-CTRL-006
    /// Khong co du lieu quet thi tra ve danh sach trong
    /// </summary>
    [Fact]
    public async Task GetQRInformation_Empty_ReturnsOk()
    {
        var service = new FakeQrInformationService();
        var controller = new QRInformationController(service);

        var result = await controller.GetQRInformation(null, null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<QRInformationDto>>(ok.Value);
        Assert.Empty(list);
    }

    /// <summary>
    /// TC-QR-CTRL-011
    /// Truyen UserId am thi van goi service va tra ve Ok theo hanh vi hien tai
    /// </summary>
    [Fact]
    public async Task GetQRInformation_NegativeUserId_ReturnsOk()
    {
        var service = new FakeQrInformationService();
        var controller = new QRInformationController(service);

        var result = await controller.GetQRInformation(-1, null);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// TC-QR-CTRL-012
    /// Service nem exception khi lay danh sach QR thi controller day loi ra ngoai
    /// </summary>
    [Fact]
    public async Task GetQRInformation_ServiceThrows_PropagatesException()
    {
        var service = new FakeQrInformationService { ThrowOnGet = true };
        var controller = new QRInformationController(service);

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.GetQRInformation(null, null));
    }

    /// <summary>
    /// TC-QR-CTRL-004
    /// Xoa ban ghi QR ton tai thi tra ve Ok
    /// </summary>
    [Fact]
    public async Task DeleteQRInformation_Valid_ReturnsOk()
    {
        var service = new FakeQrInformationService();
        var controller = new QRInformationController(service);

        var result = await controller.DeleteQRInformation(10);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.DeleteCallCount);
    }

    /// <summary>
    /// TC-QR-CTRL-008
    /// Xoa ban ghi quet khong ton tai thi van tra ve Ok
    /// </summary>
    [Fact]
    public async Task DeleteQRInformation_NotFound_ReturnsOk()
    {
        var service = new FakeQrInformationService();
        var controller = new QRInformationController(service);

        var result = await controller.DeleteQRInformation(9999);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.DeleteCallCount);
    }

    /// <summary>
    /// TC-QR-CTRL-013
    /// Service nem exception khi xoa QR thi controller day loi ra ngoai
    /// </summary>
    [Fact]
    public async Task DeleteQRInformation_ServiceThrows_PropagatesException()
    {
        var service = new FakeQrInformationService { ThrowOnDelete = true };
        var controller = new QRInformationController(service);

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.DeleteQRInformation(1));
    }

    private sealed class FakeQrInformationService : IQRInformationService
    {
        public int AddCallCount { get; private set; }
        public int DeleteCallCount { get; private set; }
        public List<QRInformationDto> Result { get; } = new();
        public bool ThrowOnAdd { get; set; }
        public bool ThrowOnGet { get; set; }
        public bool ThrowOnDelete { get; set; }

        public Task AddQRInformationAsync(QRInformationDto dto)
        {
            if (ThrowOnAdd)
            {
                throw new InvalidOperationException("Simulated add failure");
            }

            AddCallCount++;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<QRInformationDto>> GetQRInformationAsync(int? userId, int? restaurantId)
        {
            if (ThrowOnGet)
            {
                throw new InvalidOperationException("Simulated get failure");
            }

            return Task.FromResult<IEnumerable<QRInformationDto>>(Result);
        }

        public Task DeleteQRInformationAsync(int id)
        {
            if (ThrowOnDelete)
            {
                throw new InvalidOperationException("Simulated delete failure");
            }

            DeleteCallCount++;
            return Task.CompletedTask;
        }
    }
}
