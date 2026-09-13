namespace CafeMenu.Api.Models;

public class ParentCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CafeId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Cafe? Cafe { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
