using CsvHelper;
using CsvHelper.Configuration;
using Nini.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VP;

namespace VPServ.Services
{
    public class Trivia : IService
    {
        const  string keyTriviaPoints = "TriviaPoints";
        const  string tag             = "Trivia";

        Dictionary<string, List<string>> history = new Dictionary<string, List<string>>();
        
        object        mutex         = new object();
        bool          inProgress    = false;
        Task          task;
        TriviaEntry[] entries;
        TriviaEntry   currentEntry;
        DateTime      progressSince;
        VPServ        app;

        public string Name { get { return "Trivia"; } }
        public void Init(VPServ app, Instance bot)
        {
            this.app = app;
            app.Commands.Add(new Command("Trivia",          "^trivia$",     cmdBeginTrivia,  @"Outputs a trivia question with optional category filter: `!trivia *category*`", 1));
            app.Commands.Add(new Command("(Re)load trivia", "^loadtrivia$", cmdReloadTrivia, @"Loads or reloads the trivia database"));
        
            app.Commands.Add(new Command("Trivia scores", "^scores?$", (s,a,d) => { s.Bot.Say(s.PublicUrl + "scores"); },
                @"Prints the URL to a listing of trivia scores", 60));

            app.Routes.Add(new WebRoute("Scores", "^(trivia)scores?$", webListScores,
                @"Provides a rundown of user scores from trivia"));
        }

        public void Dispose() {
            lock ( mutex ) { endTrivia(); }
        }

        void cmdReloadTrivia(VPServ serv, Avatar who, string data)
        {
            entries = null;
            loadTrivia();
        }

        void cmdBeginTrivia(VPServ serv, Avatar who, string data)
        {
            if ( entries == null ) loadTrivia();

            // Skip question
            if ( inProgress )
            {
                if (task != null) {
                    endTrivia();
                    task.Wait();
                }
            }

        getEntry:
            var search = data.ToLower().Trim();
            var query  = (from e in entries
                          where e.Category.ToLower().Contains(search)
                          select e).ToArray();

            int         skip;
            TriviaEntry entry;

            try
            {
                skip  = VPServ.Rand.Next(query.Length);
                entry = query.Skip(skip).Take(1).First();
            } 
            catch (Exception e)
            {
                serv.Bot.Say("I was unable to accept that filter, {0}", who.Name);
                return;
            }

            inProgress = true;

            if ( query.Length < 1 && entry.Equals(currentEntry) )
                goto getEntry;
            else
                currentEntry = entry;

            serv.Bot.Say("{0}: {1}", entry.Category, entry.Question);
            serv.Bot.Chat += onChat;
            progressSince  = DateTime.Now;

            task = new Task(taskTimeout);
            task.Start();
        }

        void taskTimeout()
        {
            while ( inProgress )
            {
                if ( progressSince.SecondsToNow() >= 60 )
                    lock ( mutex )
                    {
                        endTrivia();
                        app.Bot.Say("Timeout! The answer was {0}.", currentEntry.CanonicalAnswer);
                        Log.Debug(tag, "Trivia question '{0}' timed out", currentEntry.Question);
                    }

                Thread.Sleep(500);
            }
        }

        void onChat(Instance bot, Chat chat)
        {
            lock ( mutex )
            {
                string[] match;
                string[] wrongMatch;

                if ( TBXRegex.TryMatch(chat.Message, currentEntry.Answer, out match) )
                {
                    if ( currentEntry.Wrong != null && TBXRegex.TryMatch(chat.Message, currentEntry.Wrong, out wrongMatch) )
                    {
                        Log.Debug(tag, "Given answer '{0}' by {1} matched, but turned out to be wrong; rejecting", wrongMatch[0], chat.Name);
                        return;
                    }

                    endTrivia();

                    var welldones = new[]
                    {
                        "well done", "good show", "GG", "nice one", "not bad,", "jolly good", "keep going"
                    };

                    var welldone = welldones.Skip(VPServ.Rand.Next(welldones.Length)).Take(1).Single();

                    if ( match[0].Equals(currentEntry.CanonicalAnswer, StringComparison.CurrentCultureIgnoreCase) )
                        bot.Say("Ding! The answer was {0}, {1} {2}", currentEntry.CanonicalAnswer, welldone ,chat.Name);
                    else
                        bot.Say("Ding! The answer was {0} (accepted from {1}), {2} {3}", currentEntry.CanonicalAnswer, match[0], welldone, chat.Name);

                    Log.Debug(tag, "Correct answer '{0}' by {1}", match[0], chat.Name);
                    awardPoint(chat.Name);
                }
            }
        }

        void awardPoint(string who)
        {
            var config = app.GetUserSettings(who);
            var points = config.GetInt(keyTriviaPoints, 0);
            
            points++;
            config.Set(keyTriviaPoints, points);
            Log.Fine(tag, "{0} is now up to {1} points", who, points);
        }

        void loadTrivia()
        {
            var config   = app.Settings.Configs["Trivia"] ?? app.Settings.Configs.Add("Trivia");
            var fileName = config.Get("Database", "TriviaQuickfire.csv");

            if (!File.Exists(fileName))
            {
                app.Bot.Say("Sorry, I was unable to start trivia as my database is missing");
                Log.Warn(tag, "Could not begin; set database '{0}' is missing", fileName);
                return;
            }

            var file        = new StreamReader(fileName);
            var reader      = new CsvReader(file);
            var fileEntries = reader.GetRecords<TriviaEntry>();
            var list        = new List<TriviaEntry>();

            foreach ( var entry in fileEntries )
            {
                if ( entry.Question.Trim() == "" || entry.Answer.Trim() == "" )
                    continue;

                if ( entry.Wrong.Trim() == "" )
                    entry.Wrong = null;

                list.Add(entry);
            }

            entries = list.ToArray();
            reader.Dispose();
            file  .Dispose();
            Log.Debug(tag, "Loaded trivia database '{0}', {1} entries", fileName, entries.Length);
        }

        void endTrivia()
        {
            if ( !inProgress ) return;

            inProgress    = false;
            app.Bot.Chat -= onChat;
            Log.Fine(tag, "Trivia has ended");
        }

        string webListScores(VPServ serv, string data)
        {
            string listing = "# Trivia scores:\n";
            var    scores  = from IConfig c in serv.UserSettings.Configs
                             where c.Contains(keyTriviaPoints) && c.GetInt(keyTriviaPoints) > 0
                             orderby c.GetInt(keyTriviaPoints) descending
                             select new Tuple<int, string>(c.GetInt(keyTriviaPoints), c.Name);

            foreach (var score in scores)
            {
                listing += string.Format(
@"* **{0}** : {1} point(s)

", score.Item2, score.Item1);
            }

            return serv.MarkdownParser.Transform(listing);
        }
    }

    class TriviaEntry
    {
        [CsvField(Name = "Category")]
        public string Category { get; set; }
        
        [CsvField(Name = "Question")]
        public string Question { get; set; }
        
        [CsvField(Name = "Answer")]
        public string Answer   { get; set; }

        [CsvField(Name = "Wrong")]
        public string Wrong    { get; set; }

        string canonical;
        [CsvField(Name = "Canonical answer")]
        public string CanonicalAnswer
        {
            get { return canonical ?? Answer; }
            set
            {
                if ( value.Trim() == "" )
                    canonical = null;
                else
                    canonical = value;
            }
        }
    }
}
