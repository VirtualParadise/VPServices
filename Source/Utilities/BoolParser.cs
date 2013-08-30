using System.Text.RegularExpressions;

namespace VPServices
{
    static class BoolParser
    {
        public static bool Try(string msg, out bool value)
        {
            if ( TRegex.IsMatch(msg, "^(true|1|yes|on)$") )
            {
                value = true;
                return true;
            }
            else if ( TRegex.IsMatch(msg, "^(false|0|no|off)$") )
            {
                value = false;
                return true;
            }
            else
            {
                value = false;
                return false;
            }
        }
    }
}
