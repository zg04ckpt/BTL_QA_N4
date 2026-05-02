using BusinessLogicLayer.Interfaces;
using DataAccessLayer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[Route("api/reviews")]
[ApiController]
public class ReviewController : Controller
{
    private readonly IReviewService _reviewService;

    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpPost]
    public async Task<IActionResult> AddReview([FromBody] ReviewDto reviewDto)
    {
        if (reviewDto == null)
        {
            return BadRequest("Review data cannot be null.");
        }

        try
        {
            var result = await _reviewService.AddReviewAsync(reviewDto);

            if (result.Success)
            {
                return Ok(new { message = result.Message });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = result.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var (success, message) = await _reviewService.DeleteReviewAsync(id);
        return success ? Ok(new { message }) : NotFound(new { message });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReview(int id, [FromBody] ReviewDto reviewDto)
    {
        var (success, message) = await _reviewService.UpdateReviewAsync(id, reviewDto);
        return success ? Ok(new { message }) : NotFound(new { message });
    }
    [HttpGet]
    public async Task<IActionResult> GetAllReviews()
    {
        var reviews = await _reviewService.GetAllReviewsAsync();
        return Ok(reviews);
    }
    [HttpGet("by-user/{userId}")]
    public async Task<IActionResult> GetReviewsByUserId(int userId)
    {
        var reviews = await _reviewService.GetReviewsByUserIdAsync(userId);
        if (reviews == null || !reviews.Any())
            return NotFound("No reviews found for the given user.");
        return Ok(reviews);
    }

    [HttpGet("by-restaurant/{restaurantId}")]
    public async Task<IActionResult> GetReviewsByRestaurantId(int restaurantId)
    {
        var reviews = await _reviewService.GetReviewsByRestaurantIdAsync(restaurantId);
        if (reviews == null || !reviews.Any())
            return NotFound("No reviews found for the given restaurant.");
        return Ok(reviews);
    }

    [HttpGet("high-reports")]
    public async Task<IActionResult> GetReviewsWithHighReports([FromQuery] int reportCount = 5)
    {
        var reviews = await _reviewService.GetReviewsWithHighReportsAsync(reportCount);
        return Ok(reviews);
    }
}