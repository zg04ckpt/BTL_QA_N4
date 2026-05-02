namespace DataAccessLayer.Models.DTOs;

public class ReportListItemDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public int ReviewId { get; set; }
    public string? Reason { get; set; }
    public int? Status { get; set; }
}
