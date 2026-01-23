namespace DataAccessLayer.Models.DTOs;

public class ReviewDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? User { get; set; }
    public int RestaurantId { get; set; }
    public string? RestaurantName { get; set; }
    public string? Content { get; set; }
    public float? Score { get; set; }
    public long? CreateDate { get; set; }
    public int ReportsCount { get; set; }
    public List<string> PhotoUrls { get; set; } = new List<string>();
    
}

public class ReviewDetailDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? User { get; set; }
    public int RestaurantId { get; set; }

    public string? Content { get; set; }
    public float? Score { get; set; }
    public long? CreateDate { get; set; }
    public List<string>? PhotoUrls { get; set; } 
}
public class ReviewPhotoDto
{
    public string? ImageUrl { get; set; }
    public int? ReviewId { get; set; }
}
