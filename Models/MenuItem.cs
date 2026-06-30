using System.ComponentModel;
using System.Data.Common;

namespace CafeMenu.Api.Models;

public class MenuItem
{
    public int Id {get; set;}
    public string Title {get; set;} = string.Empty;
    public string Description {get; set;} = string.Empty;
    public decimal Price {get; set;}
    public string ImageUrl {get; set;} = string.Empty;
    public bool IsAvailable {get; set;} = true;

    //تفکیک کافه ها(Multi-tenancy)
    public int CafeId {get; set;}
    public Cafe Cafe {get; set;} = new();

    //Connection
    public int CategoryId {get; set;}
    public Category? Category {get; set;}

}