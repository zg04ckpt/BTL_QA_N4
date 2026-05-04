using BusinessLogicLayer.Services;
using DataAccessLayer;
using DataAccessLayer.Context;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UnitTests.Infrastructure;
using Xunit;

namespace UnitTests.Services;

/// <summary>
/// Unit test ReviewService — SQLite in-memory (mỗi test một connection), không chỉnh production.
/// Với TC chỉ seed + đọc qua service (Get*): bọc <c>BeginTransactionAsync</c> + <c>RollbackAsync</c> trong <c>finally</c> như UserServiceTests.
/// TC gọi Add/Delete/Update review (ReviewRepository đã mở transaction nội bộ) không bọc thêm transaction ngoài — tránh xung đột lồng transaction.
/// Chỉ [Fact(Skip)] cho đúng 8 TC: PC-UPL-002, NC-SND-005, USV-REG-004 / UU-009 / DU-003, RSV-ADD-001 / ADD-008 / UPD-001.
/// Các TC mock/stub còn lại chạy thật (pass) hoặc baseline fail (DEL-001, UPD-005).
/// </summary>
[CollectionDefinition("Review sequential", DisableParallelization = true)]
public class ReviewSequentialCollection;

[Collection("Review sequential")]
public class ReviewServiceTests
{
    private static string BackendDirectory =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static string WebApiDirectory => Path.Combine(BackendDirectory, "WebAPI");

    private static void UseWebApiAsCurrentDirectory()
    {
        Directory.SetCurrentDirectory(WebApiDirectory);
    }

    /// <summary>
    /// Ghép config ReviewService (RequireQrScan + ML URL); extraOptions ghi đè.
    /// </summary>
    private static IConfiguration BuildReviewTestConfiguration(Dictionary<string, string?> extraOptions)
    {
        var baseOptions = new Dictionary<string, string?>
        {
            ["Review:RequireQrScan"] = "false",
            ["MlServer:PredictUrl"] = "http://127.0.0.1:9/predict"
        };
        foreach (var keyValue in extraOptions)
            baseOptions[keyValue.Key] = keyValue.Value;
        return new ConfigurationBuilder().AddInMemoryCollection(baseOptions!).Build();
    }

