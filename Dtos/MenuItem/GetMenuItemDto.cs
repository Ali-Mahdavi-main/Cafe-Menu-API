using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CafeMenu.Api.Dtos.MenuItem
{
    public class GetMenuItemDto
    {
    public int Id { get; set; } 
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public bool IsSpecial { get; set; } = false;

    public string CategoryName { get; set; } = string.Empty;
    }
}