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

        public Command[] Commands
        {
            get { return new[] {
                new Command("Add", "^(addfact|af|define)$", cmdAddFact,
                    @"Adds or overwrites a factoid for a topic, allowing it to be locked or alias another topic",
                    @"!addfact [--lock] Topic: [What|@Alias]"),

                new Command("Delete", "^(del(ete)?fact|df)$", cmdDeleteFact,
                    @"Clears a factoid for a topic",
                    @"!delfact Topic"),

                new Command("Get", "^(what(is)?|explain|fact)$", cmdGetFact,
                    @"Explains a given topic",
                    "!what Topic [@ Target] (or: ?Topic [@ Target])"),

                new Command("List", "^(listfacts?|lf|facts?list)$", cmdFactList,
                    @"Searches list of facts for topics matching search term",
                    @"!lf Search Term"),
            }; }
        }

        public void Load()
        {
            VPServices.Messages.Incoming += onIncoming;

            sql = VPServices.Data.SQL;

            if ( sql.CreateTable<sqlFact>() != 0 )
                Log.Debug(Name, "Created SQLite table for facts");
        }

        public void Unload()
        {
            VPServices.Messages.Incoming -= onIncoming;
        }

        #region Privates and strings
        const string msgFact         = "{0} is {1}";
        const string msgFactTargeted = "{0}: {1}";
        const string msgAdded        = "Added factoid for {1}topic '{0}'";
        const string msgOverwritten  = "Overwritten previous factoid for {1}topic '{0}'";
        const string msgDeleted      = "Factoid deleted";
        const string msgResults      = "*** Search results for '{0}'";
        const string msgResult       = "{0}{1} : {2}";
        const string msgResult2      = "➜ defined on {0}";
        const string errNonExistant  = "No factoid for that topic was found";
        const string errBrokenAlias  = "Could not resolve alias '@{0}' from topic '{1}'";
        const string errLocked       = "Topic locked by user ID {0}; can only be modified or deleted by them, moderators or admins";
        const string errNotFound     = "Could not match any facts for '{0}'";

        SQLiteConnection sql; 
        #endregion

        #region Command handlers
        bool cmdAddFact(User who, string data)
        {
            if ( string.IsNullOrWhiteSpace(data) )
                return false;

            var match = Regex.Match(data, @"^(?<lock>-+lock )?(?<topic>.+?): *(?<data>.+)$");
            if (!match.Success)
                return false;

            var locked = match.Groups["lock"].Success;
            var topic  = match.Groups["topic"].Value.Trim();
            var what   = match.Groups["data"].Value.Trim();
            var old    = getFact(topic);
            var msg    = old == null ? msgAdded : msgOverwritten;

            if ( string.IsNullOrWhiteSpace(topic) || string.IsNullOrWhiteSpace(what) )
                return false;

            // Only allow overwrite of locked previous factoid if owner or moderator
            if (old != null && old.Locked)
            if ( !who.Rights.Contains(Rights.Admin) && !who.Rights.Contains(Rights.Moderator) )
            if (old.WhoID != who.Id)
            {
                who.Send.Warn(errLocked, old.WhoID);
                return true;
            }

            sql.Execute("DELETE FROM Facts WHERE Topic = ? COLLATE NOCASE", topic);
            sql.Insert( new sqlFact
            {
                Topic       = topic,
                Description = what,
                When        = DateTime.Now,
                WhoID       = who.Id,
                Locked      = locked
            });

            who.Send.Info(msg, topic, locked ? "locked " : "");
            Log.Info(Name, "Saved a fact from {0} for topic {1} (locked: {2})", who, topic, locked);
            return true;
        }

        bool cmdDeleteFact(User who, string data)
        {
            if ( string.IsNullOrWhiteSpace(data) )
                return false;

            var fact = getFact(data);

            if (fact == null)
            {
                who.Send.Warn(errNonExistant);
                return true;
            }

            // Only allow deletion of locked factoid if owner or bot owner
            if (fact.Locked)
            if ( !who.Rights.Contains(Rights.Admin) && !who.Rights.Contains(Rights.Moderator) )
            if (fact.WhoID != who.Id)
            {
                who.Send.Warn(errLocked, fact.WhoID);
                return true;
            }

            sql.Execute("DELETE FROM Facts WHERE Topic = ? COLLATE NOCASE", data);
            who.Send.Info(msgDeleted);
            Log.Info(Name, "{0} deleted factoid for topic {1}", who, data);
            return true;
        }

        void onIncoming(User source, string message)
        {
            var match  = Regex.Match(message, @"^\?(?<topic>.+?)(@(?<who>.+))?$");
            var topic  = match.Groups["topic"].Value.Trim();
            var target = match.Groups["who"].Value.Trim();

            onGetFact(source, topic, target, false);
        }

        bool cmdGetFact(User who, string data)
        {
            var match  = Regex.Match(data, @"^(?<topic>.+?)(@(?<who>.+))?$");
            var topic  = match.Groups["topic"].Value.Trim();
            var target = match.Groups["who"].Value.Trim();

            return onGetFact(who, topic, target, true);
        }

        bool onGetFact(User who, string topic, string target, bool notify)
        {
            if ( string.IsNullOrWhiteSpace(topic) )
                return false;

            var fact     = getFact(topic);
            var targeted = !string.IsNullOrWhiteSpace(target);

            // Undefined topics
            if (fact == null)
            {
                if (notify)
                    who.Send.Warn(errNonExistant);

                return true;
            }
            
            // Alias topics
            if ( fact.Description.StartsWith("@") )
            {
                var aliasTopic = fact.Description.Substring(1);
                var alias      = getFact(aliasTopic);
                
                if (alias == null)
                {
                    who.Send.Warn(errBrokenAlias, aliasTopic, topic);
                    return true;
                }
                else
                    fact = alias;
            }

            if (targeted)
                who.World.Send.Info(msgFactTargeted, target, fact.Description);
            else
                who.World.Send.Info(msgFact, fact.Topic, fact.Description);
            return true;
        }

        bool cmdFactList(User who, string data)
        {
            if ( string.IsNullOrWhiteSpace(data) )
                return false;

            //var query = from   f in sql.Table<sqlFact>()
            //            where  f.Topic.Contains(data)
            //            select f;

            //if (query.Count() == 0)
            //    who.Send.Warn(errNotFound, data);
            //else
            //{
            //    app.Bot.ConsoleMessage(who.Session, ChatEffect.BoldItalic, VPServices.ColorInfo, "", msgResults, data);

            //    foreach ( var q in query )
            //    {
            //        var locked = q.Locked ? " (locked)" : "";

            //        app.Bot.ConsoleMessage(who.Session, ChatEffect.Italic, VPServices.ColorInfo, "", msgResult , q.Topic, locked, q.Description);
            //        app.Bot.ConsoleMessage(who.Session, ChatEffect.Italic, VPServices.ColorInfo, "", msgResult2, q.When);
            //    }
            //}

            return true;
        }
        #endregion

        #region Facts logic
        sqlFact getFact(string topic)
        {
            return sql.Query<sqlFact>("SELECT * FROM Facts WHERE Topic = ? COLLATE NOCASE", topic).FirstOrDefault();
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
