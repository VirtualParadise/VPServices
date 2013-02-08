using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VP;
using CsvHelper;
using CsvHelper.TypeConversion;
using CsvHelper.Configuration;

namespace VPServ.Services
{
    public class Trivia : IService
    {
        const  string keyTriviaPoints = "TriviaPoints";
        const  string tag             = "Trivia";

        
        object        mutex         = new object();
        bool          inProgress    = false;
        TriviaEntry[] entries;
        TriviaEntry   currentEntry;
        DateTime      progressSince;
        VPServ        app;

        public string Name { get { return "Trivia"; } }
        public void Init(VPServ app, Instance bot)
        {
            this.app = app;
            app.Commands.Add(new Command("Trivia", "^trivia$", cmdBeginTrivia, @"Outputs a trivia question"));
        }

        public void Dispose() {
            lock ( mutex ) { endTrivia(); }
        }

        void cmdBeginTrivia(VPServ serv, Avatar who, string data)
        {
            if ( inProgress ) return;
            else              inProgress = true;

            if ( entries == null ) loadTrivia();

        getEntry:
            var entry  = entries[ VPServ.Rand.Next(entries.Length) ];

            if ( entry.Equals(currentEntry) )
                goto getEntry;
            else
                currentEntry = entry;

            serv.Bot.Say("{0}: {1}", entry.Category, entry.Question);
            serv.Bot.Chat += onChat;
            progressSince  = DateTime.Now;

            Task.Factory.StartNew(taskTimeout);
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

                    if ( match[0].Equals(currentEntry.CanonicalAnswer, StringComparison.CurrentCultureIgnoreCase) )
                        bot.Say("Ding! The answer was {0}, well done {1}", currentEntry.CanonicalAnswer, chat.Name);
                    else
                        bot.Say("Ding! The answer was {0} (accepted from {1}), well done {2}", currentEntry.CanonicalAnswer, match[0], chat.Name);

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

            if ( points % 10 == 0 )
                app.Bot.Say("{0} is climbing the scoreboard at {1} points", who, points);

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
