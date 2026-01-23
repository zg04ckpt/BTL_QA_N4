using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;

namespace BusinessLogicLayer.Interfaces;

public interface IUserService
{
    Task<(bool Success, string Message)> RegisterUserAsync(UserDTO userDto);
    Task<UserDTO?> GetUserByIdAsync(int id);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<LoginResult> LoginAsync(LoginDTO loginDto);
    Task<(bool Success, string Message)> UpdateUserAsync(int id, UserUpdateDTO userDto);
    Task<(bool Success, string Message)> DeleteUserAsync(int id);
}