using System;
using System.Collections.Generic;

namespace MedStock.UI.Entities;

public partial class Batch
{
    public long BatchId { get; set; }

    public int ItemId { get; set; }

    public string BatchCode { get; set; } = null!;

    public DateOnly ReceivedDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public decimal InitialQty { get; set; }

    public decimal CurrentQty { get; set; }

    public decimal UnitCost { get; set; }

    public string? LocationCode { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual ICollection<TransactionDetail> TransactionDetails { get; set; } = new List<TransactionDetail>();
}
