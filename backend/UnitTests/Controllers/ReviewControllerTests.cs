using BusinessLogicLayer.Interfaces;
using DataAccessLayer.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebAPI.Controllers;
using Xunit;

namespace UnitTests.Controllers;

/// <summary>
/// Unit test ReviewController — mock IReviewService.
/// </summary>
public class ReviewControllerTests
{
    private static ReviewController CreateController(IReviewService svc)
    {
        return new ReviewController(svc)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    /// <summary>TC-RC-ADD-001 — body null => 400.</summary>
    [Fact]
    public async Task AddReview_NullBody_TC_RC_ADD_001()
    {
        var mock = new Mock<IReviewService>();
        var c = CreateController(mock.Object);

        // Body null → controller từ chối ngay, không gọi service
        var result = await c.AddReview(null!);

        // 400 với message cứng, service không được gọi
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Review data cannot be null.", bad.Value);
        mock.Verify(s => s.AddReviewAsync(It.IsAny<ReviewDto>()), Times.Never);
    }

    /// <summary>TC-RC-ADD-002 — thành công => 200 + body có trường message = "Review added successfully".</summary>
    [Fact]
    public async Task AddReview_Success_TC_RC_ADD_002()
    {
        var mock = new Mock<IReviewService>();
        mock.Setup(s => s.AddReviewAsync(It.IsAny<ReviewDto>()))
            .ReturnsAsync((true, "Review added successfully"));
        var c = CreateController(mock.Object);

        // Service mock trả success — gọi AddReview với DTO đầy đủ
        var result = await c.AddReview(new ReviewDto
        {
            UserId = 1,
            RestaurantId = 3,
            Content = "Món phở rất ngon",
            Score = 5,
            CreateDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            PhotoUrls = new List<string> { "/uploads/review1.jpg" }
        });

        // 200, body { message } khớp service response
        var ok = Assert.IsType<OkObjectResult>(result);
        var message = ok.Value!.GetType().GetProperty("message")?.GetValue(ok.Value) as string;
        Assert.Equal("Review added successfully", message);
    }

    /// <summary>TC-RC-ADD-003 — Success false => 500.</summary>
    [Fact]
    public async Task AddReview_ServiceFails_TC_RC_ADD_003()
    {
        var mock = new Mock<IReviewService>();
        mock.Setup(s => s.AddReviewAsync(It.IsAny<ReviewDto>()))
            .ReturnsAsync((false, "Bình luận vi phạm"));
        var c = CreateController(mock.Object);

        // Service mock trả Success=false (ML chặn hoặc lỗi)
        var result = await c.AddReview(new ReviewDto
        {
            UserId = 1,
            RestaurantId = 1,
            Content = "x",
            Score = 1,
            CreateDate = 1
        });

        // Controller trả 500 khi service thất bại
        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, obj.StatusCode);
    }

    /// <summary>TC-RC-ADD-004 — exception (QR) => 400 + message.</summary>
    [Fact]
    public async Task AddReview_QrException_TC_RC_ADD_004()
    {
        var mock = new Mock<IReviewService>();
        mock.Setup(s => s.AddReviewAsync(It.IsAny<ReviewDto>()))
            .ThrowsAsync(new Exception("You must scan the restaurant's QR code before writing a review."));
        var c = CreateController(mock.Object);

        // Service throw exception QR → controller bắt và trả 400
        var result = await c.AddReview(new ReviewDto
        {
            UserId = 1,
            RestaurantId = 3,
            Content = "x",
            Score = 5,
            CreateDate = 1
        });

        // 400 với body chứa message exception
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(bad.Value);
    }

    /// <summary>TC-RC-DEL-001 — xóa thành công => 200 + body có trường message.</summary>
    [Fact]
    public async Task DeleteReview_Success_TC_RC_DEL_001()
    {
        var mock = new Mock<IReviewService>();
        mock.Setup(s => s.DeleteReviewAsync(12)).ReturnsAsync((true, "Review deleted successfully"));
        var c = CreateController(mock.Object);

        var result = await c.DeleteReview(12);

        var ok = Assert.IsType<OkObjectResult>(result);
        var message = ok.Value!.GetType().GetProperty("message")?.GetValue(ok.Value) as string;
        Assert.Equal("Review deleted successfully", message);
    }

