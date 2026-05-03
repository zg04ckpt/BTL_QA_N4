using BusinessLogicLayer.Interfaces;
using DataAccessLayer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Controllers;
using Xunit;

namespace UnitTests.Controllers;

public class OrdersControllerTests
{
    /// <summary>
    /// TC-ORD-CTRL-001
    /// Gui yeu cau dat ban thieu thong tin thi tra ve BadRequest
    /// </summary>
    [Fact]
    public async Task AddOrder_InvalidModelState_ReturnsBadRequest()
    {
        var service = new FakeOrderService();
        var controller = new OrdersController(service);
        controller.ModelState.AddModelError("Name", "Name is required");

        var result = await controller.AddOrder(new AddOrderDto());

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.AddOrderCallCount);
    }

    /// <summary>
    /// TC-ORD-CTRL-003
    /// Gui yeu cau dat ban thong tin lien lac khong hop le thi tra ve BadRequest
    /// </summary>
    [Fact]
    public async Task AddOrder_InvalidContact_ReturnsBadRequest()
    {
        var service = new FakeOrderService();
        var controller = new OrdersController(service);
        controller.ModelState.AddModelError("PhoneNumber", "Invalid phone");

        var result = await controller.AddOrder(new AddOrderDto());

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.AddOrderCallCount);
    }

    /// <summary>
    /// TC-ORD-CTRL-002
    /// Gui yeu cau dat ban hop le thi tra ve Ok va goi service
    /// </summary>
    [Fact]
    public async Task AddOrder_Valid_ReturnsOk()
    {
        var service = new FakeOrderService();
        var controller = new OrdersController(service);

        var dto = new AddOrderDto
        {
            Name = "Nguyen Van B",
            PhoneNumber = "0909123456",
            Email = "nguoidung@test.vn",
            UserId = 1,
            RestaurantId = 2,
            NumOfMembers = 4,
            ReservationTime = "2026-05-20 19:00",
            SpecialRequest = "Ban gan cua so",
            CreatedAt = 1714724200
        };

        var result = await controller.AddOrder(dto);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.AddOrderCallCount);
        Assert.Equal("Nguyen Van B", service.LastAddOrder?.Name);
    }

    /// <summary>
    /// TC-ORD-CTRL-015
    /// Service nem exception khi them don thi controller day loi ra ngoai
    /// </summary>
    [Fact]
    public async Task AddOrder_ServiceThrows_PropagatesException()
    {
        var service = new FakeOrderService { ThrowOnAdd = true };
        var controller = new OrdersController(service);

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.AddOrder(new AddOrderDto()));
    }

    /// <summary>
    /// TC-ORD-CTRL-003
    /// Tra cuu yeu cau dat ban khong ton tai thi tra ve NotFound
    /// </summary>
    [Fact]
    public async Task GetOrderById_NotFound_ReturnsNotFound()
    {
        var service = new FakeOrderService { OrderByIdResult = null };
        var controller = new OrdersController(service);

        var result = await controller.GetOrderById(9999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// TC-ORD-CTRL-004
    /// Tra cuu yeu cau dat ban ton tai thi tra ve Ok
    /// </summary>
    [Fact]
    public async Task GetOrderById_Found_ReturnsOk()
    {
        var service = new FakeOrderService
        {
            OrderByIdResult = new OrderDetailDto { Id = 10, Name = "Nguyen Van B" }
        };
        var controller = new OrdersController(service);

        var result = await controller.GetOrderById(10);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<OrderDetailDto>(ok.Value);
    }

    /// <summary>
    /// TC-ORD-CTRL-016
    /// Service nem exception khi lay chi tiet don thi controller day loi ra ngoai
    /// </summary>
    [Fact]
    public async Task GetOrderById_ServiceThrows_PropagatesException()
    {
        var service = new FakeOrderService { ThrowOnGetById = true };
        var controller = new OrdersController(service);

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.GetOrderById(1));
    }

    /// <summary>
    /// TC-ORD-CTRL-005
    /// Huy yeu cau dat ban khong ton tai thi tra ve NotFound
    /// </summary>
    [Fact]
    public async Task DeleteOrder_NotFound_ReturnsNotFound()
    {
        var service = new FakeOrderService { RemoveOrderResult = false };
        var controller = new OrdersController(service);

        var result = await controller.DeleteOrder(9999);

        Assert.IsType<NotFoundResult>(result);
    }

    /// <summary>
    /// TC-ORD-CTRL-006
    /// Huy yeu cau dat ban ton tai thi tra ve NoContent
    /// </summary>
    [Fact]
    public async Task DeleteOrder_Success_ReturnsNoContent()
    {
        var service = new FakeOrderService { RemoveOrderResult = true };
        var controller = new OrdersController(service);

        var result = await controller.DeleteOrder(10);

        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>
    /// TC-ORD-CTRL-017
    /// Service nem exception khi xoa don thi controller day loi ra ngoai
    /// </summary>
    [Fact]
    public async Task DeleteOrder_ServiceThrows_PropagatesException()
    {
        var service = new FakeOrderService { ThrowOnDelete = true };
        var controller = new OrdersController(service);

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.DeleteOrder(1));
    }

    /// <summary>
    /// TC-ORD-CTRL-007
    /// Cap nhat trang thai khong ton tai thi tra ve NotFound
    /// </summary>
    [Fact]
    public async Task UpdateStatus_NotFound_ReturnsNotFound()
    {
        var service = new FakeOrderService { ChangeStatusResult = false };
        var controller = new OrdersController(service);

        var result = await controller.UpdateStatus(9999, 1);

        Assert.IsType<NotFoundResult>(result);
    }

    /// <summary>
    /// TC-ORD-CTRL-008
    /// Cap nhat trang thai thanh cong thi tra ve Ok
    /// </summary>
    [Fact]
    public async Task UpdateStatus_Success_ReturnsOk()
    {
        var service = new FakeOrderService { ChangeStatusResult = true };
        var controller = new OrdersController(service);

        var result = await controller.UpdateStatus(10, 1);

        Assert.IsType<OkResult>(result);
    }

    /// <summary>
    /// TC-ORD-CTRL-009
    /// Nha hang tu choi yeu cau dat ban thi tra ve Ok
    /// </summary>
    [Fact]
    public async Task UpdateStatus_Declined_ReturnsOk()
    {
        var service = new FakeOrderService { ChangeStatusResult = true };
        var controller = new OrdersController(service);

        var result = await controller.UpdateStatus(10, 0);

        Assert.IsType<OkResult>(result);
    }

    /// <summary>
    /// TC-ORD-CTRL-018
    /// Service nem exception khi cap nhat trang thai thi controller day loi ra ngoai
    /// </summary>
    [Fact]
    public async Task UpdateStatus_ServiceThrows_PropagatesException()
    {
        var service = new FakeOrderService { ThrowOnUpdateStatus = true };
        var controller = new OrdersController(service);

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.UpdateStatus(1, 1));
    }

    /// <summary>
    /// TC-ORD-CTRL-009
    /// Nguoi dung khong co yeu cau dat ban thi tra ve NotFound
    /// </summary>
    [Fact]
    public async Task GetOrdersByUserId_Empty_ReturnsNotFound()
    {
        var service = new FakeOrderService();
        var controller = new OrdersController(service);

        var result = await controller.GetOrdersByUserId(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// TC-ORD-CTRL-010
    /// Nguoi dung co yeu cau dat ban thi tra ve Ok
    /// </summary>
    [Fact]
    public async Task GetOrdersByUserId_HasOrders_ReturnsOk()
    {
        var service = new FakeOrderService();
        service.OrdersByUser.Add(new OrderDetailDto { Id = 1, UserId = 1, Name = "Nguyen Van B" });
        var controller = new OrdersController(service);

        var result = await controller.GetOrdersByUserId(1);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// TC-ORD-CTRL-019
    /// UserId am va khong co du lieu thi tra ve NotFound
    /// </summary>
    [Fact]
    public async Task GetOrdersByUserId_NegativeId_ReturnsNotFound()
    {
        var service = new FakeOrderService();
        var controller = new OrdersController(service);

        var result = await controller.GetOrdersByUserId(-1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// TC-ORD-CTRL-020
    /// Service nem exception khi lay danh sach theo user thi controller day loi ra ngoai
    /// </summary>
    [Fact]
    public async Task GetOrdersByUserId_ServiceThrows_PropagatesException()
    {
        var service = new FakeOrderService { ThrowOnGetByUser = true };
        var controller = new OrdersController(service);

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.GetOrdersByUserId(1));
    }

    /// <summary>
    /// TC-ORD-CTRL-011
    /// Nha hang chua co yeu cau dat ban thi tra ve NotFound
    /// </summary>
    [Fact]
    public async Task GetOrdersByRestaurantId_Empty_ReturnsNotFound()
    {
        var service = new FakeOrderService();
        var controller = new OrdersController(service);

        var result = await controller.GetOrdersByRestaurantId(2);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// TC-ORD-CTRL-012
    /// Nha hang co yeu cau dat ban thi tra ve Ok
    /// </summary>
    [Fact]
    public async Task GetOrdersByRestaurantId_HasOrders_ReturnsOk()
    {
        var service = new FakeOrderService();
        service.OrdersByRestaurant.Add(new OrderDetailDto { Id = 2, RestaurantId = 2, Name = "Nguyen Van B" });
        var controller = new OrdersController(service);

        var result = await controller.GetOrdersByRestaurantId(2);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// TC-ORD-CTRL-021
    /// RestaurantId am va khong co du lieu thi tra ve NotFound
    /// </summary>
    [Fact]
    public async Task GetOrdersByRestaurantId_NegativeId_ReturnsNotFound()
    {
        var service = new FakeOrderService();
        var controller = new OrdersController(service);

        var result = await controller.GetOrdersByRestaurantId(-1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// TC-ORD-CTRL-022
    /// Service nem exception khi lay danh sach theo nha hang thi controller day loi ra ngoai
    /// </summary>
    [Fact]
    public async Task GetOrdersByRestaurantId_ServiceThrows_PropagatesException()
    {
        var service = new FakeOrderService { ThrowOnGetByRestaurant = true };
        var controller = new OrdersController(service);

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.GetOrdersByRestaurantId(1));
    }

    private sealed class FakeOrderService : IOrderService
    {
        public int AddOrderCallCount { get; private set; }
        public AddOrderDto? LastAddOrder { get; private set; }
        public OrderDetailDto? OrderByIdResult { get; set; }
        public bool RemoveOrderResult { get; set; }
        public bool ChangeStatusResult { get; set; }
        public List<OrderDetailDto> OrdersByUser { get; } = new();
        public List<OrderDetailDto> OrdersByRestaurant { get; } = new();
        public bool ThrowOnAdd { get; set; }
        public bool ThrowOnGetById { get; set; }
        public bool ThrowOnDelete { get; set; }
        public bool ThrowOnUpdateStatus { get; set; }
        public bool ThrowOnGetByUser { get; set; }
        public bool ThrowOnGetByRestaurant { get; set; }

        public Task AddOrderAsync(AddOrderDto addOrderDto)
        {
            if (ThrowOnAdd)
            {
                throw new InvalidOperationException("Simulated add failure");
            }

            AddOrderCallCount++;
            LastAddOrder = addOrderDto;
            return Task.CompletedTask;
        }

        public Task<OrderDetailDto?> GetOrderByIdAsync(int id)
        {
            if (ThrowOnGetById)
            {
                throw new InvalidOperationException("Simulated get by id failure");
            }

            return Task.FromResult(OrderByIdResult);
        }

        public Task<bool> RemoveOrderAsync(int orderId)
        {
            if (ThrowOnDelete)
            {
                throw new InvalidOperationException("Simulated delete failure");
            }

            return Task.FromResult(RemoveOrderResult);
        }

        public Task<bool> ChangeOrderStatusAsync(int orderId, int newStatus)
        {
            if (ThrowOnUpdateStatus)
            {
                throw new InvalidOperationException("Simulated update status failure");
            }

            return Task.FromResult(ChangeStatusResult);
        }

        public Task<List<OrderDetailDto>> GetOrdersByUserIdAsync(int userId)
        {
            if (ThrowOnGetByUser)
            {
                throw new InvalidOperationException("Simulated get by user failure");
            }

            return Task.FromResult(OrdersByUser);
        }

        public Task<List<OrderDetailDto>> GetOrdersByRestaurantIdAsync(int restaurantId)
        {
            if (ThrowOnGetByRestaurant)
            {
                throw new InvalidOperationException("Simulated get by restaurant failure");
            }

            return Task.FromResult(OrdersByRestaurant);
        }
    }
}
