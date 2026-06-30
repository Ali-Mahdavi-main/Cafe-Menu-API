using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CafeMenu.Api.Models
{
    public class Cafe
    {
        public int Id {get; set;}
        public string Name {get; set;} = string.Empty;
        public string Address {get; set;} = string.Empty;
        public string LogoUrl {get; set;} = string.Empty;
        public string InstagramUrl {get; set;} = string.Empty; 
        public string ThemeJson {get; set;} = string.Empty; // For Dynamic Themes

        //Security Fields
        public string UserName {get; set;} = string.Empty;
        public string PasswordHash {get; set;} = string.Empty; // Password Saves As Hash Code

        //Connection Field
        public ICollection<Category> Categories {get; set;} = new List<Category>(); 
        public ICollection<MenuItem> MenuItems {get; set;} = new List<MenuItem>();
    }
}