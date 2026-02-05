using System;
using System.Collections.Generic;

namespace MedStock.UI.Entities;

public partial class Requisition
{
    public long RequisitionId { get; set; }

    public string RequisitionNo { get; set; } = null!;

    public int DepartmentId { get; set; }

    public string Status { get; set; } = null!;

    public int RequestedByUserId { get; set; }

    public int? ApprovedByUserId { get; set; }

    public DateTime RequestDate { get; set; }

    public DateTime? DecisionDate { get; set; }

    public string? Notes { get; set; }

    public virtual User? ApprovedByUser { get; set; }

    public virtual Department Department { get; set; } = null!;

    public virtual User RequestedByUser { get; set; } = null!;

    public virtual ICollection<RequisitionDetail> RequisitionDetails { get; set; } = new List<RequisitionDetail>();
}
