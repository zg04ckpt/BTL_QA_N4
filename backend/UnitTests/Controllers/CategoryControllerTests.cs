using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

using BusinessLogicLayer.Interfaces;
using DataAccessLayer.Models.DTOs;
using WebAPI.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests.Controllers
{
    public class CategoryControllerTests
    {
        /// <summary>
        /// TC-AC-2-001: Test với trường hợp CategoryService inject đúng
        /// Expected: Pass
        /// </summary>
        [Fact]
        public async Task AddCategory_InjectedCorrectly_ReturnsCreatedAtAction()
        {
            // Arrange
            var mockCategoryService = new Mock<ICategoryService>();
            
            var validDto = new CategoryDto { Name = "Trà sữa" };
            var returnedDto = new CategoryDto { Id = 1, Name = "Trà sữa" };

            mockCategoryService.Setup(s => s.AddCategoryAsync(validDto)).ReturnsAsync(returnedDto);

            var controller = new CategoriesController(mockCategoryService.Object);

            // Act
            var result = await controller.AddCategory(validDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var actualDto = Assert.IsType<CategoryDto>(createdResult.Value);
            Assert.Equal("Trà sữa", actualDto.Name);
            Assert.Equal(1, actualDto.Id);

            Assert.Equal(nameof(CategoriesController.GetCategoryById), createdResult.ActionName);
            Assert.NotNull(createdResult.RouteValues);
            Assert.True(createdResult.RouteValues!.ContainsKey("id"));
            Assert.Equal(1, createdResult.RouteValues["id"]);
             
            // Verify
            mockCategoryService.Verify(s => s.AddCategoryAsync(validDto), Times.Once);
        }

        /// <summary>
        /// TC-AC-1-001 (Đại diện luôn cho TC-GAC-1-001 và TC-GCBI-1-001)
        /// 1 -- Test với trường hợp CategoryService inject sai nhầm tên với service khác
        /// Expected: Failed (Ném ra lỗi InvalidOperationException)
        /// </summary>
        [Fact]
        public void Constructor_InjectWrongService_ThrowsInvalidOperationException()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // CỐ TÌNH INJECT SAI: Đưa IAddressService vào thay vì ICategoryService
            var mockWrongService = new Mock<IAddressService>();
            services.AddSingleton(mockWrongService.Object);
            services.AddTransient<CategoriesController>();
            
            var serviceProvider = services.BuildServiceProvider();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
            {
                serviceProvider.GetRequiredService<CategoriesController>();
            });

            Assert.Contains("ICategoryService", exception.Message);
        }
    }
}
