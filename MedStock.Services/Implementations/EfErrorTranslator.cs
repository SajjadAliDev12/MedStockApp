using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace MedStock.Services.Implementations
{
    public static class EfErrorTranslator
    {
        public static string ToUserMessage(Exception ex)
        {
            if (ex is DbUpdateException dbu)
            {
                // SQL Server errors usually end up here
                if (dbu.InnerException is DbException dbEx)
                    return dbEx.Message;

                if (dbu.InnerException != null)
                    return dbu.InnerException.Message;

                return dbu.Message;
            }

            return ex.Message;
        }
    }
}
