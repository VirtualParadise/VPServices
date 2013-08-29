using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VP;

namespace VPServices.Services
{
    public partial class Facts : IService
    {
        public string Name
        { 
            get { return "Facts"; }
        }

        public void Load(VPServices app, Instance bot)
        {
            app.Commands.AddRange(new[] {
                new Command
                (
                    "Facts: Add", "^(addfact|af|define)$", cmdAddFact,
                    @"Adds or overwrites a factoid for a topic, allowing it to be locked or alias another topic",
                    @"!addfact [--lock] `topic: [what|@alias]`"
                ),

                new Command
                (
                    "Facts: Delete", "^(del(ete)?fact|df)$", cmdDeleteFact,
                    @"Clears a factoid for a topic",
                    @"!delfact `topic`"
                ),

                new Command
                (
                    "Facts: Get", "^(what(is)?|explain|fact)$", cmdGetFact,
                    @"Explains a given topic",
                    "!what `topic`"
                ),

                new Command
                (
                    "Facts: List", "^(listfacts?|lf|facts?list)$", cmdFactList,
                    @"Searches list of facts for topics matching search term",
                    @"!lf `search term`"
                ),
            });

            this.connection = app.Connection;
        }

        public void Dispose() { }

        #region Privates and strings
        const string msgFact        = "{0} is {1}";
        const string msgAdded       = "Added factoid for {1}topic '{0}'";
        const string msgOverwritten = "Overwritten previous factoid for {1}topic '{0}'";
        const string msgDeleted     = "Factoid deleted";
        const string msgResults     = "*** Search results for '{0}'";
        const string msgResult      = "{0}{1} : {2}";
        const string msgResult2     = "➜ defined on {0}";
        const string errNonExistant = "No factoid for that topic was found";
        const string errBrokenAlias = "Could not resolve alias '@{0}' from topic '{1}'";
        const string errLocked      = "Topic locked by user ID {0}; can only be modified or deleted by them or the bot's owner";
        const string errNotFound    = "Could not match any facts for '{0}'";

        SQLiteConnection connection; 
        #endregion

        #region Command handlers
        bool cmdAddFact(VPServices app, Avatar who, string data)
        {
            var matches = Regex.Match(data, "^(-+lock )?(.+?): (.+)$");
            if ( !matches.Success )
                return false;

            var parts  = matches.ToArray();
            var locked = parts[1] != "";
            var topic  = parts[2].Trim();
            var what   = parts[3].Trim();
            var old    = getFact(topic);
            var msg    = old == null ? msgAdded : msgOverwritten;

            // Only allow overwrite of locked previous factoid if owner or bot owner
            if ( old != null && old.Locked && !who.Name.IEquals(app.Owner) )
            if (old.WhoID != who.Id)
            {
                app.Warn(who.Session, errLocked, old.WhoID);
                return true;
            }
          
            lock (app.DataMutex)
            {
                connection.Execute("DELETE FROM Facts WHERE Topic = ? COLLATE NOCASE", topic);
                connection.Insert( new sqlFact
                {
                    Topic       = topic,
                    Description = what,
                    When        = DateTime.Now,
                    WhoID       = who.Id,
                    Locked      = locked
                });
            }

            app.Notify(who.Session, msg, topic, locked ? "locked " : "");
            return Log.Info(Name, "Saved a fact from {0} for topic {1} (locked: {2})", who.Name, topic, locked);
        }

        bool cmdDeleteFact(VPServices app, Avatar who, string data)
        {
            var fact = getFact(data);

            if (fact == null)
            {
                app.Warn(who.Session, errNonExistant);
                return true;
            }

            // Only allow deletion of locked factoid if owner or bot owner
            if ( fact.Locked && !who.Name.IEquals(app.Owner) )
            if (fact.WhoID != who.Id)
            {
                app.Warn(who.Session, errLocked, fact.WhoID);
                return true;
            }

            lock (app.DataMutex)
                connection.Execute("DELETE FROM Facts WHERE Topic = ? COLLATE NOCASE", data);

            app.Notify(who.Session, msgDeleted);
            return Log.Info(Name, "{0} deleted factoid for topic {1}", who.Name, data);
        }

        bool cmdGetFact(VPServices app, Avatar who, string data)
        {
            var fact = getFact(data);

            // Undefined topics
            if (fact == null)
            {
                app.Warn(who.Session, errNonExistant);
                return true;
            }
            
            // Alias topics
            if ( fact.Description.StartsWith("@") )
            {
                var aliasTopic = fact.Description.Substring(1);
                var alias      = getFact(aliasTopic);
                
                if (alias == null)
                {
                    app.Warn(who.Session, errBrokenAlias, aliasTopic, data);
                    return true;
                }

                app.NotifyAll(msgFact, alias.Topic, alias.Description);
                return true;
            }

            app.NotifyAll(msgFact, fact.Topic, fact.Description);
            return true;
        }

        bool cmdFactList(VPServices app, Avatar who, string data)
        {
            if (data == "")
                return false;

            lock (app.DataMutex)
            {
                var query = from   f in connection.Table<sqlFact>()
                            where  f.Topic.Contains(data)
                            select f;

                if (query.Count() == 0)
                    app.Warn(who.Session, errNotFound, data);
                else
                {
                    app.Bot.ConsoleMessage(who.Session, ChatEffect.BoldItalic, VPServices.ColorInfo, "", msgResults, data);

                    foreach ( var q in query )
                    {
                        var locked = q.Locked ? " (locked)" : "";

                        app.Bot.ConsoleMessage(who.Session, ChatEffect.Italic, VPServices.ColorInfo, "", msgResult , q.Topic, locked, q.Description);
                        app.Bot.ConsoleMessage(who.Session, ChatEffect.Italic, VPServices.ColorInfo, "", msgResult2, q.When);
                    }
                }
            }

            return true;
        }
        #endregion

        #region Facts logic
        sqlFact getFact(string topic)
        {
            lock (VPServices.App.DataMutex)
                return connection.Query<sqlFact>("SELECT * FROM Facts WHERE Topic = ? COLLATE NOCASE", topic).FirstOrDefault();
        }
        #endregion
    }

    [Table("Facts")]
    class sqlFact
    {
        [Indexed]
        public string   Topic       { get; set; }
        [MaxLength(255)]
        public string   Description { get; set; }
        public int      WhoID       { get; set; }
        public DateTime When        { get; set; }
        public bool     Locked      { get; set; }
    }
}
