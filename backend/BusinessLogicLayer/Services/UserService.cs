using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BusinessLogicLayer.Interfaces;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BusinessLogicLayer.Services;

public class UserService : IUserService
{
    private readonly UserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly IAddressService _addressService;
    public UserService(UserRepository userRepository,IAddressService addressService, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _addressService = addressService;
    }

    public async Task<(bool Success, string Message)> RegisterUserAsync(UserDTO userDto)
    {
        var existingUser = await _userRepository.GetUserByEmailAsync(userDto.Email);
        if (existingUser != null)
        {
            return (false, "Email already exists");
        }

        
        var address = await _addressService.AddAddressAsync(userDto.Address);

        var newUser = new User
        {
            Email = userDto.Email,
            Password = userDto.Password,
            Name = userDto.Name,
            PhoneNumber = userDto.PhoneNumber,
            Role = userDto.Role == "admin" ? "admin" : "customer",
            Status = 1,
            AddressId = address.Id,
            Address = address ,
           
        };

        var createdUser = await _userRepository.CreateAsync(newUser);

        if (createdUser != null)
        {
            return (true, "User registered successfully");
        }

        return (false, "Failed to register user");
    }



    public async Task<LoginResult> LoginAsync(LoginDTO loginDto)
    {
        var user = await _userRepository.GetUserByEmailAsync(loginDto.Email);

        if (user == null || user.Password != loginDto.Password)
        {
            return new LoginResult
            {
                Message = "Email or Password is incorrect",
                Token = null
            };
        }

        if (user.Status != 1)
        {
            return new LoginResult
            {
                Message = "Account is locked. Please contact the administrator.",
                Token = null
            };
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

        ClaimsIdentity claims = new ClaimsIdentity(new Claim[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, _configuration["Jwt:Subject"]),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("Id", user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        });

        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];


        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = claims,
            Issuer = issuer,
            Audience = audience,
            Expires = DateTime.UtcNow.AddMinutes(1000),
            SigningCredentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        var token = tokenHandler.WriteToken(securityToken);

        return new LoginResult
        {
            Message = "Login successfully",
            Token = token
        };
    }


    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _userRepository.GetAllUsersAsync();
    }

    public async Task<IEnumerable<AdminUserSummaryDto>> GetAllUserSummariesForAdminAsync()
    {
        var users = await _userRepository.GetAllUsersForAdminListingAsync();
        return users.Select(u => new AdminUserSummaryDto
        {
            Id = u.Id,
            Email = u.Email,
            Name = u.Name,
            PhoneNumber = u.PhoneNumber,
            Role = u.Role,
            Status = u.Status,
            AvtImage = u.AvtImage,
            Address = u.Address == null
                ? null
                : new AddressDto
                {
                    City = u.Address.City,
                    District = u.Address.District,
                    Ward = u.Address.Ward,
                    Detail = u.Address.Detail,
                    Lat = u.Address.Lat,
                    Lon = u.Address.Lon
                }
        });
    }

    public async Task<UserDTO?> GetUserByIdAsync(int id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);

        if (user == null)
        {
            return null;
        }

        return new UserDTO
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role,
            Name = user.Name,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address != null ? new AddressDto
            {
                City = user.Address.City,
                District = user.Address.District,
                Ward = user.Address.Ward,
                Detail = user.Address.Detail,
                Lon = user.Address.Lon,
                Lat = user.Address.Lat,
            } : null,
            Status = user.Status,
            AvtImage = user.AvtImage,
        };
       

    }
    public async Task<(bool Success, string Message)> UpdateUserAsync(int id, UserUpdateDTO userUpdateDto)
    {
      
        var existingUser = await _userRepository.GetUserByIdAsync(id);
        if (existingUser == null)
        {
            return (false, "User not found");
        }

        existingUser.Status = userUpdateDto.Status;
        if (userUpdateDto.Name != null)
            existingUser.Name = userUpdateDto.Name;
        if (userUpdateDto.PhoneNumber != null)
            existingUser.PhoneNumber = userUpdateDto.PhoneNumber;
        if (userUpdateDto.AvtImage != null)
            existingUser.AvtImage = userUpdateDto.AvtImage;

        if (userUpdateDto.Address != null)
        {
            if (existingUser.Address != null)
            {
                existingUser.Address.City = userUpdateDto.Address.City ?? existingUser.Address.City;
                existingUser.Address.District = userUpdateDto.Address.District ?? existingUser.Address.District;
                existingUser.Address.Ward = userUpdateDto.Address.Ward ?? existingUser.Address.Ward;
                existingUser.Address.Detail = userUpdateDto.Address.Detail ?? existingUser.Address.Detail;
                if (userUpdateDto.Address.Lon.HasValue)
                    existingUser.Address.Lon = userUpdateDto.Address.Lon.Value;
                if (userUpdateDto.Address.Lat.HasValue)
                    existingUser.Address.Lat = userUpdateDto.Address.Lat.Value;
            }
            else
            {
                existingUser.Address = new Address
                {
                    City = userUpdateDto.Address.City ?? "",
                    District = userUpdateDto.Address.District ?? "",
                    Ward = userUpdateDto.Address.Ward ?? "",
                    Detail = userUpdateDto.Address.Detail ?? "",
                    Lon = userUpdateDto.Address.Lon ?? 0,
                    Lat = userUpdateDto.Address.Lat ?? 0
                };
            }
        }
        
        var updatedUser = await _userRepository.UpdateAsync(existingUser);
        if (updatedUser != null)
        {
            return (true, "User updated successfully");
        }

        return (false, "Failed to update user");
    }

    public async Task<(bool Success, string Message)> DeleteUserAsync(int id)
    {
       
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
        {
            return (false, "User not found");
        }

      
        var deleted = await _userRepository.DeleteAsync(id);
        if (deleted)
        {
            return (true, "User deleted successfully");
        }

        return (false, "Failed to delete user");
    }
}