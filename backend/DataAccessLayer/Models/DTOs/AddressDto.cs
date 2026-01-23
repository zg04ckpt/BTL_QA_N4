namespace DataAccessLayer.Models.DTOs;

public class AddressDto
{
    public string City { get; set; }
    public string? District { get; set; }
    public string? Ward { get; set; }
    public string? Detail { get; set; }
    public double? Lon { get; set; }
    public double? Lat { get; set; }
}