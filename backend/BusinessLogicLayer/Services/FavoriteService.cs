using BusinessLogicLayer.Interfaces;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;

namespace BusinessLogicLayer.Services;

public class FavoriteService : IFavoriteService
{
    private readonly UserFavoriteRepository _favoriteRepository;
    private readonly UserRepository _userRepository;
    private readonly RestaurantRepository _restaurantRepository;

    public FavoriteService(UserFavoriteRepository favoriteRepository, UserRepository userRepository, RestaurantRepository restaurantRepository)
    {
        _favoriteRepository = favoriteRepository;
        _userRepository = userRepository;
        _restaurantRepository = restaurantRepository;
    }

    public async Task<(bool Success, string Message)> ToggleFavoriteAsync(int userId, int restaurantId)
    {
        // Kiểm tra User có tồn tại không
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            return (false, "Người dùng không tồn tại");
        }

        // Kiểm tra Nhà hàng có tồn tại không
        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(restaurantId);
        if (restaurant == null)
        {
            return (false, "Nhà hàng không tồn tại");
        }

        var existingFavorite = await _favoriteRepository.GetFavoriteAsync(userId, restaurantId);
        if (existingFavorite != null)
        {
            await _favoriteRepository.RemoveFavoriteAsync(userId, restaurantId);
            return (true, "Đã xóa khỏi danh sách yêu thích");
        }
        else
        {
            var favorite = new UserFavorite
            {
                UserId = userId,
                RestaurantId = restaurantId,
                CreateDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            await _favoriteRepository.AddFavoriteAsync(favorite);
            return (true, "Đã thêm vào danh sách yêu thích");
        }
    }

    public async Task<bool> IsFavoriteAsync(int userId, int restaurantId)
    {
        var favorite = await _favoriteRepository.GetFavoriteAsync(userId, restaurantId);
        return favorite != null;
    }

    public async Task<IEnumerable<RestaurantDetailDto>> GetFavoriteRestaurantsAsync(int userId)
    {
        var favorites = await _favoriteRepository.GetFavoritesByUserIdAsync(userId);
        return favorites.Select(f => new RestaurantDetailDto
        {
            Id = f.Restaurant.Id,
            Name = f.Restaurant.Name,
            Email = f.Restaurant.Email,
            Description = f.Restaurant.Description,
            PhoneNumber = f.Restaurant.PhoneNumber,
            Address = f.Restaurant.Address,
            Category = f.Restaurant.Category?.Name,
            Status = f.Restaurant.Status,
            AvtImage = f.Restaurant.AvtImage,
            AverageScore = f.Restaurant.AverageScore,
            UserId = f.Restaurant.UserId,
            RestaurantPhotos = f.Restaurant.RestaurantPhotos?.Select(rp => rp.ImageUrl).ToList()
        });
    }
}
