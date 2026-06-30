using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CafeMenu.Api.Data;
/// <summary>
/// Dto for registering new cafe
/// </summary>
    public class RegisterCafeDto
    {
    public string CafeName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string InstagramUrl { get; set; } = string.Empty;
    public string ThemeJson { get; set; } = string.Empty;  
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    }
