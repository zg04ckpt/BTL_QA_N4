namespace DataAccessLayer.Models.DTOs;

public class OrderDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public int UserId { get; set; }
    public UserDetailOrderDto? User { get; set; }
    public int RestaurantId { get; set; }
    public RestaurantDetailOrderDto? Restaurant { get; set; }
    public int Status { get; set; }
    public int NumOfMembers { get; set; }
    public string ReservationTime { get; set; }
    public string? SpecialRequest { get; set; }
    public long? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}