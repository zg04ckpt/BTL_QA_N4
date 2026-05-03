using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.Services;
using DataAccessLayer.Context;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using UnitTests.Infrastructure;
using Xunit;

namespace UnitTests.Services;

public class OrderServiceTests
{
    /// <summary>
    /// TC-ORD-SVC-001
    /// Them moi yeu cau dat ban thanh cong va gui thong bao cho nha hang
    /// </summary>
    [Fact]
    public async Task AddOrderAsync_Success_SendsNotification()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            var dto = new AddOrderDto
            {
                Name = "Nguyen Van B",
                PhoneNumber = "0909123456",
                Email = "nguoidung@test.vn",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 4,
                ReservationTime = "2026-05-20 19:00",
                SpecialRequest = "Ban gan cua so",
                CreatedAt = 1714725200
            };

            await service.AddOrderAsync(dto);

            Assert.Equal(1, await context.Orders.CountAsync());
            Assert.Single(firebase.SentTopics);
            Assert.Equal($"admin_{restaurantId}", firebase.SentTopics[0]);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-SVC-013
    /// Them yeu cau dat ban khong co ghi chu thi van luu thanh cong
    /// </summary>
    [Fact]
    public async Task AddOrderAsync_WithoutSpecialRequest_SavesSuccessfully()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            var dto = new AddOrderDto
            {
                Name = "Tran Thi D",
                PhoneNumber = "0909333444",
                Email = "khach@test.vn",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 2,
                ReservationTime = "2026-05-20 12:00",
                CreatedAt = 1714725250,
                SpecialRequest = null
            };

            await service.AddOrderAsync(dto);

            var saved = await context.Orders.FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Null(saved!.SpecialRequest);
            Assert.Single(firebase.SentTopics);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-SVC-014
    /// Them yeu cau dat ban voi so luong nguoi toi thieu thi van luu thanh cong
    /// </summary>
    [Fact]
    public async Task AddOrderAsync_MinMembers_SavesSuccessfully()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            var dto = new AddOrderDto
            {
                Name = "Le Van C",
                PhoneNumber = "0911222333",
                Email = "khach2@test.vn",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 1,
                ReservationTime = "2026-05-21 11:00",
                CreatedAt = 1714725260
            };

            await service.AddOrderAsync(dto);

