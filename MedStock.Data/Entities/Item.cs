using System;
using System.Collections.Generic;

namespace MedStock.Data.Entities;

public partial class Item
{
    public int ItemId { get; set; }

    public string Sku { get; set; } = null!;

    public string ItemName { get; set; } = null!;

    public string? Description { get; set; }

    public string UnitOfMeasure { get; set; } = null!;

    public decimal ReorderLevel { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public decimal? MinStok { get; set; }

    public virtual ICollection<Batch> Batches { get; set; } = new List<Batch>();

    public virtual ICollection<ItemCategory> ItemCategories { get; set; } = new List<ItemCategory>();

    public virtual ICollection<RequisitionDetail> RequisitionDetails { get; set; } = new List<RequisitionDetail>();

    public virtual ICollection<StocktakeDetail> StocktakeDetails { get; set; } = new List<StocktakeDetail>();
}
