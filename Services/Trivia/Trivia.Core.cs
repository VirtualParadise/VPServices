using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VP;

namespace VPServ.Services
{
    public partial class Trivia : IService
    {
        #region Settings strings
        const string keyTriviaPoints = "TriviaPoints";
        const string tag             = "Trivia";
        #endregion
        
        object   mutex         = new object();
        bool     inProgress    = false;
        Task     task;
        DateTime progressSince;
        VPServ   app;

        public string Name { get { return "Trivia"; } }
        public void Init(VPServ app, Instance bot)
        {
            this.app = app;
            addCommands ();
            addWebRoutes();
        }

        public void Dispose()
        {
            lock ( mutex ) { gameEnd(); }
        }

        void skipQuestion()
        {
            if (task == null) return;
                
            Log.Debug(tag, "Skipping question...");
            gameEnd();
            task.Wait();
        }

        void gameBegin(TriviaEntry entry)
        {
            app.Bot.Say("{0}: {1}", entry.Category, entry.Question);
            app.Bot.Chat     += onChat;
            inProgress        = true;
            progressSince     = DateTime.Now;
            entryInPlay       = entry;
            entryInPlay.Used  = true;

            Log.Debug(tag, "Beginning game with question:\n\t[{0}] {1}\n\tAnswer: {2}",
                entryInPlay.Category, entryInPlay.Question, entryInPlay.Answer);

            task = new Task(gameTimeout);
            task.Start();
        }

        void gameTimeout()
        {
            while ( inProgress )
            {
                if ( progressSince.SecondsToNow() >= 60 )
                    lock ( mutex )
                    {
                        gameEnd();
                        app.Bot.Say("Timeout! The answer was {0}.", entryInPlay.CanonicalAnswer);
                        Log.Debug(tag, "Question timed out");
                    }

                Thread.Sleep(500);
            }
        }

        void gameEnd()
        {
            if ( !inProgress ) return;

            inProgress    = false;
            app.Bot.Chat -= onChat;
            Log.Fine(tag, "Game has ended");
        }

        void onChat(Instance bot, Chat chat)
        {
            lock ( mutex )
            {
                string[] match;
                string[] wrongMatch;

                if ( TRegex.TryMatch(chat.Message, entryInPlay.Answer, out match) )
                {
                    if ( entryInPlay.Wrong != null && TRegex.TryMatch(chat.Message, entryInPlay.Wrong, out wrongMatch) )
                    {
                        Log.Debug(tag, "Given answer '{0}' by {1} matched, but turned out to be wrong; rejecting", wrongMatch[0], chat.Name);
                        return;
                    }

                    gameEnd();

                    var welldones = new[]
                    {
                        "well done", "good show", "GG", "nice one", "not bad,", "jolly good", "keep going"
                    };

                    var welldone = welldones.Skip(VPServ.Rand.Next(welldones.Length)).Take(1).Single();

                    if ( match[0].IEquals(entryInPlay.CanonicalAnswer) )
                        bot.Say("Ding! The answer was {0}, {1} {2}", entryInPlay.CanonicalAnswer, welldone ,chat.Name);
                    else
                        bot.Say("Ding! The answer was {0} (accepted from {1}), {2} {3}", entryInPlay.CanonicalAnswer, match[0], welldone, chat.Name);

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
    }   
}
