using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class User
{
    [Key]
    public int Id { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string Role { get; set; }
    public string? Name { get; set; }
    public string? AvtImage { get; set; }
    public string? PhoneNumber { get; set; }
    public int Status { get; set; }
    public int?  AddressId { get; set; }
    public virtual Address? Address { get; set; }
    public ICollection<Report>? Reports { get; set; }
    public ICollection<Review>?  Reviews { get; set; }
    public ICollection<Restaurant>? Restaurants { get; set; }
    public ICollection<Order>? Orders { get; set; } = new List<Order>();
    
}