using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VPServices.Services;

namespace VPServices
{
    /// <summary>
    /// TODO: Get rid of this and use SQLite
    /// </summary>
    public static class VPServExtensions
    {
        const string commaToken = "%COMMA%";

        public static string[] SplitCSV(this string csv, bool terse)
        {
            string[] arr;

            arr = terse ? csv.TerseSplit(',') : csv.Split(',');
            for (var i = 0; i < arr.Length; i++)
                arr[i] = arr[i].Replace(commaToken, ",");

            return arr;
        }

        public static string PartsToCSV(params object[] parts)
        {
            for (var i = 0; i < parts.Length; i++)
                parts[i] = parts[i]
                    .ToString()
                    .Replace(",", commaToken);

            return string.Join(",", parts);
        }
    }
}
