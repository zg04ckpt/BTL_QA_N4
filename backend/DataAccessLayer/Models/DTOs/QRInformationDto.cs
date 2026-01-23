namespace DataAccessLayer.Models.DTOs;

public class QRInformationDto
{ 
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RestaurantId { get; set; }
    public long CreateTime { get; set; }
}