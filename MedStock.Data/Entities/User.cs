using System;
using System.Collections.Generic;

namespace MedStock.Data.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public byte[] PasswordHash { get; set; } = null!;

    public byte[] PasswordSalt { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<Requisition> RequisitionApprovedByUsers { get; set; } = new List<Requisition>();

    public virtual ICollection<Requisition> RequisitionRequestedByUsers { get; set; } = new List<Requisition>();

    public virtual ICollection<Stocktake> StocktakeCancelledByUsers { get; set; } = new List<Stocktake>();

    public virtual ICollection<Stocktake> StocktakeCreatedByUsers { get; set; } = new List<Stocktake>();

    public virtual ICollection<Stocktake> StocktakePostedByUsers { get; set; } = new List<Stocktake>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
