using Microsoft.EntityFrameworkCore;

namespace MedStock.Data.Context
{
    // Keep this in a separate file to survive re-scaffolding
    public partial class HospitalInventoryDbContext
    {
        // Optional: set global behaviors safely
        public void ConfigureDefaults()
        {
            // Example: disable lazy loading if enabled (usually not scaffolded anyway)
            ChangeTracker.LazyLoadingEnabled = false;
        }
    }
}
