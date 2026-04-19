using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using Xunit;

namespace BusinessLogicLayer.Tests;

public class OrderServiceIntegrationTests : TestDatabaseFixture
{
    private static int _seed;

    private async Task<int> CreateTestAddress()
    {
        var token = Interlocked.Increment(ref _seed);
        var address = new Address
        {
            City = $"Order City {token}",
            District = "District 1",
            Ward = "Ward 1",
            Detail = "Detail",
            Lon = 106.7,
            Lat = 10.8
        };
        DbContext.Addresses.Add(address);
        await DbContext.SaveChangesAsync();
        return address.Id;
    }

    private async Task<int> CreateTestCategory()
    {
        var token = Interlocked.Increment(ref _seed);
        var category = new Category { Name = $"Order Category {token}" };
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();
        return category.Id;
    }

    private async Task<int> CreateTestUser()
    {
        var token = Interlocked.Increment(ref _seed);
        var addressId = await CreateTestAddress();

        var user = new User
        {
            Name = $"Test User {token}",
            PhoneNumber = ("01" + token.ToString("D8")).Substring(0, 10),
            Email = $"testuser{token}@example.com",
            Password = "Password@123",
            Role = "User",
            Status = 1,
            AddressId = addressId
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
        return user.Id;
    }

    private async Task<int> CreateTestRestaurant()
    {
        var token = Interlocked.Increment(ref _seed);
        var ownerId = await CreateTestUser();
        var categoryId = await CreateTestCategory();
        var addressId = await CreateTestAddress();

        var restaurant = new Restaurant
        {
            Name = $"Test Restaurant {token}",
            PhoneNumber = ("02" + token.ToString("D8")).Substring(0, 10),
            Email = $"restaurant{token}@example.com",
            Status = 1,
            UserId = ownerId,
            CateId = categoryId,
            AddressId = addressId
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

    [Fact]
    public async Task TC_OA_010_AddOrderAsync_ShouldAcceptReservationTimeAsMinValueByCurrentLogic()
    {
        // Test Case ID: TC-OA-010
        await BeginTransactionAsync();
        try
        {
            var service = new OrderService(new OrderRepository(DbContext), new FirebaseService());
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();

            await service.AddOrderAsync(new AddOrderDto
            {
                Name = "Min Time",
                PhoneNumber = "0123000000",
                Email = "mintime@example.com",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 1,
                ReservationTime = DateTime.MinValue,
                CreatedAt = DateTime.Now
            });

            Assert.NotEmpty(await DbContext.Orders.ToListAsync());
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_OA_011_AddOrderAsync_ShouldPersistLongTextFieldsByCurrentLogic()
    {
        // Test Case ID: TC-OA-011
        await BeginTransactionAsync();
        try
        {
            var service = new OrderService(new OrderRepository(DbContext), new FirebaseService());
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();
            var longText = new string('A', 200);

            await service.AddOrderAsync(new AddOrderDto
            {
                Name = longText,
                PhoneNumber = "0123999999",
                Email = "long@example.com",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 2,
                ReservationTime = DateTime.Now.AddDays(1),
                SpecialRequest = longText,
                CreatedAt = DateTime.Now
            });

            var saved = await DbContext.Orders.OrderByDescending(o => o.Id).FirstAsync();
            Assert.Equal(longText, saved.Name);
            Assert.Equal(longText, saved.SpecialRequest);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_GOBRIA_001_GetOrdersByRestaurantIdAsync_ShouldReturnOrdersForExistingRestaurant()
    {
        // Test Case ID: TC-GOBRIA-001
        await BeginTransactionAsync();
        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();
            DbContext.Orders.Add(new Order
            {
                Name = "R1",
                PhoneNumber = "0900000001",
                Email = "r1@example.com",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 2,
                ReservationTime = DateTime.Now,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = 0
            });
            await DbContext.SaveChangesAsync();

            var result = await new OrderService(new OrderRepository(DbContext), new FirebaseService()).GetOrdersByRestaurantIdAsync(restaurantId);
            Assert.NotEmpty(result);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_GOBRIA_002_GetOrdersByRestaurantIdAsync_ShouldReturnEmptyWhenRestaurantHasNoOrders()
    {
        // Test Case ID: TC-GOBRIA-002
        await BeginTransactionAsync();
        try
        {
            var restaurantId = await CreateTestRestaurant();
            var result = await new OrderService(new OrderRepository(DbContext), new FirebaseService()).GetOrdersByRestaurantIdAsync(restaurantId);
            Assert.Empty(result);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_GOBRIA_003_GetOrdersByRestaurantIdAsync_ShouldReturnEmptyForInvalidRestaurantId()
    {
        // Test Case ID: TC-GOBRIA-003
        await BeginTransactionAsync();
        try
        {
            var result = await new OrderService(new OrderRepository(DbContext), new FirebaseService()).GetOrdersByRestaurantIdAsync(999999);
            Assert.Empty(result);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_GOBYIA_001_GetOrdersByUserIdAsync_ShouldReturnOrdersForExistingUser()
    {
        // Test Case ID: TC-GOBYIA-001
        await BeginTransactionAsync();
        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();
            DbContext.Orders.Add(new Order
            {
                Name = "U1",
                PhoneNumber = "0900000002",
                Email = "u1@example.com",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 2,
                ReservationTime = DateTime.Now,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = 0
            });
            await DbContext.SaveChangesAsync();
            var result = await new OrderService(new OrderRepository(DbContext), new FirebaseService()).GetOrdersByUserIdAsync(userId);
            Assert.NotEmpty(result);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_GOBYIA_002_GetOrdersByUserIdAsync_ShouldReturnEmptyWhenUserHasNoOrders()
    {
        // Test Case ID: TC-GOBYIA-002
        await BeginTransactionAsync();
        try
        {
            var userId = await CreateTestUser();
            var result = await new OrderService(new OrderRepository(DbContext), new FirebaseService()).GetOrdersByUserIdAsync(userId);
            Assert.Empty(result);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_GOBYIA_003_GetOrdersByUserIdAsync_ShouldReturnEmptyForInvalidUserId()
    {
        // Test Case ID: TC-GOBYIA-003
        await BeginTransactionAsync();
        try
        {
            var result = await new OrderService(new OrderRepository(DbContext), new FirebaseService()).GetOrdersByUserIdAsync(0);
            Assert.Empty(result);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_COSA_001_ChangeOrderStatusAsync_ShouldSetStatusToAccepted()
    {
        // Test Case ID: TC-COSA-001
        await BeginTransactionAsync();
        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();
            var order = new Order
            {
                Name = "S1",
                PhoneNumber = "0900000003",
                Email = "s1@example.com",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 2,
                ReservationTime = DateTime.Now,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = 0
            };
            DbContext.Orders.Add(order);
            await DbContext.SaveChangesAsync();

            await new OrderService(new OrderRepository(DbContext), new FirebaseService()).ChangeOrderStatusAsync(order.Id, 1);
            var updated = await DbContext.Orders.FindAsync(order.Id);
            Assert.Equal(1, updated!.Status);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_COSA_002_ChangeOrderStatusAsync_ShouldSetStatusToRejected()
    {
        // Test Case ID: TC-COSA-002
        await BeginTransactionAsync();
        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();
            var order = new Order
            {
                Name = "S2",
                PhoneNumber = "0900000004",
                Email = "s2@example.com",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 2,
                ReservationTime = DateTime.Now,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = 1
            };
            DbContext.Orders.Add(order);
            await DbContext.SaveChangesAsync();

            await new OrderService(new OrderRepository(DbContext), new FirebaseService()).ChangeOrderStatusAsync(order.Id, 0);
            var updated = await DbContext.Orders.FindAsync(order.Id);
            Assert.Equal(0, updated!.Status);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_COSA_003_ChangeOrderStatusAsync_ShouldHandleSameStatusValue()
    {
        // Test Case ID: TC-COSA-003
        await BeginTransactionAsync();
        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();
            var order = new Order
            {
                Name = "S3",
                PhoneNumber = "0900000005",
                Email = "s3@example.com",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 2,
                ReservationTime = DateTime.Now,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = 1
            };
            DbContext.Orders.Add(order);
            await DbContext.SaveChangesAsync();
            var result = await new OrderService(new OrderRepository(DbContext), new FirebaseService()).ChangeOrderStatusAsync(order.Id, 1);
            Assert.True(result);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_COSA_004_ChangeOrderStatusAsync_ShouldThrowForInvalidOrderIdByCurrentServiceFlow()
    {
        // Test Case ID: TC-COSA-004
        await BeginTransactionAsync();
        try
        {
            var service = new OrderService(new OrderRepository(DbContext), new FirebaseService());
            await Assert.ThrowsAnyAsync<Exception>(() => service.ChangeOrderStatusAsync(999999, 1));
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_ROA_001_RemoveOrderAsync_ShouldReturnTrueWhenOrderExists()
    {
        // Test Case ID: TC-ROA-001
        await BeginTransactionAsync();
        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();
            var order = new Order
            {
                Name = "Rmv1",
                PhoneNumber = "0900000006",
                Email = "rmv1@example.com",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 2,
                ReservationTime = DateTime.Now,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = 0
            };
            DbContext.Orders.Add(order);
            await DbContext.SaveChangesAsync();

            var result = await new OrderService(new OrderRepository(DbContext), new FirebaseService()).RemoveOrderAsync(order.Id);
            Assert.True(result);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_ROA_002_RemoveOrderAsync_ShouldReturnFalseWhenOrderNotFound()
    {
        // Test Case ID: TC-ROA-002
        await BeginTransactionAsync();
        try
        {
            var result = await new OrderService(new OrderRepository(DbContext), new FirebaseService()).RemoveOrderAsync(999999);
            Assert.False(result);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_ROA_003_RemoveOrderAsync_ShouldReturnFalseForZeroId()
    {
        // Test Case ID: TC-ROA-003
        await BeginTransactionAsync();
        try
        {
            var result = await new OrderService(new OrderRepository(DbContext), new FirebaseService()).RemoveOrderAsync(0);
            Assert.False(result);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_GOBIA_001_GetOrderByIdAsync_ShouldReturnOrderWhenIdExists()
    {
        // Test Case ID: TC-GOBIA-001
        await BeginTransactionAsync();
        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();
            var order = new Order
            {
                Name = "GO1",
                PhoneNumber = "0900000007",
                Email = "go1@example.com",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 2,
                ReservationTime = DateTime.Now,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = 0
            };
            DbContext.Orders.Add(order);
            await DbContext.SaveChangesAsync();

            var dto = await new OrderService(new OrderRepository(DbContext), new FirebaseService()).GetOrderByIdAsync(order.Id);
            Assert.NotNull(dto);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_GOBIA_002_GetOrderByIdAsync_ShouldReturnNullForInvalidId()
    {
        // Test Case ID: TC-GOBIA-002
        await BeginTransactionAsync();
        try
        {
            var dto = await new OrderService(new OrderRepository(DbContext), new FirebaseService()).GetOrderByIdAsync(999999);
            Assert.Null(dto);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_GOBIA_003_GetOrderByIdAsync_ShouldReturnNullForZeroId()
    {
        // Test Case ID: TC-GOBIA-003
        await BeginTransactionAsync();
        try
        {
            var dto = await new OrderService(new OrderRepository(DbContext), new FirebaseService()).GetOrderByIdAsync(0);
            Assert.Null(dto);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_GOBIA_004_GetOrderByIdAsync_ShouldReturnNullForNegativeId()
    {
        // Test Case ID: TC-GOBIA-004
        await BeginTransactionAsync();
        try
        {
            var dto = await new OrderService(new OrderRepository(DbContext), new FirebaseService()).GetOrderByIdAsync(-1);
            Assert.Null(dto);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }
}
