using DataAccessLayer.Models.DTOs;

namespace BusinessLogicLayer.Interfaces;

public interface IFavoriteService
{
    Task<(bool Success, string Message)> ToggleFavoriteAsync(int userId, int restaurantId);
    Task<bool> IsFavoriteAsync(int userId, int restaurantId);
    Task<IEnumerable<RestaurantDetailDto>> GetFavoriteRestaurantsAsync(int userId);
}
