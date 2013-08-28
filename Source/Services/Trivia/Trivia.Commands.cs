using System;
using System.Collections.Generic;
using VP;

namespace VPServices.Services
{
    public partial class Trivia : IService
    {
        const string msgFirstLoad = "Loading trivia database for the first time...";
        const string msgSkipping  = "Skipping previous question...";
        const string msgNoResults = "I was unable to fetch a trivia entry; perhaps try a different category?";
        const string msgReloaded  = "The trivia database has been reloaded, with {0} entries";

        void addCommands()
        {
            app.Commands.AddRange(new[] {
                new Command
                (
                    "Trivia: Start", "^trivia$", cmdBeginTrivia,
                    @"Outputs a trivia question with optional category filter",
                    @"!trivia `[category]`", 3
                ),

                new Command
                (
                    "Trivia: Load", "^(re)?loadtrivia$", cmdReloadTrivia,
                    @"Loads or reloads the trivia database",
                    @"!loadtrivia", 10
                ),

                new Command
                (
                    "Trivia: Scores", "^(trivia)?scores$", cmdShowUrl,
                    @"Shows you the URL to a listing of trivia scores",
                    @"!scores"
                )
            });
        }

        bool cmdBeginTrivia(VPServices app, Avatar who, string data)
        {
            if ( entries == null )
            {
                app.Notify(who.Session, msgFirstLoad);
                Log.Debug(tag, msgFirstLoad);

                if ( !loadTrivia() )
                {
                    app.Bot.Say("Sorry, I was unable to start trivia as my database is missing");
                    return true;
                }
            }

            // Skip question
            if ( inProgress )
            {
                app.Notify(who.Session, msgSkipping);
                Log.Debug(tag, msgSkipping);
                skipQuestion();
            }

            var entry = fetchEntry(data);

            if (entry == null)
                app.Warn(who.Session, msgNoResults);
            else
                gameBegin(entry);

            return true;
        }

        bool cmdReloadTrivia(VPServices app, Avatar who, string data)
        {
            entries = null;
            if ( !loadTrivia() )
                app.Bot.Say("Sorry, I was unable to find my trivia database");
            else
                app.Notify(who.Session, msgReloaded, entries.Length);

            return true;
        }

        bool cmdShowUrl(VPServices app, Avatar who, string data)
        {
            app.Notify(who.Session, app.PublicUrl + "scores");

            return true;
        }
    }
}
