namespace DataAccessLayer.Models.DTOs;

public class AddOrderDto
{
    public string Name { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public int UserId { get; set; }
    public int RestaurantId { get; set; }
    public int NumOfMembers { get; set; }
    public string ReservationTime { get; set; }
    public string? SpecialRequest { get; set; }
    public long? CreatedAt { get; set; }
}