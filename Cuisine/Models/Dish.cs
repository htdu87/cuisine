using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cuisine.Models
{
    public class Dish
    {
        public long Id { get; set; }
        public string Uri { get; set; }
        public string Name { get; set; }
        public string Desc { get; set; }
        public string Picture { get; set; }
        public bool Chay { get; set; }
    }
}