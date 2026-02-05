using System;
using System.Collections.Generic;

namespace MedStock.Services.Implementations
{
    internal static class Guard
    {
        public static void Positive(int value, string name)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(name, "Must be > 0.");
        }

        public static void Positive(long value, string name)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(name, "Must be > 0.");
        }

        public static void PositiveDecimal(decimal value, string name)
        {
            if (value <= 0m) throw new ArgumentOutOfRangeException(name, "Must be > 0.");
        }

        public static void NotNullOrWhiteSpace(string? value, string name, int maxLen = 200)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Required.", name);
            if (value.Length > maxLen) throw new ArgumentException($"Too long (>{maxLen}).", name);
        }

        public static void NotNull<T>(T? obj, string name) where T : class
        {
            if (obj is null) throw new ArgumentNullException(name);
        }

        public static void NotEmpty<T>(ICollection<T> col, string name)
        {
            if (col == null || col.Count == 0) throw new ArgumentException("At least one item is required.", name);
        }
    }
}
