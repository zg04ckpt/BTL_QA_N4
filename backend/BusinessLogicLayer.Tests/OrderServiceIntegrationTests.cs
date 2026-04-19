using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BusinessLogicLayer.Tests;

public class OrderServiceIntegrationTests : TestDatabaseFixture
{
    private async Task<int> CreateTestUser()
    {
        var user = new User
        {
            Name = "Test User",
            PhoneNumber = "0123456789",
            Email = "testuser@example.com",
            Address = "123 Test Street"
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
        return user.Id;
    }

    private async Task<int> CreateTestRestaurant()
    {
        var restaurant = new Restaurant
        {
            Name = "Test Restaurant",
            PhoneNumber = "0987654321",
            Email = "restaurant@example.com",
            Address = "456 Restaurant Street"
        };
        DbContext.Restaurants.Add(restaurant);
        await DbContext.SaveChangesAsync();
        return restaurant.Id;
    }

    [Fact]
    public async Task TC_OA_001_AddOrderAsync_ShouldCreateOrderWithValidDataAndPersistToDb()
    {
        // Test Case ID: TC-OA-001
        // Mục tiêu: Kiểm tra tạo mới Order thành công khi tất cả trường bắt buộc hợp lệ.
        await BeginTransactionAsync();

        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();

            var orderRepository = new OrderRepository(DbContext);
            var firebaseService = new FirebaseService();
            var orderService = new OrderService(orderRepository, firebaseService);

            var addOrderDto = new AddOrderDto
            {
                Name = "Nguyen Van A",
                PhoneNumber = "0123456789",
                Email = "customer@example.com",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 4,
                ReservationTime = DateTime.Now.AddDays(1),
                SpecialRequest = "Window seat please",
                CreatedAt = DateTime.Now
            };

            await orderService.AddOrderAsync(addOrderDto);

            var orders = await DbContext.Orders.ToListAsync();
            Assert.NotEmpty(orders);
            var savedOrder = orders.Last();
            Assert.Equal(addOrderDto.Name, savedOrder.Name);
            Assert.Equal(addOrderDto.PhoneNumber, savedOrder.PhoneNumber);
            Assert.Equal(addOrderDto.Email, savedOrder.Email);
            Assert.Equal(userId, savedOrder.UserId);
            Assert.Equal(restaurantId, savedOrder.RestaurantId);
            Assert.Equal(4, savedOrder.NumOfMembers);
            Assert.Equal(0, savedOrder.Status);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_OA_002_AddOrderAsync_ShouldAllowNullableSpecialRequest()
    {
        // Test Case ID: TC-OA-002
        // Mục tiêu: Kiểm tra AddOrderAsync cho phép SpecialRequest = null.
        await BeginTransactionAsync();

        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();

            var orderRepository = new OrderRepository(DbContext);
            var firebaseService = new FirebaseService();
            var orderService = new OrderService(orderRepository, firebaseService);

            var addOrderDto = new AddOrderDto
            {
                Name = "Tran Thi B",
                PhoneNumber = "0987654321",
                Email = "customer2@example.com",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 2,
                ReservationTime = DateTime.Now.AddDays(2),
                SpecialRequest = null,
                CreatedAt = DateTime.Now
            };

            await orderService.AddOrderAsync(addOrderDto);

            var orders = await DbContext.Orders.ToListAsync();
            var savedOrder = orders.Last();
            Assert.Null(savedOrder.SpecialRequest);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_OA_003_AddOrderAsync_ShouldCreateOrderWithValidNameContainingSpecialCharacters()
    {
        // Test Case ID: TC-OA-003
        // Mục tiêu: Kiểm tra tạo Order với Name chứa kí tự đặc biệt.
        await BeginTransactionAsync();

        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();

            var orderRepository = new OrderRepository(DbContext);
            var firebaseService = new FirebaseService();
            var orderService = new OrderService(orderRepository, firebaseService);

            var addOrderDto = new AddOrderDto
            {
                Name = "Nguyễn Văn C-D'Lê",
                PhoneNumber = "0111111111",
                Email = "customer3@example.com",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 3,
                ReservationTime = DateTime.Now.AddDays(3),
                SpecialRequest = "Không cay",
                CreatedAt = DateTime.Now
            };

            await orderService.AddOrderAsync(addOrderDto);

            var orders = await DbContext.Orders.ToListAsync();
            var savedOrder = orders.Last();
            Assert.Equal("Nguyễn Văn C-D'Lê", savedOrder.Name);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_OA_004_GetOrderByIdAsync_ShouldReturnOrderDetailWithValidId()
    {
        // Test Case ID: TC-OA-004
        // Mục tiêu: Kiểm tra GetOrderByIdAsync trả về chi tiết Order khi ID hợp lệ.
        await BeginTransactionAsync();

        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();

            var order = new Order
            {
                Name = "Test Order",
                PhoneNumber = "0222222222",
                Email = "order@example.com",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 5,
                ReservationTime = DateTime.Now.AddDays(4),
                SpecialRequest = "Allergic to peanuts",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = 0
            };
            DbContext.Orders.Add(order);
            await DbContext.SaveChangesAsync();

            var orderRepository = new OrderRepository(DbContext);
            var firebaseService = new FirebaseService();
            var orderService = new OrderService(orderRepository, firebaseService);

            var orderDetail = await orderService.GetOrderByIdAsync(order.Id);

            Assert.NotNull(orderDetail);
            Assert.Equal(order.Id, orderDetail.Id);
            Assert.Equal("Test Order", orderDetail.Name);
            Assert.Equal(5, orderDetail.NumOfMembers);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_OA_005_GetOrderByIdAsync_ShouldReturnNullForInvalidId()
    {
        // Test Case ID: TC-OA-005
        // Mục tiêu: Kiểm tra GetOrderByIdAsync trả về null khi ID không tồn tại.
        await BeginTransactionAsync();

        try
        {
            var orderRepository = new OrderRepository(DbContext);
            var firebaseService = new FirebaseService();
            var orderService = new OrderService(orderRepository, firebaseService);

            var orderDetail = await orderService.GetOrderByIdAsync(9999);

            Assert.Null(orderDetail);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_OA_006_RemoveOrderAsync_ShouldDeleteOrderWhenIdExists()
    {
        // Test Case ID: TC-OA-006
        // Mục tiêu: Kiểm tra RemoveOrderAsync xóa Order khi ID hợp lệ.
        await BeginTransactionAsync();

        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();

            var order = new Order
            {
                Name = "Order to Delete",
                PhoneNumber = "0333333333",
                Email = "delete@example.com",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 2,
                ReservationTime = DateTime.Now.AddDays(5),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = 0
            };
            DbContext.Orders.Add(order);
            await DbContext.SaveChangesAsync();
            var orderId = order.Id;

            var orderRepository = new OrderRepository(DbContext);
            var firebaseService = new FirebaseService();
            var orderService = new OrderService(orderRepository, firebaseService);

            var result = await orderService.RemoveOrderAsync(orderId);

            Assert.True(result);
            var deletedOrder = await DbContext.Orders.FindAsync(orderId);
            Assert.Null(deletedOrder);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_OA_007_ChangeOrderStatusAsync_ShouldUpdateOrderStatusAndSendNotification()
    {
        // Test Case ID: TC-OA-007
        // Mục tiêu: Kiểm tra ChangeOrderStatusAsync cập nhật status thành công.
        await BeginTransactionAsync();

        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();

            var order = new Order
            {
                Name = "Status Update Test",
                PhoneNumber = "0444444444",
                Email = "status@example.com",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 3,
                ReservationTime = DateTime.Now.AddDays(6),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = 0
            };
            DbContext.Orders.Add(order);
            await DbContext.SaveChangesAsync();

            var orderRepository = new OrderRepository(DbContext);
            var firebaseService = new FirebaseService();
            var orderService = new OrderService(orderRepository, firebaseService);

            var result = await orderService.ChangeOrderStatusAsync(order.Id, 1);

            Assert.True(result);
            var updatedOrder = await DbContext.Orders.FindAsync(order.Id);
            Assert.NotNull(updatedOrder);
            Assert.Equal(1, updatedOrder.Status);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_OA_008_GetOrdersByUserIdAsync_ShouldReturnAllOrdersForUser()
    {
        // Test Case ID: TC-OA-008
        // Mục tiêu: Kiểm tra GetOrdersByUserIdAsync trả về tất cả Order của User.
        await BeginTransactionAsync();

        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();

            var order1 = new Order
            {
                Name = "User Order 1",
                PhoneNumber = "0555555555",
                Email = "order1@example.com",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 2,
                ReservationTime = DateTime.Now.AddDays(7),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = 0
            };

            var order2 = new Order
            {
                Name = "User Order 2",
                PhoneNumber = "0666666666",
                Email = "order2@example.com",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 4,
                ReservationTime = DateTime.Now.AddDays(8),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = 1
            };

            DbContext.Orders.AddRange(order1, order2);
            await DbContext.SaveChangesAsync();

            var orderRepository = new OrderRepository(DbContext);
            var firebaseService = new FirebaseService();
            var orderService = new OrderService(orderRepository, firebaseService);

            var userOrders = await orderService.GetOrdersByUserIdAsync(userId);

            Assert.NotEmpty(userOrders);
            Assert.Equal(2, userOrders.Count);
            Assert.All(userOrders, o => Assert.Equal(userId, o.UserId));
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_OA_009_GetOrdersByRestaurantIdAsync_ShouldReturnAllOrdersForRestaurant()
    {
        // Test Case ID: TC-OA-009
        // Mục tiêu: Kiểm tra GetOrdersByRestaurantIdAsync trả về tất cả Order của nhà hàng.
        await BeginTransactionAsync();

        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();

            var order1 = new Order
            {
                Name = "Restaurant Order 1",
                PhoneNumber = "0777777777",
                Email = "restorder1@example.com",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 3,
                ReservationTime = DateTime.Now.AddDays(9),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = 0
            };

            var order2 = new Order
            {
                Name = "Restaurant Order 2",
                PhoneNumber = "0888888888",
                Email = "restorder2@example.com",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 5,
                ReservationTime = DateTime.Now.AddDays(10),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = 0
            };

            DbContext.Orders.AddRange(order1, order2);
            await DbContext.SaveChangesAsync();

            var orderRepository = new OrderRepository(DbContext);
            var firebaseService = new FirebaseService();
            var orderService = new OrderService(orderRepository, firebaseService);

            var restaurantOrders = await orderService.GetOrdersByRestaurantIdAsync(restaurantId);

            Assert.NotEmpty(restaurantOrders);
            Assert.Equal(2, restaurantOrders.Count);
            Assert.All(restaurantOrders, o => Assert.Equal(restaurantId, o.RestaurantId));
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }
}
