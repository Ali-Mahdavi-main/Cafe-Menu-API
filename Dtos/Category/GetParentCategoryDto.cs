namespace CafeMenu.Api.Dtos.Category;

public class GetParentCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int CategoryCount { get; set; }
}
