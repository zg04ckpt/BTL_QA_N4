using DataAccessLayer.Models.DTOs;

namespace BusinessLogicLayer.Interfaces;

public interface  ICategoryService
{
    Task<CategoryDto> AddCategoryAsync(CategoryDto dto);
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
    Task<CategoryDto> GetCategoryByIdAsync(int id);
}