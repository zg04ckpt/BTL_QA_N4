using DataAccessLayer.Context;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

public class OrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Order> AddOrderAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
        return order;
    }
    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.User)
                .ThenInclude(u => u.Address)
            .Include(o => o.Restaurant)
                .ThenInclude(r => r.Address)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<bool> DeleteOrderAsync(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return false;

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, int newStatus)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return false;

        order.Status = newStatus;
        order.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Order?>> GetOrdersByUserIdAsync(int userId)
    {
        return await _context.Orders
            .Include(o => o.User)
                 .ThenInclude(u => u.Address)
            .Include(o => o.Restaurant)
                 .ThenInclude(r => r.Address)
            .Where(o => o.UserId == userId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetOrdersByRestaurantIdAsync(int restaurantId)
    {
        return await _context.Orders
            .Include(o => o.User)
                .ThenInclude(u => u.Address)
            .Include(o => o.Restaurant)
                .ThenInclude(r => r.Address)
            .Where(o => o.RestaurantId == restaurantId)
            .ToListAsync();
    }
}