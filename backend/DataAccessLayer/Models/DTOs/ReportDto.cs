namespace DataAccessLayer.Models.DTOs;

public class ReportDto
{
    public int UserId { get; set; }
    public int ReviewId { get; set; }
    public string? Reason { get; set; }
    public int? Status { get; set; } = 0;
}