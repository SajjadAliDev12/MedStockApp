namespace MedStock.Services.DTOs // أو MedStock.Core
{
    public static class TransactionReasons
    {
        // --- حركات التوريد (Stock In) ---
        public const string Purchase = "PURCHASE";       // شراء
        public const string Donation = "DONATION";       // منحة/تبرع
        public const string ReturnIn = "RETURN_IN";      // مرتجع من قسم
        public const string AdjIn = "ADJ_IN";            // تسوية زيادة
        public const string Initial = "INITIAL";         // رصيد افتتاحي

        // --- حركات الصرف (Stock Out) ---
        public const string Dispense = "DISPENSE";       // صرف لقسم
        public const string Expired = "EXPIRED";         // إتلاف منتهي
        public const string Damage = "DAMAGE";           // إتلاف تالف
        public const string AdjOut = "ADJ_OUT";          // تسوية نقص
        public const string ReqFulfill = "REQ_FULFILL";// تلبية طلب صرف (مهم جداً للربط)
        public const string STOCKTAKEOUT = "STOCKTAKE_OUT";//تسوية زيادة جرد
        public const string STOCKTAKEIN = "STOCKTAKE_IN"; //تسوية نقص جرد

    }
}