using System;
using System.Linq;

namespace VPServices.Services
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
        string webListScores(VPServices app, string data)
        {
            var listing = "# Trivia scores:\n";
            var query   = from   s in app.Connection.Table<sqlUserSettings>()
                          where  s.Name == keyTriviaPoints
                          select s;

            // XXX: ID only; investigate getting names
            foreach (var score in query)
                listing += "* **{0}** : {1} point(s)\n\n".LFormat(score.UserID, score.Value);

            return app.MarkdownParser.Transform(listing);
        }
    }
}
