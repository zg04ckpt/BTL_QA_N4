using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class Report
{
    [Key]
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public int ReviewId { get; set; }
    public Review Review { get; set; }
    public string? Reason { get; set; }
    public int? Status { get; set; }
}