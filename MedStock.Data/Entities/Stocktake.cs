using System;
using System.Collections.Generic;

namespace MedStock.Data.Entities;

public partial class Stocktake
{
    public int StocktakeId { get; set; }

    public string StocktakeNo { get; set; } = null!;

    public DateTime StocktakeDate { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? PostedByUserId { get; set; }

    public DateTime? PostedAt { get; set; }

    public int? CancelledByUserId { get; set; }

    public DateTime? CancelledAt { get; set; }

    public virtual User? CancelledByUser { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual User? PostedByUser { get; set; }

    public virtual ICollection<StocktakeDetail> StocktakeDetails { get; set; } = new List<StocktakeDetail>();
}
