namespace DataAccessLayer.Models.DTOs;

public class NotificationRequest
{
    public string Topic { get; set; } 
    public string Title { get; set; }
    public string Body { get; set; }
}