using BusinessLogicLayer.Services;
using DataAccessLayer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : Controller
{
    private readonly FirebaseService _firebaseService;

    public NotificationsController(FirebaseService firebaseService)
    {
        _firebaseService = firebaseService;
    }

    /// <summary>
    /// Tạm thời: gửi thông báo thử qua FCM tới topic (mặc định all_user). Dùng Postman/curl GET.
    /// </summary>
    [HttpGet("test-firebase")]
    public async Task<IActionResult> TestFirebase([FromQuery] string topic = "all_user")
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return BadRequest("topic is required");
        }

        try
        {
            var messageId = await _firebaseService.SendNotificationToTopicAsync(
                topic,
                "Thử thông báo Firebase",
                $"Gửi lúc {DateTime.UtcNow:O} (UTC) — nếu app đã subscribe topic \"{topic}\" sẽ nhận được.");
            return Ok(new { messageId, topic });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendNotification([FromBody] NotificationRequest request)
    {
        if (string.IsNullOrEmpty(request.Topic) || string.IsNullOrEmpty(request.Title) || string.IsNullOrEmpty(request.Body))
        {
            return BadRequest("Invalid notification payload.");
        }

        try
        {
            var response = await _firebaseService.SendNotificationToTopicAsync(request.Topic, request.Title, request.Body);
            return Ok(new { MessageId = response });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }
}