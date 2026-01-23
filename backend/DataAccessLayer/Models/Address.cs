using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class Address
{
    [Key]
    public int Id { get; set; }
    public string City { get; set; }
    public string? District { get; set; }
    public string? Ward { get; set; }
    public string? Detail { get; set; }
    public double? Lon { get; set; }
    public double? Lat { get; set; }
   
}