using DataAccessLayer.Context;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

public class QRInformationRepository
{
    private readonly ApplicationDbContext _context;

    public QRInformationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddQRInformationAsync(QRInformation qrInformation)
    {
        await _context.QRInformations.AddAsync(qrInformation);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<QRInformation>> GetQRInformationAsync(int? userId, int? restaurantId)
    {
        var query =   _context.QRInformations
            .Include(q => q.User)
            .Include(q => q.Restaurant)
            .AsQueryable();
        if (userId.HasValue)
            query = query.Where(r => r.UserId == userId.Value);
        if (restaurantId.HasValue)
            query = query.Where(r => r.RestaurantId == restaurantId.Value);
        
        return await query.ToListAsync();
    }
    public async Task<QRInformation?> GetQRInformationAsync(int userId, int restaurantId)
    {
        return await _context.QRInformations
            .FirstOrDefaultAsync(q => q.UserId == userId && q.RestaurantId == restaurantId);
    }
    
    public async Task<QRInformation?> GetLatestQRInformationAsync(int userId, int restaurantId)
    {
        return await _context.QRInformations
            .Where(q => q.UserId == userId && q.RestaurantId == restaurantId)
            .OrderByDescending(q => q.CreateTime)
            .FirstOrDefaultAsync();
    }

    public async Task UpdateQRInformationAsync(QRInformation qrInformation)
    {
        _context.QRInformations.Update(qrInformation);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteQRInformationAsync(int id)
    {
        var qrInformation = await _context.QRInformations.FindAsync(id);
        if (qrInformation != null)
        {
            _context.QRInformations.Remove(qrInformation);
            await _context.SaveChangesAsync();
        }
    } 
}