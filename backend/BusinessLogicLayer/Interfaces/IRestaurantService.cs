using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;

namespace BusinessLogicLayer.Interfaces;

public interface  IRestaurantService
{
    Task<Restaurant?> AddRestaurantAsync(CreateRestaurantDto dto);

    Task<IEnumerable<RestaurantDto>> GetRestaurantsByCategoryAsync(int categoryId);
    Task<IEnumerable<RestaurantDto>> GetRestaurantsByUserAsync(int userId);
    Task<IEnumerable<RestaurantDto>> SearchRestaurantsAsync(string searchTerm);
    Task<RestaurantDetailDto?> GetRestaurantByIdAsync(int id);
    Task UpdateRestaurantAsync(int id, UpdateRestaurantDto updateDto);
    Task DeleteRestaurantAsync(int id);
    Task<IEnumerable<RestaurantDetailDto>> GetRestaurantsAsync(
        int? categoryId ,
        int? userId ,
        string? searchTerm ,
        string? city ,
        string? district,
        string? ward );
    Task<IEnumerable<RestaurantDto>> GetRestaurantsByAddressAsync(string? city = null, string? district = null, string? ward = null, string? street = null);
}

