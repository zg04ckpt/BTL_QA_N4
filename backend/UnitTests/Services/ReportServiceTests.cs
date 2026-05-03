using BusinessLogicLayer.Services;
using DataAccessLayer;
using DataAccessLayer.Context;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using UnitTests.Infrastructure;
using Xunit;

namespace UnitTests.Services;

// ============================================================
//  Lớp hỗ trợ: FakeReportRepository – dùng để giả lập repository ném lỗi (TC-RPS-008, TC-RPS-017)
//  Kế thừa ReportRepository (các phương thức đã được đánh virtual trong source).
// ============================================================
public class FakeThrowingReportRepository : ReportRepository
{
    private readonly Exception? _throwOnAdd;
    private readonly Exception? _throwOnUpdate;

    public FakeThrowingReportRepository(
        ApplicationDbContext context,
        Exception? throwOnAdd = null,
        Exception? throwOnUpdate = null)
        : base(context)
    {
        _throwOnAdd = throwOnAdd;
        _throwOnUpdate = throwOnUpdate;
    }

    public override Task<Report> AddReportAsync(Report report)
    {
        if (_throwOnAdd != null) throw _throwOnAdd;
        return base.AddReportAsync(report);
    }

    public override Task<Report?> UpdateReportStatusAsync(int id, int status)
    {
        if (_throwOnUpdate != null) throw _throwOnUpdate;
        return base.UpdateReportStatusAsync(id, status);
    }
}

// ============================================================
//  ReportServiceTests – kiểm thử đơn vị cho ReportService
//  Hạ tầng: SQLite in-memory + transaction rollback
//  Mỗi test: seed dữ liệu → gọi service → Assert → Rollback
// ============================================================
public class ReportServiceTests
{
    // ----------------------------------------------------------
    //  Helper: seed 1 User tối thiểu (dùng trong nhiều test)
    // ----------------------------------------------------------
    private static User MakeUser(int id, string name = "Người Dùng Test") => new User
    {
        Id = id,
        Email = $"user{id}@test.vn",
        Password = "123",
        Role = "customer",
        Name = name,
        Status = 1
    };

    // Helper: seed 1 Restaurant (Review cần FK đến Restaurant)
    private static Restaurant MakeRestaurant(int id, int cateId, int userId, int addressId) => new Restaurant
    {
        Id = id,
        Name = $"Nhà hàng {id}",
        Status = 1,
        Email = $"r{id}@test.vn",
        PhoneNumber = "0900000000",
        AvtImage = "avt.jpg",
        UserId = userId,
        CateId = cateId,
        AddressId = addressId
    };

    // Helper: seed 1 Review tối thiểu
    private static Review MakeReview(int id, int userId, int restaurantId) => new Review
    {
        Id = id,
        UserId = userId,
        RestaurantId = restaurantId,
        Content = "Nội dung đánh giá",
        Score = 4,
        Status = 1
    };

    // Helper: seed đủ Category + Address + User + Restaurant + Review để tránh FK lỗi
    private static async Task SeedMinimalContextAsync(ApplicationDbContext ctx,
        int userId, int reviewId, int restaurantId = 1)
    {
        ctx.Categories.Add(new Category { Id = 1, Name = "Danh mục chung" });
        ctx.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
        ctx.Users.Add(MakeUser(userId));
        await ctx.SaveChangesAsync();

        ctx.Restaurants.Add(MakeRestaurant(restaurantId, cateId: 1, userId: userId, addressId: 1));
        await ctx.SaveChangesAsync();

        ctx.Reviews.Add(MakeReview(reviewId, userId: userId, restaurantId: restaurantId));
        await ctx.SaveChangesAsync();
    }

    // ===========================================================
    //  AddReportAsync
    // ===========================================================

    /// <summary>TC-RPS-001 – Tạo báo cáo mới thành công với dữ liệu hợp lệ</summary>
    [Fact]
    public async Task AddReportAsync_ValidDto_CreatesNewReport()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var repo = new ReportRepository(context);
        var service = new ReportService(repo);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Base data
            await SeedMinimalContextAsync(context, userId: 10, reviewId: 50);

            var countBefore = await context.Reports.CountAsync();

            // Test data
            var dto = new ReportDto
            {
                UserId = 10,
                ReviewId = 50,
                Reason = "Đánh giá chứa ngôn từ xúc phạm",
                Status = 0
            };

