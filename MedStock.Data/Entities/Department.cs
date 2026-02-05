using System;
using System.Collections.Generic;

namespace MedStock.Data.Entities;

public partial class Department
{
    public int DepartmentId { get; set; }

    public string? DepartmentCode { get; set; }

    public string DepartmentName { get; set; } = null!;

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Requisition> Requisitions { get; set; } = new List<Requisition>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
