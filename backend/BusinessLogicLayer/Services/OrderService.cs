using BusinessLogicLayer.Interfaces;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;

namespace BusinessLogicLayer.Services;

public class OrderService:IOrderService
{
    private readonly OrderRepository _orderRepository;
    private readonly FirebaseService _firebaseService;

    public OrderService(OrderRepository orderRepository, FirebaseService firebaseService)
    {
        _orderRepository = orderRepository;
        _firebaseService = firebaseService;
    }

    public async Task AddOrderAsync(AddOrderDto addOrderDto)
    {
        var order = new Order
        {
            Name = addOrderDto.Name,
            PhoneNumber = addOrderDto.PhoneNumber,
            Email = addOrderDto.Email,
            UserId = addOrderDto.UserId,
            RestaurantId = addOrderDto.RestaurantId,
            NumOfMembers = addOrderDto.NumOfMembers,
            ReservationTime = addOrderDto.ReservationTime,
            SpecialRequest = addOrderDto.SpecialRequest,
            CreatedAt = addOrderDto.CreatedAt,
            UpdatedAt = DateTime.Now,
            Status = 0 
        };

        await _orderRepository.AddOrderAsync(order);

        var notificationTitle = "Bạn có đơn đặt bàn mới!";
        var notificationBody = $"Khách hàng {order.Name} đã đặt bàn cho {order.NumOfMembers} người vào lúc {order.ReservationTime}. Hãy kiểm tra chi tiết!";
        await _firebaseService.SendNotificationToTopicAsync($"admin_{order.RestaurantId}", notificationTitle, notificationBody);
    }
    public async Task<OrderDetailDto?> GetOrderByIdAsync(int id)
    {
        var order = await _orderRepository.GetOrderByIdAsync(id);
        if (order == null) return null;

        return new OrderDetailDto
        {
            Id = order.Id,
            Name = order.Name,
            PhoneNumber = order.PhoneNumber,
            Email = order.Email,
            UserId = order.UserId,
            User = order.User != null ? new UserDetailOrderDto
            {
                Name = order.User.Name,
                PhoneNumber = order.User.PhoneNumber,
                Address = order.User.Address,
            } : null,
            RestaurantId = order.RestaurantId,
            Restaurant = order.Restaurant != null ? new RestaurantDetailOrderDto
            {
                Name = order.Restaurant.Name,
                PhoneNumber = order.Restaurant.PhoneNumber,
                Email = order.Restaurant.Email,
                Address = order.Restaurant.Address
            } : null,
            Status = order.Status,
            NumOfMembers = order.NumOfMembers,
            ReservationTime = order.ReservationTime,
            SpecialRequest = order.SpecialRequest,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }

    public async Task<bool> RemoveOrderAsync(int orderId)
    {
        return await _orderRepository.DeleteOrderAsync(orderId);
    }

    public async Task<bool> ChangeOrderStatusAsync(int orderId, int newStatus)
    {
        await _orderRepository.UpdateOrderStatusAsync(orderId, newStatus);
        var order = await _orderRepository.GetOrderByIdAsync(orderId);
        var userId = order.UserId;

        var notificationBody = "";
        var notificationTitle = "Cập nhật đơn đặt bàn";

        if (newStatus == 1)
        {
             notificationBody = $"Nhà hàng đã chấp nhận đơn đặt bàn của bạn ";
        }
        else
        {
            notificationBody = $"Nhà hàng đã từ chối đơn đặt bàn của bạn ";
        }
       
        await _firebaseService.SendNotificationToTopicAsync($"user_{userId}", notificationTitle, notificationBody);
        return true;
    }

    public async Task<List<OrderDetailDto>> GetOrdersByUserIdAsync(int userId)
    {
        var orders = await _orderRepository.GetOrdersByUserIdAsync(userId);
        return orders.Select(order => new OrderDetailDto
        {
            Id = order.Id,
            Name = order.Name,
            PhoneNumber = order.PhoneNumber,
            Email = order.Email,
            UserId = order.UserId,
            User = order.User != null ? new UserDetailOrderDto
            {
                Name = order.User.Name,
                PhoneNumber = order.User.PhoneNumber,
                Address = order.User.Address
            } : null,
            RestaurantId = order.RestaurantId,
            Restaurant = order.Restaurant != null ? new RestaurantDetailOrderDto
            {
                Name = order.Restaurant.Name,
                PhoneNumber = order.Restaurant.PhoneNumber,
                Email = order.Restaurant.Email,
                Address = order.Restaurant.Address
            } : null,
            Status = order.Status,
            NumOfMembers = order.NumOfMembers,
            ReservationTime = order.ReservationTime,
            SpecialRequest = order.SpecialRequest,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        }).ToList();
    }

    public async Task<List<OrderDetailDto>> GetOrdersByRestaurantIdAsync(int restaurantId)
    {
        var orders = await _orderRepository.GetOrdersByRestaurantIdAsync(restaurantId);
        return orders.Select(order => new OrderDetailDto
        {
            Id = order.Id,
            Name = order.Name,
            PhoneNumber = order.PhoneNumber,
            Email = order.Email,
            UserId = order.UserId,
            User = order.User != null ? new UserDetailOrderDto
            {
                Name = order.User.Name,
                PhoneNumber = order.User.PhoneNumber,
                Address = order.User.Address
            } : null,
            RestaurantId = order.RestaurantId,
            Restaurant = order.Restaurant != null ? new RestaurantDetailOrderDto
            {
                Name = order.Restaurant.Name,
                PhoneNumber = order.Restaurant.PhoneNumber,
                Email = order.Restaurant.Email,
                Address = order.Restaurant.Address
            } : null,
            Status = order.Status,
            NumOfMembers = order.NumOfMembers,
            ReservationTime = order.ReservationTime,
            SpecialRequest = order.SpecialRequest,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        }).ToList();
    }
}