// CafeMenu.Api.Models.CafeDisableStatus.cs
namespace CafeMenu.Api.Models;

public class CafeDisableStatus
{
    public int Id { get; set; }
    public int CafeId { get; set; }
    public Cafe Cafe { get; set; } = null!;
    public bool IsDisabled { get; set; }
    public DateTime DisabledAt { get; set; }
    public string? DisabledBy { get; set; }
}