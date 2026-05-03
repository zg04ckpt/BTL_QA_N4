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
        // === CHUẨN BỊ (Arrange) ===
        // Khởi tạo controller và mock IReportService
        var (controller, mock) = BuildController();

        // Tạo dữ liệu giả lập (mock data) cho báo cáo sẽ được trả về từ service
        var fakeReport = new Report
        {
            Id = 300, UserId = 10, ReviewId = 50,
            Reason = "Bình luận spam quảng cáo", Status = 0
        };
        
        // Cấu hình mock: khi gọi AddReportAsync với bất kỳ ReportDto nào, trả về fakeReport
        mock.Setup(s => s.AddReportAsync(It.IsAny<ReportDto>()))
            .ReturnsAsync(fakeReport);

        // Chuẩn bị dữ liệu đầu vào (DTO) từ phía client
        var dto = new ReportDto
        {
            UserId = 10, ReviewId = 50,
            Reason = "Bình luận spam quảng cáo", Status = 0
        };

        // === THỰC THI (Act) ===
        // Gọi action AddReport của controller
        var actionResult = await controller.AddReport(dto);

        // === KIỂM TRA (Assert) ===
        // Xác nhận kết quả trả về là 200 OK
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(200, okResult.StatusCode);
        
        // Xác nhận dữ liệu bên trong response đúng là kiểu Report
        var report = Assert.IsType<Report>(okResult.Value);
        
        // Xác nhận các trường dữ liệu của Report trùng khớp với fakeReport
        Assert.Equal(300, report.Id);
        Assert.Equal("Bình luận spam quảng cáo", report.Reason);
        Assert.Equal(0, report.Status);
    }

    /// <summary>TC-RPC-002 – Service trả Report với Status=0 dù DTO gửi Status=5</summary>
    [Fact]
    public async Task AddReport_ServiceAlwaysReturnsStatusZero_ResponseStatusIsZero()
    {
        // === CHUẨN BỊ (Arrange) ===
        var (controller, mock) = BuildController();

        // Giả lập service luôn trả về báo cáo với Status = 0 (chờ duyệt)
        mock.Setup(s => s.AddReportAsync(It.IsAny<ReportDto>()))
            .ReturnsAsync(new Report { Id = 301, UserId = 11, ReviewId = 51, Reason = "test", Status = 0 });

        // === THỰC THI (Act) ===
        // Client cố tình gửi Status = 5 trong DTO
        var actionResult = await controller.AddReport(new ReportDto
        {
            UserId = 11, ReviewId = 51, Reason = "test", Status = 5
        });

        // === KIỂM TRA (Assert) ===
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var report = Assert.IsType<Report>(okResult.Value);
        
        // Đảm bảo Status của báo cáo trả về vẫn là 0, bỏ qua giá trị 5 từ client
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
        // === CHUẨN BỊ (Arrange) ===
        var (controller, mock) = BuildController();

        // Controller không có null-guard; service sẽ được gọi với giá trị null
        // Giả lập service trả về báo cáo mặc định khi nhận null
        mock.Setup(s => s.AddReportAsync(null!))
            .ReturnsAsync(new Report { Id = 1, Status = 0 });

        // === THỰC THI (Act) ===
        // Gọi trực tiếp hàm controller với null (mô phỏng request body rỗng vượt qua được model-binder)
        var actionResult = await controller.AddReport(null!);

        // === KIỂM TRA (Assert) ===
        // Controller hiện tại không kiểm tra null → trả 200 OK nếu service không ném lỗi
        Assert.IsType<OkObjectResult>(actionResult);

        // Đảm bảo mock service thực sự được gọi với tham số null đúng 1 lần
        mock.Verify(s => s.AddReportAsync(null!), Times.Once);
    }

    /// <summary>TC-RPC-004 – Service ném Exception → controller lan truyền exception</summary>
    [Fact]
    public async Task AddReport_ServiceThrows_ExceptionPropagates()
    {
        // === CHUẨN BỊ (Arrange) ===
        var (controller, mock) = BuildController();

        // Cấu hình service ném ra một Exception khi được gọi
        mock.Setup(s => s.AddReportAsync(It.IsAny<ReportDto>()))
            .ThrowsAsync(new Exception("service failure"));

        // === THỰC THI (Act) & KIỂM TRA (Assert) ===
        // Sử dụng Assert.ThrowsAsync để bắt exception và đảm bảo nó được lan truyền ra ngoài
        var ex = await Assert.ThrowsAsync<Exception>(
            () => controller.AddReport(new ReportDto
            {
                UserId = 10, ReviewId = 50, Reason = "test", Status = 0
            }));

        // Xác nhận thông báo lỗi trùng khớp với exception từ service
        Assert.Equal("service failure", ex.Message);
    }

    /// <summary>TC-RPC-005 – Giữ nguyên tiếng Việt có dấu và ký tự đặc biệt trong response</summary>
    [Fact]
    public async Task AddReport_VietnameseReasonPreserved_InResponse()
    {
        // === CHUẨN BỊ (Arrange) ===
        var (controller, mock) = BuildController();

        // Chuẩn bị chuỗi lý do có chứa tiếng Việt và ký tự đặc biệt (emoji)
        const string reason = "Nội dung phản cảm – vi phạm cộng đồng ❌";
        
        // Cấu hình service trả về chuỗi lý do y hệt
        mock.Setup(s => s.AddReportAsync(It.IsAny<ReportDto>()))
            .ReturnsAsync(new Report { Id = 302, UserId = 12, ReviewId = 52, Reason = reason, Status = 0 });

        // === THỰC THI (Act) ===
        var actionResult = await controller.AddReport(new ReportDto
        {
            UserId = 12, ReviewId = 52, Reason = reason, Status = 0
        });

        // === KIỂM TRA (Assert) ===
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var report = Assert.IsType<Report>(okResult.Value);
        
        // Xác nhận chuỗi không bị lỗi font hoặc mất ký tự
        Assert.Equal(reason, report.Reason);
    }

    /// <summary>
    /// TC-RPC-006 – UserId/ReviewId âm: controller truyền xuống service thật (SQLite in-memory).
    /// Service sẽ ném DbUpdateException vì FK không tồn tại.
    /// </summary>
    [Fact]
    public async Task AddReport_NegativeUserIdReviewId_ServiceThrowsDbUpdateException()
    {
        // === CHUẨN BỊ (Arrange) ===
        // Dùng service thật + SQLite in-memory (không dùng mock) để kiểm tra tương tác DB
        await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
        await using var context = SqliteMemoryDb.CreateContext(connection);
        var realService = new BusinessLogicLayer.Services.ReportService(new ReportRepository(context));
        var controller = new ReportController(realService);

        // === THỰC THI (Act) & KIỂM TRA (Assert) ===
        // Truyền UserId và ReviewId âm (không tồn tại trong DB)
        // Hệ thống sẽ ném ra DbUpdateException do vi phạm khóa ngoại (Foreign Key Constraint)
        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(
            () => controller.AddReport(new ReportDto
            {
                UserId = -1, ReviewId = -1, Reason = "test", Status = 0
            }));

        // CheckDB: Xác nhận không có bản ghi report nào bị ghi lỗi vào DB
        Assert.Equal(0, await context.Reports.CountAsync());
    }

    // ===========================================================
    //  UpdateReportStatus
    // ===========================================================

    /// <summary>TC-RPC-007 – Trả 200 OK kèm message khi báo cáo tồn tại</summary>
    [Fact]
    public async Task UpdateReportStatus_ReportExists_Returns200WithMessage()
    {
        // === CHUẨN BỊ (Arrange) ===
        var (controller, mock) = BuildController();

        // Giả lập service trả về thành công (true) và kèm thông báo khi cập nhật status
        mock.Setup(s => s.UpdateReportStatusAsync(100, 1))
            .ReturnsAsync((true, "Report status updated successfully."));

        // === THỰC THI (Act) ===
        // Gọi controller để cập nhật status của báo cáo ID = 100 thành 1 (đã duyệt)
        var actionResult = await controller.UpdateReportStatus(100, 1);

        // === KIỂM TRA (Assert) ===
        // Xác nhận kết quả trả về là 200 OK
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        // Xác nhận thông báo trả về khớp với thông báo từ service
        Assert.Equal("Report status updated successfully.", okResult.Value);
    }

    /// <summary>TC-RPC-008 – Trả 404 NotFound khi báo cáo không tồn tại</summary>
    [Fact]
    public async Task UpdateReportStatus_ReportNotFound_Returns404WithMessage()
    {
        // === CHUẨN BỊ (Arrange) ===
        var (controller, mock) = BuildController();

        // Giả lập service trả về thất bại (false) do không tìm thấy báo cáo (ID = 999)
        mock.Setup(s => s.UpdateReportStatusAsync(999, 1))
            .ReturnsAsync((false, "Report not found."));

        // === THỰC THI (Act) ===
        var actionResult = await controller.UpdateReportStatus(999, 1);

        // === KIỂM TRA (Assert) ===
        // Xác nhận controller trả về 404 Not Found
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(404, notFoundResult.StatusCode);
        Assert.Equal("Report not found.", notFoundResult.Value);
    }

    /// <summary>TC-RPC-009 – Phê duyệt báo cáo (status=1) → 200 OK, mock verify gọi đúng 1 lần với (101,1)</summary>
    [Fact]
    public async Task UpdateReportStatus_ApproveReport_Returns200AndVerifyCall()
    {
        // === CHUẨN BỊ (Arrange) ===
        var (controller, mock) = BuildController();

        mock.Setup(s => s.UpdateReportStatusAsync(101, 1))
            .ReturnsAsync((true, "Report status updated successfully."));

        // === THỰC THI (Act) ===
        var actionResult = await controller.UpdateReportStatus(101, 1);

        // === KIỂM TRA (Assert) ===
        Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("Report status updated successfully.", ((OkObjectResult)actionResult).Value);

        // Kiểm chứng (Verify): Đảm bảo service.UpdateReportStatusAsync được gọi đúng 1 lần với tham số (101, 1)
        mock.Verify(s => s.UpdateReportStatusAsync(101, 1), Times.Once);
    }

    /// <summary>TC-RPC-010 – Từ chối báo cáo (status=2) → 200 OK, mock verify gọi đúng 1 lần với (102,2)</summary>
    [Fact]
    public async Task UpdateReportStatus_RejectReport_Returns200AndVerifyCall()
    {
        // === CHUẨN BỊ (Arrange) ===
        var (controller, mock) = BuildController();

        mock.Setup(s => s.UpdateReportStatusAsync(102, 2))
            .ReturnsAsync((true, "Report status updated successfully."));

        // === THỰC THI (Act) ===
        var actionResult = await controller.UpdateReportStatus(102, 2);

        // === KIỂM TRA (Assert) ===
        Assert.IsType<OkObjectResult>(actionResult);
        // Xác nhận tham số gọi service là (102, 2) ứng với trạng thái từ chối
        mock.Verify(s => s.UpdateReportStatusAsync(102, 2), Times.Once);
    }

    /// <summary>TC-RPC-011 – id âm hoặc 0 trả 404 (vì service trả Success=false)</summary>
    [Fact]
    public async Task UpdateReportStatus_NegativeOrZeroId_Returns404()
    {
        // === CHUẨN BỊ (Arrange) ===
        var (controller, mock) = BuildController();

        // Cấu hình mock: ID = -1 hoặc 0 đều trả về không tìm thấy
        mock.Setup(s => s.UpdateReportStatusAsync(-1, 1))
            .ReturnsAsync((false, "Report not found."));
        mock.Setup(s => s.UpdateReportStatusAsync(0, 1))
            .ReturnsAsync((false, "Report not found."));

        // === THỰC THI & KIỂM TRA (Act & Assert) ===
        // Lần 1: Thử với ID = -1
        var r1 = await controller.UpdateReportStatus(-1, 1);
        var nf1 = Assert.IsType<NotFoundObjectResult>(r1);
        Assert.Equal("Report not found.", nf1.Value);

        // Lần 2: Thử với ID = 0
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

        // === CHUẨN BỊ (Arrange) ===
        var (controller, mock) = BuildController();

        // Giả lập: binder parse lỗi và trả về giá trị mặc định là 0
        mock.Setup(s => s.UpdateReportStatusAsync(100, 0))
            .ReturnsAsync((false, "Report not found."));

        // === THỰC THI (Act) ===
        var actionResult = await controller.UpdateReportStatus(100, 0);

        // === KIỂM TRA (Assert) ===
        Assert.IsType<NotFoundObjectResult>(actionResult);
        // Trong môi trường integration test thực sự, dòng verify sau sẽ được áp dụng:
        // mock.Verify(s => s.UpdateReportStatusAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    /// <summary>TC-RPC-013 – Service ném Exception → exception lan truyền lên</summary>
    [Fact]
    public async Task UpdateReportStatus_ServiceThrows_ExceptionPropagates()
    {
        // === CHUẨN BỊ (Arrange) ===
        var (controller, mock) = BuildController();

        mock.Setup(s => s.UpdateReportStatusAsync(100, 1))
            .ThrowsAsync(new Exception("service failure"));

        // === THỰC THI & KIỂM TRA (Act & Assert) ===
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
        // === CHUẨN BỊ (Arrange) ===
        var (controller, mock) = BuildController();

        // Giả lập danh sách 2 báo cáo trả về từ service
        var fakeList = new List<ReportListItemDto>
        {
            new ReportListItemDto { Id = 1, UserId = 10, UserName = "Nguyễn Văn An", ReviewId = 200, Reason = "R1", Status = 0 },
            new ReportListItemDto { Id = 2, UserId = 11, UserName = "Trần Thị Bích", ReviewId = 200, Reason = "R2", Status = 1 }
        };
        mock.Setup(s => s.GetReportsByReviewIdAsync(200))
            .ReturnsAsync(fakeList);

        // === THỰC THI (Act) ===
        // Gọi API để lấy danh sách báo cáo cho Review ID = 200
        var actionResult = await controller.GetReportsByReviewId(200);

        // === KIỂM TRA (Assert) ===
        // Kiểm tra mã trạng thái trả về là 200 OK
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(200, okResult.StatusCode);
        
        // Kiểm tra dữ liệu danh sách báo cáo
        var list = Assert.IsAssignableFrom<IEnumerable<ReportListItemDto>>(okResult.Value);
        Assert.Equal(2, list.Count());
        
        // Trích xuất danh sách các lý do và kiểm tra từng phần tử có tồn tại hay không
        var reasons = list.Select(r => r.Reason).ToHashSet();
        Assert.Contains("R1", reasons);
        Assert.Contains("R2", reasons);
    }

    /// <summary>TC-RPC-015 – Trả 200 OK kèm mảng rỗng khi review không có báo cáo</summary>
    [Fact]
    public async Task GetReportsByReviewId_NoReports_Returns200WithEmptyList()
    {
        // === CHUẨN BỊ (Arrange) ===
        var (controller, mock) = BuildController();

        // Giả lập service trả về danh sách rỗng cho Review ID = 201
        mock.Setup(s => s.GetReportsByReviewIdAsync(201))
            .ReturnsAsync(Enumerable.Empty<ReportListItemDto>());

        // === THỰC THI (Act) ===
        var actionResult = await controller.GetReportsByReviewId(201);

        // === KIỂM TRA (Assert) ===
        // Xác nhận vẫn trả về 200 OK kèm theo một danh sách rỗng
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

        // === CHUẨN BỊ (Arrange) ===
        // Unit test placeholder: gọi bình thường với int hợp lệ (123)
        var (controller, mock) = BuildController();
        mock.Setup(s => s.GetReportsByReviewIdAsync(123))
            .ReturnsAsync(Enumerable.Empty<ReportListItemDto>());

        // === THỰC THI (Act) ===
        var actionResult = await controller.GetReportsByReviewId(123);

        // === KIỂM TRA (Assert) ===
        Assert.IsType<OkObjectResult>(actionResult);
        mock.Verify(s => s.GetReportsByReviewIdAsync(123), Times.Once);
    }

    /// <summary>TC-RPC-017 – reviewId âm/0 → controller gọi service bình thường, trả 200 rỗng</summary>
    [Fact]
    public async Task GetReportsByReviewId_NegativeOrZeroId_Returns200Empty()
    {
        // === CHUẨN BỊ (Arrange) ===
        var (controller, mock) = BuildController();

        // Cấu hình mock: ID âm hoặc 0 sẽ trả về rỗng (logic service thực tế cũng vậy)
        mock.Setup(s => s.GetReportsByReviewIdAsync(-1))
            .ReturnsAsync(Enumerable.Empty<ReportListItemDto>());
        mock.Setup(s => s.GetReportsByReviewIdAsync(0))
            .ReturnsAsync(Enumerable.Empty<ReportListItemDto>());

        // === THỰC THI & KIỂM TRA (Act & Assert) ===
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
        // === CHUẨN BỊ (Arrange) ===
        var (controller, mock) = BuildController();

        // Tạo danh sách giả lập chỉ có 1 phần tử nhưng chứa đầy đủ các trường hiển thị
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

        // === THỰC THI (Act) ===
        var actionResult = await controller.GetReportsByReviewId(202);

        // === KIỂM TRA (Assert) ===
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var list = ((IEnumerable<ReportListItemDto>)okResult.Value!).ToList();
        
        // Đảm bảo chỉ có 1 bản ghi trả về
        Assert.Single(list);
        
        // Kiểm tra chi tiết từng trường thông tin của bản ghi
        Assert.Equal(400, list[0].Id);
        Assert.Equal(20, list[0].UserId);
        Assert.Equal("Ngô Bảo Thanh", list[0].UserName);
        Assert.Equal(202, list[0].ReviewId);
        Assert.Equal("Ngôn từ gây thù ghét", list[0].Reason);
        Assert.Equal(0, list[0].Status);
    }
}
