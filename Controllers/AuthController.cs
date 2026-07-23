using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CafeMenu.Api.Data;
using CafeMenu.Api.Models;
using CafeMenu.Api.Dtos;
using CafeMenu.Api.Dtos.Cafe; // Assume you have DTOs

namespace CafeMenu.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCafeDto dto)
        {
            if (await _context.Cafes.AnyAsync(c => c.UserName == dto.Username))
                return BadRequest("نام کاربری قبلا انتخاب شده است");

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var cafe = new Cafe
            {
                Name = dto.CafeName,
                Address = dto.Address,
                LogoUrl = dto.LogoUrl,
                InstagramUrl = dto.InstagramUrl,
                ThemeConfigJson = dto.ThemeConfigJson,
                UserName = dto.Username,
                PasswordHash = hashedPassword
            };

            _context.Cafes.Add(cafe);
            await _context.SaveChangesAsync();

            return Ok(new { message = "کافه با موفقیت ثبت شد", cafeId = cafe.Id });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var cafe = await _context.Cafes.FirstOrDefaultAsync(c => c.UserName == dto.Username);
            if (cafe == null || !BCrypt.Net.BCrypt.Verify(dto.Password, cafe.PasswordHash))
                return Unauthorized("نام کاربری یا رمز عبور اشتباه است");

            var token = GenerateJwtToken(cafe);
            return Ok(new { 
                message = "ورود موفقیت‌آمیز بود", 
                token,
                cafeId = cafe.Id,
                cafeName = cafe.Name,
                theme = cafe.ThemeConfigJson,
                instagram = cafe.InstagramUrl
            });
        }

        private string GenerateJwtToken(Cafe cafe)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, cafe.Id.ToString()),
                new Claim("CafeId", cafe.Id.ToString()),
                new Claim(ClaimTypes.Name, cafe.UserName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}