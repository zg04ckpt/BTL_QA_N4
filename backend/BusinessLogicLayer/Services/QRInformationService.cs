using BusinessLogicLayer.Interfaces;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;

namespace BusinessLogicLayer.Services;

public class QRInformationService : IQRInformationService
{
    private readonly QRInformationRepository _qrInformationRepository;

    public QRInformationService(QRInformationRepository qrInformationRepository)
    {
        _qrInformationRepository = qrInformationRepository;
    }

    public async Task AddQRInformationAsync(QRInformationDto dto)
    {
        var existingQRInformation = await _qrInformationRepository.GetQRInformationAsync(dto.UserId, dto.RestaurantId);

        if (existingQRInformation != null)
        {
            existingQRInformation.CreateTime = dto.CreateTime;
            await _qrInformationRepository.UpdateQRInformationAsync(existingQRInformation);
        }
        else
        {
            var qrInformation = new QRInformation
            {
                UserId = dto.UserId,
                RestaurantId = dto.RestaurantId,
                CreateTime = dto.CreateTime
            };

            await _qrInformationRepository.AddQRInformationAsync(qrInformation);
        }
    }

    public async Task<IEnumerable<QRInformationDto>> GetQRInformationAsync(int? userId, int? restaurantId)
    {
        var qr = await _qrInformationRepository.GetQRInformationAsync(userId, restaurantId);
        return qr.Select(r => new QRInformationDto
        {
            Id = r.Id,
            UserId = r.UserId,
            RestaurantId = r.RestaurantId
        }).ToList();
            
    }

    public async Task DeleteQRInformationAsync(int id)
    {
        await _qrInformationRepository.DeleteQRInformationAsync(id);
    }
}