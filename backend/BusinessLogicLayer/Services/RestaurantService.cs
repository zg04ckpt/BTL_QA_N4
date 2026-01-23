using BusinessLogicLayer.Interfaces;
using DataAccessLayer;
using DataAccessLayer.Context;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;

namespace BusinessLogicLayer.Services;
public class RestaurantService : IRestaurantService
{
    private readonly RestaurantRepository _restaurantRepository;
    private readonly IAddressService _addressService;
    private readonly FirebaseService _firebaseService;
    public RestaurantService(RestaurantRepository restaurantRepository,IAddressService addressService, FirebaseService firebaseService)
    {
        _restaurantRepository = restaurantRepository;
        _addressService = addressService;
        _firebaseService = firebaseService;
    }

    public async Task<Restaurant?> AddRestaurantAsync(CreateRestaurantDto dto)
    {
        var address = await _addressService.AddAddressAsync(dto.Address);
        var restaurant = new Restaurant
        {
            Name = dto.Name,
            Status = 0,
            Email = dto.Email,
            Description = dto.Description,
            PhoneNumber = dto.PhoneNumber,
            AvtImage = dto.AvtImage,
            UserId = dto.UserId,
            CateId = dto.CateId,
            AddressId = address.Id,
            AverageScore = 0,      
            TotalReviews = 0
        };
       
            restaurant.RestaurantPhotos = dto.RestaurantPhotos.Select(url => new RestaurantPhoto
            {
                ImageUrl = url
            }).ToList();
        

        return await _restaurantRepository.AddRestaurantAsync(restaurant);
    }
    

    public async Task<IEnumerable<RestaurantDto>> GetRestaurantsByCategoryAsync(int categoryId)
    {
        var restaurants = await _restaurantRepository.GetRestaurantsByCategoryAsync(categoryId);
        return restaurants.Select(r => new RestaurantDto(r));
    }
    public async Task<IEnumerable<RestaurantDto>> GetRestaurantsByUserAsync(int userId)
    {
        var restaurants = await _restaurantRepository.GetRestaurantsByCategoryAsync(userId);
        return restaurants.Select(r => new RestaurantDto(r));
    }

    public async Task<IEnumerable<RestaurantDto>> SearchRestaurantsAsync(string searchTerm)
    {
        var restaurants = await _restaurantRepository.SearchRestaurantsAsync(searchTerm);
        return restaurants.Select(r => new RestaurantDto(r));
    }

    public async Task<RestaurantDetailDto?> GetRestaurantByIdAsync(int id)
    {
        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(id);

        if (restaurant == null)
            return null;


        return new RestaurantDetailDto
        {
            Id = restaurant.Id,
            Name = restaurant.Name,
            Email = restaurant.Email,
            Description = restaurant.Description,
            PhoneNumber = restaurant.PhoneNumber,
            Address = restaurant.Address,
            Category = restaurant.Category?.Name,
            Status = restaurant.Status,
            AvtImage = restaurant.AvtImage,
            TotalReviews = restaurant.TotalReviews,
            AverageScore = restaurant.AverageScore,
            UserId = restaurant.UserId,
            RestaurantPhotos = restaurant.RestaurantPhotos?.Select(p => p.ImageUrl).ToList()
            .ToList()
        
    };
    }

    public async Task UpdateRestaurantAsync(int id, UpdateRestaurantDto updateDto)
    {
        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(id);
        if (restaurant == null) throw new KeyNotFoundException("Restaurant not found");

        restaurant.Name = updateDto.Name;
        restaurant.Email = updateDto.Email;
        restaurant.Description = updateDto.Description;
        restaurant.PhoneNumber = updateDto.PhoneNumber;
        restaurant.Status = updateDto.Status;
        restaurant.AvtImage = updateDto.AvtImage;
        restaurant.CateId = updateDto.CateId;

       
        restaurant.Address.City = updateDto.Address.City;
        restaurant.Address.District = updateDto.Address.District;
        restaurant.Address.Ward = updateDto.Address.Ward;
        restaurant.Address.Detail = updateDto.Address.Detail;
        restaurant.Address.Lon = updateDto.Address.Lon;
        restaurant.Address.Lat = updateDto.Address.Lat;
        
       

        restaurant.RestaurantPhotos = updateDto.RestaurantPhotos?.Select(url => new RestaurantPhoto { ImageUrl = url }).ToList();
        await _restaurantRepository.UpdateRestaurantAsync(restaurant);
        var notificationTitle = "Nhà hàng đã được cập nhật!";
        var notificationBody = $"Nhà hàng {restaurant.Name} vừa cập nhật thông tin mới. Hãy kiểm tra ngay!";
        await _firebaseService.SendNotificationToTopicAsync($"user_{restaurant.Id}", notificationTitle, notificationBody);
    }

    public async Task DeleteRestaurantAsync(int id)
    {
        await _restaurantRepository.DeleteRestaurantAsync(id);
    }
    public async Task<IEnumerable<RestaurantDto>> GetRestaurantsByAddressAsync(string? city = null, string? district = null, string? ward = null, string? street = null)
    {
        var restaurants = await _restaurantRepository.GetRestaurantsByAddressAsync(city, district, ward, street);
        return restaurants.Select(r => new RestaurantDto(r));
    }
    public async Task<IEnumerable<RestaurantDetailDto>> GetRestaurantsAsync(
        int? categoryId,
        int? userId,
        string? searchTerm,
        string? city ,
        string? district,
        string? ward )
    {
        var restaurants = await _restaurantRepository.GetRestaurantsAsync(
            categoryId,userId, searchTerm, city, district, ward);

        return restaurants.Select(r => new RestaurantDetailDto
        {
            Id = r.Id,
            Name = r.Name,
            Email = r.Email,
            Description = r.Description,
            PhoneNumber = r.PhoneNumber,
            Address = r.Address,
            Category = r.Category?.Name,
            Status = r.Status,
            AvtImage = r.AvtImage,
            TotalReviews = r.TotalReviews,
            AverageScore = r.AverageScore,
            UserId = r.UserId,
            RestaurantPhotos = r.RestaurantPhotos?
            .Select(p => p.ImageUrl).ToList()

        }).ToList();
        
    }

    
}