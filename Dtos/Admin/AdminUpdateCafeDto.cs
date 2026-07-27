namespace CafeMenu.Api.Dtos;

public class AdminUpdateCafeDto
{
    public string CafeName { get; set; }
    public string? Username { get; set; }         // optional – update only if provided
    public string? Password { get; set; }          // optional
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? Phone { get; set; }
    public string? WorkingHours { get; set; }
    public string? ThemeConfigJson { get; set; }
    public bool? EventsEnabled { get; set; }
}