using Microsoft.Extensions.Configuration;
using MedStock.Services.Interfaces;

namespace MedStock.Services.Implementations
{
    public sealed class LanguageService : ILanguageService
    {
        private readonly string _activityType;

        public LanguageService(IConfiguration config)
        {
            // قراءة نوع النشاط من appsettings.json
            _activityType = config["AppSettings:ActivityType"] ?? "Hospital";
        }

        public string DepartmentLabel => _activityType == "Hospital" ? "القسم الطالب" : "الزبون / الصيدلية";

        public string RequisitionLabel => _activityType == "Hospital" ? "طلب صرف داخلي" : "طلب تجهيز مبيعات";

        public string ActivityName => _activityType == "Hospital" ? "نظام إدارة المستشفيات" : "نظام إدارة المذاخر";
    }
}