using System.Collections.Generic;
using API_ASP.Models;

namespace ASPNETKEK.Models
{
    public class ProductDetailsViewModel
    {
        public Catalog Product { get; set; }
        public List<Review> Reviews { get; set; }
    }
}
