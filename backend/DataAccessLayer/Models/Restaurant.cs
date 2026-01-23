using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataAccessLayer.Models;

public class Restaurant
{
    [Key]
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Status { get; set; }
    public string? Email { get; set; }
    public string? Description { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvtImage { get; set; }
    public float? AverageScore { get; set; }
    public int? TotalReviews { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; }
    
    public int CateId { get; set; }
    

    public virtual Category Category { get; set; }
    
    public int AddressId { get; set; }
    public virtual  Address Address { get;set; }
    public ICollection<Review>? Reviews { get; set; }
    public ICollection<RestaurantPhoto>? RestaurantPhotos { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}