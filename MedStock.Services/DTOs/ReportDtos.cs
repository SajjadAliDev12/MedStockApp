using System;

namespace MedStock.Services.DTOs
{
    // السطر في الجدول الرئيسي (الملخص)
    public class ConsumptionSummaryRow
    {
        public int ItemId { get; set; }
        public string Sku { get; set; } = "";
        public string ItemName { get; set; } = "";
        public string UnitName { get; set; } = "";

        // إجمالي الكمية المصروفة خلال الفترة
        public decimal TotalQty { get; set; }

        // عدد عمليات الصرف (مثلاً: صرفنا 10 مرات)
        public int TransactionCount { get; set; }
    }

    // تفاصيل الحركات (عند الضغط على مادة معينة)
    public class ConsumptionDetailRow
    {
        public long TransactionId { get; set; }
        public DateTime Date { get; set; }
        public string TransactionNo { get; set; } = "";
        public string DepartmentName { get; set; } = ""; // الجهة المستلمة
        public string Reason { get; set; } = ""; // سبب الصرف
        public decimal Qty { get; set; }
        public string Notes { get; set; } = "";
        public string UserName { get; set; } = ""; // الموظف الذي صرف
    }
}