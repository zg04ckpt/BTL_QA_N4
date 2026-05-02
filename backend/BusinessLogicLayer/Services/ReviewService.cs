using BusinessLogicLayer.Interfaces;
using DataAccessLayer;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;
namespace BusinessLogicLayer.Services;

public class ReviewService : IReviewService
{
    private readonly ReviewRepository _reviewRepository;
    private readonly FirebaseService _firebaseService;
    private readonly QRInformationRepository _qrInformationRepository;
    private readonly IConfiguration _configuration;
    private static readonly HttpClient _httpClient = new HttpClient();

    /// <summary>
    /// URL đầy đủ tới POST /predict (Docker: hostname là tên service trong docker-compose, ví dụ ml_server).
    /// </summary>
    private string MlPredictUrl =>
        _configuration["MlServer:PredictUrl"] ?? "http://ml_server:2002/predict";

    public ReviewService(
        ReviewRepository reviewRepository,
        FirebaseService firebaseService,
        QRInformationRepository qrInformationRepository,
        IConfiguration configuration)
    {
        _reviewRepository = reviewRepository;
        _firebaseService = firebaseService;
        _qrInformationRepository = qrInformationRepository;
        _configuration = configuration;
    }

    /// <returns>(false, message) nếu ML chặn nội dung; (true, null) nếu cho phép hoặc bỏ qua kiểm tra.</returns>
    private async Task<(bool Ok, string? Message)> ModerateCommentWithMlAsync(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return (true, null);

        try
        {
            var response = await _httpClient.PostAsJsonAsync(MlPredictUrl, new { comment = content });
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"ML moderation HTTP {(int)response.StatusCode} at {MlPredictUrl}");
                return (true, null);
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (result.TryGetProperty("predicted_class_id", out var classIdProp))
            {
                var predictedClassId = classIdProp.GetInt32();
                if (predictedClassId != 0)
                {
                    return (false, "Bình luận của bạn chứa ngôn từ tiêu cực hoặc không phù hợp. Vui lòng chỉnh sửa lại!");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calling AI moderation ({MlPredictUrl}): {ex.Message}");
        }

        return (true, null);
    }

    public async Task<(bool Success, string Message)> AddReviewAsync(ReviewDto reviewDto)
    {
        var requireQr = _configuration.GetValue("Review:RequireQrScan", false);

        if (requireQr)
        {
            var latestScan = await _qrInformationRepository.GetLatestQRInformationAsync(reviewDto.UserId, reviewDto.RestaurantId);
            if (latestScan == null)
            {
                throw new Exception("You must scan the restaurant's QR code before writing a review.");
            }

            var scanTime = DateTimeOffset.FromUnixTimeMilliseconds(latestScan.CreateTime).UtcDateTime;
            if (latestScan.CreateTime < 10000000000)
                scanTime = DateTimeOffset.FromUnixTimeSeconds(latestScan.CreateTime).UtcDateTime;

            if ((DateTime.UtcNow - scanTime).TotalDays > 30)
                throw new Exception("Your QR scan has expired. Please scan again.");
        }

        // Call AI moderation (ML server trong Docker: service ml_server, cổng 2002)
        int status = 1; // 1: Approved, 0: Pending, -1: Rejected
        var (mlOk, mlMsg) = await ModerateCommentWithMlAsync(reviewDto.Content);
        if (!mlOk && mlMsg != null)
            return (false, mlMsg);

        var review = new Review
        {
            UserId = reviewDto.UserId,
            RestaurantId = reviewDto.RestaurantId,
            Content = reviewDto.Content,
            Score = reviewDto.Score,
            CreateDate = reviewDto.CreateDate,
            Status = status,
            Photos = reviewDto.PhotoUrls?.Select(url => new ReviewPhoto
            {
                ImageUrl = url
            }).ToList() ?? new List<ReviewPhoto>()
        };

        try
        {
            
            await _reviewRepository.AddReviewAsync(review);
            var notificationTitle = "Nhà hàng của bạn có đánh giá mới!";
            var notificationBody = $"Nhà hàng {reviewDto.RestaurantId} vừa nhận được một đánh giá với số điểm {reviewDto.Score}.";
            await _firebaseService.SendNotificationToTopicAsync($"admin_{reviewDto.RestaurantId}", notificationTitle, notificationBody);

            return (true, "Review added successfully");
        }
        catch (Exception ex)
        {
            
            return (false, $"Failed to add review: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> DeleteReviewAsync(int reviewId)
    {
        try
        {
            await _reviewRepository.DeleteReviewAsync(reviewId);
            return (true, "Review deleted successfully");
        }
        catch (KeyNotFoundException)
        {
            return (false, "Review not found");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return (false, "An error occurred while deleting the review");
        }
    }

    public async Task<(bool Success, string Message)> UpdateReviewAsync(int reviewId, ReviewDto reviewDto)
    {
        try
        {
            var review = await _reviewRepository.GetReviewByIdAsync(reviewId);
            if (review == null) return (false, "Review not found");

            var (mlOk, mlMsg) = await ModerateCommentWithMlAsync(reviewDto.Content);
            if (!mlOk && mlMsg != null)
                return (false, mlMsg);

            review.Content = reviewDto.Content;
            review.Score = reviewDto.Score;

            review.Photos = reviewDto.PhotoUrls?.Select(url => new ReviewPhoto { ImageUrl = url }).ToList();

            await _reviewRepository.UpdateReviewAsync(review);
            return (true, "Review updated successfully");
        }
        catch (KeyNotFoundException)
        {
            return (false, "Review not found");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return (false, "An error occurred while updating the review");
        }
    }


    public async Task<IEnumerable<ReviewDto>> GetAllReviewsAsync()
    {
        var reviews = await _reviewRepository.GetAllAsync();

      
        return reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            UserId = r.UserId,
            User = r.User?.Name ,
            RestaurantId = r.RestaurantId,
            RestaurantName = r.Restaurant?.Name, 
            Content = r.Content,
            Score = r.Score ?? 0, 
            CreateDate = r.CreateDate,
            ReportsCount = r.Reports?.Count ?? 0,
            PhotoUrls = r.Photos?.Select(p => p.ImageUrl ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList()
                ?? new List<string>()
        });
    }
    public async Task<IEnumerable<ReviewDto>> GetReviewsByUserIdAsync(int userId)
    {
        var reviews = await _reviewRepository.GetReviewsByUserIdAsync(userId);

        return reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            UserId = r.UserId,
            User = r.User?.Name, 
            RestaurantId = r.RestaurantId,
            RestaurantName = r.Restaurant?.Name, 
            Content = r.Content,
            Score = r.Score,
            CreateDate = r.CreateDate,
            ReportsCount = r.Reports?.Count ?? 0,
            PhotoUrls = r.Photos?.Select(p => p.ImageUrl ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList()
                ?? new List<string>()
        });
    }

    public async Task<IEnumerable<ReviewDto>> GetReviewsByRestaurantIdAsync(int restaurantId)
    {
        var reviews = await _reviewRepository.GetReviewsByRestaurantIdAsync(restaurantId);

        return reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            UserId = r.UserId,
            User = r.User?.Name,
            RestaurantId = r.RestaurantId,
            RestaurantName = r.Restaurant?.Name, 
            Content = r.Content,
            Score = r.Score,
            CreateDate = r.CreateDate,
            ReportsCount = r.Reports?.Count ?? 0,
            PhotoUrls = r.Photos?.Select(p => p.ImageUrl ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList()
                ?? new List<string>()
        });
    }
    public async Task<IEnumerable<ReviewDto>> GetReviewsWithHighReportsAsync(int reportCount)
    {
        var reviews = await _reviewRepository.GetReviewsWithReportsGreaterThanAsync(reportCount);

        
        return reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            Content = r.Content,
            Score = r.Score,
            User = r.User?.Name,
            CreateDate = r.CreateDate,
            UserId = r.UserId,
            RestaurantId = r.RestaurantId,
            ReportsCount = r.Reports?.Count ?? 0,
            PhotoUrls = r.Photos?.Select(p => p.ImageUrl ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList()
                ?? new List<string>()
        });
    }

}
