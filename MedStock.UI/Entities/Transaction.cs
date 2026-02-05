using System;
using System.Collections.Generic;

namespace MedStock.UI.Entities;

public partial class Transaction
{
    public long TransactionId { get; set; }

    public string TransactionNo { get; set; } = null!;

    public string TransactionType { get; set; } = null!;

    public DateTime TransactionDate { get; set; }

    public string? Notes { get; set; }

    public int? DepartmentId { get; set; }

    public int? CreatedByUserId { get; set; }

    public int? ReasonId { get; set; }

    public int? SupplierId { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual Department? Department { get; set; }

    public virtual TransactionReason? Reason { get; set; }

    public virtual Supplier? Supplier { get; set; }

    public virtual ICollection<TransactionDetail> TransactionDetails { get; set; } = new List<TransactionDetail>();
}
