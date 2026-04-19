using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BusinessLogicLayer.Tests;

public class QRInformationServiceIntegrationTests : TestDatabaseFixture
{
    private async Task<int> CreateTestUser()
    {
        var user = new User
        {
            Name = "QR Test User",
            PhoneNumber = "0111111111",
            Email = "qruser@example.com",
            Address = "123 QR Street"
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
        return user.Id;
    }

    private async Task<int> CreateTestRestaurant()
    {
        var restaurant = new Restaurant
        {
            Name = "QR Test Restaurant",
            PhoneNumber = "0222222222",
            Email = "qrrestaurant@example.com",
            Address = "456 QR Restaurant Street"
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
                CreateTime = DateTime.Now
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
                CreateTime = DateTime.Now.AddDays(-1)
            };
            DbContext.QRInformations.Add(existingQR);
            await DbContext.SaveChangesAsync();

            var qrRepository = new QRInformationRepository(DbContext);
            var qrService = new QRInformationService(qrRepository);

            var newTime = DateTime.Now;
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
                CreateTime = DateTime.Now
            };

            var qrDto2 = new QRInformationDto
            {
                UserId = userId,
                RestaurantId = restaurantId2,
                CreateTime = DateTime.Now
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
                CreateTime = DateTime.Now
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
                CreateTime = DateTime.Now
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
                CreateTime = DateTime.Now
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
                CreateTime = DateTime.Now
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
                CreateTime = DateTime.Now
            };

            var qrDto2 = new QRInformationDto
            {
                UserId = user2,
                RestaurantId = restaurant2,
                CreateTime = DateTime.Now
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
                CreateTime = DateTime.Now
            };

            var qrInfo2 = new QRInformation
            {
                UserId = user2,
                RestaurantId = restaurantId,
                CreateTime = DateTime.Now
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
}