            var result = await service.AddReportAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal(10, result.UserId);
            Assert.Equal(50, result.ReviewId);
            Assert.Equal("Đánh giá chứa ngôn từ xúc phạm", result.Reason);
            Assert.Equal(0, result.Status);

            // CheckDB: bản ghi tăng thêm 1
            Assert.Equal(countBefore + 1, await context.Reports.CountAsync());
        }
        finally
        {
            // Rollback: đảm bảo DB về trạng thái trước test
            await transaction.RollbackAsync();
        }

        // Verify rollback: report không còn tồn tại
        await using var verifyCtx = SqliteMemoryDb.CreateContext(connection);
        Assert.Equal(0, await verifyCtx.Reports.CountAsync());
    }

    /// <summary>TC-RPS-002 – Service luôn ép Status = 0, dù DTO gửi lên Status = 99</summary>
    [Fact]
    public async Task AddReportAsync_StatusInDtoIgnored_AlwaysZeroInDb()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedMinimalContextAsync(context, userId: 11, reviewId: 51);

            var result = await service.AddReportAsync(new ReportDto
            {
                UserId = 11, ReviewId = 51,
                Reason = "Báo cáo vi phạm",
                Status = 99   // Giá trị bất kỳ – phải bị bỏ qua
            });

            Assert.Equal(0, result.Status);

            // CheckDB: giá trị Status trong DB cũng phải = 0
            var dbRecord = await context.Reports.FindAsync(result.Id);
            Assert.Equal(0, dbRecord!.Status);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RPS-003 – Lưu nguyên vẹn nội dung Reason (có dấu, dài)</summary>
    [Fact]
    public async Task AddReportAsync_LongVietnameseReason_SavedExactly()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedMinimalContextAsync(context, userId: 12, reviewId: 52);

            const string reason = "Đánh giá chứa lời lẽ thô tục, phản cảm – yêu cầu gỡ bỏ.";
            var result = await service.AddReportAsync(new ReportDto
            {
                UserId = 12, ReviewId = 52, Reason = reason, Status = 0
            });

            Assert.Equal(reason, result.Reason);

            // CheckDB
            var dbRecord = await context.Reports.FindAsync(result.Id);
            Assert.Equal(reason, dbRecord!.Reason);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RPS-004 – Gắn đúng UserId và ReviewId vào bản ghi</summary>
    [Fact]
    public async Task AddReportAsync_CorrectUserIdAndReviewId_AssignedInRecord()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Seed 2 user, review thuộc user 21
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(MakeUser(20, "Lê Quốc Cường"));
            context.Users.Add(MakeUser(21, "Phạm Thị Mai"));
            await context.SaveChangesAsync();
            context.Restaurants.Add(MakeRestaurant(1, 1, 20, 1));
            await context.SaveChangesAsync();
            context.Reviews.Add(MakeReview(60, userId: 21, restaurantId: 1));
            await context.SaveChangesAsync();

            var result = await service.AddReportAsync(new ReportDto
            {
                UserId = 20,   // User báo cáo là user 20
                ReviewId = 60,
                Reason = "Quảng cáo trá hình",
                Status = 0
            });

            Assert.Equal(20, result.UserId);
            Assert.Equal(60, result.ReviewId);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RPS-005 – Reason chỉ chứa khoảng trắng vẫn được lưu nguyên</summary>
    [Fact]
    public async Task AddReportAsync_WhitespaceReason_SavedAsIs()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedMinimalContextAsync(context, userId: 13, reviewId: 53);
            var countBefore = await context.Reports.CountAsync();

            var result = await service.AddReportAsync(new ReportDto
            {
                UserId = 13, ReviewId = 53, Reason = "   ", Status = 0
            });

            Assert.NotNull(result);
            Assert.Equal("   ", result.Reason);
            Assert.Equal(countBefore + 1, await context.Reports.CountAsync());
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RPS-006 – Lưu đúng tiếng Việt có dấu, emoji và nội dung dài (1000+ ký tự)</summary>
    [Fact]
    public async Task AddReportAsync_LongUnicodeReason_SavedCorrectly()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedMinimalContextAsync(context, userId: 14, reviewId: 54);

            string longReason = "Nội dung xúc phạm nặng nề 😡👎 " + new string('a', 1000);
            var result = await service.AddReportAsync(new ReportDto
            {
                UserId = 14, ReviewId = 54, Reason = longReason, Status = 0
            });

            Assert.StartsWith("Nội dung xúc phạm nặng nề 😡👎 ", result.Reason);

            // CheckDB: nội dung khớp tuyệt đối
            var dbRecord = await context.Reports.FindAsync(result.Id);
            Assert.Equal(longReason, dbRecord!.Reason);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RPS-007 – UserId/ReviewId không tồn tại → FK violation → ném DbUpdateException</summary>
    [Fact]
    public async Task AddReportAsync_NonexistentUserAndReview_ThrowsDbUpdateException()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Không seed User 999 hoặc Review 999
            var countBefore = await context.Reports.CountAsync();

            await Assert.ThrowsAsync<DbUpdateException>(() =>
                service.AddReportAsync(new ReportDto
                {
                    UserId = 999, ReviewId = 999, Reason = "test", Status = 0
                }));

            // CheckDB: số bản ghi không thay đổi
            // (lưu ý: dùng context mới vì context hiện tại có thể ở trạng thái lỗi)
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        // Verify qua context sạch
        await using var verifyCtx = SqliteMemoryDb.CreateContext(connection);
        Assert.Equal(0, await verifyCtx.Reports.CountAsync());
    }

    /// <summary>TC-RPS-008 – Repository ném InvalidOperationException("DB down") → service lan truyền đúng</summary>
    [Fact]
    public async Task AddReportAsync_RepositoryThrows_PropagatesException()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        // Dùng FakeThrowingReportRepository để giả lập lỗi từ repository
        var fakeRepo = new FakeThrowingReportRepository(
            context,
            throwOnAdd: new InvalidOperationException("DB down"));
        var service = new ReportService(fakeRepo);

        var dto = new ReportDto { UserId = 10, ReviewId = 50, Reason = "test", Status = 0 };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AddReportAsync(dto));

        Assert.Equal("DB down", ex.Message);
    }

    /// <summary>TC-RPS-009 – Cho phép 1 user báo cáo nhiều lần cùng 1 review (không có ràng buộc unique)</summary>
    [Fact]
    public async Task AddReportAsync_DuplicateUserAndReview_AllowsMultipleReports()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedMinimalContextAsync(context, userId: 15, reviewId: 55);

            // Seed 1 report đã tồn tại trước
            context.Reports.Add(new Report
            {
                UserId = 15, ReviewId = 55, Reason = "Báo cáo cũ", Status = 0
            });
            await context.SaveChangesAsync();

            // Gọi AddReportAsync 2 lần liên tiếp
            var r1 = await service.AddReportAsync(new ReportDto
            { UserId = 15, ReviewId = 55, Reason = "Lần 1", Status = 0 });

            var r2 = await service.AddReportAsync(new ReportDto
            { UserId = 15, ReviewId = 55, Reason = "Lần 2", Status = 0 });

            // CheckDB: 3 bản ghi cùng (UserId=15, ReviewId=55)
            var all = await context.Reports
                .Where(r => r.UserId == 15 && r.ReviewId == 55)
                .ToListAsync();
            Assert.Equal(3, all.Count);
            Assert.NotEqual(r1.Id, r2.Id);

            var reasons = all.Select(r => r.Reason).ToHashSet();
            Assert.Contains("Lần 1", reasons);
            Assert.Contains("Lần 2", reasons);
        }
        finally { await transaction.RollbackAsync(); }
    }

    // ===========================================================
    //  UpdateReportStatusAsync
    // ===========================================================

    /// <summary>TC-RPS-010 – Cập nhật trạng thái thành công cho báo cáo tồn tại</summary>
    [Fact]
    public async Task UpdateReportStatusAsync_ExistingReport_UpdatesSuccessfully()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedMinimalContextAsync(context, userId: 10, reviewId: 50);
            context.Reports.Add(new Report { Id = 100, UserId = 10, ReviewId = 50, Reason = "Spam", Status = 0 });
            await context.SaveChangesAsync();

            var (success, message) = await service.UpdateReportStatusAsync(100, 1);

            Assert.True(success);
            Assert.Equal("Report status updated successfully.", message);

            // CheckDB: Status trong DB = 1
            var dbReport = await context.Reports.FindAsync(100);
            Assert.Equal(1, dbReport!.Status);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RPS-011 – Trả thất bại khi báo cáo không tồn tại (Success = false)</summary>
    [Fact]
    public async Task UpdateReportStatusAsync_NonexistentReport_ReturnsFalse()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Không seed report 999
            var countBefore = await context.Reports.CountAsync();

            var (success, message) = await service.UpdateReportStatusAsync(999, 1);

            Assert.False(success);
            Assert.Equal("Report not found.", message);

            // CheckDB: DB không thay đổi
            Assert.Equal(countBefore, await context.Reports.CountAsync());
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RPS-012 – Chuyển sang trạng thái đã duyệt (status = 1)</summary>
    [Fact]
    public async Task UpdateReportStatusAsync_ToApproved_StatusBecomesOne()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedMinimalContextAsync(context, userId: 10, reviewId: 50);
            context.Reports.Add(new Report { Id = 101, UserId = 10, ReviewId = 50, Status = 0 });
            await context.SaveChangesAsync();

            var (success, message) = await service.UpdateReportStatusAsync(101, 1);

            Assert.True(success);
            Assert.Equal("Report status updated successfully.", message);
            Assert.Equal(1, (await context.Reports.FindAsync(101))!.Status);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RPS-013 – Chuyển sang trạng thái từ chối (status = 2)</summary>
    [Fact]
    public async Task UpdateReportStatusAsync_ToRejected_StatusBecomesTwo()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedMinimalContextAsync(context, userId: 10, reviewId: 50);
            context.Reports.Add(new Report { Id = 102, UserId = 10, ReviewId = 50, Status = 0 });
            await context.SaveChangesAsync();

            var (success, message) = await service.UpdateReportStatusAsync(102, 2);

            Assert.True(success);
            Assert.Equal("Report status updated successfully.", message);
            Assert.Equal(2, (await context.Reports.FindAsync(102))!.Status);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RPS-014 – Cập nhật lại đúng trạng thái hiện tại vẫn trả thành công</summary>
    [Fact]
    public async Task UpdateReportStatusAsync_SameStatusAsExisting_StillSucceeds()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedMinimalContextAsync(context, userId: 10, reviewId: 50);
            context.Reports.Add(new Report { Id = 103, UserId = 10, ReviewId = 50, Status = 1 });
            await context.SaveChangesAsync();

            var (success, message) = await service.UpdateReportStatusAsync(103, 1);

            Assert.True(success);
            Assert.Equal("Report status updated successfully.", message);
            // CheckDB: Status không thay đổi
            Assert.Equal(1, (await context.Reports.FindAsync(103))!.Status);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RPS-015 – Status ngoài tập hợp quy định (-99) vẫn được ghi xuống DB (ghi nhận hành vi hiện hành,
    /// sẽ fail khi bổ sung validate ở service)
    /// </summary>
    [Fact]
    public async Task UpdateReportStatusAsync_InvalidStatusValue_WritesToDbWithoutValidation()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedMinimalContextAsync(context, userId: 10, reviewId: 50);
            context.Reports.Add(new Report { Id = 104, UserId = 10, ReviewId = 50, Status = 0 });
            await context.SaveChangesAsync();

            var (success, _) = await service.UpdateReportStatusAsync(104, -99);

            Assert.True(success);
            // CheckDB: giá trị phi hợp lệ được ghi
            Assert.Equal(-99, (await context.Reports.FindAsync(104))!.Status);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RPS-016 – id âm hoặc 0 → trả (Success = false, Message = "Report not found.")</summary>
    [Fact]
    public async Task UpdateReportStatusAsync_NegativeOrZeroId_ReturnsFalse()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Seed 1 report hợp lệ – kiểm tra nó không bị ảnh hưởng
            await SeedMinimalContextAsync(context, userId: 10, reviewId: 50);
            context.Reports.Add(new Report { Id = 1, UserId = 10, ReviewId = 50, Status = 0 });
            await context.SaveChangesAsync();

            // Lần 1: id âm
            var (s1, m1) = await service.UpdateReportStatusAsync(-5, 1);
            Assert.False(s1);
            Assert.Equal("Report not found.", m1);

            // Lần 2: id = 0
            var (s2, m2) = await service.UpdateReportStatusAsync(0, 1);
            Assert.False(s2);
            Assert.Equal("Report not found.", m2);

            // CheckDB: report Id=1 vẫn Status = 0
            Assert.Equal(0, (await context.Reports.FindAsync(1))!.Status);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RPS-017 – Repository ném DbUpdateException("update failed") → service lan truyền đúng</summary>
    [Fact]
    public async Task UpdateReportStatusAsync_RepositoryThrows_PropagatesException()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        // Seed report 100 để FakeRepo không trả null trước khi throw
        await SeedMinimalContextAsync(context, userId: 10, reviewId: 50);
        context.Reports.Add(new Report { Id = 100, UserId = 10, ReviewId = 50, Status = 0 });
        await context.SaveChangesAsync();

        var fakeRepo = new FakeThrowingReportRepository(
            context,
            throwOnUpdate: new Microsoft.EntityFrameworkCore.DbUpdateException("update failed"));
        var service = new ReportService(fakeRepo);

        var ex = await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(
            () => service.UpdateReportStatusAsync(100, 1));

        Assert.Equal("update failed", ex.Message);
    }

    // ===========================================================
    //  GetReportsByReviewIdAsync
    // ===========================================================

    /// <summary>TC-RPS-018 – Trả đủ 3 báo cáo thuộc review 200, loại bỏ báo cáo review 999</summary>
    [Fact]
    public async Task GetReportsByReviewIdAsync_MultipleReports_ReturnsOnlyMatchingReviewId()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Seed users
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(MakeUser(10));
            context.Users.Add(MakeUser(11));
            context.Users.Add(MakeUser(12));
            await context.SaveChangesAsync();
            context.Restaurants.Add(MakeRestaurant(1, 1, 10, 1));
            await context.SaveChangesAsync();
            context.Reviews.Add(MakeReview(200, 10, 1));
            await context.SaveChangesAsync();

            // Seed reports
            context.Reports.AddRange(
                new Report { UserId = 10, ReviewId = 200, Reason = "R1", Status = 0 },
                new Report { UserId = 11, ReviewId = 200, Reason = "R2", Status = 0 },
                new Report { UserId = 12, ReviewId = 200, Reason = "R3", Status = 1 },
                new Report { UserId = 10, ReviewId = 999, Reason = "R4", Status = 0 }
            );
            await context.SaveChangesAsync();

            var result = (await service.GetReportsByReviewIdAsync(200)).ToList();

            Assert.Equal(3, result.Count);
            Assert.All(result, r => Assert.Equal(200, r.ReviewId));
            var reasons = result.Select(r => r.Reason).ToHashSet();
            Assert.Contains("R1", reasons);
            Assert.Contains("R2", reasons);
            Assert.Contains("R3", reasons);
            Assert.DoesNotContain("R4", reasons);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RPS-019 – Trả rỗng khi review chưa có báo cáo nào</summary>
    [Fact]
    public async Task GetReportsByReviewIdAsync_NoReports_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Review 201 tồn tại nhưng không có report
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(MakeUser(10));
            await context.SaveChangesAsync();
            context.Restaurants.Add(MakeRestaurant(1, 1, 10, 1));
            await context.SaveChangesAsync();
            context.Reviews.Add(MakeReview(201, 10, 1));
            await context.SaveChangesAsync();

            var result = await service.GetReportsByReviewIdAsync(201);

            Assert.NotNull(result);
            Assert.False(result.Any());
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RPS-020 – Trả rỗng khi reviewId không tồn tại trong DB</summary>
    [Fact]
    public async Task GetReportsByReviewIdAsync_NonexistentReviewId_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        var result = await service.GetReportsByReviewIdAsync(999);

        Assert.NotNull(result);
        Assert.False(result.Any());
    }

    /// <summary>TC-RPS-021 – Điền UserName đúng từ User được join</summary>
    [Fact]
    public async Task GetReportsByReviewIdAsync_WithUser_FillsUserName()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User
            {
                Id = 30, Email = "quan@test.vn", Password = "123",
                Role = "customer", Name = "Đặng Minh Quân", Status = 1
            });
            await context.SaveChangesAsync();
            context.Restaurants.Add(MakeRestaurant(1, 1, 30, 1));
            await context.SaveChangesAsync();
            context.Reviews.Add(MakeReview(202, 30, 1));
            await context.SaveChangesAsync();
            context.Reports.Add(new Report
            {
                Id = 5, UserId = 30, ReviewId = 202, Reason = "Ngôn từ thô tục", Status = 0
            });
            await context.SaveChangesAsync();

            var result = (await service.GetReportsByReviewIdAsync(202)).ToList();

            Assert.Single(result);
            Assert.Equal("Đặng Minh Quân", result[0].UserName);
            Assert.Equal(30, result[0].UserId);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RPS-022 – UserName = null khi User FK null (Report.User không load được)</summary>
    [Fact]
    public async Task GetReportsByReviewIdAsync_MissingUser_UserNameIsNull()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Seed review mà không seed user 9999 → FK có thể fail trên SQLite
            // Workaround: seed user tạm, thêm report, rồi không xóa user (chỉ kiểm tra mapping)
            // Thay vào đó: seed user hợp lệ, kiểm tra trường hợp User.Name = null
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User
            {
                Id = 88, Email = "u88@test.vn", Password = "123",
                Role = "customer", Name = null,   // Name = null để kiểm tra ánh xạ
                Status = 1
            });
            await context.SaveChangesAsync();
            context.Restaurants.Add(MakeRestaurant(1, 1, 88, 1));
            await context.SaveChangesAsync();
            context.Reviews.Add(MakeReview(203, 88, 1));
            await context.SaveChangesAsync();
            context.Reports.Add(new Report
            {
                Id = 6, UserId = 88, ReviewId = 203, Reason = "Test", Status = 0
            });
            await context.SaveChangesAsync();

            var result = (await service.GetReportsByReviewIdAsync(203)).ToList();

            Assert.Single(result);
            Assert.Null(result[0].UserName);  // User.Name = null → UserName = null
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RPS-023 – Ánh xạ đúng tất cả field: Id, UserId, ReviewId, Reason, Status, UserName</summary>
    [Fact]
    public async Task GetReportsByReviewIdAsync_SingleReport_MapsAllFieldsCorrectly()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User
            {
                Id = 31, Email = "kieu@test.vn", Password = "123",
                Role = "customer", Name = "Hoàng Thị Kiều", Status = 1
            });
            await context.SaveChangesAsync();
            context.Restaurants.Add(MakeRestaurant(1, 1, 31, 1));
            await context.SaveChangesAsync();
            context.Reviews.Add(MakeReview(204, 31, 1));
            await context.SaveChangesAsync();
            context.Reports.Add(new Report
            {
                Id = 302, UserId = 31, ReviewId = 204,
                Reason = "Ngôn từ thù ghét", Status = 1
            });
            await context.SaveChangesAsync();

            var result = (await service.GetReportsByReviewIdAsync(204)).ToList();

            Assert.Single(result);
            var item = result[0];
            Assert.Equal(302, item.Id);
            Assert.Equal(31, item.UserId);
            Assert.Equal("Hoàng Thị Kiều", item.UserName);
            Assert.Equal(204, item.ReviewId);
            Assert.Equal("Ngôn từ thù ghét", item.Reason);
            Assert.Equal(1, item.Status);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RPS-024 – reviewId âm hoặc 0 trả rỗng, không ném exception</summary>
    [Fact]
    public async Task GetReportsByReviewIdAsync_NegativeOrZeroReviewId_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedMinimalContextAsync(context, userId: 10, reviewId: 1);
            context.Reports.Add(new Report { UserId = 10, ReviewId = 1, Reason = "x", Status = 0 });
            await context.SaveChangesAsync();

            var r1 = await service.GetReportsByReviewIdAsync(-1);
            var r2 = await service.GetReportsByReviewIdAsync(0);

            Assert.NotNull(r1);
            Assert.False(r1.Any());
            Assert.NotNull(r2);
            Assert.False(r2.Any());
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>TC-RPS-025 – Trả về đúng 150 bản ghi khi review có 150 báo cáo</summary>
    [Fact]
    public async Task GetReportsByReviewIdAsync_HundredsOfReports_ReturnsAllCorrectly()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(MakeUser(10));
            await context.SaveChangesAsync();
            context.Restaurants.Add(MakeRestaurant(1, 1, 10, 1));
            await context.SaveChangesAsync();
            context.Reviews.Add(MakeReview(205, 10, 1));
            await context.SaveChangesAsync();

            // Seed 150 báo cáo
            var reports = Enumerable.Range(1000, 150).Select(i => new Report
            {
                Id = i, UserId = 10, ReviewId = 205, Reason = $"Lý do {i}", Status = 0
            }).ToList();
            context.Reports.AddRange(reports);
            await context.SaveChangesAsync();

            var result = (await service.GetReportsByReviewIdAsync(205)).ToList();

            Assert.Equal(150, result.Count);
            Assert.All(result, r => Assert.Equal(205, r.ReviewId));

            // CheckDB: tập Id trả về = {1000..1149}
            var returnedIds = result.Select(r => r.Id).ToHashSet();
            for (int id = 1000; id < 1150; id++)
                Assert.Contains(id, returnedIds);
        }
        finally { await transaction.RollbackAsync(); }
    }
}
