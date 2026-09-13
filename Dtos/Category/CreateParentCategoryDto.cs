using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.Dtos.Category;

public class CreateParentCategoryDto
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
}
