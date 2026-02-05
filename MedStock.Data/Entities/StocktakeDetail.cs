using System;
using System.Collections.Generic;

namespace MedStock.Data.Entities;

public partial class StocktakeDetail
{
    public long StocktakeDetailId { get; set; }

    public int StocktakeId { get; set; }

    public int ItemId { get; set; }

    public decimal SystemQty { get; set; }

    public decimal? PhysicalQty { get; set; }

    public decimal? Difference { get; set; }

    public int? ReasonId { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual TransactionReason? Reason { get; set; }

    public virtual Stocktake Stocktake { get; set; } = null!;
}
