using DataAccessLayer.Context;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using UnitTests.Infrastructure;
using Xunit;

namespace UnitTests.Repositories;

public class OrderRepositoryTests
{
    /// <summary>
    /// TC-ORD-001
    /// Them moi yeu cau dat ban thi luu thanh cong vao co so du lieu
    /// </summary>
    [Fact]
    public async Task AddOrderAsync_InsertSuccess()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var repository = new OrderRepository(context);

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
                ReservationTime = "2026-05-10 19:00",
                SpecialRequest = "Ban gan cua so",
                CreatedAt = 1714723200,
                Status = 0
            };

            var saved = await repository.AddOrderAsync(order);

            Assert.True(saved.Id > 0);
            Assert.Equal(1, await context.Orders.CountAsync());
            Assert.Equal("Ban gan cua so", saved.SpecialRequest);
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        Assert.Equal(0, await verifyContext.Orders.CountAsync());
    }

    /// <summary>
    /// TC-ORD-004
    /// Them moi yeu cau dat ban khong co ghi chu dac biet thi van luu thanh cong
    /// </summary>
    [Fact]
    public async Task AddOrderAsync_InsertWithoutSpecialRequest()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var repository = new OrderRepository(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            var order = new Order
            {
                Name = "Tran Thi D",
                PhoneNumber = "0909333444",
                Email = "khach@test.vn",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 2,
                ReservationTime = "2026-05-10 12:00",
                CreatedAt = 1714723250,
                Status = 0
            };

            var saved = await repository.AddOrderAsync(order);

            Assert.True(saved.Id > 0);
            Assert.Null(saved.SpecialRequest);
            Assert.Equal(1, await context.Orders.CountAsync());
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        Assert.Equal(0, await verifyContext.Orders.CountAsync());
    }

    /// <summary>
    /// TC-ORD-008
    /// Them moi yeu cau dat ban thi luu dung thoi gian dat ban
    /// </summary>
    [Fact]
    public async Task AddOrderAsync_SaveReservationTime()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var repository = new OrderRepository(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            var order = new Order
            {
                Name = "Pham Thi E",
                PhoneNumber = "0909555666",
                Email = "khach2@test.vn",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 6,
                ReservationTime = "2026-05-15 20:30",
                CreatedAt = 1714723700,
                Status = 0
            };

            var saved = await repository.AddOrderAsync(order);

            Assert.Equal("2026-05-15 20:30", saved.ReservationTime);
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        Assert.Equal(0, await verifyContext.Orders.CountAsync());
    }

    /// <summary>
    /// TC-ORD-002
    /// Cap nhat trang thai yeu cau dat ban thi luu lai trang thai moi
    /// </summary>
    [Fact]
    public async Task UpdateOrderStatusAsync_UpdateStatus()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var repository = new OrderRepository(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            var order = new Order
            {
                Name = "Le Van C",
                PhoneNumber = "0911222333",
                Email = "khach@test.vn",
                UserId = userId,
                RestaurantId = restaurantId,
                NumOfMembers = 2,
                ReservationTime = "2026-05-11 12:00",
                CreatedAt = 1714723300,
                Status = 0
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var result = await repository.UpdateOrderStatusAsync(order.Id, 1);

            Assert.True(result);
            var updated = await context.Orders.FindAsync(order.Id);
            Assert.NotNull(updated);
            Assert.Equal(1, updated!.Status);
            Assert.NotNull(updated.UpdatedAt);
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        Assert.Equal(0, await verifyContext.Orders.CountAsync());
    }

    /// <summary>
    /// TC-ORD-010
    /// Cap nhat trang thai yeu cau dat ban khong ton tai thi tra ve khong thanh cong
    /// </summary>
    [Fact]
    public async Task UpdateOrderStatusAsync_ReturnFalseWhenNotFound()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var repository = new OrderRepository(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var result = await repository.UpdateOrderStatusAsync(9999, 1);

            Assert.False(result);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-ORD-003
    /// Lay danh sach dat ban theo nguoi dung thi chi tra ve du lieu cua nguoi do
    /// </summary>
    [Fact]
    public async Task GetOrdersByUserIdAsync_FilterByUser()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var repository = new OrderRepository(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            var otherUser = new User
            {
                Email = "nguoidung2@test.vn",
                Password = "matkhau123",
                Role = "customer",
                Name = "Tran Thi D",
                PhoneNumber = "0909333444",
                Status = 1
            };
            context.Users.Add(otherUser);
            await context.SaveChangesAsync();

            context.Orders.AddRange(
                new Order
                {
                    Name = "Nguyen Van B",
                    PhoneNumber = "0909123456",
                    Email = "nguoidung@test.vn",
                    UserId = userId,
                    RestaurantId = restaurantId,
                    NumOfMembers = 3,
                    ReservationTime = "2026-05-12 18:30",
                    CreatedAt = 1714723400,
                    Status = 0
                },
                new Order
                {
                    Name = "Tran Thi D",
                    PhoneNumber = "0909333444",
                    Email = "nguoidung2@test.vn",
                    UserId = otherUser.Id,
                    RestaurantId = restaurantId,
                    NumOfMembers = 5,
                    ReservationTime = "2026-05-12 20:00",
                    CreatedAt = 1714723500,
                    Status = 0
                });
            await context.SaveChangesAsync();

            var result = await repository.GetOrdersByUserIdAsync(userId);

            Assert.Single(result);
            Assert.Equal(userId, result.First()!.UserId);
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        Assert.Equal(0, await verifyContext.Orders.CountAsync());
    }

    /// <summary>
    /// TC-ORD-005
    /// Nguoi dung khong co yeu cau dat ban thi tra ve danh sach trong
    /// </summary>
    [Fact]
    public async Task GetOrdersByUserIdAsync_ReturnEmptyWhenNoOrders()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var repository = new OrderRepository(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, _) = await SeedUserRestaurantAsync(context);

            var result = await repository.GetOrdersByUserIdAsync(userId);

            Assert.Empty(result);
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        Assert.Equal(0, await verifyContext.Orders.CountAsync());
    }

    /// <summary>
    /// TC-ORD-009
    /// Nguoi dung co nhieu yeu cau dat ban thi tra ve day du danh sach
    /// </summary>
    [Fact]
    public async Task GetOrdersByUserIdAsync_ReturnMultipleOrders()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var repository = new OrderRepository(context);

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
                    ReservationTime = "2026-05-16 18:00",
                    CreatedAt = 1714723800,
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
                    ReservationTime = "2026-05-17 19:30",
                    CreatedAt = 1714723900,
                    Status = 0
                });
            await context.SaveChangesAsync();

            var result = await repository.GetOrdersByUserIdAsync(userId);

            Assert.Equal(2, result.Count());
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        Assert.Equal(0, await verifyContext.Orders.CountAsync());
    }

    /// <summary>
    /// TC-ORD-011
    /// Nha hang co nhieu yeu cau dat ban thi tra ve day du danh sach
    /// </summary>
    [Fact]
    public async Task GetOrdersByRestaurantIdAsync_ReturnMultipleOrders()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var repository = new OrderRepository(context);

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
                    ReservationTime = "2026-05-18 18:00",
                    CreatedAt = 1714724000,
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
                    ReservationTime = "2026-05-19 19:30",
                    CreatedAt = 1714724100,
                    Status = 0
                });
            await context.SaveChangesAsync();

            var result = await repository.GetOrdersByRestaurantIdAsync(restaurantId);

            Assert.Equal(2, result.Count());
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        Assert.Equal(0, await verifyContext.Orders.CountAsync());
    }

    /// <summary>
    /// TC-ORD-012
    /// Nha hang chua co yeu cau dat ban thi tra ve danh sach trong
    /// </summary>
    [Fact]
    public async Task GetOrdersByRestaurantIdAsync_ReturnEmptyWhenNoOrders()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var repository = new OrderRepository(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (_, restaurantId) = await SeedUserRestaurantAsync(context);

            var result = await repository.GetOrdersByRestaurantIdAsync(restaurantId);

            Assert.Empty(result);
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        Assert.Equal(0, await verifyContext.Orders.CountAsync());
    }

    /// <summary>
    /// TC-ORD-006
    /// Huy yeu cau dat ban ton tai thi xoa thanh cong
    /// </summary>
    [Fact]
    public async Task RemoveOrderAsync_DeleteSuccess()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var repository = new OrderRepository(context);

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
                ReservationTime = "2026-05-11 19:00",
                CreatedAt = 1714723600,
                Status = 0
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var result = await repository.DeleteOrderAsync(order.Id);

            Assert.True(result);
            Assert.Equal(0, await context.Orders.CountAsync());
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        Assert.Equal(0, await verifyContext.Orders.CountAsync());
    }

    /// <summary>
    /// TC-ORD-007
    /// Huy yeu cau dat ban khong ton tai thi thong bao khong thanh cong
    /// </summary>
    [Fact]
    public async Task RemoveOrderAsync_ReturnFalseWhenNotFound()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var repository = new OrderRepository(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var result = await repository.DeleteOrderAsync(9999);

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
