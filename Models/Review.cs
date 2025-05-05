using System;
using System.Collections.Generic;

namespace API_ASP.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int? ProductId { get; set; }

    public int? UserId { get; set; }

    public int Rating { get; set; }

    public string ReviewText { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Catalog? Product { get; set; }

    public virtual User? User { get; set; }
}
