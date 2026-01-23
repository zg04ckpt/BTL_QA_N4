namespace DataAccessLayer.Models.DTOs;

public class RestaurantDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Description { get; set; }
    public string? PhoneNumber { get; set; }
    public int TotalReviews { get; set; }
    public float? AverageScore { get; set; }

    public RestaurantDto(Restaurant restaurant)
    {
        Id = restaurant.Id;
        Name = restaurant.Name;
        Email = restaurant.Email;
        PhoneNumber = restaurant.PhoneNumber;
        AverageScore = restaurant.AverageScore;
    }
}

public class RestaurantDetailDto 
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Description { get; set; }
    public string? PhoneNumber { get; set; }
    public Address? Address { get; set; }
    public string? Category { get; set; }
    public int Status { get; set; }
    public string? AvtImage { get; set; }
    public int? TotalReviews { get; set; }
    public float? AverageScore { get; set; }
    public int UserId { get; set; }
    public List<string?>? RestaurantPhotos { get; set; }

}
public class RestaurantPhotoDto
{
    public string? ImageUrl { get; set; } 
    public int RestaurantId { get; set; } 
}

public class UpdateRestaurantDto
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Description { get; set; }
    public int Status { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvtImage { get; set; }
    public int CateId { get; set; }
    public int AddressId { get; set; }
    public AddressDto? Address { get; set; }
    public List<string> RestaurantPhotos { get; set; } = new List<string>();
}
public class CreateRestaurantDto
{
    public string Name { get; set; }
    public int Status { get; set; } = 0;
    public string Email { get; set; }
    public string? Description { get; set; }
    public string PhoneNumber { get; set; }
    public string AvtImage { get; set; }
    public int CateId { get; set; }
    public int UserId { get; set; }
    
    public AddressDto Address { get; set; }
    public List<string> RestaurantPhotos { get; set; } = new List<string>();
}
public class RestaurantDetailOrderDto
{
    public string? Name { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public Address? Address { get; set; }
}