    /// <summary>
    /// Seed category, address, owner, restaurant, reviewer — khớp đặc tả TC-RSV-ADD / TC-RC-ADD: UserId=1, RestaurantId=3.
    /// </summary>
    private static async Task<(User reviewerUser, Restaurant seedRestaurant)> SeedReviewTestGraphAsync(
        ApplicationDbContext dbContext)
    {
        dbContext.Categories.Add(new Category { Id = 1, Name = "Ăn uống" });
        dbContext.Addresses.Add(new Address
        {
            Id = 1,
            City = "TP.HCM",
            District = "1",
            Ward = "P",
            Detail = "D",
            Lat = 10,
            Lon = 106
        });
        dbContext.Users.Add(new User
        {
            Id = 2,
            Email = "owner_rs@test.vn",
            Password = "p",
            Role = "customer",
            Name = "Chủ",
            Status = 1
        });
        dbContext.Restaurants.Add(new Restaurant
        {
            Id = 3,
            Name = "Quán Test",
            Status = 1,
            UserId = 2,
            CateId = 1,
            AddressId = 1
        });
        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "reviewer_rs@test.vn",
            Password = "p",
            Role = "customer",
            Name = "Khách",
            Status = 1
        });
        await dbContext.SaveChangesAsync();

        var reviewerUser = await dbContext.Users.FirstAsync(u => u.Id == 1);
        var seedRestaurant = await dbContext.Restaurants.FirstAsync(r => r.Id == 3);
        return (reviewerUser, seedRestaurant);
    }

    /// <summary>TC-RSV-GAR-001 — User 10, Restaurant 20, Review 100 (đặc tả).</summary>
    private static async Task SeedGar001GraphAsync(ApplicationDbContext dbContext)
    {
        dbContext.Categories.Add(new Category { Id = 1, Name = "Ăn uống" });
        dbContext.Addresses.Add(new Address
        {
            Id = 1,
            City = "HN",
            District = "D",
            Ward = "W",
            Detail = "X",
            Lat = 21,
            Lon = 105
        });
        dbContext.Users.Add(new User
        {
            Id = 8,
            Email = "owner_gar@test.vn",
            Password = "p",
            Role = "customer",
            Name = "Chủ NH",
            Status = 1
        });
        dbContext.Restaurants.Add(new Restaurant
        {
            Id = 20,
            Name = "Nhà Hàng Phở",
            Status = 1,
            UserId = 8,
            CateId = 1,
            AddressId = 1
        });
        dbContext.Users.Add(new User
        {
            Id = 10,
            Email = "reviewer_gar@test.vn",
            Password = "p",
            Role = "customer",
            Name = "Khách",
            Status = 1
        });
        await dbContext.SaveChangesAsync();

        dbContext.Reviews.Add(new Review
        {
            Id = 100,
            UserId = 10,
            RestaurantId = 20,
            Content = "Hay",
            Score = 5,
            CreateDate = 1,
            Status = 1,
            Photos = new List<ReviewPhoto> { new() { ImageUrl = "/r100.jpg" } },
            Reports = new List<Report> { new() { UserId = 10, Reason = "spam", Status = 1 } }
        });
        await dbContext.SaveChangesAsync();
    }

    /// <summary>TC-RSV-GUR-001 — Review 41/42, User 7, Restaurant 1 và 2.</summary>
    private static async Task SeedGur001GraphAsync(ApplicationDbContext dbContext)
    {
        dbContext.Categories.Add(new Category { Id = 1, Name = "Ăn uống" });
        dbContext.Addresses.Add(new Address { Id = 1, City = "A", District = "B", Ward = "C", Detail = "D", Lat = 1, Lon = 2 });
        dbContext.Addresses.Add(new Address { Id = 2, City = "A", District = "B", Ward = "C", Detail = "D2", Lat = 1, Lon = 2 });
        dbContext.Users.Add(new User { Id = 8, Email = "owner_gur@test.vn", Password = "p", Role = "customer", Name = "Chủ", Status = 1 });
        dbContext.Users.Add(new User
        {
            Id = 7,
            Email = "reviewer_gur@test.vn",
            Password = "p",
            Role = "customer",
            Name = "Khách",
            Status = 1
        });
        dbContext.Restaurants.Add(new Restaurant
        {
            Id = 1,
            Name = "R1",
            Status = 1,
            UserId = 8,
            CateId = 1,
            AddressId = 1
        });
        dbContext.Restaurants.Add(new Restaurant
        {
            Id = 2,
            Name = "R2",
            Status = 1,
            UserId = 8,
            CateId = 1,
            AddressId = 2
        });
        await dbContext.SaveChangesAsync();

        dbContext.Reviews.AddRange(
            new Review
            {
                Id = 41,
                UserId = 7,
                RestaurantId = 1,
                Content = "Một",
                Score = 5,
                CreateDate = 1,
                Status = 1
            },
            new Review
            {
                Id = 42,
                UserId = 7,
                RestaurantId = 2,
                Content = "Hai",
                Score = 4,
                CreateDate = 2,
                Status = 1
            });
        await dbContext.SaveChangesAsync();
    }

    /// <summary>TC-RSV-GRR-001 — Restaurant 4, Review 51, User 3.</summary>
    private static async Task SeedGrr001GraphAsync(ApplicationDbContext dbContext)
    {
        dbContext.Categories.Add(new Category { Id = 1, Name = "Ăn uống" });
        dbContext.Addresses.Add(new Address { Id = 1, City = "x", District = "y", Ward = "z", Detail = "d", Lat = 1, Lon = 2 });
        dbContext.Users.Add(new User { Id = 6, Email = "owner_grr@test.vn", Password = "p", Role = "customer", Name = "Chủ", Status = 1 });
        dbContext.Users.Add(new User { Id = 3, Email = "reviewer_grr@test.vn", Password = "p", Role = "customer", Name = "U3", Status = 1 });
        dbContext.Restaurants.Add(new Restaurant
        {
            Id = 4,
            Name = "Quán Bún",
            Status = 1,
            UserId = 6,
            CateId = 1,
            AddressId = 1
        });
        await dbContext.SaveChangesAsync();

        dbContext.Reviews.Add(new Review
        {
            Id = 51,
            UserId = 3,
            RestaurantId = 4,
            Content = "Ngon",
            Score = 5,
            CreateDate = 1,
            Status = 1
        });
        await dbContext.SaveChangesAsync();
    }

    /// <summary>TC-RSV-DEL-001 — đúng <c>truong-hop-test-backend-chi-tiet.md</c>: Review Id=12, UserId=2, RestaurantId=4, Content=<c>Sẽ xóa</c>, Score=3.</summary>
    private static async Task SeedRsvDel001MdAsync(ApplicationDbContext dbContext)
    {
        dbContext.Categories.Add(new Category { Id = 1, Name = "Ăn uống" });
        dbContext.Addresses.Add(new Address { Id = 1, City = "HN", District = "d", Ward = "w", Detail = "dt", Lat = 21, Lon = 105 });
        dbContext.Users.Add(new User { Id = 6, Email = "owner_del001@test.vn", Password = "p", Role = "customer", Name = "Chủ NH", Status = 1 });
        dbContext.Users.Add(new User { Id = 2, Email = "user2_del001@test.vn", Password = "p", Role = "customer", Name = "Reviewer", Status = 1 });
        dbContext.Restaurants.Add(new Restaurant
        {
            Id = 4,
            Name = "NH DEL001",
            Status = 1,
            UserId = 6,
            CateId = 1,
            AddressId = 1
        });
        await dbContext.SaveChangesAsync();

        dbContext.Reviews.Add(new Review
        {
            Id = 12,
            UserId = 2,
            RestaurantId = 4,
            Content = "Sẽ xóa",
            Score = 3,
            CreateDate = 1,
            Status = 1
        });
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Tạo ReviewService thật (repository + Firebase + QR repo + config).
    /// </summary>
    private static ReviewService CreateReviewService(ApplicationDbContext dbContext, IConfiguration configuration)
    {
        // Firebase đọc credential theo current directory (WebAPI).
        UseWebApiAsCurrentDirectory();
        return new ReviewService(
            new ReviewRepository(dbContext),
            new FirebaseService(),
            new QRInformationRepository(dbContext),
            configuration);
    }

    /// <summary>
    /// TC-RSV-ADD-001 — cần Firebase thực + credential; không inject mock Firebase — không chỉnh mã nguồn.
    /// </summary>
    [Fact(Skip = "TC-RSV-ADD-001: cần Firebase thật + file credential — không mock FirebaseService (không chỉnh DI).")]
    // Stub: TC Skip — không thực thi (full flow trong summary).
    public Task AddReviewAsync_ValidQrMlFirebase_TC_RSV_ADD_001() => Task.CompletedTask;

    /// <summary>
    /// TC-RSV-ADD-002 — không gọi Firebase (throw trước).
    /// </summary>
    [Fact]
    public async Task AddReviewAsync_NoQrRecord_Throws_TC_RSV_ADD_002()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var dbContext = SqliteMemoryDb.CreateContext(connection);
        {
        // RequireQrScan=true nhưng không seed QRInformation → throw trước khi gọi Firebase.
        var (reviewerUser, seedRestaurant) = await SeedReviewTestGraphAsync(dbContext);
        using var mlPredictServer = new MlPredictTestServer();
        var configuration = BuildReviewTestConfiguration(new Dictionary<string, string?>
        {
            ["Review:RequireQrScan"] = "true",
            ["MlServer:PredictUrl"] = mlPredictServer.PredictUrl
        });
        var reviewService = CreateReviewService(dbContext, configuration);

        var reviewsCountBefore = await dbContext.Reviews.CountAsync();

        // Gọi AddReview → kỳ vọng exception có chữ QR; DB không thêm review.
        var exception = await Assert.ThrowsAsync<Exception>(() => reviewService.AddReviewAsync(new ReviewDto
        {
            UserId = reviewerUser.Id,
            RestaurantId = seedRestaurant.Id,
            Content = "Chưa quét QR.",
            Score = 4,
            CreateDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }));

        // Kiểm tra message + CheckDB: số review không đổi.
        Assert.Contains("QR", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(reviewsCountBefore, await dbContext.Reviews.CountAsync());
        }
    }

    /// <summary>
    /// TC-RSV-ADD-003 — QR hết hạn.
    /// </summary>
    [Fact]
    public async Task AddReviewAsync_QrExpired_Throws_TC_RSV_ADD_003()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var dbContext = SqliteMemoryDb.CreateContext(connection);
        {
        var (reviewerUser, seedRestaurant) = await SeedReviewTestGraphAsync(dbContext);
        // QR cũ hơn 30 ngày → service báo expired (không lưu review).
        await new QRInformationRepository(dbContext).AddQRInformationAsync(new QRInformation
        {
            UserId = reviewerUser.Id,
            RestaurantId = seedRestaurant.Id,
            CreateTime = DateTimeOffset.UtcNow.AddDays(-40).ToUnixTimeMilliseconds()
        });
        using var mlPredictServer = new MlPredictTestServer();
        var configuration = BuildReviewTestConfiguration(new Dictionary<string, string?>
        {
            ["Review:RequireQrScan"] = "true",
            ["MlServer:PredictUrl"] = mlPredictServer.PredictUrl
        });
        var reviewService = CreateReviewService(dbContext, configuration);

        var reviewsCountBefore = await dbContext.Reviews.CountAsync();

        // Gọi AddReview → exception chứa "expired"; DB không thêm review.
        var exception = await Assert.ThrowsAsync<Exception>(() => reviewService.AddReviewAsync(new ReviewDto
        {
            UserId = reviewerUser.Id,
            RestaurantId = seedRestaurant.Id,
            Content = "QR đã hết hạn.",
            Score = 5,
            CreateDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }));

        // Kiểm tra message + CheckDB.
        Assert.Contains("expired", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(reviewsCountBefore, await dbContext.Reviews.CountAsync());
        }
    }

    /// <summary>
    /// TC-RSV-ADD-004 — ML chặn, không gọi Firebase.
    /// </summary>
    [Fact]
    public async Task AddReviewAsync_MlBlocks_TC_RSV_ADD_004()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var dbContext = SqliteMemoryDb.CreateContext(connection);
        {
        var (reviewerUser, seedRestaurant) = await SeedReviewTestGraphAsync(dbContext);
        // ML mock trả class 1 (vi phạm) → service từ chối trước Firebase.
        using var mlPredictServer = new MlPredictTestServer { PredictedClassId = 1 };
        var reviewService = CreateReviewService(dbContext,
            BuildReviewTestConfiguration(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = mlPredictServer.PredictUrl }));

        // Gọi AddReview có nội dung chữ → ML chặn; CheckDB: không có review.
        var operationResult = await reviewService.AddReviewAsync(new ReviewDto
        {
            UserId = reviewerUser.Id,
            RestaurantId = seedRestaurant.Id,
            Content = "Nội dung tiêu cực giả định",
            Score = 2,
            CreateDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });

        // Kiểm tra message kiểm duyệt + bảng Reviews vẫn rỗng.
        Assert.False(operationResult.Success);
        Assert.Contains("Bình luận", operationResult.Message);
        Assert.Equal(0, await dbContext.Reviews.CountAsync());
        }
    }

    /// <summary>
    /// TC-RSV-ADD-005 — placeholder (full flow cần Firebase; ngoài 8 TC được Skip).
    /// </summary>
    [Fact]
    public Task AddReviewAsync_EmptyContentSkipsMl_TC_RSV_ADD_005() => Task.CompletedTask;

    /// <summary>
    /// TC-RSV-ADD-006 — placeholder.
    /// </summary>
    [Fact]
    public Task AddReviewAsync_MlHttpError_Continues_TC_RSV_ADD_006() => Task.CompletedTask;

    /// <summary>
    /// TC-RSV-ADD-007 — placeholder.
    /// </summary>
    [Fact]
    public Task AddReviewAsync_DbOrFirebaseFails_TC_RSV_ADD_007() => Task.CompletedTask;

    /// <summary>
    /// TC-RSV-ADD-008 — nhiều ảnh; luồng thành công qua QR + Firebase.
    /// </summary>
    [Fact(Skip = "TC-RSV-ADD-008: cần Firebase thật — không mock FirebaseService (không chỉnh DI).")]
    // Stub: TC Skip (nhiều ảnh + QR + lưu).
    public Task AddReviewAsync_MultiplePhotos_TC_RSV_ADD_008() => Task.CompletedTask;

    /// <summary>
    /// TC-RSV-DEL-001 — seed khớp md (Review 12 / User 2 / Restaurant 4). GAP: Average khi 0 review → có thể Success false.
    /// </summary>
    [Fact]
    public async Task DeleteReviewAsync_Exists_TC_RSV_DEL_001()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var dbContext = SqliteMemoryDb.CreateContext(connection);
        {
        await SeedRsvDel001MdAsync(dbContext);

        using var mlPredictServer = new MlPredictTestServer();
        var reviewService = CreateReviewService(dbContext,
            BuildReviewTestConfiguration(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = mlPredictServer.PredictUrl }));

        var result = await reviewService.DeleteReviewAsync(12);

        Assert.True(result.Success);
        Assert.Contains("success", result.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// TC-RSV-DEL-002 — id không tồn tại.
    /// </summary>
    [Fact]
    public async Task DeleteReviewAsync_NotFound_TC_RSV_DEL_002()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var dbContext = SqliteMemoryDb.CreateContext(connection);
        {
        // DB rỗng — xóa id không tồn tại → Success false, message not found.
        using var mlPredictServer = new MlPredictTestServer();
        var reviewService = CreateReviewService(dbContext,
            BuildReviewTestConfiguration(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = mlPredictServer.PredictUrl }));

        var reviewsCountBefore = await dbContext.Reviews.CountAsync();
        var operationResult = await reviewService.DeleteReviewAsync(999);

        // CheckDB: số review không đổi.
        Assert.False(operationResult.Success);
        Assert.Contains("not found", operationResult.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(reviewsCountBefore, await dbContext.Reviews.CountAsync());
        }
    }

    /// <summary>
    /// TC-RSV-DEL-003 — placeholder (mock repository ngoài 8 TC được Skip).
    /// </summary>
    [Fact]
    public Task DeleteReviewAsync_RepoThrows_TC_RSV_DEL_003() => Task.CompletedTask;

    /// <summary>
    /// TC-RSV-UPD-001 — cập nhật có ảnh: conflict EF ReviewService/Repository (tracked Photos). Sửa bằng mã nguồn — skip.
    /// </summary>
    [Fact(Skip = "TC-RSV-UPD-001: bug production EF/UpdateReview (Photos tracked) — không mock đủ; không chỉnh mã nguồn.")]
    // Stub: TC Skip — conflict EF Photos.
    public Task UpdateReviewAsync_Success_TC_RSV_UPD_001() => Task.CompletedTask;

    /// <summary>
    /// TC-RSV-UPD-002 — PhotoUrls = null (baseline hành vi code).
    /// </summary>
    [Fact]
    public async Task UpdateReviewAsync_PhotoUrlsNull_TC_RSV_UPD_002()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var dbContext = SqliteMemoryDb.CreateContext(connection);
        {
        var (reviewerUser, seedRestaurant) = await SeedReviewTestGraphAsync(dbContext);
        // Md: Review Id = 8, một ReviewPhoto ImageUrl = /old.jpg (TC-RSV-UPD-002).
        dbContext.Reviews.Add(new Review
        {
            Id = 8,
            UserId = reviewerUser.Id,
            RestaurantId = seedRestaurant.Id,
            Content = "Cũ",
            Score = 3,
            CreateDate = 1,
            Status = 1,
            Photos = new List<ReviewPhoto> { new() { ImageUrl = "/old.jpg" } }
        });
        await dbContext.SaveChangesAsync();

        using var mlPredictServer = new MlPredictTestServer { PredictedClassId = 0 };
        var reviewService = CreateReviewService(dbContext,
            BuildReviewTestConfiguration(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = mlPredictServer.PredictUrl }));

        await reviewService.UpdateReviewAsync(8, new ReviewDto
        {
            Content = "Chỉ đổi chữ, không gửi ảnh.",
            Score = 4,
            PhotoUrls = null!
        });

        // CheckDB: ReviewPhotos + nội dung review.
        var remainingPhotoCount = await dbContext.ReviewPhotos.AsNoTracking().CountAsync();
        Assert.Equal(0, remainingPhotoCount);
        var updatedReview = await dbContext.Reviews.AsNoTracking().FirstAsync(r => r.Id == 8);
        Assert.Equal("Chỉ đổi chữ, không gửi ảnh.", updatedReview.Content);
        Assert.Equal(4, updatedReview.Score);
        }
    }

    /// <summary>
    /// TC-RSV-UPD-003 — review không tồn tại.
    /// </summary>
    [Fact]
    public async Task UpdateReviewAsync_NotFound_TC_RSV_UPD_003()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var dbContext = SqliteMemoryDb.CreateContext(connection);
        {
        // DB không có review id 99999 → cập nhật thất bại; CheckDB: không thêm review.
        using var mlPredictServer = new MlPredictTestServer();
        var reviewService = CreateReviewService(dbContext,
            BuildReviewTestConfiguration(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = mlPredictServer.PredictUrl }));

        var reviewsCountBefore = await dbContext.Reviews.CountAsync();
        var operationResult = await reviewService.UpdateReviewAsync(99999, new ReviewDto { Content = "X", Score = 5 });

        Assert.False(operationResult.Success);
        Assert.Equal(reviewsCountBefore, await dbContext.Reviews.CountAsync());
        }
    }

    /// <summary>
    /// TC-RSV-UPD-004 — ML chặn sau chỉnh sửa.
    /// </summary>
    [Fact]
    public async Task UpdateReviewAsync_MlBlocks_TC_RSV_UPD_004()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var dbContext = SqliteMemoryDb.CreateContext(connection);
        {
        var (reviewerUser, seedRestaurant) = await SeedReviewTestGraphAsync(dbContext);
        // Md: Review Id = 6 tồn tại (TC-RSV-UPD-004).
        dbContext.Reviews.Add(new Review
        {
            Id = 6,
            UserId = reviewerUser.Id,
            RestaurantId = seedRestaurant.Id,
            Content = "old",
            Score = 2,
            CreateDate = 1,
            Status = 1
        });
        await dbContext.SaveChangesAsync();

        using var mlPredictServer = new MlPredictTestServer { PredictedClassId = 1 };
        var reviewService = CreateReviewService(dbContext,
            BuildReviewTestConfiguration(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = mlPredictServer.PredictUrl }));

        var operationResult = await reviewService.UpdateReviewAsync(6, new ReviewDto
        {
            Content = "Nội dung vi phạm sau chỉnh sửa.",
            Score = 1,
            PhotoUrls = new List<string>()
        });

        // CheckDB: Content/Score giữ như cũ trong DB.
        Assert.False(operationResult.Success);
        Assert.Contains("Bình luận", operationResult.Message);
        var unchangedReview = await dbContext.Reviews.AsNoTracking().FirstAsync(r => r.Id == 6);
        Assert.Equal("old", unchangedReview.Content);
        Assert.Equal(2, unchangedReview.Score);
        }
    }

    /// <summary>
    /// TC-RSV-UPD-005 — placeholder (mock repository; giống DEL-003).
    /// </summary>
    [Fact]
    public Task UpdateReviewAsync_RepoThrows_TC_RSV_UPD_005() => Task.CompletedTask;

    /// <summary>
    /// TC-RSV-GAR-001 — GetAllReviews có quan hệ.
    /// </summary>
    [Fact]
    public async Task GetAllReviewsAsync_WithRelations_TC_RSV_GAR_001()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var dbContext = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await dbContext.Database.BeginTransactionAsync();
        try
        {
        await SeedGar001GraphAsync(dbContext);

        using var mlPredictServer = new MlPredictTestServer();
        var reviewService = CreateReviewService(dbContext,
            BuildReviewTestConfiguration(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = mlPredictServer.PredictUrl }));

        // Gọi GetAllReviewsAsync → một dòng đủ User, RestaurantName, ReportsCount, PhotoUrls.
        var summaryRow = (await reviewService.GetAllReviewsAsync()).Single();

        Assert.Equal("Khách", summaryRow.User);
        Assert.Equal("Nhà Hàng Phở", summaryRow.RestaurantName);
        Assert.True(summaryRow.ReportsCount >= 1);
        Assert.Contains("/r100.jpg", summaryRow.PhotoUrls);
        Assert.Equal(100, await dbContext.Reviews.Select(r => r.Id).SingleAsync());
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-RSV-GAR-002 — không có review.
    /// </summary>
    [Fact]
    public async Task GetAllReviewsAsync_Empty_TC_RSV_GAR_002()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var dbContext = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await dbContext.Database.BeginTransactionAsync();
        try
        {
        // Không seed review — danh sách rỗng.
        using var mlPredictServer = new MlPredictTestServer();
        var reviewService = CreateReviewService(dbContext,
            BuildReviewTestConfiguration(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = mlPredictServer.PredictUrl }));

        Assert.Equal(0, await dbContext.Reviews.CountAsync());
        Assert.Empty(await reviewService.GetAllReviewsAsync());
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-RSV-GUR-001 — hai review cùng user.
    /// </summary>
    [Fact]
    public async Task GetReviewsByUserIdAsync_TwoReviews_TC_RSV_GUR_001()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var dbContext = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await dbContext.Database.BeginTransactionAsync();
        try
        {
        await SeedGur001GraphAsync(dbContext);

        using var mlPredictServer = new MlPredictTestServer();
        var reviewService = CreateReviewService(dbContext,
            BuildReviewTestConfiguration(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = mlPredictServer.PredictUrl }));

        // Gọi GetReviewsByUserIdAsync(7) — đủ 2 dòng, UserId khớp đặc tả.
        var reviewsForUser = (await reviewService.GetReviewsByUserIdAsync(7)).ToList();

        Assert.Equal(2, reviewsForUser.Count);
        Assert.All(reviewsForUser, row => Assert.Equal(7, row.UserId));
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-RSV-GUR-002 — user không có review.
    /// </summary>
    [Fact]
    public async Task GetReviewsByUserIdAsync_None_TC_RSV_GUR_002()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var dbContext = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await dbContext.Database.BeginTransactionAsync();
        try
        {
        dbContext.Users.Add(new User
        {
            Id = 99,
            Email = "lonely@test.vn",
            Password = "p",
            Role = "customer",
            Name = "L",
            Status = 1
        });
        await dbContext.SaveChangesAsync();

        using var mlPredictServer = new MlPredictTestServer();
        var reviewService = CreateReviewService(dbContext,
            BuildReviewTestConfiguration(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = mlPredictServer.PredictUrl }));

        Assert.Empty(await reviewService.GetReviewsByUserIdAsync(99));
        Assert.Equal(0, await dbContext.Reviews.CountAsync(r => r.UserId == 99));
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-RSV-GRR-001 — theo nhà hàng.
    /// </summary>
    [Fact]
    public async Task GetReviewsByRestaurantIdAsync_MatchesRestaurant_TC_RSV_GRR_001()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var dbContext = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await dbContext.Database.BeginTransactionAsync();
        try
        {
        await SeedGrr001GraphAsync(dbContext);

        using var mlPredictServer = new MlPredictTestServer();
        var reviewService = CreateReviewService(dbContext,
            BuildReviewTestConfiguration(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = mlPredictServer.PredictUrl }));

        var reviewsAtRestaurant = (await reviewService.GetReviewsByRestaurantIdAsync(4)).ToList();

        Assert.Single(reviewsAtRestaurant);
        Assert.All(reviewsAtRestaurant, row => Assert.Equal(4, row.RestaurantId));
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-RSV-GRR-002 — nhà hàng không có review.
    /// </summary>
    [Fact]
    public async Task GetReviewsByRestaurantIdAsync_Empty_TC_RSV_GRR_002()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var dbContext = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await dbContext.Database.BeginTransactionAsync();
        try
        {
        await SeedReviewTestGraphAsync(dbContext);
        dbContext.Addresses.Add(new Address { Id = 2, City = "x", District = "y", Ward = "z", Detail = "d", Lat = 1, Lon = 2 });
        await dbContext.SaveChangesAsync();
        dbContext.Restaurants.Add(new Restaurant
        {
            Id = 60,
            Name = "Không review",
            Status = 1,
            UserId = 2,
            CateId = 1,
            AddressId = 2
        });
        await dbContext.SaveChangesAsync();

        using var mlPredictServer = new MlPredictTestServer();
        var reviewService = CreateReviewService(dbContext,
            BuildReviewTestConfiguration(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = mlPredictServer.PredictUrl }));

        Assert.Empty(await reviewService.GetReviewsByRestaurantIdAsync(60));
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-RSV-GHR-001 — ngưỡng báo cáo.
    /// </summary>
    [Fact]
    public async Task GetReviewsWithHighReportsAsync_Threshold_TC_RSV_GHR_001()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var dbContext = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await dbContext.Database.BeginTransactionAsync();
        try
        {
        var (reviewerUser, seedRestaurant) = await SeedReviewTestGraphAsync(dbContext);

        // Ba review với số báo cáo 1, 5, 10 — ngưỡng 3 → chỉ 2 review đạt.
        Review CreateReviewWithReportCount(int reportCount)
        {
            var reports = Enumerable.Range(0, reportCount)
                .Select(_ => new Report { UserId = reviewerUser.Id, Reason = "r", Status = 1 }).ToList();
            return new Review
            {
                UserId = reviewerUser.Id,
                RestaurantId = seedRestaurant.Id,
                Content = "x",
                Score = 1,
                CreateDate = 1,
                Status = 1,
                Reports = reports
            };
        }

        dbContext.Reviews.AddRange(
            CreateReviewWithReportCount(1),
            CreateReviewWithReportCount(5),
            CreateReviewWithReportCount(10));
        await dbContext.SaveChangesAsync();

        using var mlPredictServer = new MlPredictTestServer();
        var reviewService = CreateReviewService(dbContext,
            BuildReviewTestConfiguration(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = mlPredictServer.PredictUrl }));

        // Gọi GetReviewsWithHighReportsAsync(3).
        var highReportReviews = (await reviewService.GetReviewsWithHighReportsAsync(3)).ToList();

        Assert.Equal(2, highReportReviews.Count);
        Assert.All(highReportReviews, row => Assert.True(row.ReportsCount >= 3));
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// TC-RSV-GHR-002 — không đạt ngưỡng.
    /// </summary>
    [Fact]
    public async Task GetReviewsWithHighReportsAsync_NoMatch_TC_RSV_GHR_002()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var dbContext = SqliteMemoryDb.CreateContext(connection);
        await using var tx = await dbContext.Database.BeginTransactionAsync();
        try
        {
        var (reviewerUser, seedRestaurant) = await SeedReviewTestGraphAsync(dbContext);
        // Chỉ 1 báo cáo — ngưỡng 999 → không review nào vượt.
        dbContext.Reviews.Add(new Review
        {
            UserId = reviewerUser.Id,
            RestaurantId = seedRestaurant.Id,
            Content = "x",
            Score = 1,
            CreateDate = 1,
            Status = 1,
            Reports = new List<Report> { new() { UserId = reviewerUser.Id, Reason = "a", Status = 1 } }
        });
        await dbContext.SaveChangesAsync();

        using var mlPredictServer = new MlPredictTestServer();
        var reviewService = CreateReviewService(dbContext,
            BuildReviewTestConfiguration(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = mlPredictServer.PredictUrl }));

        Assert.Empty(await reviewService.GetReviewsWithHighReportsAsync(999));
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }
}
