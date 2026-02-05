namespace MedStock.Services.DTOs
{
    public class TransactionReasonDto
    {
        public int ReasonId { get; set; }
        public string ReasonName { get; set; } // الاسم العربي الذي يظهر للمستخدم
        public string ReasonCode { get; set; } // الكود الذي يرسل للسيرفر
    }
}