using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedStock.Services.DTOs
{
    public class ExpiryReportRow
    {
        public string ItemName { get; init; } = "";
        public string BatchNo { get; init; } = "";
        public System.DateTime? ExpiryDate { get; init; }
        public decimal Qty { get; init; }

        // حساب الأيام المتبقية ديناميكياً
        public int DaysRemaining => ExpiryDate.HasValue
            ? (ExpiryDate.Value - System.DateTime.Today).Days
            : 9999;

        public string Status => DaysRemaining < 0 ? "منتهية الصلاحية" :
                                DaysRemaining <= 30 ? "حرجة جداً" :
                                DaysRemaining <= 90 ? "قريبة الانتهاء" : "سارية";
    }

}