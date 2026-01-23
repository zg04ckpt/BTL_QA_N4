using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

public class Order
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } 
    
    public string PhoneNumber { get; set; } 
    
    public string Email { get; set; }
    public int Status { get; set; } 
    public int NumOfMembers { get; set; }
    
    [Range(1, int.MaxValue)]
    public string ReservationTime { get; set; }

    public string? SpecialRequest { get; set; }

    public long? CreatedAt { get; set; } 

    public DateTime? UpdatedAt { get; set; } = DateTime.Now;
    public int UserId { get; set; }
    
    public User? User { get; set; }

    public int RestaurantId { get; set; }


    public Restaurant? Restaurant { get; set; }
    
   

    
   
}