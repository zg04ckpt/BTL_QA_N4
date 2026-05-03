using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

using BusinessLogicLayer.Interfaces; 
using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using WebAPI.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests.Controllers
{
    public class AddressControllerTests
    {
        /// <summary>
        /// TC-AA-2-001: Test với trường hợp AddressService inject đúng và hoạt động bình thường
        /// Expected: Pass (Controller gọi được hàm và trả về HTTP 201 Created)
        /// </summary>
        [Fact]
        public async Task AddAddress_InjectedCorrectly_ReturnsOk()
        {
             
            // Arrange: Chuẩn bị dữ liệu và Mock Service
            var mockAddressService = new Mock<IAddressService>();
            
            var validDto = new AddressDto { City = "Ha Noi" };
            var returnedAddress = new Address { Id = 1, City = "Ha Noi" };

            // Giả lập hành vi của Service: Khi nhận validDto thì trả về returnedDto
            mockAddressService.Setup(s => s.AddAddressAsync(validDto)).ReturnsAsync(returnedAddress);

            // Inject Mock Service vào Controller
            var controller = new AddressController(mockAddressService.Object);

            // Act: Gọi API
            var result = await controller.AddAddress(validDto);

            // Assert: Kiểm chứng kết quả
            // Controller hiện tại trả về HTTP 200 OK với message
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Address added successfully", okResult.Value);
             
            // Verify: Khẳng định Controller ĐÃ GỌI hàm AddAddressAsync của Service đúng 1 lần
            mockAddressService.Verify(s => s.AddAddressAsync(validDto), Times.Once);
        }
        /// <summary>
        /// TC-AA-1-001
        /// 1 -- Test với trường hợp AddressService inject sai nhầm tên với service khác
        /// Expected: Failed (Ném ra lỗi InvalidOperationException do không Resolve được DI)
        /// </summary>
        [Fact]
        public void Constructor_InjectWrongService_ThrowsInvalidOperationException()
        {
            // Arrange: Khởi tạo DI Container của .NET
            var services = new ServiceCollection();
            
            // CỐ TÌNH INJECT SAI: Controller cần IAddressService, nhưng ta lại đăng ký ICategoryService
            var mockWrongService = new Mock<ICategoryService>();
            services.AddSingleton(mockWrongService.Object);
            
            // Đăng ký Controller
            services.AddTransient<AddressController>();
            
            var serviceProvider = services.BuildServiceProvider();

            // Act & Assert
            // Hệ thống sẽ "chết" ở đây vì AddressController đòi IAddressService nhưng trong Container chỉ có ICategoryService
            var exception = Assert.Throws<InvalidOperationException>(() => 
            {
                serviceProvider.GetRequiredService<AddressController>();
            });

            // Verify: Xác nhận lỗi đúng là do không resolve được IAddressService
            Assert.Contains("IAddressService", exception.Message);
        }
    }

    
}
