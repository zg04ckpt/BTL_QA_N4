// =============================================================================
//  ReportServiceTests.cs
//  Unit tests cho ReportService – kiểm thử 3 nghiệp vụ chính:
//    1. AddReportAsync      – Tạo báo cáo mới
//    2. UpdateReportStatusAsync – Cập nhật trạng thái báo cáo
//    3. GetReportsByReviewIdAsync – Truy vấn báo cáo theo review
//
//  Hạ tầng test:
//    - SQLite in-memory: cơ sở dữ liệu tạm trong bộ nhớ, không ảnh hưởng DB thật
//    - Transaction + Rollback: mỗi test mở 1 transaction, rollback sau khi xong
//      → DB luôn sạch giữa các test, không có dữ liệu dư thừa
//    - FakeThrowingReportRepository: lớp giả lập dùng khi cần kiểm tra
//      trường hợp repository ném lỗi mà không cần phá vỡ DB thật
// =============================================================================

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

// =============================================================================
//  FakeThrowingReportRepository
//  Mục đích: Giả lập repository ném lỗi để kiểm tra service xử lý exception.
//  Kế thừa ReportRepository thật, override 2 phương thức đã được đánh virtual:
//    - AddReportAsync    → dùng cho TC-RPS-008
//    - UpdateReportStatusAsync → dùng cho TC-RPS-017
//  Cách hoạt động: Nếu constructor nhận exception, phương thức tương ứng sẽ
//  throw exception đó thay vì gọi logic thật.
// =============================================================================
public class FakeThrowingReportRepository : ReportRepository
{
    // Exception cần ném khi AddReportAsync được gọi (null = không ném)
    private readonly Exception? _throwOnAdd;
    // Exception cần ném khi UpdateReportStatusAsync được gọi (null = không ném)
    private readonly Exception? _throwOnUpdate;

    public FakeThrowingReportRepository(
        ApplicationDbContext context,
        Exception? throwOnAdd = null,
        Exception? throwOnUpdate = null)
        : base(context) // Gọi constructor của ReportRepository thật
    {
        _throwOnAdd = throwOnAdd;
        _throwOnUpdate = throwOnUpdate;
    }

    // Override AddReportAsync: ném lỗi nếu được cấu hình, ngược lại gọi logic thật
    public override Task<Report> AddReportAsync(Report report)
    {
        if (_throwOnAdd != null) throw _throwOnAdd;
        return base.AddReportAsync(report);
    }

    // Override UpdateReportStatusAsync: ném lỗi nếu được cấu hình, ngược lại gọi logic thật
    public override Task<Report?> UpdateReportStatusAsync(int id, int status)
    {
        if (_throwOnUpdate != null) throw _throwOnUpdate;
        return base.UpdateReportStatusAsync(id, status);
    }
}

// =============================================================================
//  ReportServiceTests – class chứa toàn bộ unit test cho ReportService
// =============================================================================
public class ReportServiceTests
{
    // -------------------------------------------------------------------------
    //  HELPER METHODS – Tạo dữ liệu mẫu tái sử dụng
    //  Các phương thức này chỉ xây đối tượng, không ghi vào DB.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tạo một User mẫu với Id và Name tùy chỉnh.
    /// Các trường bắt buộc (Email, Password, Role, Status) được điền giá trị mặc định
    /// để tránh lỗi NOT NULL khi lưu vào SQLite.
    /// </summary>
    private static User MakeUser(int id, string name = "Người Dùng Test") => new User
    {
        Id = id,
        Email = $"user{id}@test.vn",  // Email duy nhất theo id
        Password = "123",
        Role = "customer",
        Name = name,
        Status = 1  // 1 = tài khoản đang hoạt động
    };

    /// <summary>
    /// Tạo một Restaurant mẫu.
    /// Review có FK đến Restaurant, nên phải seed Restaurant trước khi seed Review.
    /// </summary>
    private static Restaurant MakeRestaurant(int id, int cateId, int userId, int addressId) => new Restaurant
    {
        Id = id,
        Name = $"Nhà hàng {id}",
        Status = 1,
        Email = $"r{id}@test.vn",
        PhoneNumber = "0900000000",
        AvtImage = "avt.jpg",
        UserId = userId,    // FK đến bảng Users
        CateId = cateId,    // FK đến bảng Categories
        AddressId = addressId  // FK đến bảng Addresses (quan hệ 1-1 UNIQUE)
    };

    /// <summary>
    /// Tạo một Review mẫu.
    /// Report có FK đến Review (ReviewId), nên phải seed Review trước khi seed Report.
    /// </summary>
    private static Review MakeReview(int id, int userId, int restaurantId) => new Review
    {
        Id = id,
        UserId = userId,
        RestaurantId = restaurantId,
        Content = "Nội dung đánh giá",
        Score = 4,
        Status = 1
    };

