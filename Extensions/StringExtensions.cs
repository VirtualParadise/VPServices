using System;
using System.Linq;

namespace VPServices.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Shortcut to string.Split(char[]) that trims all entries and removes any that
        /// are whitespace or empty
        /// </summary>
        public static string[] TerseSplit(this string str, params char[] separators)
        {
            return str.Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(entry => entry.Trim())
                .Where(entry => !string.IsNullOrWhiteSpace(entry))
                .ToArray();
        }

        /// <summary>
        /// Shortcut to string.Split(string[]) that trims all entries and removes any that
        /// are whitespace or empty
        /// </summary>
        public static string[] TerseSplit(this string str, params string[] separators)
        {
            return str.Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(entry => entry.Trim())
                .Where(entry => !string.IsNullOrWhiteSpace(entry))
                .ToArray();
        }
    }
}
