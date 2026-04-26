using DataAccessLayer.Context;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

public class UserFavoriteRepository
{
    private readonly ApplicationDbContext _context;

    public UserFavoriteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddFavoriteAsync(UserFavorite favorite)
    {
        await _context.UserFavorites.AddAsync(favorite);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveFavoriteAsync(int userId, int restaurantId)
    {
        var favorite = await _context.UserFavorites.FirstOrDefaultAsync(f => f.UserId == userId && f.RestaurantId == restaurantId);
        if (favorite != null)
        {
            _context.UserFavorites.Remove(favorite);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<UserFavorite> GetFavoriteAsync(int userId, int restaurantId)
    {
        return await _context.UserFavorites.FirstOrDefaultAsync(f => f.UserId == userId && f.RestaurantId == restaurantId);
    }

    public async Task<IEnumerable<UserFavorite>> GetFavoritesByUserIdAsync(int userId)
    {
        return await _context.UserFavorites
            .Include(f => f.Restaurant)
                .ThenInclude(r => r.Address)
            .Include(f => f.Restaurant)
                .ThenInclude(r => r.Category)
            .Include(f => f.Restaurant)
                .ThenInclude(r => r.RestaurantPhotos)
            .Where(f => f.UserId == userId)
            .ToListAsync();
    }
}
