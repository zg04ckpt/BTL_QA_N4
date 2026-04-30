using BusinessLogicLayer.Interfaces;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;

namespace BusinessLogicLayer.Services;

public class ReportService : IReportService
{
    private readonly ReportRepository _reportRepository;

    public ReportService(ReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<Report> AddReportAsync(ReportDto reportDto)
    {
        var report = new Report
        {
            UserId = reportDto.UserId,
            ReviewId = reportDto.ReviewId,
            Reason = reportDto.Reason,
            Status = 0,
        };

        return await _reportRepository.AddReportAsync(report);
    }

    public async Task<(bool Success, string Message)> UpdateReportStatusAsync(int id, int status)
    {
        var updatedReport = await _reportRepository.UpdateReportStatusAsync(id, status);
        if (updatedReport == null)
        {
            return (false, "Report not found.");
        }
        return (true, "Report status updated successfully.");
    }

    public async Task<IEnumerable<Report>> GetAllReportsAsync()
    {
        return await _reportRepository.GetAllReportsAsync();
    }

    public async Task<IEnumerable<Report>> GetReportsByUserIdAsync(int userId)
    {
        return await _reportRepository.GetReportsByUserIdAsync(userId);
    }
}