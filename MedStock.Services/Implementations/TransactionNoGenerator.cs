using System;
using System.Security.Cryptography;

namespace MedStock.Services.Implementations
{
    internal static class TransactionNoGenerator
    {
        // Format example: TRX-20260201-153045-4821
        public static string NewTransactionNo()
        {
            var now = DateTime.Now;
            var rnd = RandomNumberGenerator.GetInt32(1000, 9999);
            return $"TRX-{now:yyyyMMdd-HHmmss}-{rnd}";
        }
    }
}
