namespace DataAccessLayer.Models.DTOs;

public class LoginDTO
{
    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;
}