using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;

namespace BusinessLogicLayer.Interfaces;

public interface IOrderService
{
    Task AddOrderAsync(AddOrderDto addOrderDto);
    Task<OrderDetailDto?> GetOrderByIdAsync(int id);
    Task<bool> RemoveOrderAsync(int orderId);
    Task<bool> ChangeOrderStatusAsync(int orderId, int newStatus);
    Task<List<OrderDetailDto>> GetOrdersByUserIdAsync(int userId);
    Task<List<OrderDetailDto>> GetOrdersByRestaurantIdAsync(int restaurantId);
}