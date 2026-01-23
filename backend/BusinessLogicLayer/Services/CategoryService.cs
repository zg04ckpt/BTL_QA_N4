using BusinessLogicLayer.Interfaces;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;

namespace BusinessLogicLayer.Services;

public class CategoryService : ICategoryService
{
    private readonly CategoryRepository _categoryRepository;

    public CategoryService(CategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryDto> AddCategoryAsync(CategoryDto dto)
    {
        var category = new Category
        {
            Name = dto.Name
        };

        var addedCategory = await _categoryRepository.AddCategoryAsync(category);

        return new CategoryDto
        {
            Id = addedCategory.Id,
            Name = addedCategory.Name
        };
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await _categoryRepository.GetAllCategoriesAsync();

        return categories.Select(category => new CategoryDto
        {
            Id = category.Id,
            Name = category.Name
        });
    }
    public async Task<CategoryDto> GetCategoryByIdAsync(int id)
    {
        var category = await _categoryRepository.GetCategoryByIdAsync(id);

        if (category == null)
        {
            return null; // Or throw an exception if preferred
        }

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name
        };
    }

}