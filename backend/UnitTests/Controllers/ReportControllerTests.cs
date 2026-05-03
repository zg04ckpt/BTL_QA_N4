using BusinessLogicLayer.Interfaces;
using DataAccessLayer;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using UnitTests.Infrastructure;
using WebAPI.Controllers;
using Xunit;

namespace UnitTests.Controllers;

// ============================================================
//  ReportControllerTests – kiểm thử đơn vị cho ReportController
//
//  Chiến lược:
//  - Đa số test: dùng Moq<IReportService>, khởi tạo controller trực tiếp
//    (không qua HTTP pipeline), gọi action method, kiểm tra IActionResult.
//  - TC-RPC-003, TC-RPC-012, TC-RPC-016 (body null / type binding sai):
//    ASP.NET model-binder xử lý lớp này ở mức HTTP; để test chuẩn cần
//    WebApplicationFactory. Trong scope unit-test ta mô phỏng bằng cách
//    thêm ModelState error thủ công (với action không kiểm tra ModelState)
//    hoặc ghi chú "cần integration test".
// ============================================================
public class ReportControllerTests
{
    // Helper: tạo controller với mock IReportService
    private static (ReportController controller, Mock<IReportService> mock) BuildController()
    {
        var mock = new Mock<IReportService>();
        var controller = new ReportController(mock.Object);
        return (controller, mock);
    }

    // ===========================================================
    //  AddReport
    // ===========================================================

    /// <summary>TC-RPC-001 – Trả 200 OK kèm Report vừa tạo khi DTO hợp lệ</summary>
    [Fact]
    public async Task AddReport_ValidDto_Returns200WithReport()
    {
        var (controller, mock) = BuildController();

        // Base data: mock service trả report
        var fakeReport = new Report
        {
            Id = 300, UserId = 10, ReviewId = 50,
            Reason = "Bình luận spam quảng cáo", Status = 0
        };
        mock.Setup(s => s.AddReportAsync(It.IsAny<ReportDto>()))
            .ReturnsAsync(fakeReport);

        // Test data
        var dto = new ReportDto
        {
            UserId = 10, ReviewId = 50,
            Reason = "Bình luận spam quảng cáo", Status = 0
        };

        var actionResult = await controller.AddReport(dto);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(200, okResult.StatusCode);
        var report = Assert.IsType<Report>(okResult.Value);
        Assert.Equal(300, report.Id);
        Assert.Equal("Bình luận spam quảng cáo", report.Reason);
        Assert.Equal(0, report.Status);
    }

