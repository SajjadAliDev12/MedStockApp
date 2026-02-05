using System;
using System.Collections.Generic;

namespace MedStock.Data.Entities;

public partial class TransactionReason
{
    public int ReasonId { get; set; }

    public string ReasonCode { get; set; } = null!;

    public string ReasonName { get; set; } = null!;

    public string Scope { get; set; } = null!;

    public bool IsSystem { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<StocktakeDetail> StocktakeDetails { get; set; } = new List<StocktakeDetail>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
