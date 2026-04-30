using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhotoController : ControllerBase
    {
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No file uploaded." });

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(new { Image = $"/images/{fileName}" });
            }
            catch (Exception ex)
            {

                Console.WriteLine("File upload error: " + ex.Message);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}
