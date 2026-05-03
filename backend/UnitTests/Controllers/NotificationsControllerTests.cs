using BusinessLogicLayer.Services;
using DataAccessLayer.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Controllers;
using Xunit;

namespace UnitTests.Controllers;

/// <summary>
/// NotificationsController nhận FirebaseService cụ thể — không mock được qua Moq (method không virtual).
/// Đặt <c>WebAPI/wwwroot/Configs/firebase-adminsdk.json</c> — các TC gọi FCM chạy khi có file; thiếu file thì Skip (không Fail).
/// </summary>
public class NotificationsControllerTests
{
    private static string WebApiRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "WebAPI"));

    private static bool HasFirebaseJson =>
        File.Exists(Path.Combine(WebApiRoot, "wwwroot", "Configs", "firebase-adminsdk.json"));

    private static NotificationsController? TryCreateController()
    {
        if (!HasFirebaseJson)
            return null;
        try
        {
            Directory.SetCurrentDirectory(WebApiRoot);
            return new NotificationsController(new FirebaseService());
        }
        catch
        {
            return null;
        }
    }

    // /// <summary>TC-NC-TFB-001 — FCM thành công (cần credential).</summary>
    // [SkippableFact]
    // public async Task TestFirebase_Ok_TC_NC_TFB_001()
    // {
    //     Skip.IfNot(HasFirebaseJson, "Cần WebAPI/wwwroot/Configs/firebase-adminsdk.json để test FCM thực.");
    //     var c = TryCreateController();
    //     Skip.If(c == null, "Không khởi tạo được NotificationsController (Firebase).");

    //     // Gọi TestFirebase với topic hợp lệ — FCM thật
    //     c.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    //     var result = await c.TestFirebase("nha_hang_pho_thin");

    //     // 200 + body có messageId và topic
    //     var ok = Assert.IsType<OkObjectResult>(result);
    //     Assert.NotNull(ok.Value);
    // }

    // /// <summary>TC-NC-TFB-002 — topic trống => 400.</summary>
    // [SkippableFact]
    // public async Task TestFirebase_EmptyTopic_TC_NC_TFB_002()
    // {
    //     Skip.IfNot(HasFirebaseJson, "Cần firebase-adminsdk.json.");
    //     var c = TryCreateController();
    //     Skip.If(c == null, "Không khởi tạo được NotificationsController.");

    //     // Topic chỉ khoảng trắng → controller validation
    //     c.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    //     var result = await c.TestFirebase("   ");

    //     // 400 do topic rỗng
    //     Assert.IsType<BadRequestObjectResult>(result);
    // }

    // /// <summary>TC-NC-TFB-003 — FCM lỗi => 500 (không mock được — chỉ khi send thực lỗi).</summary>
    // [Fact(Skip = "TC-NC-TFB-003: cần Firebase trả lỗi gửi — không inject mock trong production.")]
    // public Task TestFirebase_FcmThrows_TC_NC_TFB_003() => Task.CompletedTask;

    /// <summary>TC-NC-SND-001</summary>
    [SkippableFact]
    public async Task SendNotification_Ok_TC_NC_SND_001()
    {
        Skip.IfNot(HasFirebaseJson, "Cần firebase-adminsdk.json.");
        var c = TryCreateController();
        Skip.If(c == null, "Không khởi tạo được NotificationsController.");

        // Gửi notification đầy đủ Topic/Title/Body — FCM thật
        c.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var result = await c.SendNotification(new NotificationRequest
        {
            Topic = "nha_hang_pho_thin",
            Title = "Thông báo thử",
            Body = "Nội dung tin nhắn mẫu."
        });

        // 200 + MessageId
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>TC-NC-SND-002</summary>
    [SkippableFact]
    public async Task SendNotification_MissingTopic_TC_NC_SND_002()
    {
        Skip.IfNot(HasFirebaseJson, "Cần firebase-adminsdk.json.");
        var c = TryCreateController();
        Skip.If(c == null, "Không khởi tạo được NotificationsController.");

        // Topic rỗng → controller validation từ chối trước khi gọi Firebase
        c.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var result = await c.SendNotification(new NotificationRequest
        {
            Topic = "",
            Title = "T",
            Body = "B"
        });

        // 400 do thiếu Topic
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>TC-NC-SND-003</summary>
    [SkippableFact]
    public async Task SendNotification_MissingTitle_TC_NC_SND_003()
    {
        Skip.IfNot(HasFirebaseJson, "Cần firebase-adminsdk.json.");
        var c = TryCreateController();
        Skip.If(c == null, "Không khởi tạo được NotificationsController.");

        // Title rỗng → validation từ chối
        c.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var result = await c.SendNotification(new NotificationRequest
        {
            Topic = "t",
            Title = "",
            Body = "b"
        });

        // 400 do thiếu Title
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>TC-NC-SND-004</summary>
    [SkippableFact]
    public async Task SendNotification_MissingBody_TC_NC_SND_004()
    {
        Skip.IfNot(HasFirebaseJson, "Cần firebase-adminsdk.json.");
        var c = TryCreateController();
        Skip.If(c == null, "Không khởi tạo được NotificationsController.");

        // Body rỗng → validation từ chối
        c.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var result = await c.SendNotification(new NotificationRequest
        {
            Topic = "t",
            Title = "title",
            Body = ""
        });

        // 400 do thiếu Body
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>TC-NC-SND-005</summary>
    [Fact(Skip = "TC-NC-SND-005: cần Firebase throw khi gửi — không inject mock.")]
    public Task SendNotification_FirebaseThrows_TC_NC_SND_005() => Task.CompletedTask;
}
