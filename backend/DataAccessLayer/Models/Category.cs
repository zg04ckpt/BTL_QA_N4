using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataAccessLayer.Models;

public class Category
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    
    public virtual  ICollection<Restaurant>? Restaurants { get; set; }
}