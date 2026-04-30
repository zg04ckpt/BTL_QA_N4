using BusinessLogicLayer.Interfaces;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;
[Route("api/[controller]")]
[ApiController]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> AddOrder([FromBody] AddOrderDto addOrderDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid order payload.", data = ModelState });
        }

        await _orderService.AddOrderAsync(addOrderDto);

        return Ok(new { message = "Order added successfully" });
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var orderDetail = await _orderService.GetOrderByIdAsync(id);
        if (orderDetail == null)
        {
            return NotFound(new { message = "Order not found" });
        }

        return Ok(orderDetail);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var result = await _orderService.RemoveOrderAsync(id);
        if (!result) return NotFound(new { success = false, message = "Order not found" });
        return Ok(new { success = true, message = "Order deleted successfully" });
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] int status)
    {
        var result = await _orderService.ChangeOrderStatusAsync(id, status);
        if (!result) return NotFound(new { success = false, message = "Order not found" });
        return Ok(new { success = true, message = "Order status updated successfully" });
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetOrdersByUserId(int userId)
    {
        var orders = await _orderService.GetOrdersByUserIdAsync(userId);
        if (orders == null || !orders.Any())
        {
            return NotFound(new { success = false, message = "No orders found for the user." });
        }
        return Ok(orders);
    }

    [HttpGet("restaurant/{restaurantId}")]
    public async Task<IActionResult> GetOrdersByRestaurantId(int restaurantId)
    {
        var orders = await _orderService.GetOrdersByRestaurantIdAsync(restaurantId);
        if (orders == null || !orders.Any())
        {
            return NotFound(new { success = false, message = "No orders found for the restaurant." });
        }
        return Ok(orders);
    }
}