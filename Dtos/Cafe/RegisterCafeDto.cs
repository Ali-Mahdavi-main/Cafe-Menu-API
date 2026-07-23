using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CafeMenu.Api.Dtos.Cafe;
/// <summary>
/// Dto for registering new cafe
/// </summary>
    public class RegisterCafeDto
    {
    [Required]
    [MaxLength(100)]
    [MinLength(1)]
    public string CafeName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [Url]
    public string LogoUrl { get; set; } = string.Empty;

    [Url]
    public string InstagramUrl { get; set; } = string.Empty;
    public string ThemeConfigJson { get; set; } = string.Empty;  

    [Required]
    [MinLength(3)]
    [MaxLength(30)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
    }
