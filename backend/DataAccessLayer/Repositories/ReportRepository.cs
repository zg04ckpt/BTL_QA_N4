using DataAccessLayer.Context;
using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories;

public class ReportRepository
{
    private readonly ApplicationDbContext _context;

    public ReportRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<Report> AddReportAsync(Report report)
    {
        await _context.Reports.AddAsync(report);
        await _context.SaveChangesAsync();
        return report;
    }

    public async Task<Report?> GetReportByIdAsync(int id)
    {
        return await _context.Reports.FindAsync(id);
    }

    public async Task<Report?> UpdateReportStatusAsync(int id, int status)
    {
        var report = await _context.Reports.FindAsync(id);
        if (report == null) return null;

        report.Status = status;
        await _context.SaveChangesAsync();
        return report;
    }
}