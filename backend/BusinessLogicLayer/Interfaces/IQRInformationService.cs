using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;

namespace BusinessLogicLayer.Interfaces;

public interface IQRInformationService
{ 
    Task AddQRInformationAsync(QRInformationDto dto);
    Task<IEnumerable<QRInformationDto>> GetQRInformationAsync(int? userId, int? restaurantId);
    Task DeleteQRInformationAsync(int id);
}