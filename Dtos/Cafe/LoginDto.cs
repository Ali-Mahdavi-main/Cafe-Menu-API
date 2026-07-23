using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CafeMenu.Api.Dtos.Cafe;
/// <summary>
/// Dto for login the user
/// </summary>
    public class LoginDto
    {
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
    }
