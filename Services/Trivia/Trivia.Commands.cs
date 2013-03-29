using VP;
using System;

namespace VPServ.Services
{
    public partial class Trivia : IService
    {
        void addCommands()
        {
            app.Commands.Add(new Command("Trivia",          "^trivia$",                     cmdBeginTrivia, 
                @"Outputs a trivia question with optional category filter: `!trivia *category*`"));

            app.Commands.Add(new Command("(Re)load trivia", "^(re)?loadtrivia$",            cmdReloadTrivia,
                @"Loads or reloads the trivia database"));

            app.Commands.Add(new Command("Trivia scores", "^(show|list)?(trivia)?scores?$", cmdShowUrl,
                @"Prints the URL to a listing of trivia scores", 60));
        }

        void cmdBeginTrivia(VPServ app, Avatar who, string data)
        {
            if ( entries == null )
            {
                Log.Debug(tag, "Loading trivia database for first time...");
                loadTrivia();
            }

            // Skip question
            if ( inProgress )
                skipQuestion();

            var entry = fetchEntry(data);

            if (entry == null)
            {
                app.Bot.Say("I was unable to fetch a trivia entry; perhaps try a different category?");
                return;
            }
            else
                gameBegin(entry);
        }

        void cmdReloadTrivia(VPServ app, Avatar who, string data)
        {
            entries = null;
            loadTrivia();
            app.Bot.Say("The trivia database has been reloaded, with {0} entries", entries.Length);
        }

        void cmdShowUrl(VPServ app, Avatar who, string data)
        {
            app.Bot.Say(app.PublicUrl + "scores");
        }
    }
}
