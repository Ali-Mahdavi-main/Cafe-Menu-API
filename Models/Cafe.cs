using System;
using System.Collections.Generic;

namespace CafeMenu.Api.Models
{
    public class Cafe
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public string InstagramUrl { get; set; } = string.Empty;
        public string ThemeConfigJson { get; set; } = string.Empty;

        public string? Phone { get; set; }            
        public string? WorkingHours { get; set; }  
        public bool EventsEnabled { get; set; } = true;

        // Security
        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? PublicAccessKey { get; set; }

        // Navigation
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
        public ICollection<CafeEvent> CafeEvents { get; set; } = new List<CafeEvent>();
    }
}