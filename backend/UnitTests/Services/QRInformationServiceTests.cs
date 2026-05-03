using BusinessLogicLayer.Services;
using DataAccessLayer.Context;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using UnitTests.Infrastructure;
using Xunit;

namespace UnitTests.Services;

public class QRInformationServiceTests
{
    /// <summary>
    /// TC-QR-001
    /// Quet QR dung lan dau thi luu thong tin quet thanh cong
    /// </summary>
    [Fact]
    public async Task AddQRInformationAsync_InsertNewRecord()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new QRInformationRepository(context);
        var service = new QRInformationService(repository);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            var dto = new QRInformationDto
            {
                UserId = userId,
                RestaurantId = restaurantId,
                CreateTime = 1714723200
            };

            await service.AddQRInformationAsync(dto);

            var saved = await context.QRInformations
                .FirstOrDefaultAsync(q => q.UserId == userId && q.RestaurantId == restaurantId);

            Assert.NotNull(saved);
            Assert.Equal(1714723200, saved!.CreateTime);
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var remaining = await verifyContext.QRInformations.CountAsync();
        Assert.Equal(0, remaining);
    }

    /// <summary>
    /// TC-QR-002
    /// Quet lai QR da ton tai thi cap nhat thoi diem quet moi
    /// </summary>
    [Fact]
    public async Task AddQRInformationAsync_UpdateExistingRecord()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new QRInformationRepository(context);
        var service = new QRInformationService(repository);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            context.QRInformations.Add(new QRInformation
            {
                UserId = userId,
                RestaurantId = restaurantId,
                CreateTime = 1700000000
            });
            await context.SaveChangesAsync();

            var dto = new QRInformationDto
            {
                UserId = userId,
                RestaurantId = restaurantId,
                CreateTime = 1714723999
            };

            await service.AddQRInformationAsync(dto);

            var saved = await context.QRInformations
                .Where(q => q.UserId == userId && q.RestaurantId == restaurantId)
                .ToListAsync();

            Assert.Single(saved);
            Assert.Equal(1714723999, saved[0].CreateTime);
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var remaining = await verifyContext.QRInformations.CountAsync();
        Assert.Equal(0, remaining);
    }

    /// <summary>
    /// TC-QR-SVC-009
    /// Quet lai voi thoi diem cu hon thi van cap nhat theo hanh vi hien tai
    /// </summary>
    [Fact]
    public async Task AddQRInformationAsync_UpdateWithOlderTime_StillUpdates()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new QRInformationRepository(context);
        var service = new QRInformationService(repository);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            context.QRInformations.Add(new QRInformation
            {
                UserId = userId,
                RestaurantId = restaurantId,
                CreateTime = 1714725000
            });
            await context.SaveChangesAsync();

            await service.AddQRInformationAsync(new QRInformationDto
            {
                UserId = userId,
                RestaurantId = restaurantId,
                CreateTime = 1714724000
            });

            var saved = await context.QRInformations.FirstAsync(q => q.UserId == userId && q.RestaurantId == restaurantId);
            Assert.Equal(1714724000, saved.CreateTime);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-QR-003
    /// Xem lich su quet theo tai khoan thi chi lay dung du lieu cua nguoi do
    /// </summary>
    [Fact]
    public async Task GetQRInformationAsync_FilterByUser()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new QRInformationRepository(context);
        var service = new QRInformationService(repository);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            var otherUser = new User
            {
                Email = "nguoidung2@test.vn",
                Password = "matkhau123",
                Role = "customer",
                Name = "Le Van C",
                PhoneNumber = "0911222333",
                Status = 1
            };
            context.Users.Add(otherUser);
            await context.SaveChangesAsync();

            context.QRInformations.AddRange(
                new QRInformation
                {
                    UserId = userId,
                    RestaurantId = restaurantId,
                    CreateTime = 1714723200
                },
                new QRInformation
                {
                    UserId = otherUser.Id,
                    RestaurantId = restaurantId,
                    CreateTime = 1714723999
                });
            await context.SaveChangesAsync();

            var result = await service.GetQRInformationAsync(userId, null);

            Assert.Single(result);
            Assert.Equal(userId, result.First().UserId);
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var remaining = await verifyContext.QRInformations.CountAsync();
        Assert.Equal(0, remaining);
    }

    /// <summary>
    /// TC-QR-004
    /// Lay lich su quet theo nha hang thi tra ve dung danh sach
    /// </summary>
    [Fact]
    public async Task GetQRInformationAsync_FilterByRestaurant()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new QRInformationRepository(context);
        var service = new QRInformationService(repository);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            context.QRInformations.Add(new QRInformation
            {
                UserId = userId,
                RestaurantId = restaurantId,
                CreateTime = 1714724200
            });
            await context.SaveChangesAsync();

            var result = await service.GetQRInformationAsync(null, restaurantId);

            Assert.Single(result);
            Assert.Equal(restaurantId, result.First().RestaurantId);
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
        var remaining = await verifyContext.QRInformations.CountAsync();
        Assert.Equal(0, remaining);
    }

    /// <summary>
    /// TC-QR-SVC-010
    /// Loc theo ca userId va restaurantId thi chi tra ve dung giao cat
    /// </summary>
    [Fact]
    public async Task GetQRInformationAsync_FilterByUserAndRestaurant()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new QRInformationRepository(context);
        var service = new QRInformationService(repository);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            var secondUser = new User
            {
                Email = "nguoidung3@test.vn",
                Password = "matkhau123",
                Role = "customer",
                Name = "Pham Thi E",
                PhoneNumber = "0909555666",
                Status = 1
            };
            context.Users.Add(secondUser);
            await context.SaveChangesAsync();

            context.QRInformations.AddRange(
                new QRInformation { UserId = userId, RestaurantId = restaurantId, CreateTime = 1714725100 },
                new QRInformation { UserId = secondUser.Id, RestaurantId = restaurantId, CreateTime = 1714725200 });
            await context.SaveChangesAsync();

            var result = await service.GetQRInformationAsync(userId, restaurantId);

            Assert.Single(result);
            Assert.Equal(userId, result.First().UserId);
            Assert.Equal(restaurantId, result.First().RestaurantId);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-QR-SVC-011
    /// Khong truyen bo loc thi tra ve toan bo du lieu
    /// </summary>
    [Fact]
    public async Task GetQRInformationAsync_NoFilter_ReturnsAll()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new QRInformationRepository(context);
        var service = new QRInformationService(repository);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            var secondUser = new User
            {
                Email = "nguoidung4@test.vn",
                Password = "matkhau123",
                Role = "customer",
                Name = "Tran Thi D",
                PhoneNumber = "0909333444",
                Status = 1
            };
            context.Users.Add(secondUser);
            await context.SaveChangesAsync();

            context.QRInformations.AddRange(
                new QRInformation { UserId = userId, RestaurantId = restaurantId, CreateTime = 1714725300 },
                new QRInformation { UserId = secondUser.Id, RestaurantId = restaurantId, CreateTime = 1714725400 });
            await context.SaveChangesAsync();

            var result = await service.GetQRInformationAsync(null, null);

            Assert.Equal(2, result.Count());
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-QR-SVC-012
    /// Truy van voi bo loc am thi tra ve rong
    /// </summary>
    [Theory]
    [InlineData(-1, null)]
    [InlineData(null, -1)]
    [InlineData(-1, -1)]
    public async Task GetQRInformationAsync_InvalidFilters_ReturnsEmpty(int? userId, int? restaurantId)
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new QRInformationRepository(context);
        var service = new QRInformationService(repository);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var result = await service.GetQRInformationAsync(userId, restaurantId);
            Assert.Empty(result);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-QR-005
    /// Khong co du lieu quet thi tra ve danh sach trong
    /// </summary>
    [Fact]
    public async Task GetQRInformationAsync_ReturnEmptyWhenNoData()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new QRInformationRepository(context);
        var service = new QRInformationService(repository);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            var result = await service.GetQRInformationAsync(userId, restaurantId);

            Assert.Empty(result);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-QR-006
    /// Xoa ban ghi quet ton tai thi thanh cong
    /// </summary>
    [Fact]
    public async Task DeleteQRInformationAsync_DeleteExisting()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new QRInformationRepository(context);
        var service = new QRInformationService(repository);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var (userId, restaurantId) = await SeedUserRestaurantAsync(context);

            var qr = new QRInformation
            {
                UserId = userId,
                RestaurantId = restaurantId,
                CreateTime = 1714724300
            };
            context.QRInformations.Add(qr);
            await context.SaveChangesAsync();

            await service.DeleteQRInformationAsync(qr.Id);

            Assert.Equal(0, await context.QRInformations.CountAsync());
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-QR-007
    /// Xoa ban ghi quet khong ton tai thi khong thay doi du lieu
    /// </summary>
    [Fact]
    public async Task DeleteQRInformationAsync_NoChangeWhenNotFound()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new QRInformationRepository(context);
        var service = new QRInformationService(repository);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await service.DeleteQRInformationAsync(9999);

            Assert.Equal(0, await context.QRInformations.CountAsync());
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-QR-SVC-013
    /// Xoa ban ghi voi ID am hoac bang 0 thi khong thay doi du lieu
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task DeleteQRInformationAsync_InvalidId_NoChange(int id)
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        var repository = new QRInformationRepository(context);
        var service = new QRInformationService(repository);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await service.DeleteQRInformationAsync(id);
            Assert.Equal(0, await context.QRInformations.CountAsync());
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
