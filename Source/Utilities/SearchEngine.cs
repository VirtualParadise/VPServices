using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VP;

namespace VPServices
{
    class SearchEngine<T>
        where T : new()
    {
        public string   Query;
        public object[] Params;

        public Func<T, string[]> Formatter;

        public string RespNotFound    = "Could not match any results for '{0}'";
        public string RespResultsHead = "Search results for '{0}' ({1} total)";
        public string RespTooMany     = "Too many results (retry with different query?)";

        public string FragHeaderPre = "*** ";

        public int Limit = 50;

        public bool Execute(User who, string query, SQLiteConnection sql)
        {
            if ( string.IsNullOrWhiteSpace(query) )
                return false;
            else
                query = query.Trim();

            var results = sql.Query<T>(Query, Params);
            var bot     = who.World.Bot;
            var count   = results.Count;

            if (count == 0)
            {
                bot.ConsoleMessage(who.Session, ChatEffect.Italic, Colors.Warn, "", FragHeaderPre + RespNotFound, query);
                return true;
            }

            bot.ConsoleMessage(who.Session, ChatEffect.BoldItalic, Colors.Info, "", FragHeaderPre + RespResultsHead, query, count);

            foreach ( var result in results.Take(Limit) )
                foreach ( var line in Formatter(result) )
                    bot.ConsoleMessage(who.Session, ChatEffect.Italic, Colors.Info, "", "{0}", line);

            if (count > Limit)
                bot.ConsoleMessage(who.Session, ChatEffect.BoldItalic, Colors.Info, "", FragHeaderPre + RespTooMany);

            return true;
        }
    }
}
