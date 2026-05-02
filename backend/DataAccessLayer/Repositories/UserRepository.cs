using DataAccessLayer.Context;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

public class UserRepository
{
    private readonly ApplicationDbContext _dbContext;


    public UserRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;

    }
    
    public async Task<User> GetUserByEmailAsync(string email)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _dbContext.Users
            .Include(u => u.Address)
            .Include(u => u.Reports)
            .Include(u => u.Reviews)
            .Include(u => u.Restaurants)
            .FirstOrDefaultAsync(u => u.Id == id);
    }


    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _dbContext.Users
            .Include(u => u.Address)
            .Include(u => u.Reports)
            .Include(u => u.Reviews)
            .Include(u => u.Restaurants)
            .ToListAsync();
    }

    /// <summary>Chỉ địa chỉ — dùng cho màn admin, nhẹ hơn và an toàn khi map sang DTO (không trả password).</summary>
    public async Task<IEnumerable<User>> GetAllUsersForAdminListingAsync()
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.Address)
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Email)
            .ToListAsync();
    }
    
    public async Task<User> CreateAsync(User user)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

   
    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return false;

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();
        return true;
    }
    public async Task<User> UpdateAsync(User user)
    {
        var existingUser = await _dbContext.Users.FindAsync(user.Id);
        if (existingUser == null) return null; 

        _dbContext.Entry(existingUser).CurrentValues.SetValues(user);
        await _dbContext.SaveChangesAsync();
        return existingUser;
    }
}