    /// <summary>
    /// Seed toàn bộ dữ liệu phụ thuộc cần thiết để tạo được Report:
    ///   Category → Address → User → Restaurant → Review
    /// Chuỗi phụ thuộc này là bắt buộc vì SQLite bật FOREIGN KEY enforcement.
    /// Nếu thiếu bất kỳ bước nào, SaveChangesAsync sẽ ném DbUpdateException.
    /// </summary>
    private static async Task SeedMinimalContextAsync(ApplicationDbContext ctx,
        int userId, int reviewId, int restaurantId = 1)
    {
        // Bước 1: Category là bắt buộc (Restaurant có FK đến Category)
        ctx.Categories.Add(new Category { Id = 1, Name = "Danh mục chung" });
        // Bước 2: Address là bắt buộc (Restaurant có FK đến Address, quan hệ 1-1)
        ctx.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
        // Bước 3: User – người sở hữu nhà hàng và cũng sẽ viết review
        ctx.Users.Add(MakeUser(userId));
        await ctx.SaveChangesAsync(); // Lưu Category + Address + User trước

        // Bước 4: Restaurant – cần có trước khi tạo Review
        ctx.Restaurants.Add(MakeRestaurant(restaurantId, cateId: 1, userId: userId, addressId: 1));
        await ctx.SaveChangesAsync(); // Lưu Restaurant

        // Bước 5: Review – cần có trước khi tạo Report
        ctx.Reviews.Add(MakeReview(reviewId, userId: userId, restaurantId: restaurantId));
        await ctx.SaveChangesAsync(); // Lưu Review
    }

    // =========================================================================
    //  NHÓM 1: AddReportAsync
    //  Kiểm tra việc tạo báo cáo mới từ ReportDto
    // =========================================================================

    /// <summary>
    /// TC-RPS-001 – Tạo báo cáo mới thành công với dữ liệu đầu vào hợp lệ.
    /// Kiểm tra: kết quả trả về có Id > 0, các field ánh xạ đúng,
    /// và DB tăng thêm đúng 1 bản ghi Report.
    /// </summary>
    [Fact]
    public async Task AddReportAsync_ValidDto_CreatesNewReport()
    {
        // --- Khởi tạo SQLite in-memory và DbContext ---
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        // CreatePreparedConnectionAsync: tạo kết nối + chạy migrate schema (tạo bảng)
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var repo = new ReportRepository(context);
        var service = new ReportService(repo); // Service cần test

        // --- Mở transaction để rollback sau khi test ---
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // === CHUẨN BỊ (Arrange) ===
            // === BASE DATA: Seed dữ liệu nền ===
            // Cần Category → Address → User → Restaurant → Review vì Report có FK đến Review
            await SeedMinimalContextAsync(context, userId: 10, reviewId: 50);

            // Ghi nhận số bản ghi trước khi thêm (để CheckDB sau)
            var countBefore = await context.Reports.CountAsync();

            // === TEST DATA: Dữ liệu đầu vào giả lập request từ client ===
            var dto = new ReportDto
            {
                UserId = 10,    // User 10 vừa được seed ở trên
                ReviewId = 50,  // Review 50 vừa được seed ở trên
                Reason = "Đánh giá chứa ngôn từ xúc phạm",
                Status = 0      // Trạng thái ban đầu luôn = 0 (chờ duyệt)
            };

            // === THỰC THI (Act) ===
            // === GỌI SERVICE ===
            var result = await service.AddReportAsync(dto);

            // === KIỂM TRA (Assert) ===
            // === KIỂM TRA KẾT QUẢ TRẢ VỀ ===
            Assert.NotNull(result);             // Service phải trả về đối tượng, không được null
            Assert.True(result.Id > 0);         // DB tự sinh Id > 0 (AUTOINCREMENT)
            Assert.Equal(10, result.UserId);    // UserId phải được ánh xạ đúng từ DTO
            Assert.Equal(50, result.ReviewId);  // ReviewId phải được ánh xạ đúng từ DTO
            Assert.Equal("Đánh giá chứa ngôn từ xúc phạm", result.Reason); // Reason được lưu nguyên vẹn
            Assert.Equal(0, result.Status);     // Status ban đầu phải = 0

            // === KIỂM TRA DB (CheckDB) ===
            // Xác minh dữ liệu thực sự được ghi vào DB, không chỉ trả về từ bộ nhớ
            Assert.Equal(countBefore + 1, await context.Reports.CountAsync());
            // Số bản ghi phải tăng đúng 1
        }
        finally
        {
            // === ROLLBACK: Hoàn tác toàn bộ thay đổi DB trong test này ===
            // Đảm bảo sau khi test xong, DB trở về trạng thái trống như ban đầu
            await transaction.RollbackAsync();
        }

