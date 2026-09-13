namespace CafeMenu.Api.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CafeId { get; set; }
    public Cafe? Cafe { get; set; }
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();

    public int? ParentCategoryId { get; set; }
    public ParentCategory? ParentCategory { get; set; }
}