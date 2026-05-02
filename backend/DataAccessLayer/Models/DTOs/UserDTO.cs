namespace DataAccessLayer.Models.DTOs;

public class UserDTO
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string Role { get; set; } = "customer";
    public string? Name { get; set; }
    public string? AvtImage { get; set; }
    public string? PhoneNumber { get; set; }
    public AddressDto? Address { get; set; }
    public int Status { get; set; } = 1;
}
public class UserUpdateDTO
{

    public string? Name { get; set; }

    public string? PhoneNumber { get; set; }
    public AddressDto? Address { get; set; }
    public int Status { get; set; } = 1;
    public string? AvtImage { get; set; }
}

/// <summary>Danh sách admin — không chứa mật khẩu.</summary>
public class AdminUserSummaryDto
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Name { get; set; }
    public string? PhoneNumber { get; set; }
    public int Status { get; set; }
    public string? AvtImage { get; set; }
    public AddressDto? Address { get; set; }
}
public class UserDetailOrderDto
{
    public string? Name { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvtImage { get; set; }
    public Address? Address { get; set; }
}