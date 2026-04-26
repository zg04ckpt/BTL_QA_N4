using BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FavoriteController : ControllerBase
{
    private readonly IFavoriteService _favoriteService;

    public FavoriteController(IFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    [HttpPost("toggle")]
    public async Task<IActionResult> ToggleFavorite(int userId, int restaurantId)
    {
        var result = await _favoriteService.ToggleFavoriteAsync(userId, restaurantId);
        if (result.Success)
        {
            return Ok(new { message = result.Message });
        }
        return BadRequest(new { message = result.Message });
    }

    [HttpGet("check")]
    public async Task<IActionResult> IsFavorite(int userId, int restaurantId)
    {
        var isFavorite = await _favoriteService.IsFavoriteAsync(userId, restaurantId);
        return Ok(new { isFavorite });
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserFavorites(int userId)
    {
        var favorites = await _favoriteService.GetFavoriteRestaurantsAsync(userId);
        return Ok(favorites);
    }
}
