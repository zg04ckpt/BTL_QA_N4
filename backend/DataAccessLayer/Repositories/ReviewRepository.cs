using DataAccessLayer.Context;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

public class ReviewRepository
{
    private readonly ApplicationDbContext _context;

    public ReviewRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddReviewAsync(Review review)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
           
            await _context.Reviews.AddAsync(review);
            await _context.SaveChangesAsync();

            var restaurant = await _context.Restaurants
                .Include(r => r.Reviews)
                .FirstOrDefaultAsync(r => r.Id == review.RestaurantId);

            if (restaurant != null)
            {
                restaurant.TotalReviews = restaurant.Reviews.Count;
                restaurant.AverageScore = restaurant.Reviews.Average(r => r.Score);
                _context.Restaurants.Update(restaurant);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteReviewAsync(int reviewId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            var restaurant = await _context.Restaurants
                 .Include(r => r.Reviews)
                 .FirstOrDefaultAsync(r => r.Id == review.RestaurantId);

            if (restaurant != null)
            {
                restaurant.TotalReviews = restaurant.Reviews.Count;
                restaurant.AverageScore = restaurant.Reviews.Average(r => r.Score);
                _context.Restaurants.Update(restaurant);
                await _context.SaveChangesAsync();
            }
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateReviewAsync(Review updatedReview)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
           
            _context.Reviews.Update(updatedReview);
            await _context.SaveChangesAsync();

            var restaurant = await _context.Restaurants
                 .Include(r => r.Reviews)
                 .FirstOrDefaultAsync(r => r.Id == updatedReview.RestaurantId);

            if (restaurant != null)
            {
                restaurant.TotalReviews = restaurant.Reviews.Count;
                restaurant.AverageScore = restaurant.Reviews.Average(r => r.Score);
                _context.Restaurants.Update(restaurant);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Review?> GetReviewByIdAsync(int reviewId)
    {
        return await _context.Reviews.FindAsync(reviewId);
    }
    public async Task<IEnumerable<Review>> GetAllAsync()
    {
        return await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Restaurant)
            .Include(r => r.Photos)
            .ToListAsync();
    }
    public async Task<IEnumerable<Review>> GetReviewsByUserIdAsync(int userId)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Restaurant)
            .Include(r => r.Photos)
            .Where(r => r.UserId == userId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Review>> GetReviewsByRestaurantIdAsync(int restaurantId)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Restaurant)
            .Include(r => r.Photos)
            .Where(r => r.RestaurantId == restaurantId)
            .ToListAsync();
    }
    public async Task<IEnumerable<Review>> GetReviewsWithReportsGreaterThanAsync(int reportCount)
    {
        return await _context.Reviews
            .Where(r => r.Reports != null && r.Reports.Count >= reportCount)
            .Include(r => r.User) 
            .Include(r => r.Reports) 
            .Include(r => r.Restaurant) 
            .ToListAsync();
    }

}