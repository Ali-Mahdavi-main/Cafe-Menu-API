using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CafeMenu.Api.Data;
using CafeMenu.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AuthController(AppDbContext context)
        {
            _context = context;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register( [FromBody] RegisterCafeDto dto)
        {
            if (await _context.Cafes.AnyAsync(c => c.UserName == dto.Username))
            {
                return BadRequest("نام کاربری قبلا انتخواب شده است");
            }
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var cafe = new Cafe
            {
                Name = dto.CafeName,
                Address = dto.Address,
                LogoUrl = dto.LogoUrl,
                InstagramUrl = dto.InstagramUrl,
                ThemeJson = dto.ThemeJson,
                UserName = dto.Username,
                PasswordHash = hashedPassword
            };
            _context.Cafes.Add(cafe);
            await _context.SaveChangesAsync();
            return Ok(new{message = "کافه با موفقیت ثبت شد", cafeId = cafe.Id});
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var cafe = await _context.Cafes.FirstOrDefaultAsync(c => c.UserName == dto.Username);
            if (cafe == null) return Unauthorized("نام کاربری یا رمز عبور اشتباه است");

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, cafe.PasswordHash);
            if(!isPasswordValid) return Unauthorized("نام کاربری یا رمز عبور اشتباه است");
            
            return Ok(new { 
            message = "ورود موفقیت‌آمیز بود", 
            cafeId = cafe.Id, 
            cafeName = cafe.Name,
            theme = cafe.ThemeJson,
            instagram = cafe.InstagramUrl
            });
        }
    }
}