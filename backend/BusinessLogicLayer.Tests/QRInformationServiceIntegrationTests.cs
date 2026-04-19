using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using Xunit;

namespace BusinessLogicLayer.Tests;

public class QRInformationServiceIntegrationTests : TestDatabaseFixture
{
    private static int _seed;

    private static long NowUnixTime()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private static string BuildPhone(string prefix, int token)
    {
        return (prefix + token.ToString("D8")).Substring(0, 10);
    }

    private async Task<int> CreateTestAddress()
    {
        var token = Interlocked.Increment(ref _seed);
        var address = new Address
        {
            City = $"QR City {token}",
            District = "District 1",
            Ward = "Ward 1",
            Detail = "123 Test Detail",
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
        var category = new Category
        {
            Name = $"QR Category {token}"
        };

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
            Name = $"QR Test User {token}",
            PhoneNumber = BuildPhone("01", token),
            Email = $"qruser{token}@example.com",
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
            Name = $"QR Test Restaurant {token}",
            PhoneNumber = BuildPhone("02", token),
            Email = $"qrrestaurant{token}@example.com",
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
    public async Task TC_QIRA_001_AddQRInformationAsync_ShouldCreateQRInformationWithValidData()
    {
        // Test Case ID: TC-QIRA-001
        // Mục tiêu: Kiểm tra tạo mới QRInformation thành công với dữ liệu hợp lệ.
        await BeginTransactionAsync();

        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();

            var qrRepository = new QRInformationRepository(DbContext);
            var qrService = new QRInformationService(qrRepository);

            var qrDto = new QRInformationDto
            {
                UserId = userId,
                RestaurantId = restaurantId,
                CreateTime = NowUnixTime()
            };

            await qrService.AddQRInformationAsync(qrDto);

            var qrInfos = await DbContext.QRInformations.ToListAsync();
            Assert.NotEmpty(qrInfos);
            var savedQR = qrInfos.Last();
            Assert.Equal(userId, savedQR.UserId);
            Assert.Equal(restaurantId, savedQR.RestaurantId);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_QIRA_002_AddQRInformationAsync_ShouldUpdateExistingQRInformation()
    {
        // Test Case ID: TC-QIRA-002
        // Mục tiêu: Kiểm tra AddQRInformationAsync cập nhật QR nếu đã tồn tại.
        await BeginTransactionAsync();

        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();

            var existingQR = new QRInformation
            {
                UserId = userId,
                RestaurantId = restaurantId,
                CreateTime = NowUnixTime() - 86400
            };
            DbContext.QRInformations.Add(existingQR);
            await DbContext.SaveChangesAsync();

            var qrRepository = new QRInformationRepository(DbContext);
            var qrService = new QRInformationService(qrRepository);

            var newTime = NowUnixTime();
            var qrDto = new QRInformationDto
            {
                UserId = userId,
                RestaurantId = restaurantId,
                CreateTime = newTime
            };

            await qrService.AddQRInformationAsync(qrDto);

            var qrInfos = await DbContext.QRInformations.Where(q => q.UserId == userId && q.RestaurantId == restaurantId).ToListAsync();
            Assert.Single(qrInfos);
            Assert.Equal(newTime, qrInfos.First().CreateTime);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_QIRA_003_AddQRInformationAsync_ShouldCreateMultipleQRInformationForDifferentRestaurants()
    {
        // Test Case ID: TC-QIRA-003
        // Mục tiêu: Kiểm tra tạo QRInformation cho cùng User nhưng khác Restaurant.
        await BeginTransactionAsync();

        try
        {
            var userId = await CreateTestUser();
            var restaurantId1 = await CreateTestRestaurant();
            var restaurantId2 = await CreateTestRestaurant();

            var qrRepository = new QRInformationRepository(DbContext);
            var qrService = new QRInformationService(qrRepository);

            var qrDto1 = new QRInformationDto
            {
                UserId = userId,
                RestaurantId = restaurantId1,
                CreateTime = NowUnixTime()
            };

            var qrDto2 = new QRInformationDto
            {
                UserId = userId,
                RestaurantId = restaurantId2,
                CreateTime = NowUnixTime()
            };

            await qrService.AddQRInformationAsync(qrDto1);
            await qrService.AddQRInformationAsync(qrDto2);

            var qrInfos = await DbContext.QRInformations.Where(q => q.UserId == userId).ToListAsync();
            Assert.Equal(2, qrInfos.Count);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_QIRA_004_GetQRInformationAsync_ShouldReturnQRInformationByUserId()
    {
        // Test Case ID: TC-QIRA-004
        // Mục tiêu: Kiểm tra GetQRInformationAsync trả về QR theo UserId.
        await BeginTransactionAsync();

        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();

            var qrInfo = new QRInformation
            {
                UserId = userId,
                RestaurantId = restaurantId,
                CreateTime = NowUnixTime()
            };
            DbContext.QRInformations.Add(qrInfo);
            await DbContext.SaveChangesAsync();

            var qrRepository = new QRInformationRepository(DbContext);
            var qrService = new QRInformationService(qrRepository);

            var results = await qrService.GetQRInformationAsync(userId, null);

            Assert.NotEmpty(results);
            var result = results.First();
            Assert.Equal(userId, result.UserId);
            Assert.Equal(restaurantId, result.RestaurantId);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_QIRA_005_GetQRInformationAsync_ShouldReturnQRInformationByRestaurantId()
    {
        // Test Case ID: TC-QIRA-005
        // Mục tiêu: Kiểm tra GetQRInformationAsync trả về QR theo RestaurantId.
        await BeginTransactionAsync();

        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();

            var qrInfo = new QRInformation
            {
                UserId = userId,
                RestaurantId = restaurantId,
                CreateTime = NowUnixTime()
            };
            DbContext.QRInformations.Add(qrInfo);
            await DbContext.SaveChangesAsync();

            var qrRepository = new QRInformationRepository(DbContext);
            var qrService = new QRInformationService(qrRepository);

            var results = await qrService.GetQRInformationAsync(null, restaurantId);

            Assert.NotEmpty(results);
            var result = results.First();
            Assert.Equal(restaurantId, result.RestaurantId);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_QIRA_006_GetQRInformationAsync_ShouldReturnQRInformationByBothUserAndRestaurantId()
    {
        // Test Case ID: TC-QIRA-006
        // Mục tiêu: Kiểm tra GetQRInformationAsync trả về QR theo cả UserId và RestaurantId.
        await BeginTransactionAsync();

        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();

            var qrInfo = new QRInformation
            {
                UserId = userId,
                RestaurantId = restaurantId,
                CreateTime = NowUnixTime()
            };
            DbContext.QRInformations.Add(qrInfo);
            await DbContext.SaveChangesAsync();

            var qrRepository = new QRInformationRepository(DbContext);
            var qrService = new QRInformationService(qrRepository);

            var results = await qrService.GetQRInformationAsync(userId, restaurantId);

            Assert.NotEmpty(results);
            var result = results.First();
            Assert.Equal(userId, result.UserId);
            Assert.Equal(restaurantId, result.RestaurantId);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_QIRA_007_GetQRInformationAsync_ShouldReturnEmptyWhenNoMatch()
    {
        // Test Case ID: TC-QIRA-007
        // Mục tiêu: Kiểm tra GetQRInformationAsync trả về rỗng khi không tìm thấy.
        await BeginTransactionAsync();

        try
        {
            var qrRepository = new QRInformationRepository(DbContext);
            var qrService = new QRInformationService(qrRepository);

            var results = await qrService.GetQRInformationAsync(9999, 9999);

            Assert.Empty(results);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_QIRA_008_DeleteQRInformationAsync_ShouldDeleteQRInformationWhenIdExists()
    {
        // Test Case ID: TC-QIRA-008
        // Mục tiêu: Kiểm tra DeleteQRInformationAsync xóa QR khi ID hợp lệ.
        await BeginTransactionAsync();

        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();

            var qrInfo = new QRInformation
            {
                UserId = userId,
                RestaurantId = restaurantId,
                CreateTime = NowUnixTime()
            };
            DbContext.QRInformations.Add(qrInfo);
            await DbContext.SaveChangesAsync();
            var qrId = qrInfo.Id;

            var qrRepository = new QRInformationRepository(DbContext);
            var qrService = new QRInformationService(qrRepository);

            await qrService.DeleteQRInformationAsync(qrId);

            var deletedQR = await DbContext.QRInformations.FindAsync(qrId);
            Assert.Null(deletedQR);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_QIRA_009_AddQRInformationAsync_ShouldHandleMultipleOperationsSequentially()
    {
        // Test Case ID: TC-QIRA-009
        // Mục tiêu: Kiểm tra thực hiện nhiều AddQRInformationAsync liên tiếp.
        await BeginTransactionAsync();

        try
        {
            var user1 = await CreateTestUser();
            var user2 = await CreateTestUser();
            var restaurant1 = await CreateTestRestaurant();
            var restaurant2 = await CreateTestRestaurant();

            var qrRepository = new QRInformationRepository(DbContext);
            var qrService = new QRInformationService(qrRepository);

            var qrDto1 = new QRInformationDto
            {
                UserId = user1,
                RestaurantId = restaurant1,
                CreateTime = NowUnixTime()
            };

            var qrDto2 = new QRInformationDto
            {
                UserId = user2,
                RestaurantId = restaurant2,
                CreateTime = NowUnixTime()
            };

            await qrService.AddQRInformationAsync(qrDto1);
            await qrService.AddQRInformationAsync(qrDto2);

            var allQRs = await DbContext.QRInformations.ToListAsync();
            Assert.NotEmpty(allQRs);
            Assert.True(allQRs.Count >= 2);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_QIRA_010_GetQRInformationAsync_ShouldReturnMultipleResultsForRestaurantWithMultipleUsers()
    {
        // Test Case ID: TC-QIRA-010
        // Mục tiêu: Kiểm tra GetQRInformationAsync trả về nhiều kết quả cho một Restaurant.
        await BeginTransactionAsync();

        try
        {
            var user1 = await CreateTestUser();
            var user2 = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();

            var qrInfo1 = new QRInformation
            {
                UserId = user1,
                RestaurantId = restaurantId,
                CreateTime = NowUnixTime()
            };

            var qrInfo2 = new QRInformation
            {
                UserId = user2,
                RestaurantId = restaurantId,
                CreateTime = NowUnixTime()
            };

            DbContext.QRInformations.AddRange(qrInfo1, qrInfo2);
            await DbContext.SaveChangesAsync();

            var qrRepository = new QRInformationRepository(DbContext);
            var qrService = new QRInformationService(qrRepository);

            var results = await qrService.GetQRInformationAsync(null, restaurantId);

            Assert.NotEmpty(results);
            Assert.True(results.Count() >= 2);
            Assert.All(results, r => Assert.Equal(restaurantId, r.RestaurantId));
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_QIRA_011_AddQRInformationAsync_ShouldThrowWhenUserDoesNotExist()
    {
        // Test Case ID: TC-QIRA-011
        await BeginTransactionAsync();

        try
        {
            var restaurantId = await CreateTestRestaurant();
            var qrRepository = new QRInformationRepository(DbContext);
            var qrService = new QRInformationService(qrRepository);

            var qrDto = new QRInformationDto
            {
                UserId = -1,
                RestaurantId = restaurantId,
                CreateTime = NowUnixTime()
            };

            await Assert.ThrowsAsync<DbUpdateException>(() => qrService.AddQRInformationAsync(qrDto));
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_QIRA_012_AddQRInformationAsync_ShouldThrowWhenRestaurantDoesNotExist()
    {
        // Test Case ID: TC-QIRA-012
        await BeginTransactionAsync();

        try
        {
            var userId = await CreateTestUser();
            var qrRepository = new QRInformationRepository(DbContext);
            var qrService = new QRInformationService(qrRepository);

            var qrDto = new QRInformationDto
            {
                UserId = userId,
                RestaurantId = -1,
                CreateTime = NowUnixTime()
            };

            await Assert.ThrowsAsync<DbUpdateException>(() => qrService.AddQRInformationAsync(qrDto));
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_QIRA_013_AddQRInformationAsync_ShouldSaveWhenCreateTimeIsZero()
    {
        // Test Case ID: TC-QIRA-013
        await BeginTransactionAsync();

        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();
            var qrRepository = new QRInformationRepository(DbContext);
            var qrService = new QRInformationService(qrRepository);

            await qrService.AddQRInformationAsync(new QRInformationDto
            {
                UserId = userId,
                RestaurantId = restaurantId,
                CreateTime = 0
            });

            var qr = await DbContext.QRInformations.SingleAsync(q => q.UserId == userId && q.RestaurantId == restaurantId);
            Assert.Equal(0, qr.CreateTime);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_QIRA_014_AddQRInformationAsync_ShouldSaveWhenCreateTimeIsVeryLarge()
    {
        // Test Case ID: TC-QIRA-014
        await BeginTransactionAsync();

        try
        {
            var userId = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();
            var qrRepository = new QRInformationRepository(DbContext);
            var qrService = new QRInformationService(qrRepository);
            var veryLargeTime = long.MaxValue;

            await qrService.AddQRInformationAsync(new QRInformationDto
            {
                UserId = userId,
                RestaurantId = restaurantId,
                CreateTime = veryLargeTime
            });

            var qr = await DbContext.QRInformations.SingleAsync(q => q.UserId == userId && q.RestaurantId == restaurantId);
            Assert.Equal(veryLargeTime, qr.CreateTime);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_QIRA_015_GetQRInformationAsync_ShouldReturnAllWhenBothFiltersAreNull()
    {
        // Test Case ID: TC-QIRA-015
        await BeginTransactionAsync();

        try
        {
            var user1 = await CreateTestUser();
            var user2 = await CreateTestUser();
            var restaurant1 = await CreateTestRestaurant();
            var restaurant2 = await CreateTestRestaurant();

            DbContext.QRInformations.AddRange(
                new QRInformation { UserId = user1, RestaurantId = restaurant1, CreateTime = NowUnixTime() },
                new QRInformation { UserId = user2, RestaurantId = restaurant2, CreateTime = NowUnixTime() }
            );
            await DbContext.SaveChangesAsync();

            var qrService = new QRInformationService(new QRInformationRepository(DbContext));
            var results = await qrService.GetQRInformationAsync(null, null);

            Assert.True(results.Count() >= 2);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_QIRA_016_GetQRInformationAsync_ShouldFilterByUserOnly()
    {
        // Test Case ID: TC-QIRA-016
        await BeginTransactionAsync();

        try
        {
            var userId = await CreateTestUser();
            var anotherUserId = await CreateTestUser();
            var restaurant1 = await CreateTestRestaurant();
            var restaurant2 = await CreateTestRestaurant();

            DbContext.QRInformations.AddRange(
                new QRInformation { UserId = userId, RestaurantId = restaurant1, CreateTime = NowUnixTime() },
                new QRInformation { UserId = userId, RestaurantId = restaurant2, CreateTime = NowUnixTime() },
                new QRInformation { UserId = anotherUserId, RestaurantId = restaurant1, CreateTime = NowUnixTime() }
            );
            await DbContext.SaveChangesAsync();

            var qrService = new QRInformationService(new QRInformationRepository(DbContext));
            var results = (await qrService.GetQRInformationAsync(userId, null)).ToList();

            Assert.Equal(2, results.Count);
            Assert.All(results, x => Assert.Equal(userId, x.UserId));
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_QIRA_017_GetQRInformationAsync_ShouldFilterByRestaurantOnly()
    {
        // Test Case ID: TC-QIRA-017
        await BeginTransactionAsync();

        try
        {
            var user1 = await CreateTestUser();
            var user2 = await CreateTestUser();
            var restaurantId = await CreateTestRestaurant();
            var anotherRestaurantId = await CreateTestRestaurant();

            DbContext.QRInformations.AddRange(
                new QRInformation { UserId = user1, RestaurantId = restaurantId, CreateTime = NowUnixTime() },
                new QRInformation { UserId = user2, RestaurantId = restaurantId, CreateTime = NowUnixTime() },
                new QRInformation { UserId = user1, RestaurantId = anotherRestaurantId, CreateTime = NowUnixTime() }
            );
            await DbContext.SaveChangesAsync();

            var qrService = new QRInformationService(new QRInformationRepository(DbContext));
            var results = (await qrService.GetQRInformationAsync(null, restaurantId)).ToList();

            Assert.Equal(2, results.Count);
            Assert.All(results, x => Assert.Equal(restaurantId, x.RestaurantId));
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_QIRA_018_GetQRInformationAsync_ShouldReturnEmptyForZeroOrInvalidIds()
    {
        // Test Case ID: TC-QIRA-018
        await BeginTransactionAsync();

        try
        {
            var qrService = new QRInformationService(new QRInformationRepository(DbContext));
            var results = await qrService.GetQRInformationAsync(0, 0);

            Assert.Empty(results);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_QIRA_019_DeleteQRInformationAsync_ShouldNotThrowWhenIdDoesNotExist()
    {
        // Test Case ID: TC-QIRA-019
        await BeginTransactionAsync();

        try
        {
            var qrService = new QRInformationService(new QRInformationRepository(DbContext));
            var initialCount = await DbContext.QRInformations.CountAsync();

            await qrService.DeleteQRInformationAsync(999999);

            var finalCount = await DbContext.QRInformations.CountAsync();
            Assert.Equal(initialCount, finalCount);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_QIRA_020_DeleteQRInformationAsync_ShouldNotThrowWhenIdIsZero()
    {
        // Test Case ID: TC-QIRA-020
        await BeginTransactionAsync();

        try
        {
            var qrService = new QRInformationService(new QRInformationRepository(DbContext));
            var initialCount = await DbContext.QRInformations.CountAsync();

            await qrService.DeleteQRInformationAsync(0);

            var finalCount = await DbContext.QRInformations.CountAsync();
            Assert.Equal(initialCount, finalCount);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }
}
