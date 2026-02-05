using System;
using System.Collections.Generic;

namespace MedStock.Data.Entities;

public partial class RequisitionDetail
{
    public long RequisitionDetailId { get; set; }

    public long RequisitionId { get; set; }

    public int ItemId { get; set; }

    public decimal RequestedQty { get; set; }

    public decimal FulfilledQty { get; set; }

    public string? Notes { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual Requisition Requisition { get; set; } = null!;

    public virtual ICollection<RequisitionFulfillmentLink> RequisitionFulfillmentLinks { get; set; } = new List<RequisitionFulfillmentLink>();
}
