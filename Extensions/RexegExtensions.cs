using System.Linq;
using System.Text.RegularExpressions;

namespace VPServices.Extensions
{
    public static class RexegExtensions
    {
        /// <summary>
        /// Shortcut to using Regex.Match in if conditionals
        /// </summary>
        /// <param name="regex">Regex object to match</param>
        /// <param name="input">String to try find matches in</param>
        /// <param name="matches">Outputs a list of matches, null if unsuccessful</param>
        /// <returns>True on success, false otherwise</returns>
        public static bool TryMatch(this Regex regex, string input, out string[] matches)
        {
            var match = regex.Match(input);

            matches = match.Success
                ? (from Group m in match.Groups select m.Value).ToArray()
                : null;

            return match.Success;
        }
    }
}
