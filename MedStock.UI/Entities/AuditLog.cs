using System;
using System.Collections.Generic;

namespace MedStock.UI.Entities;

public partial class AuditLog
{
    public long AuditLogId { get; set; }

    public DateTime OccurredAt { get; set; }

    public int? UserId { get; set; }

    public string EntityName { get; set; } = null!;

    public string? EntityId { get; set; }

    public string ActionType { get; set; } = null!;

    public string Summary { get; set; } = null!;

    public string? DetailsJson { get; set; }

    public string? IpAddress { get; set; }

    public string? MachineName { get; set; }

    public virtual User? User { get; set; }
}
