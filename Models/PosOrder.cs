using System;
using System.Collections.Generic;

namespace API_ASP.Models;

public partial class PosOrder
{
    public int PosOrderId { get; set; }

    public int? OrderId { get; set; }

    public int? ProductId { get; set; }

    public int Count { get; set; }

    public virtual Order? Order { get; set; }

    public virtual Catalog? Product { get; set; }
}
