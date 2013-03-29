using Nini.Config;
using System;
using System.Linq;

namespace VPServ.Services
{
    public partial class Trivia : IService
    {
        void addWebRoutes()
        {
            app.Routes.Add(new WebRoute("Scores", "^(trivia)scores?$", webListScores,
                @"Provides a rundown of user scores from trivia"));
        }

        /// <summary>
        /// Web route that lists scores of trivia players
        /// </summary>
        string webListScores(VPServ app, string data)
        {
            string listing = "# Trivia scores:\n";
            var    scores  = from IConfig c in app.UserSettings.Configs
                             where   c.Contains(keyTriviaPoints) && c.GetInt(keyTriviaPoints) > 0
                             orderby c.GetInt(keyTriviaPoints) descending
                             select new Tuple<int, string> ( c.GetInt(keyTriviaPoints), c.Name );

            foreach (var score in scores)
                listing += "* **{0}** : {1} point(s)\n\n".LFormat(score.Item2, score.Item1);

            return app.MarkdownParser.Transform(listing);
        }
    }
}
