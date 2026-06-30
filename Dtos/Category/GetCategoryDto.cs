using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CafeMenu.Api.Dtos.Category
{
    public class GetCategoryDto
    {
        public int Id {get; set;}
        public string Name { get; set; } = string.Empty;
        public string CafeName {get; set;} = string.Empty;
    }
}