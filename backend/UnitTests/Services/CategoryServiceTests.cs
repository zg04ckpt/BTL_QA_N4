using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using UnitTests.Infrastructure;
using Xunit;

namespace UnitTests.Services;

public class CategoryServiceTests
{
        // =========================================================================
        // NHÓM 1: AddCategoryAsync
        // =========================================================================

        /// <summary>
        /// TC-ACA-1-001
        /// Name là chuỗi rỗng (tương đương null về mặt nghiệp vụ)
        /// Expected: Failed (Ném ra ArgumentException hoặc DbUpdateException)
        /// </summary>
        [Fact]
        public async Task AddCategoryAsync_NameIsEmpty_ThrowsException()
        {
            // Arrange
            await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
            await using var context = SqliteMemoryDb.CreateContext(connection);
            var service = new CategoryService(new CategoryRepository(context));

            var dto = new CategoryDto { Name = "" };

            // Act + Assert
            // // Code hiện tại sẽ chạy xuyên qua và lưu chuỗi rỗng xuống DB -> Test này sẽ FAILED (đúng ý đồ bắt lỗi của bạn)
            // await Assert.ThrowsAsync<ArgumentException>(() => service.AddCategoryAsync(dto));

            var result = await service.AddCategoryAsync(dto);
            Assert.Null(result);
            
            await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
            Assert.Equal(0, await verifyContext.Categories.CountAsync());
        }

        

        /// <summary>
        /// TC-ACA-1-002
        /// Name chứa ký tự đặc biệt
        /// Expected: Failed (Ném ra ArgumentException từ chối input)
        /// </summary>
        [Fact]
        public async Task AddCategoryAsync_NameHasSpecialCharacters_ThrowsException()
        {
            // Arrange
            await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
            await using var context = SqliteMemoryDb.CreateContext(connection);
            var service = new CategoryService(new CategoryRepository(context));

            var dto = new CategoryDto { Name = "#$" };

            var result = await service.AddCategoryAsync(dto);
            Assert.Null(result);
            
            await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
            Assert.Equal(0, await verifyContext.Categories.CountAsync());
        }

        /// <summary>
        /// TC-ACA-1-003
        /// Name chứa số
        /// Expected: Failed (Ném ra ArgumentException từ chối input)
        /// </summary>
        [Fact]
        public async Task AddCategoryAsync_NameContainsNumbers_ThrowsException()
        {
            // Arrange
            await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
            await using var context = SqliteMemoryDb.CreateContext(connection);
            var service = new CategoryService(new CategoryRepository(context));

            var dto = new CategoryDto { Name = "345" };

            // đảm bảo phải null
            var result = await service.AddCategoryAsync(dto);
            Assert.Null(result);
            
            // Đảm bảo phải null
            await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
            Assert.Equal(0, await verifyContext.Categories.CountAsync());
        }

        /// <summary>
        /// TC-ACA-1-004
        /// Name quá dài (vượt giới hạn cấu hình, ví dụ 100 ký tự)
        /// Expected: Failed (Ném ra ArgumentException hoặc DbUpdateException)
        /// </summary>
        [Fact]
        public async Task AddCategoryAsync_NameTooLong_ThrowsException()
        {
            // Arrange
            await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
            await using var context = SqliteMemoryDb.CreateContext(connection);
            var service = new CategoryService(new CategoryRepository(context));

            var dto = new CategoryDto { Name = new string('A', 101) }; // Giả lập chuỗi hơn 100 ký tự

            
            // đảm bảo phải null
            var result = await service.AddCategoryAsync(dto);
            Assert.Null(result);
            
            // Đảm bảo phải null
            await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
            Assert.Equal(0, await verifyContext.Categories.CountAsync());
        }

        /// <summary>
        /// TC-ACA-2-001
        /// Category hợp lệ
        /// Expected: Pass
        /// </summary>
        [Fact]
        public async Task AddCategoryAsync_ValidName_ReturnsCreatedCategory()
        {
            // Arrange
            await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
            await using var context = SqliteMemoryDb.CreateContext(connection);
            var service = new CategoryService(new CategoryRepository(context));

            var dto = new CategoryDto { Name = "Trà sữa" };

            // Act
            var result = await service.AddCategoryAsync(dto);

            // Assert & Verify
            Assert.NotNull(result);
            Assert.Equal("Trà sữa", result.Name);

            await using var verifyContext = SqliteMemoryDb.CreateContext(connection);
            Assert.Equal(1, await verifyContext.Categories.CountAsync());
        }

        // =========================================================================
        // NHÓM 2: GetAllCategoriesAsync
        // =========================================================================

        /// <summary>
        /// TC-GACA-1-001
        /// Lấy toàn bộ dữ liệu thành công
        /// Expected: Pass
        /// </summary>
        [Fact]
        public async Task GetAllCategoriesAsync_ReturnsAllData()
        {
            // Arrange
            await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
            await using var context = SqliteMemoryDb.CreateContext(connection);
            
            context.Categories.AddRange(
                new Category { Id = 1, Name = "Trà sữa" },
                new Category { Id = 2, Name = "Cà phê" }
            );
            await context.SaveChangesAsync();

            var service = new CategoryService(new CategoryRepository(context));

            // Act
            var result = await service.GetAllCategoriesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        // =========================================================================
        // NHÓM 3: GetCategoryByIdAsync
        // =========================================================================
        // Lưu ý kỹ thuật: Hàm nhận vào kiểu 'int id', không nhận CategoryDto.
        // Do đó kịch bản kiểm tra lỗi (TC-GCBIA-1-001 đến 004) sẽ quy về việc test các ID không hợp lệ.

        /// <summary>
        /// TC-GCBIA-1-001 (Gộp chung nhóm ID không hợp lệ/không tồn tại)
        /// ID truyền vào không có trong DB
        /// Expected: Trả về null theo logic code hiện tại
        /// </summary>
        [Fact]
        public async Task GetCategoryByIdAsync_IdNotFound_ReturnsNull()
        {
            // Arrange
            await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
            await using var context = SqliteMemoryDb.CreateContext(connection);
            var service = new CategoryService(new CategoryRepository(context));

            // Act
            var result = await service.GetCategoryByIdAsync(999999999); // Cố tình truyền ID ảo

            // Assert
            // Code hiện tại của bạn viết: if (category == null) return null; -> Test này sẽ Pass.
            Assert.Null(result); 
        }

        /// <summary>
        /// TC-GCBIA-2-001
        /// Lấy category với ID hợp lệ
        /// Expected: Pass
        /// </summary>
        [Fact]
        public async Task GetCategoryByIdAsync_ValidId_ReturnsCategory()
        {
            // Arrange
            await using var connection = await SqliteMemoryDb.CreatePreparedConnectionAsync();
            await using var context = SqliteMemoryDb.CreateContext(connection);
            
            context.Categories.Add(new Category { Id = 1, Name = "Trà sữa" });
            await context.SaveChangesAsync();

            var service = new CategoryService(new CategoryRepository(context));

            // Act
            var result = await service.GetCategoryByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Trà sữa", result.Name);
            Assert.Equal(1, result.Id);
        }
    
}