            var saved = await context.Orders.FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal(1, saved!.NumOfMembers);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-SVC-002
    /// Nha hang chap nhan yeu cau dat ban thi gui thong bao cho khach hang
    /// </summary>
    [Fact]
    public async Task ChangeOrderStatusAsync_Accepted_SendsNotification()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            var order = new Order
            {
                Name = "Nguyen Van B",
                PhoneNumber = "0909123456",
                Email = "nguoidung@test.vn",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 4,
                ReservationTime = "2026-05-21 19:00",
                CreatedAt = 1714725300,
                Status = 0
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var result = await service.ChangeOrderStatusAsync(order.Id, 1);

            Assert.True(result);
            Assert.Single(firebase.SentTopics);
            Assert.Equal($"user_{userId}", firebase.SentTopics[0]);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-SVC-003
    /// Nha hang tu choi yeu cau dat ban thi gui thong bao cho khach hang
    /// </summary>
    [Fact]
    public async Task ChangeOrderStatusAsync_Declined_SendsNotification()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            var order = new Order
            {
                Name = "Nguyen Van B",
                PhoneNumber = "0909123456",
                Email = "nguoidung@test.vn",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 4,
                ReservationTime = "2026-05-22 19:00",
                CreatedAt = 1714725400,
                Status = 0
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var result = await service.ChangeOrderStatusAsync(order.Id, 0);

            Assert.True(result);
            Assert.Single(firebase.SentTopics);
            Assert.Equal($"user_{userId}", firebase.SentTopics[0]);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-SVC-005
    /// Cap nhat trang thai yeu cau dat ban khong ton tai thi tra ve false va khong gui thong bao
    /// </summary>
    [Fact]
    public async Task ChangeOrderStatusAsync_NotFound_ThrowsNullReferenceException()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();

    
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await Assert.ThrowsAsync<NullReferenceException>(() => service.ChangeOrderStatusAsync(9999, 1));
            Assert.Empty(firebase.SentTopics);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-SVC-015
    /// Cap nhat trang thai voi ID am thi nem loi theo hanh vi hien tai
    /// </summary>
    [Fact]
    public async Task ChangeOrderStatusAsync_InvalidNegativeId_ThrowsNullReferenceException()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await Assert.ThrowsAsync<NullReferenceException>(() => service.ChangeOrderStatusAsync(-1, 1));
            Assert.Empty(firebase.SentTopics);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-SVC-004
    /// Tra cuu yeu cau dat ban khong ton tai thi tra ve null
    /// </summary>
    [Fact]
    public async Task GetOrderByIdAsync_NotFound_ReturnsNull()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var result = await service.GetOrderByIdAsync(9999);

            Assert.Null(result);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-SVC-016
    /// Tra cuu theo ID am thi tra ve null
    /// </summary>
    [Fact]
    public async Task GetOrderByIdAsync_NegativeId_ReturnsNull()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var result = await service.GetOrderByIdAsync(-1);
            Assert.Null(result);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-SVC-017
    /// Tra cuu theo ID bang 0 thi tra ve null
    /// </summary>
    [Fact]
    public async Task GetOrderByIdAsync_ZeroId_ReturnsNull()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var result = await service.GetOrderByIdAsync(0);
            Assert.Null(result);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-SVC-006
    /// Tra cuu yeu cau dat ban ton tai thi map day du thong tin
    /// </summary>
    [Fact]
    public async Task GetOrderByIdAsync_Found_ReturnsMappedDto()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            var order = new Order
            {
                Name = "Nguyen Van B",
                PhoneNumber = "0909123456",
                Email = "nguoidung@test.vn",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 4,
                ReservationTime = "2026-05-23 19:00",
                SpecialRequest = "Tang 2",
                CreatedAt = 1714725500,
                UpdatedAt = DateTime.Now,
                Status = 0
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var result = await service.GetOrderByIdAsync(order.Id);

            Assert.NotNull(result);
            Assert.Equal(order.Id, result!.Id);
            Assert.Equal("Nguyen Van B", result.Name);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(restaurantId, result.RestaurantId);
            Assert.NotNull(result.User);
            Assert.NotNull(result.Restaurant);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-SVC-007
    /// Huy yeu cau dat ban ton tai thi tra ve true
    /// </summary>
    [Fact]
    public async Task RemoveOrderAsync_Found_ReturnsTrue()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            var order = new Order
            {
                Name = "Nguyen Van B",
                PhoneNumber = "0909123456",
                Email = "nguoidung@test.vn",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 2,
                ReservationTime = "2026-05-24 12:00",
                CreatedAt = 1714725600,
                Status = 0
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var result = await service.RemoveOrderAsync(order.Id);

            Assert.True(result);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-SVC-008
    /// Huy yeu cau dat ban khong ton tai thi tra ve false
    /// </summary>
    [Fact]
    public async Task RemoveOrderAsync_NotFound_ReturnsFalse()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var result = await service.RemoveOrderAsync(9999);

            Assert.False(result);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-SVC-018
    /// Huy yeu cau voi ID am thi tra ve false
    /// </summary>
    [Fact]
    public async Task RemoveOrderAsync_NegativeId_ReturnsFalse()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var result = await service.RemoveOrderAsync(-1);
            Assert.False(result);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-SVC-019
    /// Huy yeu cau voi ID bang 0 thi tra ve false
    /// </summary>
    [Fact]
    public async Task RemoveOrderAsync_ZeroId_ReturnsFalse()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var result = await service.RemoveOrderAsync(0);
            Assert.False(result);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-SVC-009
    /// Lay danh sach dat ban theo nguoi dung co du lieu thi map day du
    /// </summary>
    [Fact]
    public async Task GetOrdersByUserIdAsync_HasOrders_ReturnsMappedList()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            context.Orders.AddRange(
                new Order
                {
                    Name = "Nguyen Van B",
                    PhoneNumber = "0909123456",
                    Email = "nguoidung@test.vn",
                    UserId = userId,
                    RestaurantId = restaurantId,
                    NumOfMembers = 2,
                    ReservationTime = "2026-05-25 18:00",
                    CreatedAt = 1714725700,
                    Status = 0
                },
                new Order
                {
                    Name = "Nguyen Van B",
                    PhoneNumber = "0909123456",
                    Email = "nguoidung@test.vn",
                    UserId = userId,
                    RestaurantId = restaurantId,
                    NumOfMembers = 5,
                    ReservationTime = "2026-05-25 20:00",
                    CreatedAt = 1714725800,
                    Status = 0
                });
            await context.SaveChangesAsync();

            var result = await service.GetOrdersByUserIdAsync(userId);

            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.Equal(userId, item.UserId));
            Assert.All(result, item => Assert.NotNull(item.User));
            Assert.All(result, item => Assert.NotNull(item.Restaurant));
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-SVC-010
    /// Lay danh sach dat ban theo nguoi dung khong co du lieu thi tra ve rong
    /// </summary>
    [Fact]
    public async Task GetOrdersByUserIdAsync_NoOrders_ReturnsEmptyList()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, _) = await SeedUserRestaurantAsync(context);

            var result = await service.GetOrdersByUserIdAsync(userId);

            Assert.Empty(result);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-SVC-020
    /// Lay danh sach theo UserId am thi tra ve rong
    /// </summary>
    [Fact]
    public async Task GetOrdersByUserIdAsync_NegativeUserId_ReturnsEmptyList()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var result = await service.GetOrdersByUserIdAsync(-1);
            Assert.Empty(result);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-SVC-011
    /// Lay danh sach dat ban theo nha hang co du lieu thi map day du
    /// </summary>
    [Fact]
    public async Task GetOrdersByRestaurantIdAsync_HasOrders_ReturnsMappedList()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            context.Orders.AddRange(
                new Order
                {
                    Name = "Nguyen Van B",
                    PhoneNumber = "0909123456",
                    Email = "nguoidung@test.vn",
                    UserId = userId,
                    RestaurantId = restaurantId,
                    NumOfMembers = 3,
                    ReservationTime = "2026-05-26 18:30",
                    CreatedAt = 1714725900,
                    Status = 0
                },
                new Order
                {
                    Name = "Nguyen Van B",
                    PhoneNumber = "0909123456",
                    Email = "nguoidung@test.vn",
                    UserId = userId,
                    RestaurantId = restaurantId,
                    NumOfMembers = 4,
                    ReservationTime = "2026-05-26 20:00",
                    CreatedAt = 1714726000,
                    Status = 0
                });
            await context.SaveChangesAsync();

            var result = await service.GetOrdersByRestaurantIdAsync(restaurantId);

            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.Equal(restaurantId, item.RestaurantId));
            Assert.All(result, item => Assert.NotNull(item.User));
            Assert.All(result, item => Assert.NotNull(item.Restaurant));
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-SVC-012
    /// Lay danh sach dat ban theo nha hang khong co du lieu thi tra ve rong
    /// </summary>
    [Fact]
    public async Task GetOrdersByRestaurantIdAsync_NoOrders_ReturnsEmptyList()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (_, restaurantId) = await SeedUserRestaurantAsync(context);

            var result = await service.GetOrdersByRestaurantIdAsync(restaurantId);

            Assert.Empty(result);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-SVC-021
    /// Lay danh sach theo RestaurantId am thi tra ve rong
    /// </summary>
    [Fact]
    public async Task GetOrdersByRestaurantIdAsync_NegativeRestaurantId_ReturnsEmptyList()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var result = await service.GetOrdersByRestaurantIdAsync(-1);
            Assert.Empty(result);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    private sealed class FakeFirebaseService : IFirebaseService
    {
        public List<string> SentTopics { get; } = new();

        public Task<string> SendNotificationToTopicAsync(string topic, string title, string body)
        {
            SentTopics.Add(topic);
            return Task.FromResult("fake-message-id");
        }
    }

    /// <summary>
    /// TC-ORD-SVC-022
    /// Mong muon: Cap nhat trang thai khong ton tai tra ve false (test nay se fail neu code hien tai nem NullReference)
    /// </summary>
    [Fact]
    public async Task ChangeOrderStatusAsync_NotFound_ReturnsFalse_Expect()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new OrderRepository(context);
        var firebase = new FakeFirebaseService();
        var service = new OrderService(repository, firebase);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var result = await service.ChangeOrderStatusAsync(9999, 1);

            // Expected behavior: return false when order not found
            Assert.False(result);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    private static async Task<(int userId, int restaurantId)> SeedUserRestaurantAsync(ApplicationDbContext context)
    {
        var address = new Address
        {
            City = "Ha Noi",
            District = "Cau Giay",
            Ward = "Dich Vong",
            Detail = "So 1"
        };

        var category = new Category
        {
            Name = "Quan an"
        };

        var user = new User
        {
            Email = "nguoidung@test.vn",
            Password = "matkhau123",
            Role = "customer",
            Name = "Nguyen Van B",
            PhoneNumber = "0909123456",
            Status = 1
        };

        context.AddRange(address, category, user);
        await context.SaveChangesAsync();

        var restaurantAddress = new Address
        {
            City = "Ha Noi",
            District = "Ba Dinh",
            Ward = "Ngoc Ha",
            Detail = "So 10"
        };

        context.Add(restaurantAddress);
        await context.SaveChangesAsync();

        var restaurant = new Restaurant
        {
            Name = "Nha Hang Pho",
            Status = 1,
            Email = "nhahang@test.vn",
            PhoneNumber = "0909888777",
            UserId = user.Id,
            CateId = category.Id,
            AddressId = restaurantAddress.Id
        };

        context.Restaurants.Add(restaurant);
        await context.SaveChangesAsync();

        return (user.Id, restaurant.Id);
    }
}
