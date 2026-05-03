using DataAccessLayer.Context;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

public class ReportRepository
{
    private readonly ApplicationDbContext _context;

    public ReportRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Report>> GetReportsByReviewIdAsync(int reviewId)
    {
        return await _context.Reports
            .AsNoTracking()
            .Where(r => r.ReviewId == reviewId)
            .Include(r => r.User)
            .OrderByDescending(r => r.Id)
            .ToListAsync();
    }

    public virtual async Task<Report> AddReportAsync(Report report)
    {
        await _context.Reports.AddAsync(report);
        await _context.SaveChangesAsync();
        return report;
    }

    public async Task<Report?> GetReportByIdAsync(int id)
    {
        return await _context.Reports.FindAsync(id);
    }

    public virtual async Task<Report?> UpdateReportStatusAsync(int id, int status)
    {
        var report = await _context.Reports.FindAsync(id);
        if (report == null) return null;

        report.Status = status;
        await _context.SaveChangesAsync();
        return report;
    }
}