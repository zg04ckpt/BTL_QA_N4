using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class RestaurantPhoto
{
    [Key]
    public int Id { get; set; }
    public string? ImageUrl { get; set; }
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; }
}