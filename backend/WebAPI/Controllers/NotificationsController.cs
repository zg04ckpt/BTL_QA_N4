using BusinessLogicLayer.Services;
using DataAccessLayer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : Controller
{
    private readonly FirebaseService _firebaseService;

    public NotificationsController()
    {
        _firebaseService = new FirebaseService();
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