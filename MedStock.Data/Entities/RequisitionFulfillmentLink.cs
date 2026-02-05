using System;
using System.Collections.Generic;

namespace MedStock.Data.Entities;

public partial class RequisitionFulfillmentLink
{
    public long LinkId { get; set; }

    public long RequisitionDetailId { get; set; }

    public long TransactionDetailId { get; set; }

    public decimal FulfilledQty { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual RequisitionDetail RequisitionDetail { get; set; } = null!;

    public virtual TransactionDetail TransactionDetail { get; set; } = null!;
}
