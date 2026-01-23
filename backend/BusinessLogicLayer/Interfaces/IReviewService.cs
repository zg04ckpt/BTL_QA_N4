using DataAccessLayer;
using DataAccessLayer.Models.DTOs;

namespace BusinessLogicLayer.Interfaces;

public interface IReviewService
{
    Task<(bool Success, string Message)> AddReviewAsync(ReviewDto reviewDto);
    Task<(bool Success, string Message)> DeleteReviewAsync(int reviewId);
    Task<(bool Success, string Message)> UpdateReviewAsync(int reviewId, ReviewDto reviewDto);
    Task<IEnumerable<ReviewDto>> GetAllReviewsAsync();
    Task<IEnumerable<ReviewDto>> GetReviewsByRestaurantIdAsync(int restaurantId);
    Task<IEnumerable<ReviewDto>> GetReviewsByUserIdAsync(int userId);
    Task<IEnumerable<ReviewDto>> GetReviewsWithHighReportsAsync(int reportCount);
}