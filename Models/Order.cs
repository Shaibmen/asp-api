using System;
using System.Collections.Generic;

namespace API_ASP.Models;

public partial class Order
{
    public int OrdersId { get; set; }

    public int? UsersId { get; set; }

    public int? CatalogsId { get; set; }

    public decimal TotalSum { get; set; }

    public virtual Catalog? Catalogs { get; set; }

    public virtual ICollection<PosOrder>? PosOrders { get; set; } = new List<PosOrder>();

    public virtual User? Users { get; set; }
}