    /// <summary>TC-RC-DEL-002</summary>
    [Fact]
    public async Task DeleteReview_NotFound_TC_RC_DEL_002()
    {
        var mock = new Mock<IReviewService>();
        mock.Setup(s => s.DeleteReviewAsync(88888)).ReturnsAsync((false, "Review not found"));
        var c = CreateController(mock.Object);

        var result = await c.DeleteReview(88888);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>TC-RC-UPD-001 — cập nhật thành công => 200 + body có trường message.</summary>
    [Fact]
    public async Task UpdateReview_Success_TC_RC_UPD_001()
    {
        var mock = new Mock<IReviewService>();
        mock.Setup(s => s.UpdateReviewAsync(5, It.IsAny<ReviewDto>()))
            .ReturnsAsync((true, "Review updated successfully"));
        var c = CreateController(mock.Object);

        var result = await c.UpdateReview(5, new ReviewDto { Content = "Sửa", Score = 5 });

        var ok = Assert.IsType<OkObjectResult>(result);
        var message = ok.Value!.GetType().GetProperty("message")?.GetValue(ok.Value) as string;
        Assert.Equal("Review updated successfully", message);
    }

    /// <summary>TC-RC-UPD-002</summary>
    [Fact]
    public async Task UpdateReview_NotFound_TC_RC_UPD_002()
    {
        var mock = new Mock<IReviewService>();
        mock.Setup(s => s.UpdateReviewAsync(99999, It.IsAny<ReviewDto>()))
            .ReturnsAsync((false, "Review not found"));
        var c = CreateController(mock.Object);

        var result = await c.UpdateReview(99999, new ReviewDto { Content = "X", Score = 3 });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>TC-RC-GAR-001</summary>
    [Fact]
    public async Task GetAllReviews_Ok_TC_RC_GAR_001()
    {
        var mock = new Mock<IReviewService>();
        mock.Setup(s => s.GetAllReviewsAsync()).ReturnsAsync(Array.Empty<ReviewDto>());
        var c = CreateController(mock.Object);

        var result = await c.GetAllReviews();

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>TC-RC-GUR-001</summary>
    [Fact]
    public async Task GetReviewsByUserId_HasData_TC_RC_GUR_001()
    {
        var mock = new Mock<IReviewService>();
        mock.Setup(s => s.GetReviewsByUserIdAsync(7))
            .ReturnsAsync(new List<ReviewDto> { new() { Id = 1, UserId = 7, RestaurantId = 1, Content = "a", Score = 5 } });
        var c = CreateController(mock.Object);

        var result = await c.GetReviewsByUserId(7);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>TC-RC-GUR-002</summary>
    [Fact]
    public async Task GetReviewsByUserId_Empty_TC_RC_GUR_002()
    {
        var mock = new Mock<IReviewService>();
        mock.Setup(s => s.GetReviewsByUserIdAsync(99)).ReturnsAsync(new List<ReviewDto>());
        var c = CreateController(mock.Object);

        var result = await c.GetReviewsByUserId(99);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>TC-RC-GRR-001</summary>
    [Fact]
    public async Task GetReviewsByRestaurantId_HasData_TC_RC_GRR_001()
    {
        var mock = new Mock<IReviewService>();
        mock.Setup(s => s.GetReviewsByRestaurantIdAsync(4))
            .ReturnsAsync(new List<ReviewDto> { new() { Id = 1, UserId = 1, RestaurantId = 4, Content = "a", Score = 5 } });
        var c = CreateController(mock.Object);

        var result = await c.GetReviewsByRestaurantId(4);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>TC-RC-GRR-002</summary>
    [Fact]
    public async Task GetReviewsByRestaurantId_Empty_TC_RC_GRR_002()
    {
        var mock = new Mock<IReviewService>();
        mock.Setup(s => s.GetReviewsByRestaurantIdAsync(60)).ReturnsAsync(new List<ReviewDto>());
        var c = CreateController(mock.Object);

        var result = await c.GetReviewsByRestaurantId(60);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>TC-RC-GHR-001</summary>
    [Fact]
    public async Task GetReviewsWithHighReports_Default_TC_RC_GHR_001()
    {
        var mock = new Mock<IReviewService>();
        mock.Setup(s => s.GetReviewsWithHighReportsAsync(5)).ReturnsAsync(Array.Empty<ReviewDto>());
        var c = CreateController(mock.Object);

        var result = await c.GetReviewsWithHighReports();

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>TC-RC-GHR-002</summary>
    [Fact]
    public async Task GetReviewsWithHighReports_Query_TC_RC_GHR_002()
    {
        var mock = new Mock<IReviewService>();
        mock.Setup(s => s.GetReviewsWithHighReportsAsync(2)).ReturnsAsync(new List<ReviewDto>());
        var c = CreateController(mock.Object);

        var result = await c.GetReviewsWithHighReports(2);

        Assert.IsType<OkObjectResult>(result);
    }
}
