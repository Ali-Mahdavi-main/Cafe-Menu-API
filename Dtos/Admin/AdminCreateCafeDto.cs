namespace CafeMenu.Api.Dtos;

public class AdminCreateCafeDto
{
    public string CafeName { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? ThemeConfigJson { get; set; }
    public string? Phone { get; set; }
    public string? WorkingHours { get; set; }
}