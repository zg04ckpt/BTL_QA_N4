using DataAccessLayer.Context;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

public class RestaurantRepository
{
    private readonly ApplicationDbContext _context;

    public RestaurantRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Restaurant?> AddRestaurantAsync(Restaurant restaurant)
    {
        await _context.Restaurants.AddAsync(restaurant);
        await _context.SaveChangesAsync();
        return restaurant;
    }
    
    
    public async Task<IEnumerable<Restaurant>> GetRestaurantsByCategoryAsync(int categoryId)
    {
        return await _context.Restaurants
            .Where(r => r.CateId == categoryId)
            .Include(r => r.Category)
            .Include(r => r.Address)
            .Include(r => r.RestaurantPhotos)
            .ToListAsync();
    }
    public async Task<IEnumerable<Restaurant>> GetRestaurantsByUserAsync(int userId)
    {
        return await _context.Restaurants
            .Where(r => r.UserId == userId)
            .Include(r => r.Category)
            .Include(r => r.Address)
            .Include(r => r.RestaurantPhotos)
            .ToListAsync();
    }

    public async Task<IEnumerable<Restaurant>> SearchRestaurantsAsync(string searchTerm)
    {
        return await _context.Restaurants
            .Where(r => r.Name.Contains(searchTerm) || r.Address.City.Contains(searchTerm))
            .Include(r => r.Category)
            .Include(r => r.Address)
            .ToListAsync();
    }

    public async Task<Restaurant?> GetRestaurantByIdAsync(int id)
    {
        return await _context.Restaurants
            .Include(r => r.Category)
            .Include(r => r.Address)
            .Include(r => r.Reviews)
            .Include(r => r.User)
            .Include(r => r.RestaurantPhotos)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task UpdateRestaurantAsync(Restaurant restaurant)
    {
        _context.Restaurants.Update(restaurant);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRestaurantAsync(int id)
    {
        var restaurant = await _context.Restaurants.FindAsync(id);
        if (restaurant != null)
        {
            _context.Restaurants.Remove(restaurant);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<IEnumerable<Restaurant>> GetRestaurantsByAddressAsync(string? city = null, string? district = null, string? ward = null, string? street = null)
    {
        var query = _context.Restaurants.Include(r => r.Address).AsQueryable();

        if (!string.IsNullOrEmpty(city))
            query = query.Where(r => r.Address.City == city);

        if (!string.IsNullOrEmpty(district))
            query = query.Where(r => r.Address.District == district);

        if (!string.IsNullOrEmpty(ward))
            query = query.Where(r => r.Address.Ward == ward);
        
        return await query.ToListAsync();
    }
    public async Task<IEnumerable<Restaurant>> GetRestaurantsAsync(
        int? categoryId = null,
        int? userId = null,
        string? searchTerm = null,
        string? city = null,
        string? district = null,
        string? ward = null)
    {
        var query = _context.Restaurants
            .Include(r => r.Category)
            .Include(r => r.Address)
            .Include(r => r.User)
            .Include(r => r.Reviews)
            .Include(r => r.RestaurantPhotos)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(r => r.CateId == categoryId.Value);
        if (userId.HasValue)
            query = query.Where(r => r.UserId == userId.Value);

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(r => r.Name.Contains(searchTerm) || r.Address.City.Contains(searchTerm));

        if (!string.IsNullOrEmpty(city))
            query = query.Where(r => r.Address.City == city);

        if (!string.IsNullOrEmpty(district))
            query = query.Where(r => r.Address.District == district);

        if (!string.IsNullOrEmpty(ward))
            query = query.Where(r => r.Address.Ward == ward);

        return await query.ToListAsync();
    }


}