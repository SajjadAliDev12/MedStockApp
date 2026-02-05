namespace MedStock.Services.Interfaces
{
    public interface ILanguageService
    {
        string DepartmentLabel { get; } // "القسم" أو "الزبون/الصيدلية"
        string RequisitionLabel { get; } // "طلب داخلي" أو "طلب بيع/تجهيز"
        string ActivityName { get; }
    }
}