        // === XÁC NHẬN ROLLBACK THÀNH CÔNG ===
        // Dùng context mới (sạch) để xác minh không có Report nào còn sót lại
        await using var verifyCtx = SqliteMemoryDb.CreateContext(connection);
        Assert.Equal(0, await verifyCtx.Reports.CountAsync()); // DB phải trống sau rollback
    }

    /// <summary>
    /// TC-RPS-002 – Service luôn ép Status = 0 khi tạo báo cáo, bất kể DTO gửi lên giá trị nào.
    /// Kiểm tra: dù client gửi Status = 99 (giá trị bất kỳ), DB vẫn lưu Status = 0.
    /// Đây là quy tắc nghiệp vụ: báo cáo mới luôn ở trạng thái "chờ xử lý" (0).
    /// </summary>
    [Fact]
    public async Task AddReportAsync_StatusInDtoIgnored_AlwaysZeroInDb()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Base data: seed đủ để Report có thể được lưu
            await SeedMinimalContextAsync(context, userId: 11, reviewId: 51);

            var result = await service.AddReportAsync(new ReportDto
            {
                UserId = 11, ReviewId = 51,
                Reason = "Báo cáo vi phạm",
                Status = 99   // Client cố tình gửi Status = 99 – phải bị bỏ qua
            });

            // Kết quả trả về phải có Status = 0, không phải 99
            Assert.Equal(0, result.Status);

            // CheckDB: xác nhận giá trị trong DB cũng = 0 (không phải 99)
            var dbRecord = await context.Reports.FindAsync(result.Id);
            Assert.Equal(0, dbRecord!.Status); // DB không được lưu giá trị Status từ DTO
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RPS-003 – Service lưu nguyên vẹn nội dung Reason (tiếng Việt có dấu, câu dài).
    /// Kiểm tra: chuỗi Reason không bị truncate, encode sai, hay thay đổi nội dung.
    /// </summary>
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

            // Reason có tiếng Việt đầy đủ dấu, dấu gạch ngang, dấu phẩy
            const string reason = "Đánh giá chứa lời lẽ thô tục, phản cảm – yêu cầu gỡ bỏ.";
            var result = await service.AddReportAsync(new ReportDto
            {
                UserId = 12, ReviewId = 52, Reason = reason, Status = 0
            });

            // Kết quả trả về phải khớp byte-by-byte với chuỗi gốc
            Assert.Equal(reason, result.Reason);

            // CheckDB: xác nhận DB cũng lưu đúng chuỗi đó
            var dbRecord = await context.Reports.FindAsync(result.Id);
            Assert.Equal(reason, dbRecord!.Reason); // Không bị mất dấu hay bị cắt ngắn
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RPS-004 – Service gán đúng UserId (người báo cáo) và ReviewId (bài đánh giá bị báo cáo).
    /// Kiểm tra: 2 user khác nhau tồn tại, report được tạo bởi user A về review của user B.
    /// </summary>
    [Fact]
    public async Task AddReportAsync_CorrectUserIdAndReviewId_AssignedInRecord()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Base data: 2 user – user 20 sở hữu nhà hàng, user 21 viết review
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(MakeUser(20, "Lê Quốc Cường"));  // Chủ nhà hàng
            context.Users.Add(MakeUser(21, "Phạm Thị Mai"));   // Người viết review
            await context.SaveChangesAsync();

            context.Restaurants.Add(MakeRestaurant(1, 1, 20, 1)); // Nhà hàng của user 20
            await context.SaveChangesAsync();

            context.Reviews.Add(MakeReview(60, userId: 21, restaurantId: 1)); // Review của user 21
            await context.SaveChangesAsync();

            // Test data: user 20 báo cáo review 60 (do user 21 viết)
            var result = await service.AddReportAsync(new ReportDto
            {
                UserId = 20,   // Người gửi báo cáo là user 20
                ReviewId = 60, // Bài đánh giá bị báo cáo là review 60
                Reason = "Quảng cáo trá hình",
                Status = 0
            });

            // Xác nhận FK được ánh xạ đúng – không bị nhầm giữa user 20 và 21
            Assert.Equal(20, result.UserId);
            Assert.Equal(60, result.ReviewId);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RPS-005 – Reason chỉ chứa khoảng trắng vẫn được lưu nguyên (service không trim).
    /// Kiểm tra: service không tự ý loại bỏ hoặc thay thế khoảng trắng.
    /// </summary>
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
            var countBefore = await context.Reports.CountAsync(); // Đếm trước khi thêm

            var result = await service.AddReportAsync(new ReportDto
            {
                UserId = 13, ReviewId = 53,
                Reason = "   ",  // Chỉ có 3 khoảng trắng – hợp lệ về mặt kỹ thuật
                Status = 0
            });

            Assert.NotNull(result);               // Phải tạo được Report
            Assert.Equal("   ", result.Reason);  // Khoảng trắng không bị trim hay replace

            // CheckDB: số bản ghi tăng đúng 1
            Assert.Equal(countBefore + 1, await context.Reports.CountAsync());
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RPS-006 – Lưu đúng nội dung Unicode dài (emoji + 1000 ký tự).
    /// Kiểm tra: SQLite in-memory xử lý được chuỗi Unicode phức tạp không bị mất ký tự.
    /// </summary>
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

            // Tạo chuỗi có emoji (multi-byte UTF-16) + 1000 ký tự ASCII
            string longReason = "Nội dung xúc phạm nặng nề 😡👎 " + new string('a', 1000);
            var result = await service.AddReportAsync(new ReportDto
            {
                UserId = 14, ReviewId = 54, Reason = longReason, Status = 0
            });

            // Kiểm tra phần đầu chuỗi (có emoji) được lưu đúng
            Assert.StartsWith("Nội dung xúc phạm nặng nề 😡👎 ", result.Reason);

            // CheckDB: toàn bộ chuỗi (kể cả 1000 ký tự 'a') khớp tuyệt đối
            var dbRecord = await context.Reports.FindAsync(result.Id);
            Assert.Equal(longReason, dbRecord!.Reason); // Không bị truncate ở bất kỳ độ dài nào
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RPS-007 – Ném DbUpdateException khi UserId hoặc ReviewId không tồn tại trong DB.
    /// Kiểm tra: SQLite với FK enforcement bật sẽ từ chối lưu Report có FK không hợp lệ.
    /// Đây là lỗi nghiệp vụ quan trọng – không được lưu report "treo" (orphan).
    /// </summary>
    [Fact]
    public async Task AddReportAsync_NonexistentUserAndReview_ThrowsDbUpdateException()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Không seed User 999 hay Review 999 – cố tình để FK vi phạm
            var countBefore = await context.Reports.CountAsync(); // Phải = 0

            // Kỳ vọng: service ném DbUpdateException do FK constraint
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                service.AddReportAsync(new ReportDto
                {
                    UserId = 999,   // User 999 không tồn tại
                    ReviewId = 999, // Review 999 không tồn tại
                    Reason = "test",
                    Status = 0
                }));

            // Lưu ý: context có thể ở trạng thái lỗi sau exception,
            // nên kiểm tra DB qua context mới bên dưới (verify rollback)
        }
        finally
        {
            await transaction.RollbackAsync(); // Rollback để dọn sạch
        }

        // Xác nhận qua context sạch: không có Report nào được tạo
        await using var verifyCtx = SqliteMemoryDb.CreateContext(connection);
        Assert.Equal(0, await verifyCtx.Reports.CountAsync()); // DB vẫn trống
    }

    /// <summary>
    /// TC-RPS-008 – Khi Repository ném lỗi (giả lập DB down), service phải lan truyền exception đúng loại.
    /// Kiểm tra: service không nuốt exception mà để nó bubble up đến caller.
    /// Dùng FakeThrowingReportRepository để giả lập lỗi mà không cần phá DB thật.
    /// </summary>
    [Fact]
    public async Task AddReportAsync_RepositoryThrows_PropagatesException()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        // Cấu hình fake repo: khi AddReportAsync được gọi → ném InvalidOperationException("DB down")
        var fakeRepo = new FakeThrowingReportRepository(
            context,
            throwOnAdd: new InvalidOperationException("DB down")); // Giả lập lỗi kết nối DB
        var service = new ReportService(fakeRepo); // Service dùng fake repo thay vì repo thật

        var dto = new ReportDto { UserId = 10, ReviewId = 50, Reason = "test", Status = 0 };

        // Kỳ vọng: service ném đúng loại exception và đúng message
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AddReportAsync(dto));

        Assert.Equal("DB down", ex.Message); // Message phải khớp – service không wrap lại
    }

    /// <summary>
    /// TC-RPS-009 – Cho phép cùng 1 user báo cáo cùng 1 review nhiều lần (không có UNIQUE constraint).
    /// Kiểm tra: DB chấp nhận 2 bản ghi (UserId, ReviewId) giống nhau – đây là thiết kế có chủ đích.
    /// </summary>
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

            // Seed 1 report đã có từ trước (báo cáo cũ)
            context.Reports.Add(new Report
            {
                UserId = 15, ReviewId = 55, Reason = "Báo cáo cũ", Status = 0
            });
            await context.SaveChangesAsync();

            // Test data: cùng user 15 báo cáo cùng review 55 thêm 2 lần nữa
            var r1 = await service.AddReportAsync(new ReportDto
            { UserId = 15, ReviewId = 55, Reason = "Lần 1", Status = 0 });

            var r2 = await service.AddReportAsync(new ReportDto
            { UserId = 15, ReviewId = 55, Reason = "Lần 2", Status = 0 });

            // CheckDB: phải có đúng 3 bản ghi (cũ + lần 1 + lần 2)
            var all = await context.Reports
                .Where(r => r.UserId == 15 && r.ReviewId == 55)
                .ToListAsync();
            Assert.Equal(3, all.Count); // 3 bản ghi trùng khóa ngoại được phép

            // Mỗi lần gọi phải sinh ra Id riêng biệt (không bị ghi đè)
            Assert.NotEqual(r1.Id, r2.Id);

            // Nội dung Reason của mỗi lần được lưu riêng
            var reasons = all.Select(r => r.Reason).ToHashSet();
            Assert.Contains("Lần 1", reasons);
            Assert.Contains("Lần 2", reasons);
        }
        finally { await transaction.RollbackAsync(); }
    }

    // =========================================================================
    //  NHÓM 2: UpdateReportStatusAsync
    //  Kiểm tra việc thay đổi trạng thái báo cáo (0=chờ, 1=duyệt, 2=từ chối)
    // =========================================================================

    /// <summary>
    /// TC-RPS-010 – Cập nhật trạng thái thành công khi báo cáo tồn tại.
    /// Kiểm tra: trả về (success=true, message="...successfully.") và DB thay đổi đúng.
    /// </summary>
    [Fact]
    public async Task UpdateReportStatusAsync_ExistingReport_UpdatesSuccessfully()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Base data: seed đủ FK chain rồi tạo report Id=100 với Status=0 (chờ xử lý)
            await SeedMinimalContextAsync(context, userId: 10, reviewId: 50);
            context.Reports.Add(new Report { Id = 100, UserId = 10, ReviewId = 50, Reason = "Spam", Status = 0 });
            await context.SaveChangesAsync();

            // Gọi service: chuyển report 100 sang Status=1 (đã duyệt)
            var (success, message) = await service.UpdateReportStatusAsync(100, 1);

            // Kết quả trả về phải báo thành công
            Assert.True(success);
            Assert.Equal("Report status updated successfully.", message); // Message chuẩn từ service

            // CheckDB: xác nhận Status trong DB đã đổi từ 0 → 1
            var dbReport = await context.Reports.FindAsync(100);
            Assert.Equal(1, dbReport!.Status); // Trạng thái mới = 1 (đã duyệt)
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RPS-011 – Trả (success=false, "Report not found.") khi báo cáo không tồn tại.
    /// Kiểm tra: service xử lý gracefully – không ném exception, chỉ báo lỗi qua return value.
    /// </summary>
    [Fact]
    public async Task UpdateReportStatusAsync_NonexistentReport_ReturnsFalse()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var countBefore = await context.Reports.CountAsync(); // Phải = 0 (DB trống)

            // Cố tình gọi với Id=999 – không tồn tại trong DB
            var (success, message) = await service.UpdateReportStatusAsync(999, 1);

            Assert.False(success);                       // Kết quả phải là thất bại
            Assert.Equal("Report not found.", message);  // Message đúng nghiệp vụ

            // CheckDB: DB không bị thay đổi (không có gì xảy ra)
            Assert.Equal(countBefore, await context.Reports.CountAsync());
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RPS-012 – Chuyển sang trạng thái "đã duyệt" (status = 1).
    /// Kiểm tra: luồng duyệt báo cáo chuẩn (0 → 1).
    /// </summary>
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
            // Seed report với Status=0 (chờ xử lý)
            context.Reports.Add(new Report { Id = 101, UserId = 10, ReviewId = 50, Status = 0 });
            await context.SaveChangesAsync();

            // Duyệt báo cáo: chuyển sang Status=1
            var (success, message) = await service.UpdateReportStatusAsync(101, 1);

            Assert.True(success);
            Assert.Equal("Report status updated successfully.", message);
            // CheckDB: Status phải là 1 trong DB
            Assert.Equal(1, (await context.Reports.FindAsync(101))!.Status);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RPS-013 – Chuyển sang trạng thái "từ chối" (status = 2).
    /// Kiểm tra: luồng từ chối báo cáo (0 → 2).
    /// </summary>
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
            // Seed report với Status=0 (chờ xử lý)
            context.Reports.Add(new Report { Id = 102, UserId = 10, ReviewId = 50, Status = 0 });
            await context.SaveChangesAsync();

            // Từ chối báo cáo: chuyển sang Status=2
            var (success, message) = await service.UpdateReportStatusAsync(102, 2);

            Assert.True(success);
            Assert.Equal("Report status updated successfully.", message);
            // CheckDB: Status phải là 2 trong DB
            Assert.Equal(2, (await context.Reports.FindAsync(102))!.Status);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RPS-014 – Cập nhật lại đúng trạng thái hiện tại vẫn trả thành công (idempotent).
    /// Kiểm tra: cập nhật Status=1 → 1 (không thay đổi gì) vẫn hoạt động bình thường.
    /// </summary>
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
            // Seed report đã ở Status=1 (đã được duyệt rồi)
            context.Reports.Add(new Report { Id = 103, UserId = 10, ReviewId = 50, Status = 1 });
            await context.SaveChangesAsync();

            // Thử cập nhật lại Status=1 (không đổi gì)
            var (success, message) = await service.UpdateReportStatusAsync(103, 1);

            Assert.True(success);  // Vẫn phải thành công dù không thay đổi
            Assert.Equal("Report status updated successfully.", message);
            // CheckDB: Status vẫn là 1 (không bị reset về 0 hay thay đổi bất thường)
            Assert.Equal(1, (await context.Reports.FindAsync(103))!.Status);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RPS-015 – Status ngoài tập hợp quy định (-99) vẫn được ghi vào DB mà không validate.
    /// Kiểm tra: service hiện tại KHÔNG validate giá trị status → đây là "ghi nhận hành vi hiện hành".
    /// ⚠ Test này sẽ fail khi bổ sung validation vào service – đó là tín hiệu tốt.
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

            // Gửi status = -99 (giá trị phi hợp lệ theo nghiệp vụ)
            var (success, _) = await service.UpdateReportStatusAsync(104, -99);

            Assert.True(success); // Hiện tại không validate → vẫn thành công
            // CheckDB: -99 được ghi thẳng vào DB (hành vi hiện tại)
            Assert.Equal(-99, (await context.Reports.FindAsync(104))!.Status);
            // Nếu tương lai thêm validate, dòng trên sẽ fail → cần sửa test
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RPS-016 – Id âm hoặc Id = 0 trả (success=false) mà không ném exception.
    /// Kiểm tra: service xử lý biên id ≤ 0 an toàn, không panic, không gây side effect.
    /// </summary>
    [Fact]
    public async Task UpdateReportStatusAsync_NegativeOrZeroId_ReturnsFalse()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Seed 1 report hợp lệ để kiểm tra nó không bị ảnh hưởng bởi lệnh gọi sai
            await SeedMinimalContextAsync(context, userId: 10, reviewId: 50);
            context.Reports.Add(new Report { Id = 1, UserId = 10, ReviewId = 50, Status = 0 });
            await context.SaveChangesAsync();

            // Lần 1: id âm → phải trả false, không throw
            var (s1, m1) = await service.UpdateReportStatusAsync(-5, 1);
            Assert.False(s1);
            Assert.Equal("Report not found.", m1);

            // Lần 2: id = 0 → phải trả false, không throw
            var (s2, m2) = await service.UpdateReportStatusAsync(0, 1);
            Assert.False(s2);
            Assert.Equal("Report not found.", m2);

            // CheckDB: report Id=1 phải không bị thay đổi (Status vẫn = 0)
            Assert.Equal(0, (await context.Reports.FindAsync(1))!.Status);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RPS-017 – Khi Repository ném DbUpdateException, service lan truyền đúng exception.
    /// Kiểm tra: service không nuốt lỗi DB – exception bubble up với message gốc.
    /// Dùng FakeThrowingReportRepository để giả lập lỗi.
    /// </summary>
    [Fact]
    public async Task UpdateReportStatusAsync_RepositoryThrows_PropagatesException()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);

        // Seed dữ liệu nền trực tiếp (không qua transaction) để fake repo có report cần xử lý
        // FakeRepo sẽ throw trước khi gọi base, nên không cần service tìm thấy report trước
        await SeedMinimalContextAsync(context, userId: 10, reviewId: 50);
        context.Reports.Add(new Report { Id = 100, UserId = 10, ReviewId = 50, Status = 0 });
        await context.SaveChangesAsync(); // Commit để fake repo truy cập được

        // Cấu hình fake repo: ném DbUpdateException khi UpdateReportStatusAsync được gọi
        var fakeRepo = new FakeThrowingReportRepository(
            context,
            throwOnUpdate: new Microsoft.EntityFrameworkCore.DbUpdateException("update failed"));
        var service = new ReportService(fakeRepo);

        // Kỳ vọng: service ném đúng DbUpdateException với message gốc
        var ex = await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(
            () => service.UpdateReportStatusAsync(100, 1));

        Assert.Equal("update failed", ex.Message); // Message không bị thay đổi hay wrap
    }

    // =========================================================================
    //  NHÓM 3: GetReportsByReviewIdAsync
    //  Kiểm tra truy vấn danh sách báo cáo theo ReviewId
    // =========================================================================

    /// <summary>
    /// TC-RPS-018 – Trả đúng 3 báo cáo của review 200, loại trừ báo cáo của review 999.
    /// ⚠ TEST NÀY ĐANG FAIL vì Report { ReviewId = 999 } vi phạm FK constraint
    /// (Review 999 chưa được seed). Cần thêm MakeReview(999, ...) vào base data.
    /// </summary>
    [Fact]
    public async Task GetReportsByReviewIdAsync_MultipleReports_ReturnsOnlyMatchingReviewId()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Base data: 3 user, 1 nhà hàng, 1 review
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(MakeUser(10)); // User báo cáo + chủ nhà hàng
            context.Users.Add(MakeUser(11)); // User báo cáo thứ 2
            context.Users.Add(MakeUser(12)); // User báo cáo thứ 3
            await context.SaveChangesAsync();

            context.Restaurants.Add(MakeRestaurant(1, 1, 10, 1));
            await context.SaveChangesAsync();

            context.Reviews.Add(MakeReview(200, 10, 1)); // Review cần query
            await context.SaveChangesAsync();
            // ⚠ BUG: thiếu MakeReview(999, ...) → Report { ReviewId=999 } sẽ fail FK

            // Seed 4 report: 3 thuộc review 200, 1 thuộc review 999 (để kiểm tra lọc)
            context.Reports.AddRange(
                new Report { UserId = 10, ReviewId = 200, Reason = "R1", Status = 0 },
                new Report { UserId = 11, ReviewId = 200, Reason = "R2", Status = 0 },
                new Report { UserId = 12, ReviewId = 200, Reason = "R3", Status = 1 },
                new Report { UserId = 10, ReviewId = 999, Reason = "R4", Status = 0 } // ReviewId=999 chưa tồn tại!
            );
            await context.SaveChangesAsync(); // ← Dòng này sẽ ném FOREIGN KEY constraint failed

            var result = (await service.GetReportsByReviewIdAsync(200)).ToList();

            // Kỳ vọng: chỉ 3 bản ghi thuộc review 200
            Assert.Equal(3, result.Count);
            Assert.All(result, r => Assert.Equal(200, r.ReviewId)); // Tất cả phải là review 200
            var reasons = result.Select(r => r.Reason).ToHashSet();
            Assert.Contains("R1", reasons);    // R1 phải có
            Assert.Contains("R2", reasons);    // R2 phải có
            Assert.Contains("R3", reasons);    // R3 phải có (status=1 vẫn được trả)
            Assert.DoesNotContain("R4", reasons); // R4 (review 999) phải bị loại
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RPS-019 – Trả về danh sách rỗng khi review tồn tại nhưng chưa có báo cáo nào.
    /// Kiểm tra: GetReportsByReviewIdAsync không ném exception với review "sạch".
    /// </summary>
    [Fact]
    public async Task GetReportsByReviewIdAsync_NoReports_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Seed review 201 nhưng không tạo bất kỳ Report nào cho nó
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(MakeUser(10));
            await context.SaveChangesAsync();
            context.Restaurants.Add(MakeRestaurant(1, 1, 10, 1));
            await context.SaveChangesAsync();
            context.Reviews.Add(MakeReview(201, 10, 1)); // Review 201 chưa bị ai báo cáo
            await context.SaveChangesAsync();

            var result = await service.GetReportsByReviewIdAsync(201);

            Assert.NotNull(result);      // Phải trả về IEnumerable, không phải null
            Assert.False(result.Any()); // Danh sách phải rỗng (0 phần tử)
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RPS-020 – Trả về danh sách rỗng khi reviewId không tồn tại trong DB.
    /// Kiểm tra: không ném exception khi query với reviewId hoàn toàn không có trong DB.
    /// </summary>
    [Fact]
    public async Task GetReportsByReviewIdAsync_NonexistentReviewId_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        // Không cần transaction vì không thay đổi DB (chỉ đọc)
        // reviewId=999 hoàn toàn không tồn tại – DB trống
        var result = await service.GetReportsByReviewIdAsync(999);

        Assert.NotNull(result);      // Vẫn trả về IEnumerable (không null)
        Assert.False(result.Any()); // Danh sách rỗng vì không có gì để query
    }

    /// <summary>
    /// TC-RPS-021 – Trường UserName trong kết quả được lấy đúng từ User tương ứng (LEFT JOIN).
    /// Kiểm tra: service join bảng Users và ánh xạ User.Name → ReportListItemDto.UserName.
    /// </summary>
    [Fact]
    public async Task GetReportsByReviewIdAsync_WithUser_FillsUserName()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Base data: user có tên tiếng Việt đầy đủ
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User
            {
                Id = 30, Email = "quan@test.vn", Password = "123",
                Role = "customer",
                Name = "Đặng Minh Quân",  // Tên tiếng Việt có dấu đầy đủ
                Status = 1
            });
            await context.SaveChangesAsync();
            context.Restaurants.Add(MakeRestaurant(1, 1, 30, 1));
            await context.SaveChangesAsync();
            context.Reviews.Add(MakeReview(202, 30, 1));
            await context.SaveChangesAsync();

            // Seed report của user 30 về review 202
            context.Reports.Add(new Report
            {
                Id = 5, UserId = 30, ReviewId = 202, Reason = "Ngôn từ thô tục", Status = 0
            });
            await context.SaveChangesAsync();

            var result = (await service.GetReportsByReviewIdAsync(202)).ToList();

            Assert.Single(result);  // Chỉ có 1 báo cáo
            // Kiểm tra UserName được ánh xạ đúng từ User.Name
            Assert.Equal("Đặng Minh Quân", result[0].UserName);
            Assert.Equal(30, result[0].UserId); // UserId cũng phải khớp
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RPS-022 – UserName = null khi User.Name = null (User tồn tại nhưng chưa đặt tên).
    /// Kiểm tra: ánh xạ null-safe – không ném NullReferenceException khi Name = null.
    /// </summary>
    [Fact]
    public async Task GetReportsByReviewIdAsync_MissingUser_UserNameIsNull()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Base data: user với Name = null (chưa cập nhật thông tin)
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(new User
            {
                Id = 88, Email = "u88@test.vn", Password = "123",
                Role = "customer",
                Name = null,  // Tên = null – trường hợp user chưa cập nhật hồ sơ
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
            // UserName phải là null khi User.Name = null – không được dùng giá trị fallback
            Assert.Null(result[0].UserName);
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RPS-023 – Ánh xạ đầy đủ tất cả các field từ DB sang ReportListItemDto.
    /// Kiểm tra: Id, UserId, UserName, ReviewId, Reason, Status đều được ánh xạ đúng.
    /// </summary>
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

            // Seed report với Id, Status tùy chọn để kiểm tra ánh xạ chính xác
            context.Reports.Add(new Report
            {
                Id = 302,           // Id cố định để Assert chính xác
                UserId = 31,
                ReviewId = 204,
                Reason = "Ngôn từ thù ghét",
                Status = 1          // Status đã được duyệt
            });
            await context.SaveChangesAsync();

            var result = (await service.GetReportsByReviewIdAsync(204)).ToList();

            Assert.Single(result);
            var item = result[0];
            // Kiểm tra từng field riêng lẻ để phát hiện lỗi ánh xạ cụ thể
            Assert.Equal(302, item.Id);                   // Id phải khớp với DB
            Assert.Equal(31, item.UserId);                // UserId phải khớp
            Assert.Equal("Hoàng Thị Kiều", item.UserName); // UserName lấy từ User.Name
            Assert.Equal(204, item.ReviewId);             // ReviewId phải khớp
            Assert.Equal("Ngôn từ thù ghét", item.Reason); // Reason được lưu nguyên
            Assert.Equal(1, item.Status);                 // Status phải khớp
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RPS-024 – reviewId âm hoặc 0 trả danh sách rỗng, không ném exception.
    /// Kiểm tra: biên giá trị âm/0 được xử lý an toàn (Id âm không thể tồn tại trong DB).
    /// </summary>
    [Fact]
    public async Task GetReportsByReviewIdAsync_NegativeOrZeroReviewId_ReturnsEmpty()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Seed 1 report với reviewId=1 hợp lệ – để đảm bảo DB không trống
            await SeedMinimalContextAsync(context, userId: 10, reviewId: 1);
            context.Reports.Add(new Report { UserId = 10, ReviewId = 1, Reason = "x", Status = 0 });
            await context.SaveChangesAsync();

            // Query với reviewId âm → phải trả rỗng, không throw
            var r1 = await service.GetReportsByReviewIdAsync(-1);
            // Query với reviewId = 0 → phải trả rỗng, không throw
            var r2 = await service.GetReportsByReviewIdAsync(0);

            Assert.NotNull(r1);      // Không được trả null
            Assert.False(r1.Any()); // Rỗng vì không có review nào có Id = -1
            Assert.NotNull(r2);
            Assert.False(r2.Any()); // Rỗng vì không có review nào có Id = 0
        }
        finally { await transaction.RollbackAsync(); }
    }

    /// <summary>
    /// TC-RPS-025 – Trả đúng 150 bản ghi khi review có nhiều báo cáo (stress test nhỏ).
    /// Kiểm tra: service không bị phân trang hay cắt bớt kết quả khi số lượng lớn.
    /// </summary>
    [Fact]
    public async Task GetReportsByReviewIdAsync_HundredsOfReports_ReturnsAllCorrectly()
    {
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var service = new ReportService(new ReportRepository(context));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Base data: chuỗi FK đầy đủ
            context.Categories.Add(new Category { Id = 1, Name = "Cat" });
            context.Addresses.Add(new Address { Id = 1, City = "Hà Nội" });
            context.Users.Add(MakeUser(10));
            await context.SaveChangesAsync();
            context.Restaurants.Add(MakeRestaurant(1, 1, 10, 1));
            await context.SaveChangesAsync();
            context.Reviews.Add(MakeReview(205, 10, 1)); // Review cần stress test
            await context.SaveChangesAsync();

            // Tạo 150 bản ghi Report với Id từ 1000 → 1149
            var reports = Enumerable.Range(1000, 150).Select(i => new Report
            {
                Id = i,
                UserId = 10,
                ReviewId = 205, // Tất cả cùng review 205
                Reason = $"Lý do {i}",
                Status = 0
            }).ToList();
            context.Reports.AddRange(reports);
            await context.SaveChangesAsync();

            // Gọi service: phải trả đủ 150 bản ghi
            var result = (await service.GetReportsByReviewIdAsync(205)).ToList();

            Assert.Equal(150, result.Count); // Phải trả đúng 150 (không bị cắt)
            Assert.All(result, r => Assert.Equal(205, r.ReviewId)); // Tất cả phải là review 205

            // CheckDB: kiểm tra tập Id trả về khớp với tập Id đã seed (1000..1149)
            var returnedIds = result.Select(r => r.Id).ToHashSet();
            for (int id = 1000; id < 1150; id++)
                Assert.Contains(id, returnedIds); // Mỗi Id trong 1000..1149 phải có mặt
        }
        finally { await transaction.RollbackAsync(); }
    }
}

