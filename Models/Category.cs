namespace CafeMenu.Api.Models;
//TODO Fix the cycling problem
public class Category
{
    public int Id {set; get;}
    public string Name {get; set;} = string.Empty;
    public int CafeId {get; set;}
    public Cafe Cafe {get; set;} = new();
    public ICollection<MenuItem> MenuItems {get; set;} = new List<MenuItem>();
}