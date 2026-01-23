using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class ReviewPhoto
{
    [Key]
    public int Id { get; set; }
    public string? ImageUrl { get; set; }
    public int ReviewId { get; set; }
    public virtual Review Review { get; set; }
}