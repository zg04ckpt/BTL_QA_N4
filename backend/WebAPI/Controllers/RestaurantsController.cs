using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RestaurantsController : Controller
{
    private readonly IRestaurantService _restaurantService;

    public RestaurantsController(IRestaurantService restaurantService)
    {
        _restaurantService = restaurantService;
    }

    [HttpPost]
    public async Task<IActionResult> AddRestaurant([FromBody] CreateRestaurantDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid restaurant payload.", data = ModelState });

        var restaurant = await _restaurantService.AddRestaurantAsync(dto);
        if (restaurant == null)
            return StatusCode(500, new { success = false, message = "Error creating the restaurant" });

        return CreatedAtAction(nameof(GetRestaurantById), new { id = restaurant.Id }, restaurant);
    }

    [HttpGet("category/{categoryId}")]
    public async Task<IActionResult> GetRestaurantsByCategory(int categoryId)
    {
        var restaurants = await _restaurantService.GetRestaurantsByCategoryAsync(categoryId);
        return Ok(restaurants);
    }
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetRestaurantsByUser(int userId)
    {
        var restaurants = await _restaurantService.GetRestaurantsByCategoryAsync(userId);
        return Ok(restaurants);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchRestaurants([FromQuery] string searchTerm)
    {
        var restaurants = await _restaurantService.SearchRestaurantsAsync(searchTerm);
        return Ok(restaurants);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRestaurantById(int id)
    {
        var restaurant = await _restaurantService.GetRestaurantByIdAsync(id);
        if (restaurant == null) return NotFound(new { success = false, message = "Restaurant not found" });
        return Ok(restaurant);
    }

    [HttpPut("update/{id}")]
    public async Task<IActionResult> UpdateRestaurant(int id, [FromBody] UpdateRestaurantDto updateDto)
    {
        await _restaurantService.UpdateRestaurantAsync(id, updateDto);
        return Ok(new { success = true, message = "Restaurant updated successfully" });
    }

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteRestaurant(int id)
    {
        await _restaurantService.DeleteRestaurantAsync(id);
        return Ok(new { success = true, message = "Restaurant deleted successfully" });
    }
    [HttpGet("get-restaurant-by-address")]
    public async Task<IActionResult> GetRestaurants([FromQuery] string? city, [FromQuery] string? district, [FromQuery] string? ward, [FromQuery] string? street)
    {
        var restaurants = await _restaurantService.GetRestaurantsByAddressAsync(city, district, ward, street);
        return Ok(restaurants);
    }
    
    [HttpGet("GetRestaurants")]
    public async Task<IActionResult> GetRestaurants(
        [FromQuery] int? categoryId,
        [FromQuery] int? userId,
        [FromQuery] string? searchTerm,
        [FromQuery] string? city,
        [FromQuery] string? district,
        [FromQuery] string? ward)
    {
        var restaurants = await _restaurantService.GetRestaurantsAsync(
            categoryId,userId, searchTerm, city, district, ward);

        return Ok(restaurants);
    }
}