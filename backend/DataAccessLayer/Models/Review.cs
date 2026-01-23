using System.ComponentModel.DataAnnotations;
using DataAccessLayer.Models;

namespace DataAccessLayer;

public class Review
{
    [Key]
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public int RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }
    public string? Content { get; set; }
    public float? Score { get; set; }
    public long? CreateDate { get; set; }
    public ICollection<Report>? Reports { get; set; }
    public ICollection<ReviewPhoto>? Photos { get; set; }
}