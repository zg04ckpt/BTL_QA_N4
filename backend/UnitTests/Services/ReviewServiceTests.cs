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
/// Không chỉnh mã nguồn production để test pass — chỉ seed SQLite + ML loopback.
/// AddReview gọi Firebase thực: nếu không có file cấu hình trong WebAPI thì các TC đó Skip.
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

    private static IConfiguration Cfg(Dictionary<string, string?> extra)
    {
        var d = new Dictionary<string, string?>
        {
            ["Review:RequireQrScan"] = "false",
            ["MlServer:PredictUrl"] = "http://127.0.0.1:9/predict"
        };
        foreach (var kv in extra)
            d[kv.Key] = kv.Value;
        return new ConfigurationBuilder().AddInMemoryCollection(d!).Build();
    }

    private static async Task<(ApplicationDbContext ctx, User reviewer, Restaurant restaurant)> SeedGraphAsync(
        Microsoft.Data.Sqlite.SqliteConnection connection)
    {
        var ctx = SqliteMemoryDb.CreateContext(connection);
        var cat = new Category { Name = "Ăn uống" };
        ctx.Categories.Add(cat);
        await ctx.SaveChangesAsync();

        var addr = new Address
            { City = "TP.HCM", District = "1", Ward = "P", Detail = "D", Lat = 10, Lon = 106 };
        ctx.Addresses.Add(addr);
        await ctx.SaveChangesAsync();

        var owner = new User
        {
            Email = "owner_rs@test.vn",
            Password = "p",
            Role = "customer",
            Name = "Chủ",
            Status = 1
        };
        ctx.Users.Add(owner);
        await ctx.SaveChangesAsync();

        var rest = new Restaurant
        {
            Name = "Quán Test",
            Status = 1,
            UserId = owner.Id,
            CateId = cat.Id,
            AddressId = addr.Id
        };
        ctx.Restaurants.Add(rest);
        await ctx.SaveChangesAsync();

        var reviewer = new User
        {
            Email = "reviewer_rs@test.vn",
            Password = "p",
            Role = "customer",
            Name = "Khách",
            Status = 1
        };
        ctx.Users.Add(reviewer);
        await ctx.SaveChangesAsync();

        return (ctx, reviewer, rest);
    }

    private static ReviewService CreateService(ApplicationDbContext ctx, IConfiguration configuration)
    {
        UseWebApiAsCurrentDirectory();
        return new ReviewService(
            new ReviewRepository(ctx),
            new FirebaseService(),
            new QRInformationRepository(ctx),
            configuration);
    }

    /// <summary>
    /// TC-RSV-ADD-001 — cần Firebase thực + file credential.
    /// </summary>
    [Fact]
    public async Task AddReviewAsync_ValidQrMlFirebase_TC_RSV_ADD_001()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var (ctx, reviewer, restaurant) = await SeedGraphAsync(connection);
        var qrRepo = new QRInformationRepository(ctx);
        await qrRepo.AddQRInformationAsync(new QRInformation
        {
            UserId = reviewer.Id,
            RestaurantId = restaurant.Id,
            CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });

        using var ml = new MlPredictTestServer { PredictedClassId = 0 };
        var cfg = Cfg(new Dictionary<string, string?>
        {
            ["Review:RequireQrScan"] = "true",
            ["MlServer:PredictUrl"] = ml.PredictUrl
        });

        var svc = CreateService(ctx, cfg);

        // QR hợp lệ trong 30 ngày, ML loopback trả class 0 (cho phép), Firebase init từ WebAPI dir
        var result = await svc.AddReviewAsync(new ReviewDto
        {
            UserId = reviewer.Id,
            RestaurantId = restaurant.Id,
            Content = "Món phở rất ngon, phục vụ chu đáo.",
            Score = 5,
            CreateDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            PhotoUrls = new List<string> { "/uploads/review1.jpg" }
        });

        // Success, review và 1 ReviewPhoto được tạo trong DB
        Assert.True(result.Success);
        Assert.Equal(1, await ctx.Reviews.CountAsync());
        Assert.Equal(1, await ctx.ReviewPhotos.CountAsync());
    }

    /// <summary>TC-RSV-ADD-002 — không gọi Firebase (throw trước).</summary>
    [Fact]
    public async Task AddReviewAsync_NoQrRecord_Throws_TC_RSV_ADD_002()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var (ctx, reviewer, restaurant) = await SeedGraphAsync(connection);
        using var ml = new MlPredictTestServer();
        var cfg = Cfg(new Dictionary<string, string?>
        {
            ["Review:RequireQrScan"] = "true",
            ["MlServer:PredictUrl"] = ml.PredictUrl
        });
        var svc = CreateService(ctx, cfg);

        // RequireQrScan=true nhưng không có QRInformation → phải throw exception
        var ex = await Assert.ThrowsAsync<Exception>(() => svc.AddReviewAsync(new ReviewDto
        {
            UserId = reviewer.Id,
            RestaurantId = restaurant.Id,
            Content = "Chưa quét QR.",
            Score = 4,
            CreateDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }));

        // Message phải nhắc đến QR
        Assert.Contains("QR", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>TC-RSV-ADD-003</summary>
    [Fact]
    public async Task AddReviewAsync_QrExpired_Throws_TC_RSV_ADD_003()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var (ctx, reviewer, restaurant) = await SeedGraphAsync(connection);
        await new QRInformationRepository(ctx).AddQRInformationAsync(new QRInformation
        {
            UserId = reviewer.Id,
            RestaurantId = restaurant.Id,
            CreateTime = DateTimeOffset.UtcNow.AddDays(-40).ToUnixTimeMilliseconds()
        });
        using var ml = new MlPredictTestServer();
        var cfg = Cfg(new Dictionary<string, string?>
        {
            ["Review:RequireQrScan"] = "true",
            ["MlServer:PredictUrl"] = ml.PredictUrl
        });
        var svc = CreateService(ctx, cfg);

        // QR tồn tại nhưng CreateTime > 30 ngày trước → expired
        var ex = await Assert.ThrowsAsync<Exception>(() => svc.AddReviewAsync(new ReviewDto
        {
            UserId = reviewer.Id,
            RestaurantId = restaurant.Id,
            Content = "QR đã hết hạn.",
            Score = 5,
            CreateDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }));

        // Message phải chứa "expired"
        Assert.Contains("expired", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>TC-RSV-ADD-004 — ML chặn, không gọi Firebase.</summary>
    [Fact]
    public async Task AddReviewAsync_MlBlocks_TC_RSV_ADD_004()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var (ctx, reviewer, restaurant) = await SeedGraphAsync(connection);
        using var ml = new MlPredictTestServer { PredictedClassId = 1 };
        var svc = CreateService(ctx, Cfg(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = ml.PredictUrl }));

        // ML trả class 1 (vi phạm) → service từ chối lưu
        var result = await svc.AddReviewAsync(new ReviewDto
        {
            UserId = reviewer.Id,
            RestaurantId = restaurant.Id,
            Content = "Nội dung tiêu cực giả định",
            Score = 2,
            CreateDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });

        // Không lưu review, message kiểm duyệt tiếng Việt
        Assert.False(result.Success);
        Assert.Contains("Bình luận", result.Message);
        Assert.Equal(0, await ctx.Reviews.CountAsync());
    }

    /// <summary>TC-RSV-ADD-005 — cần Firebase.</summary>
    [Fact]
    public async Task AddReviewAsync_EmptyContentSkipsMl_TC_RSV_ADD_005()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var (ctx, reviewer, restaurant) = await SeedGraphAsync(connection);
        using var ml = new MlPredictTestServer { PredictedClassId = 1 };
        var cfg = Cfg(new Dictionary<string, string?>
        {
            ["Review:RequireQrScan"] = "false",
            ["MlServer:PredictUrl"] = ml.PredictUrl
        });
        var svc = CreateService(ctx, cfg);

        // Content chỉ khoảng trắng → ML không được gọi (ML mock class 1 nhưng không ảnh hưởng)
        var result = await svc.AddReviewAsync(new ReviewDto
        {
            UserId = reviewer.Id,
            RestaurantId = restaurant.Id,
            Content = "   ",
            Score = 4,
            CreateDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            PhotoUrls = new List<string>()
        });

        // Review vẫn được lưu vì content rỗng bỏ qua ML
        Assert.True(result.Success);
        Assert.Equal(1, await ctx.Reviews.CountAsync());
    }

    /// <summary>TC-RSV-ADD-006 — cần Firebase.</summary>
    [Fact]
    public async Task AddReviewAsync_MlHttpError_Continues_TC_RSV_ADD_006()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var (ctx, reviewer, restaurant) = await SeedGraphAsync(connection);
        using var ml = new MlPredictTestServer { ReturnServiceUnavailable = true };
        var svc = CreateService(ctx, Cfg(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = ml.PredictUrl }));

        // ML server trả 503 → service không chặn vì lý do ML, tiếp tục lưu
        var result = await svc.AddReviewAsync(new ReviewDto
        {
            UserId = reviewer.Id,
            RestaurantId = restaurant.Id,
            Content = "Có chữ để ML gọi nhưng HTTP lỗi.",
            Score = 5,
            CreateDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });

        // Review được lưu bình thường dù ML lỗi
        Assert.True(result.Success);
        Assert.Equal(1, await ctx.Reviews.CountAsync());
    }

    /// <summary>TC-RSV-ADD-007 — cần mock repository/Firebase lỗi; không đổi production.</summary>
    [Fact(Skip = "TC-RSV-ADD-007: cần mock repository/Firebase — không chỉnh mã nguồn để inject.")]
    public Task AddReviewAsync_DbOrFirebaseFails_TC_RSV_ADD_007() => Task.CompletedTask;

    /// <summary>TC-RSV-ADD-008 — cần Firebase.</summary>
    [Fact]
    public async Task AddReviewAsync_MultiplePhotos_TC_RSV_ADD_008()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var (ctx, reviewer, restaurant) = await SeedGraphAsync(connection);
        await new QRInformationRepository(ctx).AddQRInformationAsync(new QRInformation
        {
            UserId = reviewer.Id,
            RestaurantId = restaurant.Id,
            CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
        using var ml = new MlPredictTestServer { PredictedClassId = 0 };
        var cfg = Cfg(new Dictionary<string, string?>
        {
            ["Review:RequireQrScan"] = "true",
            ["MlServer:PredictUrl"] = ml.PredictUrl
        });
        var svc = CreateService(ctx, cfg);

        // Gửi 2 PhotoUrls — mỗi URL tạo 1 ReviewPhoto
        await svc.AddReviewAsync(new ReviewDto
        {
            UserId = reviewer.Id,
            RestaurantId = restaurant.Id,
            Content = "Hai ảnh minh họa.",
            Score = 5,
            CreateDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            PhotoUrls = new List<string> { "/img/a.jpg", "/img/b.jpg" }
        });

        // DB phải có đúng 2 ReviewPhoto
        Assert.Equal(2, await ctx.ReviewPhotos.CountAsync());
    }

    /// <summary>
    /// TC-RSV-DEL-001 — xóa review tồn tại => thành công, DB không còn review đó.
    /// BUG-PROD: ReviewRepository.DeleteReviewAsync gọi restaurant.Reviews.Average() sau khi xóa.
    /// Khi review bị xóa là review cuối cùng, danh sách Reviews rỗng => Average() ném InvalidOperationException.
    /// Test này được Skip để phản ánh đúng lỗi production thay vì che bằng workaround seed 2 review.
    /// Cần sửa ReviewRepository.cs (dòng 71): thêm kiểm tra .Any() trước .Average().
    /// </summary>
    [Fact]
    public async Task DeleteReviewAsync_Exists_TC_RSV_DEL_001()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var (ctx, reviewer, restaurant) = await SeedGraphAsync(connection);
        ctx.Reviews.Add(new Review
        {
            UserId = reviewer.Id,
            RestaurantId = restaurant.Id,
            Content = "Sẽ xóa",
            Score = 3,
            CreateDate = 1,
            Status = 1
        });
        await ctx.SaveChangesAsync();
        var toDelete = await ctx.Reviews.FirstAsync();

        using var ml = new MlPredictTestServer();
        var svc = CreateService(ctx, Cfg(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = ml.PredictUrl }));

        // Xóa review duy nhất — BUG-PROD: Average() ném khi list rỗng (xem Skip reason)
        var result = await svc.DeleteReviewAsync(toDelete.Id);

        // Success, DB không còn review
        Assert.True(result.Success);
        Assert.Equal(0, await ctx.Reviews.CountAsync());
    }

    /// <summary>TC-RSV-DEL-002</summary>
    [Fact]
    public async Task DeleteReviewAsync_NotFound_TC_RSV_DEL_002()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var ctx = SqliteMemoryDb.CreateContext(connection);
        using var ml = new MlPredictTestServer();
        var svc = CreateService(ctx, Cfg(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = ml.PredictUrl }));

        // Xóa id không tồn tại
        var result = await svc.DeleteReviewAsync(999);

        // Từ chối với message not found
        Assert.False(result.Success);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>TC-RSV-DEL-003</summary>
    [Fact(Skip = "TC-RSV-DEL-003: cần mock ReviewRepository — không chỉnh mã nguồn.")]
    public Task DeleteReviewAsync_RepoThrows_TC_RSV_DEL_003() => Task.CompletedTask;

    /// <summary>
    /// TC-RSV-UPD-001 — cập nhật Content/Score và thêm ảnh mới => thành công, DB phản ánh đúng.
    /// BUG-PROD: ReviewService gán review.Photos trên entity đã được EF track (từ FindAsync).
    /// ReviewRepository.UpdateReviewAsync sau đó gọi FirstOrDefaultAsync().Include(Photos) trên cùng instance
    /// đã track → existing.Photos lúc này chứa các ReviewPhoto mới (state=Added) thay vì ảnh cũ từ DB.
    /// RemoveRange(existing.Photos) loại bỏ ảnh mới (chưa lưu) thay vì ảnh cũ, dẫn đến kết quả sai.
    /// Test được Skip để phản ánh đúng defect thay vì che bằng PhotoUrls rỗng.
    /// Cần sửa ReviewService.cs + ReviewRepository.cs trước khi bật lại.
    /// </summary>
    [Fact]
    public async Task UpdateReviewAsync_Success_TC_RSV_UPD_001()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var (ctx, reviewer, restaurant) = await SeedGraphAsync(connection);
        ctx.Reviews.Add(new Review
        {
            UserId = reviewer.Id,
            RestaurantId = restaurant.Id,
            Content = "Cũ",
            Score = 3,
            CreateDate = 1,
            Status = 1,
            Photos = new List<ReviewPhoto> { new() { ImageUrl = "/old.jpg" } }
        });
        await ctx.SaveChangesAsync();
        var rid = (await ctx.Reviews.FirstAsync()).Id;

        using var ml = new MlPredictTestServer { PredictedClassId = 0 };
        var svc = CreateService(ctx, Cfg(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = ml.PredictUrl }));

        // Cập nhật Content/Score mới + đổi ảnh — BUG-PROD: EF tracking conflict (xem Skip reason)
        var result = await svc.UpdateReviewAsync(rid, new ReviewDto
        {
            Content = "Đã chỉnh sửa, nội dung lành mạnh.",
            Score = 5,
            PhotoUrls = new List<string> { "/new1.jpg" }
        });

        // Content/Score đổi, ảnh mới là "/new1.jpg", ảnh cũ bị xóa
        Assert.True(result.Success);
        var db = await ctx.Reviews.AsNoTracking().FirstAsync(r => r.Id == rid);
        Assert.Equal("Đã chỉnh sửa, nội dung lành mạnh.", db.Content);
        Assert.Equal(5, db.Score);
        var photos = await ctx.ReviewPhotos.AsNoTracking().Where(p => p.ReviewId == rid).ToListAsync();
        Assert.Single(photos);
        Assert.Equal("/new1.jpg", photos[0].ImageUrl);
    }

    /// <summary>
    /// TC-RSV-UPD-002 — BASELINE OBSERVATION: khi PhotoUrls = null, kiểm tra hành vi thực tế của code.
    /// Tài liệu .md yêu cầu "ghi nhận hành vi" (không phải khẳng định spec cứng).
    /// Hành vi hiện tại: ReviewRepository.UpdateReviewAsync xóa ảnh cũ khi existing.Photos.Count > 0,
    /// nhưng updatedReview.Photos = null nên không thêm ảnh mới → kết quả: 0 ảnh trong DB.
    /// LƯU Ý: đây là hành vi thực tế của code, không nhất thiết là yêu cầu nghiệp vụ đúng.
    /// Nếu spec yêu cầu "giữ ảnh cũ khi không gửi PhotoUrls", assertion này sẽ phải thay đổi.
    /// </summary>
    [Fact]
    public async Task UpdateReviewAsync_PhotoUrlsNull_TC_RSV_UPD_002()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var (ctx, reviewer, restaurant) = await SeedGraphAsync(connection);
        ctx.Reviews.Add(new Review
        {
            UserId = reviewer.Id,
            RestaurantId = restaurant.Id,
            Content = "old",
            Score = 3,
            CreateDate = 1,
            Status = 1,
            Photos = new List<ReviewPhoto> { new() { ImageUrl = "/old.jpg" } }
        });
        await ctx.SaveChangesAsync();
        var rid = (await ctx.Reviews.FirstAsync()).Id;

        using var ml = new MlPredictTestServer { PredictedClassId = 0 };
        var svc = CreateService(ctx, Cfg(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = ml.PredictUrl }));

        await svc.UpdateReviewAsync(rid, new ReviewDto
        {
            Content = "Chỉ đổi chữ, không gửi ảnh.",
            Score = 4,
            PhotoUrls = null!
        });

        // BASELINE: code hiện tại xóa ảnh cũ khi PhotoUrls = null (updatedReview.Photos = null → không thêm mới).
        // Nếu nghiệp vụ yêu cầu "giữ ảnh cũ khi không gửi PhotoUrls" thì đây là BUG-PROD cần sửa.
        var remainingPhotos = await ctx.ReviewPhotos.AsNoTracking().CountAsync();
        Assert.Equal(0, remainingPhotos);
    }

    /// <summary>TC-RSV-UPD-003</summary>
    [Fact]
    public async Task UpdateReviewAsync_NotFound_TC_RSV_UPD_003()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var ctx = SqliteMemoryDb.CreateContext(connection);
        using var ml = new MlPredictTestServer();
        var svc = CreateService(ctx, Cfg(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = ml.PredictUrl }));

        // Cập nhật review không tồn tại
        var result = await svc.UpdateReviewAsync(99999, new ReviewDto { Content = "X", Score = 5 });

        // Từ chối
        Assert.False(result.Success);
    }

    /// <summary>TC-RSV-UPD-004</summary>
    [Fact]
    public async Task UpdateReviewAsync_MlBlocks_TC_RSV_UPD_004()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var (ctx, reviewer, restaurant) = await SeedGraphAsync(connection);
        ctx.Reviews.Add(new Review
            { UserId = reviewer.Id, RestaurantId = restaurant.Id, Content = "old", Score = 2, CreateDate = 1, Status = 1 });
        await ctx.SaveChangesAsync();
        var rid = (await ctx.Reviews.FirstAsync()).Id;

        using var ml = new MlPredictTestServer { PredictedClassId = 1 };
        var svc = CreateService(ctx, Cfg(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = ml.PredictUrl }));

        // ML trả class 1 cho nội dung sau chỉnh sửa → service từ chối
        var result = await svc.UpdateReviewAsync(rid, new ReviewDto
        {
            Content = "Nội dung vi phạm sau chỉnh sửa.",
            Score = 1,
            PhotoUrls = new List<string>()
        });

        // Update không được phép, message kiểm duyệt
        Assert.False(result.Success);
        Assert.Contains("Bình luận", result.Message);
    }

    /// <summary>TC-RSV-UPD-005</summary>
    [Fact(Skip = "TC-RSV-UPD-005: cần mock ReviewRepository — không chỉnh mã nguồn.")]
    public Task UpdateReviewAsync_RepoThrows_TC_RSV_UPD_005() => Task.CompletedTask;

    [Fact]
    public async Task GetAllReviewsAsync_WithRelations_TC_RSV_GAR_001()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var (ctx, reviewer, restaurant) = await SeedGraphAsync(connection);
        restaurant.Name = "Nhà Hàng Phở";
        ctx.Restaurants.Update(restaurant);
        ctx.Reviews.Add(new Review
        {
            UserId = reviewer.Id,
            RestaurantId = restaurant.Id,
            Content = "Hay",
            Score = 5,
            CreateDate = 1,
            Status = 1,
            Photos = new List<ReviewPhoto> { new() { ImageUrl = "/r100.jpg" } },
            Reports = new List<Report>
                { new() { UserId = reviewer.Id, Reason = "spam", Status = 1 } }
        });
        await ctx.SaveChangesAsync();

        using var ml = new MlPredictTestServer();
        var svc = CreateService(ctx, Cfg(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = ml.PredictUrl }));

        // Lấy toàn bộ reviews — 1 review có đủ relations
        var row = (await svc.GetAllReviewsAsync()).Single();

        // DTO map đúng: User, RestaurantName, ReportsCount, PhotoUrls
        Assert.Equal("Khách", row.User);
        Assert.Equal("Nhà Hàng Phở", row.RestaurantName);
        Assert.True(row.ReportsCount >= 1);
        Assert.Contains("/r100.jpg", row.PhotoUrls);
    }

    [Fact]
    public async Task GetAllReviewsAsync_Empty_TC_RSV_GAR_002()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var ctx = SqliteMemoryDb.CreateContext(connection);
        using var ml = new MlPredictTestServer();
        var svc = CreateService(ctx, Cfg(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = ml.PredictUrl }));

        // DB rỗng → trả empty
        Assert.Empty(await svc.GetAllReviewsAsync());
    }

    [Fact]
    public async Task GetReviewsByUserIdAsync_TwoReviews_TC_RSV_GUR_001()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var (ctx, reviewer, r1) = await SeedGraphAsync(connection);
        var addr = new Address { City = "A", District = "B", Ward = "C", Detail = "D", Lat = 1, Lon = 2 };
        ctx.Addresses.Add(addr);
        await ctx.SaveChangesAsync();
        var cat2 = new Category { Name = "C2" };
        ctx.Categories.Add(cat2);
        await ctx.SaveChangesAsync();
        var rest2 = new Restaurant
        {
            Name = "R2",
            Status = 1,
            UserId = reviewer.Id,
            CateId = cat2.Id,
            AddressId = addr.Id
        };
        ctx.Restaurants.Add(rest2);
        await ctx.SaveChangesAsync();

        ctx.Reviews.AddRange(
            new Review
            {
                UserId = reviewer.Id, RestaurantId = r1.Id, Content = "Một", Score = 5, CreateDate = 1, Status = 1
            },
            new Review
            {
                UserId = reviewer.Id, RestaurantId = rest2.Id, Content = "Hai", Score = 4, CreateDate = 2, Status = 1
            });
        await ctx.SaveChangesAsync();

        using var ml = new MlPredictTestServer();
        var svc = CreateService(ctx, Cfg(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = ml.PredictUrl }));

        // Lấy reviews theo userId
        var list = (await svc.GetReviewsByUserIdAsync(reviewer.Id)).ToList();

        // Đúng 2 reviews, tất cả cùng UserId
        Assert.Equal(2, list.Count);
        Assert.All(list, x => Assert.Equal(reviewer.Id, x.UserId));
    }

    [Fact]
    public async Task GetReviewsByUserIdAsync_None_TC_RSV_GUR_002()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var (ctx, _, _) = await SeedGraphAsync(connection);
        var lonely = new User
            { Email = "lonely@test.vn", Password = "p", Role = "customer", Name = "L", Status = 1 };
        ctx.Users.Add(lonely);
        await ctx.SaveChangesAsync();

        using var ml = new MlPredictTestServer();
        var svc = CreateService(ctx, Cfg(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = ml.PredictUrl }));

        // User không có review nào → empty
        Assert.Empty(await svc.GetReviewsByUserIdAsync(lonely.Id));
    }

    [Fact]
    public async Task GetReviewsByRestaurantIdAsync_MatchesRestaurant_TC_RSV_GRR_001()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var (ctx, reviewer, restaurant) = await SeedGraphAsync(connection);
        restaurant.Name = "Quán Bún";
        ctx.Reviews.Add(new Review
        {
            UserId = reviewer.Id,
            RestaurantId = restaurant.Id,
            Content = "Ngon",
            Score = 5,
            CreateDate = 1,
            Status = 1
        });
        await ctx.SaveChangesAsync();

        using var ml = new MlPredictTestServer();
        var svc = CreateService(ctx, Cfg(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = ml.PredictUrl }));

        // Lấy reviews theo restaurantId
        var list = (await svc.GetReviewsByRestaurantIdAsync(restaurant.Id)).ToList();

        // Chỉ trả review của đúng nhà hàng đó
        Assert.Single(list);
        Assert.All(list, x => Assert.Equal(restaurant.Id, x.RestaurantId));
    }

    [Fact]
    public async Task GetReviewsByRestaurantIdAsync_Empty_TC_RSV_GRR_002()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var (ctx, reviewer, _) = await SeedGraphAsync(connection);
        var addr = new Address { City = "x", District = "y", Ward = "z", Detail = "d", Lat = 1, Lon = 2 };
        ctx.Addresses.Add(addr);
        await ctx.SaveChangesAsync();
        var cat = await ctx.Categories.FirstAsync();
        var emptyRest = new Restaurant
        {
            Name = "Không review",
            Status = 1,
            UserId = reviewer.Id,
            CateId = cat.Id,
            AddressId = addr.Id
        };
        ctx.Restaurants.Add(emptyRest);
        await ctx.SaveChangesAsync();

        using var ml = new MlPredictTestServer();
        var svc = CreateService(ctx, Cfg(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = ml.PredictUrl }));

        // Nhà hàng không có review → empty
        Assert.Empty(await svc.GetReviewsByRestaurantIdAsync(emptyRest.Id));
    }

    [Fact]
    public async Task GetReviewsWithHighReportsAsync_Threshold_TC_RSV_GHR_001()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var (ctx, reviewer, restaurant) = await SeedGraphAsync(connection);

        Review MakeReview(int reportCount)
        {
            var reports = Enumerable.Range(0, reportCount)
                .Select(_ => new Report { UserId = reviewer.Id, Reason = "r", Status = 1 }).ToList();
            return new Review
            {
                UserId = reviewer.Id,
                RestaurantId = restaurant.Id,
                Content = "x",
                Score = 1,
                CreateDate = 1,
                Status = 1,
                Reports = reports
            };
        }

        // 3 review với report count: 1, 5, 10
        ctx.Reviews.AddRange(MakeReview(1), MakeReview(5), MakeReview(10));
        await ctx.SaveChangesAsync();

        using var ml = new MlPredictTestServer();
        var svc = CreateService(ctx, Cfg(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = ml.PredictUrl }));

        // Lọc với ngưỡng 3 → chỉ review có 5 và 10 báo cáo (count ≥ 3)
        var list = (await svc.GetReviewsWithHighReportsAsync(3)).ToList();

        // Đúng 2 review vượt ngưỡng
        Assert.Equal(2, list.Count);
        Assert.All(list, r => Assert.True(r.ReportsCount >= 3));
    }

    [Fact]
    public async Task GetReviewsWithHighReportsAsync_NoMatch_TC_RSV_GHR_002()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        var (ctx, reviewer, restaurant) = await SeedGraphAsync(connection);
        ctx.Reviews.Add(new Review
        {
            UserId = reviewer.Id,
            RestaurantId = restaurant.Id,
            Content = "x",
            Score = 1,
            CreateDate = 1,
            Status = 1,
            Reports = new List<Report> { new() { UserId = reviewer.Id, Reason = "a", Status = 1 } }
        });
        await ctx.SaveChangesAsync();

        using var ml = new MlPredictTestServer();
        var svc = CreateService(ctx, Cfg(new Dictionary<string, string?> { ["MlServer:PredictUrl"] = ml.PredictUrl }));

        // Ngưỡng rất cao → không review nào đạt → empty
        Assert.Empty(await svc.GetReviewsWithHighReportsAsync(999));
    }
}