    /// <summary>TC-RPC-002 – Service trả Report với Status=0 dù DTO gửi Status=5</summary>
    [Fact]
    public async Task AddReport_ServiceAlwaysReturnsStatusZero_ResponseStatusIsZero()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.AddReportAsync(It.IsAny<ReportDto>()))
            .ReturnsAsync(new Report { Id = 301, UserId = 11, ReviewId = 51, Reason = "test", Status = 0 });

        var actionResult = await controller.AddReport(new ReportDto
        {
            UserId = 11, ReviewId = 51, Reason = "test", Status = 5
        });

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var report = Assert.IsType<Report>(okResult.Value);
        Assert.Equal(0, report.Status);
    }

    /// <summary>
    /// TC-RPC-003 – Body null bị ASP.NET từ chối (model-binder)
    /// LƯU Ý: Kiểm tra này đúng nghĩa cần WebApplicationFactory (integration test).
    /// Trong unit-test, ta mô phỏng hành vi bằng cách service được gọi bình thường
    /// với null – controller không kiểm tra null trước khi gọi service.
    /// </summary>
    [Fact]
    public async Task AddReport_NullBody_ServiceCalledWithNull_NotCheckedByController()
    {
        var (controller, mock) = BuildController();

        // Controller không có null-guard; service sẽ được gọi với null
        // Verify rằng mock được gọi (hành vi hiện hành)
        mock.Setup(s => s.AddReportAsync(null!))
            .ReturnsAsync(new Report { Id = 1, Status = 0 });

        var actionResult = await controller.AddReport(null!);

        // Controller hiện tại không kiểm tra null → trả 200 nếu service không ném
        Assert.IsType<OkObjectResult>(actionResult);

        // Nếu muốn kiểm tra 400 khi body null, cần dùng WebApplicationFactory:
        // POST /api/report với Content-Type: application/json, body = ""
        // → ASP.NET trả 400 trước khi vào action
        mock.Verify(s => s.AddReportAsync(null!), Times.Once);
    }

    /// <summary>TC-RPC-004 – Service ném Exception → controller lan truyền exception</summary>
    [Fact]
    public async Task AddReport_ServiceThrows_ExceptionPropagates()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.AddReportAsync(It.IsAny<ReportDto>()))
            .ThrowsAsync(new Exception("service failure"));

        var ex = await Assert.ThrowsAsync<Exception>(
            () => controller.AddReport(new ReportDto
            {
                UserId = 10, ReviewId = 50, Reason = "test", Status = 0
            }));

        Assert.Equal("service failure", ex.Message);
    }

    /// <summary>TC-RPC-005 – Giữ nguyên tiếng Việt có dấu và ký tự đặc biệt trong response</summary>
    [Fact]
    public async Task AddReport_VietnameseReasonPreserved_InResponse()
    {
        var (controller, mock) = BuildController();

        const string reason = "Nội dung phản cảm – vi phạm cộng đồng ❌";
        mock.Setup(s => s.AddReportAsync(It.IsAny<ReportDto>()))
            .ReturnsAsync(new Report { Id = 302, UserId = 12, ReviewId = 52, Reason = reason, Status = 0 });

        var actionResult = await controller.AddReport(new ReportDto
        {
            UserId = 12, ReviewId = 52, Reason = reason, Status = 0
        });

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var report = Assert.IsType<Report>(okResult.Value);
        Assert.Equal(reason, report.Reason);
    }

    /// <summary>
    /// TC-RPC-006 – UserId/ReviewId âm: controller truyền xuống service thật (SQLite in-memory).
    /// Service sẽ ném DbUpdateException vì FK không tồn tại.
    /// </summary>
    [Fact]
    public async Task AddReport_NegativeUserIdReviewId_ServiceThrowsDbUpdateException()
    {
        // Dùng service thật + SQLite in-memory (không dùng mock)
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var realService = new BusinessLogicLayer.Services.ReportService(new ReportRepository(context));
        var controller = new ReportController(realService);

        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(
            () => controller.AddReport(new ReportDto
            {
                UserId = -1, ReviewId = -1, Reason = "test", Status = 0
            }));

        // CheckDB: không có report nào
        Assert.Equal(0, await context.Reports.CountAsync());
    }

    // ===========================================================
    //  UpdateReportStatus
    // ===========================================================

    /// <summary>TC-RPC-007 – Trả 200 OK kèm message khi báo cáo tồn tại</summary>
    [Fact]
    public async Task UpdateReportStatus_ReportExists_Returns200WithMessage()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.UpdateReportStatusAsync(100, 1))
            .ReturnsAsync((true, "Report status updated successfully."));

        var actionResult = await controller.UpdateReportStatus(100, 1);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("Report status updated successfully.", okResult.Value);
    }

    /// <summary>TC-RPC-008 – Trả 404 NotFound khi báo cáo không tồn tại</summary>
    [Fact]
    public async Task UpdateReportStatus_ReportNotFound_Returns404WithMessage()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.UpdateReportStatusAsync(999, 1))
            .ReturnsAsync((false, "Report not found."));

        var actionResult = await controller.UpdateReportStatus(999, 1);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(404, notFoundResult.StatusCode);
        Assert.Equal("Report not found.", notFoundResult.Value);
    }

    /// <summary>TC-RPC-009 – Phê duyệt báo cáo (status=1) → 200 OK, mock verify gọi đúng 1 lần với (101,1)</summary>
    [Fact]
    public async Task UpdateReportStatus_ApproveReport_Returns200AndVerifyCall()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.UpdateReportStatusAsync(101, 1))
            .ReturnsAsync((true, "Report status updated successfully."));

        var actionResult = await controller.UpdateReportStatus(101, 1);

        Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("Report status updated successfully.", ((OkObjectResult)actionResult).Value);

        // Verify: service được gọi đúng 1 lần với tham số (101, 1)
        mock.Verify(s => s.UpdateReportStatusAsync(101, 1), Times.Once);
    }

    /// <summary>TC-RPC-010 – Từ chối báo cáo (status=2) → 200 OK, mock verify gọi đúng 1 lần với (102,2)</summary>
    [Fact]
    public async Task UpdateReportStatus_RejectReport_Returns200AndVerifyCall()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.UpdateReportStatusAsync(102, 2))
            .ReturnsAsync((true, "Report status updated successfully."));

        var actionResult = await controller.UpdateReportStatus(102, 2);

        Assert.IsType<OkObjectResult>(actionResult);
        mock.Verify(s => s.UpdateReportStatusAsync(102, 2), Times.Once);
    }

    /// <summary>TC-RPC-011 – id âm hoặc 0 trả 404 (vì service trả Success=false)</summary>
    [Fact]
    public async Task UpdateReportStatus_NegativeOrZeroId_Returns404()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.UpdateReportStatusAsync(-1, 1))
            .ReturnsAsync((false, "Report not found."));
        mock.Setup(s => s.UpdateReportStatusAsync(0, 1))
            .ReturnsAsync((false, "Report not found."));

        // Lần 1: id = -1
        var r1 = await controller.UpdateReportStatus(-1, 1);
        var nf1 = Assert.IsType<NotFoundObjectResult>(r1);
        Assert.Equal("Report not found.", nf1.Value);

        // Lần 2: id = 0
        var r2 = await controller.UpdateReportStatus(0, 1);
        var nf2 = Assert.IsType<NotFoundObjectResult>(r2);
        Assert.Equal("Report not found.", nf2.Value);
    }

    /// <summary>
    /// TC-RPC-012 – Body status không phải int → bị ASP.NET từ chối (400).
    /// LƯU Ý: Đây là behavior của model-binder, cần WebApplicationFactory để test đúng.
    /// Unit test này ghi nhận rằng nếu binder vượt qua (truyền 0 mặc định),
    /// controller vẫn hoạt động bình thường.
    /// </summary>
    [Fact]
    public async Task UpdateReportStatus_InvalidBodyType_RequiresIntegrationTestForProper400()
    {
        // Ghi chú: test này là placeholder.
        // Để test body "abc" (sai kiểu int) → HTTP 400, cần WebApplicationFactory:
        // PUT /api/report/100/status với body "abc"
        // → ASP.NET model-binder từ chối và trả 400 TRƯỚC khi action được gọi.
        // Mock.Verify(Times.Never) đảm bảo service không bao giờ được gọi.

        var (controller, mock) = BuildController();

        // Giả lập: binder trả giá trị mặc định (0) khi parse lỗi
        mock.Setup(s => s.UpdateReportStatusAsync(100, 0))
            .ReturnsAsync((false, "Report not found."));

        var actionResult = await controller.UpdateReportStatus(100, 0);

        Assert.IsType<NotFoundObjectResult>(actionResult);
        // Với integration test thực sự: mock.Verify(s => s.UpdateReportStatusAsync(...), Times.Never);
    }

    /// <summary>TC-RPC-013 – Service ném Exception → exception lan truyền lên</summary>
    [Fact]
    public async Task UpdateReportStatus_ServiceThrows_ExceptionPropagates()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.UpdateReportStatusAsync(100, 1))
            .ThrowsAsync(new Exception("service failure"));

        var ex = await Assert.ThrowsAsync<Exception>(
            () => controller.UpdateReportStatus(100, 1));

        Assert.Equal("service failure", ex.Message);
    }

    // ===========================================================
    //  GetReportsByReviewId
    // ===========================================================

    /// <summary>TC-RPC-014 – Trả 200 OK kèm 2 item ReportListItemDto</summary>
    [Fact]
    public async Task GetReportsByReviewId_HasReports_Returns200WithList()
    {
        var (controller, mock) = BuildController();

        var fakeList = new List<ReportListItemDto>
        {
            new ReportListItemDto { Id = 1, UserId = 10, UserName = "Nguyễn Văn An", ReviewId = 200, Reason = "R1", Status = 0 },
            new ReportListItemDto { Id = 2, UserId = 11, UserName = "Trần Thị Bích", ReviewId = 200, Reason = "R2", Status = 1 }
        };
        mock.Setup(s => s.GetReportsByReviewIdAsync(200))
            .ReturnsAsync(fakeList);

        var actionResult = await controller.GetReportsByReviewId(200);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(200, okResult.StatusCode);
        var list = Assert.IsAssignableFrom<IEnumerable<ReportListItemDto>>(okResult.Value);
        Assert.Equal(2, list.Count());
        var reasons = list.Select(r => r.Reason).ToHashSet();
        Assert.Contains("R1", reasons);
        Assert.Contains("R2", reasons);
    }

    /// <summary>TC-RPC-015 – Trả 200 OK kèm mảng rỗng khi review không có báo cáo</summary>
    [Fact]
    public async Task GetReportsByReviewId_NoReports_Returns200WithEmptyList()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.GetReportsByReviewIdAsync(201))
            .ReturnsAsync(Enumerable.Empty<ReportListItemDto>());

        var actionResult = await controller.GetReportsByReviewId(201);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = Assert.IsAssignableFrom<IEnumerable<ReportListItemDto>>(okResult.Value);
        Assert.False(list.Any());
    }

    /// <summary>
    /// TC-RPC-016 – Route không phải int (GET /api/report/by-review/abc) → 400/404.
    /// LƯU Ý: Cần WebApplicationFactory vì đây là behavior của route constraint `{reviewId:int}`.
    /// Unit test này kiểm tra rằng controller hoạt động bình thường khi nhận int hợp lệ.
    /// </summary>
    [Fact]
    public async Task GetReportsByReviewId_NonIntRouteRequiresIntegrationTest()
    {
        // Với WebApplicationFactory:
        // GET /api/report/by-review/abc → ASP.NET trả 404 (route không khớp {reviewId:int})
        // mock.Verify(s => s.GetReportsByReviewIdAsync(It.IsAny<int>()), Times.Never);

        // Unit test placeholder: gọi bình thường với int
        var (controller, mock) = BuildController();
        mock.Setup(s => s.GetReportsByReviewIdAsync(123))
            .ReturnsAsync(Enumerable.Empty<ReportListItemDto>());

        var actionResult = await controller.GetReportsByReviewId(123);

        Assert.IsType<OkObjectResult>(actionResult);
        mock.Verify(s => s.GetReportsByReviewIdAsync(123), Times.Once);
    }

    /// <summary>TC-RPC-017 – reviewId âm/0 → controller gọi service bình thường, trả 200 rỗng</summary>
    [Fact]
    public async Task GetReportsByReviewId_NegativeOrZeroId_Returns200Empty()
    {
        var (controller, mock) = BuildController();

        mock.Setup(s => s.GetReportsByReviewIdAsync(-1))
            .ReturnsAsync(Enumerable.Empty<ReportListItemDto>());
        mock.Setup(s => s.GetReportsByReviewIdAsync(0))
            .ReturnsAsync(Enumerable.Empty<ReportListItemDto>());

        // Lần 1: reviewId = -1
        var r1 = await controller.GetReportsByReviewId(-1);
        var ok1 = Assert.IsType<OkObjectResult>(r1);
        Assert.False(((IEnumerable<ReportListItemDto>)ok1.Value!).Any());

        // Lần 2: reviewId = 0
        var r2 = await controller.GetReportsByReviewId(0);
        var ok2 = Assert.IsType<OkObjectResult>(r2);
        Assert.False(((IEnumerable<ReportListItemDto>)ok2.Value!).Any());
    }

    /// <summary>TC-RPC-018 – Item trong danh sách có đủ tất cả field thông tin hiển thị</summary>
    [Fact]
    public async Task GetReportsByReviewId_SingleItem_ContainsAllDisplayFields()
    {
        var (controller, mock) = BuildController();

        var fakeList = new List<ReportListItemDto>
        {
            new ReportListItemDto
            {
                Id = 400, UserId = 20, UserName = "Ngô Bảo Thanh",
                ReviewId = 202, Reason = "Ngôn từ gây thù ghét", Status = 0
            }
        };
        mock.Setup(s => s.GetReportsByReviewIdAsync(202))
            .ReturnsAsync(fakeList);

        var actionResult = await controller.GetReportsByReviewId(202);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = ((IEnumerable<ReportListItemDto>)okResult.Value!).ToList();
        Assert.Single(list);
        Assert.Equal(400, list[0].Id);
        Assert.Equal(20, list[0].UserId);
        Assert.Equal("Ngô Bảo Thanh", list[0].UserName);
        Assert.Equal(202, list[0].ReviewId);
        Assert.Equal("Ngôn từ gây thù ghét", list[0].Reason);
        Assert.Equal(0, list[0].Status);
    }
}
