namespace CafeMenu.Api.Dtos.Settings;

public class UpdateSettingsDto
{
    public string CafeName { get; set; }
    public string? LogoUrl { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Instagram { get; set; }
    public string? WorkingHours { get; set; }
}