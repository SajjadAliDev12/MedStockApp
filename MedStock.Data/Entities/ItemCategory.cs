using System;
using System.Collections.Generic;

namespace MedStock.Data.Entities;

public partial class ItemCategory
{
    public int ItemId { get; set; }

    public int CategoryId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual Item Item { get; set; } = null!;
}
