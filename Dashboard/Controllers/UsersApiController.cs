using Microsoft.AspNetCore.Mvc;
using UsersMgmt.Data;
using UsersMgmt.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;



namespace UsersMgmt.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersApiController : ControllerBase
    {
        private readonly ApplicationDbContext? _db;

        public UsersApiController(ApplicationDbContext? db)
        {
            _db = db;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers()
        {
            if (_db == null)
            {
                return StatusCode(500, "Database is not available.");
            }
            var users = await Task.Run(() => _db.Users.ToList());

            var res = users.Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                u.DateOfBirth,
                ImageData = u.ImageData != null ? Convert.ToBase64String(u.ImageData) : null
            });

            return Ok(res);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddUser(
            [FromForm] string name,
            [FromForm] string email,
            [FromForm] DateTime dateOfBirth,
            [FromForm] IFormFile? ImageData)
        {
            if (_db == null)
            {
                return StatusCode(500, "Database is not available.");
            }

            if (ImageData == null || ImageData.Length == 0)
            {
                return BadRequest("Image is required.");
            }

            byte[] imageBytes;
            using (var image = Image.Load(ImageData.OpenReadStream()))
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(100, 100),
                    Mode = ResizeMode.Crop
                }));

                using var ms = new MemoryStream();
                await image.SaveAsync(ms, new JpegEncoder { Quality = 75 });
                imageBytes = ms.ToArray();
            }

            var user = new User
            {
                Name = name,
                Email = email,
                DateOfBirth = dateOfBirth,
                ImageData = imageBytes
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(new { user.Id });
        }
    }
}
