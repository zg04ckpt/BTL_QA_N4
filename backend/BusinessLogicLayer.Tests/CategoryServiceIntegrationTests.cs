using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BusinessLogicLayer.Tests;

public class CategoryServiceIntegrationTests : TestDatabaseFixture
{
    [Fact]
    public async Task TC_ACA_001_AddCategoryAsync_ShouldCreateCategorySuccessfully()
    {
        // Test Case ID: TC-ACA-001
        // Mục tiêu: Kiểm tra tạo Category thành công với DTO hợp lệ trên DB thật.
        await BeginTransactionAsync();

        try
        {
            var categoryRepository = new CategoryRepository(DbContext);
            var categoryService = new CategoryService(categoryRepository);

            var inputCategoryDto = new CategoryDto
            {
                Name = "Electronics"
            };

            var createdCategoryDto = await categoryService.AddCategoryAsync(inputCategoryDto);

            Assert.NotNull(createdCategoryDto);
            Assert.NotEqual(0, createdCategoryDto.Id);
            Assert.Equal("Electronics", createdCategoryDto.Name);

            var savedCategory = await DbContext.Categories.FindAsync(createdCategoryDto.Id);
            Assert.NotNull(savedCategory);
            Assert.Equal("Electronics", savedCategory!.Name);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_ACA_002_AddCategoryAsync_ShouldAllowEmptyNameAndPersistToRepository()
    {
        // Test Case ID: TC-ACA-002
        // Mục tiêu: Kiểm tra service vẫn gọi xuống repository khi Name rỗng/khoảng trắng theo logic hiện tại.
        await BeginTransactionAsync();

        try
        {
            var categoryRepository = new CategoryRepository(DbContext);
            var categoryService = new CategoryService(categoryRepository);

            var inputCategoryDto = new CategoryDto
            {
                Name = "   "
            };

            var createdCategoryDto = await categoryService.AddCategoryAsync(inputCategoryDto);

            Assert.NotNull(createdCategoryDto);
            Assert.Equal("   ", createdCategoryDto.Name);

            var savedCategory = await DbContext.Categories.FindAsync(createdCategoryDto.Id);
            Assert.NotNull(savedCategory);
            Assert.Equal("   ", savedCategory!.Name);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_GAC_001_GetAllCategoriesAsync_ShouldReturnMappedCategoryList()
    {
        // Test Case ID: TC-GAC-001
        // Mục tiêu: Kiểm tra lấy danh sách Category và map đúng Id, Name từ DB thật.
        await BeginTransactionAsync();

        try
        {
            DbContext.Categories.AddRange(
                new Category { Name = "Drinks" },
                new Category { Name = "Food" });
            await DbContext.SaveChangesAsync();

            var categoryRepository = new CategoryRepository(DbContext);
            var categoryService = new CategoryService(categoryRepository);

            var categoryDtos = (await categoryService.GetAllCategoriesAsync()).ToList();

            Assert.NotEmpty(categoryDtos);
            Assert.True(categoryDtos.Any(category => category.Name == "Drinks"));
            Assert.True(categoryDtos.Any(category => category.Name == "Food"));
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_GAC_002_GetAllCategoriesAsync_ShouldReturnEmptyListWhenNoDataExists()
    {
        // Test Case ID: TC-GAC-002
        // Mục tiêu: Kiểm tra khi không có bản ghi Category nào thì trả về danh sách rỗng.
        await BeginTransactionAsync();

        try
        {
            var categoryRepository = new CategoryRepository(DbContext);
            var categoryService = new CategoryService(categoryRepository);

            var categoryDtos = (await categoryService.GetAllCategoriesAsync()).ToList();

            Assert.Empty(categoryDtos);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_GCBIA_001_GetCategoryByIdAsync_ShouldReturnCategoryWhenIdExists()
    {
        // Test Case ID: TC-GCBIA-001
        // Mục tiêu: Kiểm tra lấy đúng Category theo Id hợp lệ.
        await BeginTransactionAsync();

        try
        {
            var seedCategory = new Category
            {
                Name = "Books"
            };

            DbContext.Categories.Add(seedCategory);
            await DbContext.SaveChangesAsync();

            var categoryRepository = new CategoryRepository(DbContext);
            var categoryService = new CategoryService(categoryRepository);

            var categoryDto = await categoryService.GetCategoryByIdAsync(seedCategory.Id);

            Assert.NotNull(categoryDto);
            Assert.Equal(seedCategory.Id, categoryDto!.Id);
            Assert.Equal("Books", categoryDto.Name);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    [Fact]
    public async Task TC_GCBIA_002_GetCategoryByIdAsync_ShouldReturnNullWhenIdDoesNotExist()
    {
        // Test Case ID: TC-GCBIA-002
        // Mục tiêu: Kiểm tra service trả về null khi Id không tồn tại trong DB.
        await BeginTransactionAsync();

        try
        {
            var categoryRepository = new CategoryRepository(DbContext);
            var categoryService = new CategoryService(categoryRepository);

            var categoryDto = await categoryService.GetCategoryByIdAsync(999999);

            Assert.Null(categoryDto);
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }
}