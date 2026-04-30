using BusinessLogicLayer.Interfaces;
using DataAccessLayer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : Controller
{
    private readonly IUserService _userService;


    public UserController(IUserService userService)
    {
        _userService = userService;
    }


    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserDTO userDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid registration payload.", data = ModelState });
        }

        var (success, message) = await _userService.RegisterUserAsync(userDto);
        if (success)
        {
            return Ok(new { success = true, message });
        }

        return BadRequest(new { success = false, message });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
    {
        if (loginDto == null)
        {
            return BadRequest(new { success = false, message = "Invalid login data." });
        }

        
        var loginResult = await _userService.LoginAsync(loginDto);

        if (loginResult.Message == "Login successfully")
        {
            return Ok(new { success = true, message = loginResult.Message, data = new { token = loginResult.Token } });
        }
        else
        {
            return Unauthorized(new { success = false, message = loginResult.Message });
        }
    }


    [HttpGet("GetAllUsers")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("GetUserById")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user != null)
        {
            return Ok(user);
        }

        return NoContent(); 
    }
    
    [HttpPut("UpdateUser/{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDTO userDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid user update payload.", data = ModelState });
        }

        var (success, message) = await _userService.UpdateUserAsync(id, userDto);
        if (success)
        {
            return Ok(new { success = true, message });
        }

        return BadRequest(new { success = false, message });
    }

    [HttpDelete("DeleteUser/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var (success, message) = await _userService.DeleteUserAsync(id);
        if (success)
        {
            return Ok(new { success = true, message });
        }

        return NotFound(new { success = false, message });
    }
}

