using System;
using System.Collections.Generic;

namespace MedStock.UI.Entities;

public partial class TransactionDetail
{
    public long TransactionDetailId { get; set; }

    public long TransactionId { get; set; }

    public long BatchId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public int? StocktakeDetailId { get; set; }

    public string? SupplierRef { get; set; }

    public virtual Batch Batch { get; set; } = null!;

    public virtual ICollection<RequisitionFulfillmentLink> RequisitionFulfillmentLinks { get; set; } = new List<RequisitionFulfillmentLink>();

    public virtual Transaction Transaction { get; set; } = null!;
}
