using System;
using System.Collections.Generic;

namespace API_ASP.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public virtual ICollection<Catalog> Products { get; set; } = new List<Catalog>();
}
