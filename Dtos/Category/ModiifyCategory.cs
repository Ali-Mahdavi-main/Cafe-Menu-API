namespace CafeMenu.Api.Dtos.Category;

public class ModiifyCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CafeName { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
}
