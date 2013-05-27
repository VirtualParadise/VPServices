using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VP;
using SQLite;

namespace VPServices.Services
{
    public partial class Todo : IService
    {
        public string Name
        { 
            get { return "Ideas"; }
        }

        public void Init(VPServices app, Instance bot)
        {
            app.Commands.AddRange(new[] {
                new Command
                (
                    "Todo: Add", "^(addtodo|atd|todoadd)$", cmdAddTodo,
                    @"Adds an attributed and timestamped todo entry",
                    @"!todoadd `entry`"
                ),

                new Command
                (
                    "Todo: Finish", "^(tododone|finishtodo)$", cmdFinishTodo,
                    @"Marks a todo entry as finished",
                    @"!tododone `id`"
                ),

                new Command
                (
                    "Todo: List", "^(listtodos?|ltd|todolist)$", cmdListTodo,
                    @"Prints the URL to the todo list to chat or lists those matching a search term to you",
                    @"!todolist `[search]`"
                ),

                new Command
                (
                    "Todo: Get random", "^todo$", cmdGetTodo,
                    @"Spins the todo wheel and gives you a random unfinished todo",
                    "!todo"
                ),
            });

            app.Routes.Add(new WebRoute("Todo", "^(list)?todos?$", webListTodos,
                @"Provides a list of todo entries"));

            this.connection = app.Connection;
        }

        public void Dispose() { }

        #region Privates and strings
        const string msgAdded       = "Added todo entry";
        const string msgNonExistant = "A todo entry with that ID does not exist";
        const string msgNoUndone    = "All todo entries are marked as done";
        const string msgResults     = "*** Search results for '{0}'";
        const string msgResultA     = "[{0}] #{1} - {2}";
        const string msgResultB     = "  by {0} on {1}";
        const string msgRandom      = "#{0} by {1} on {2}:";
        const string msgNoResults   = "No results; check {0}";
        const string webTodo        = "todo";

        SQLiteConnection connection; 
        #endregion

        #region Command handlers
        bool cmdAddTodo(VPServices app, Avatar who, string data)
        {
            connection.Insert( new sqlTodo
            {
                What  = data,
                When  = DateTime.Now,
                Who   = who.Name,
                WhoID = who.Id,
                Done  = false
            });

            app.Notify(who.Session, msgAdded);
            return Log.Info(Name, "Saved a todo for {0}: {1}", who.Name, data);
        }

        bool cmdFinishTodo(VPServices app, Avatar who, string data)
        {
            int id;
            if ( !int.TryParse(data, out id) )
                return false;

            var affected = connection.Execute("UPDATE Todo SET Done = ? WHERE ID = ?", true, id);

            if (affected <= 0)
            {
                app.Warn(who.Session, msgNonExistant);
                return true;
            }
            
            app.Notify(who.Session, "Todo marked as done");
            return Log.Info(Name, "Marked todo #{0} as done for {1}", id, who.Name);
        }

        bool cmdListTodo(VPServices app, Avatar who, string data)
        {
            var todoUrl = app.PublicUrl + webTodo;

            // No search; list URL only
            if ( data == "" )
            {
                app.Notify(who.Session, todoUrl);
                return true;
            }

            var query = from    t in connection.Table<sqlTodo>()
                        where   t.What.Contains(data)
                        orderby t.Done ascending
                        orderby t.ID descending
                        select  t;

            // No results
            if ( query.Count() <= 0 )
            {
                app.Warn(who.Session, msgNoResults, todoUrl);
                return true;
            }

            // Iterate results
            app.Bot.ConsoleMessage(who.Session, ChatEffect.BoldItalic, VPServices.ColorInfo, "", msgResults, data);
            foreach ( var q in query )
            {
                var done  = q.Done ? '✓' : '✗';
                var color = q.Done ? VPServices.ColorLesser : VPServices.ColorInfo;
                app.Bot.ConsoleMessage(who.Session, "", "");
                app.Bot.ConsoleMessage(who.Session, ChatEffect.Italic, color, "", msgResultA, done, q.ID, q.What);
                app.Bot.ConsoleMessage(who.Session, ChatEffect.Italic, color, "", msgResultB, q.Who, q.When);
            }

            return true;
        }

        bool cmdGetTodo(VPServices app, Avatar who, string data)
        {
            var random = connection.Query<sqlTodo>("SELECT * FROM Todo WHERE Done = ? ORDER BY RANDOM() LIMIT 1;", false).FirstOrDefault();

            if (random == null)
                app.Warn(who.Session, msgNoUndone);
            else
            {
                app.Notify(who.Session, msgRandom, random.ID, random.Who, random.When);
                app.Notify(who.Session, "{0}", random.What);
            }

            return true;
        }
        #endregion

        #region Web routes
        string webListTodos(VPServices app, string data)
        {
            var listing = "# Todo entries:\n";
            var list    = connection.Query<sqlTodo>("SELECT * FROM Todo ORDER BY Done ASC, ID DESC;");

            foreach ( var todo in list )
            {
                var done = todo.Done ? "&#10003;" : "&#10007;";

                listing +=
@"## [{0}] #{1} - {2}
* **By:** {3} (#{4})
* **When:** {5}

".LFormat(done, todo.ID, todo.What, todo.Who, todo.WhoID, todo.When);
            }

            return app.MarkdownParser.Transform(listing);
        } 
        #endregion
    }

    [Table("Todo")]
    class sqlTodo
    {
        [PrimaryKey, AutoIncrement]
        public int      ID    { get; set; }
        public int      WhoID { get; set; }
        public string   Who   { get; set; }
        public string   What  { get; set; }
        public DateTime When  { get; set; }
        public bool     Done  { get; set; }
    }
}
