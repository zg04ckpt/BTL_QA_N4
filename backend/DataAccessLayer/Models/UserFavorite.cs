using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class UserFavorite
{
    [Key]
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; }
    
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; }
    
    public long CreateDate { get; set; }
}
