using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VpNet;

namespace VPServices.Services
{
    public partial class Facts : IService
    {
        readonly ILogger logger = Log.ForContext("Tag", "Facts");
        public string Name
        { 
            get { return "Facts"; }
        }

        public void Init(VPServices app, Instance bot)
        {
            app.Commands.Add(new Command(
                "Facts: Add", "^(addfact|af|define)$", cmdAddFact,
                @"Adds or overwrites a factoid for a topic, allowing it to be locked or alias another topic",
                @"!addfact [--lock] `topic: [what|@alias]`"));
            app.Commands.Add(new Command(
                "Facts: Delete", "^(del(ete)?fact|df)$", cmdDeleteFact,
                @"Clears a factoid for a topic",
                @"!delfact `topic`"
            ));
            app.Commands.Add(new Command(
                "Facts: Get", "^(what(is)?|explain|fact)$", cmdGetFact,
                @"Explains a given topic",
                "!what `topic`"
            ));

            app.Routes.Add(new WebRoute("Facts", "^(list)?facts?$", webListFacts,
                @"Provides a list of defined facts"));

            this.connection = app.Connection;
        }

        public void Dispose() { }

        #region Privates and strings
        const string msgFact        = "{0} is {1}";
        const string msgAdded       = "Added factoid for {1}topic '{0}'";
        const string msgOverwritten = "Overwritten previous factoid for {1}topic '{0}'";
        const string msgDeleted     = "Factoid deleted";
        const string msgNonExistant = "No factoid for that topic was found";
        const string msgBrokenAlias = "Could not resolve alias '@{0}' from topic '{1}'";
        const string msgLocked      = "Topic locked by user ID {0}; can only be modified or deleted by them or the bot's owner";

        SQLiteConnection connection; 
        #endregion

        #region Command handlers
        bool cmdAddFact(VPServices app, Avatar<Vector3> who, string data)
        {
            var matches = Regex.Match(data, "^(-+lock )?(.+?): (.+)$");
            if ( !matches.Success )
                return false;

            var parts = (from Group grp in matches.Groups
                         select grp.Value).ToArray();
            var locked = parts[1] != "";
            var topic  = parts[2].Trim();
            var what   = parts[3].Trim();
            var old    = getFact(topic);
            var msg    = old == null ? msgAdded : msgOverwritten;

            // Only allow overwrite of locked previous factoid if owner or bot owner
            if ( old != null && old.Locked && !who.Name.Equals(app.Owner, StringComparison.OrdinalIgnoreCase) )
            if (old.WhoID != who.UserId)
            {
                app.Warn(who.Session, msgLocked, old.WhoID);
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
                    WhoID       = who.UserId,
                    Locked      = locked
                });
            }

            app.Notify(who.Session, msg, topic, locked ? "locked " : "");
            logger.Information("Saved a fact from {User} for topic {Topic} (locked: {Locked})", who.Name, topic, locked);
            return true;
        }

        bool cmdDeleteFact(VPServices app, Avatar<Vector3> who, string data)
        {
            var fact = getFact(data);

            if (fact == null)
            {
                app.Warn(who.Session, msgNonExistant);
                return true;
            }

            // Only allow deletion of locked factoid if owner or bot owner
            if ( fact.Locked && !who.Name.Equals(app.Owner, StringComparison.OrdinalIgnoreCase) )
            if (fact.WhoID != who.UserId)
            {
                app.Warn(who.Session, msgLocked, fact.WhoID);
                return true;
            }

            lock (app.DataMutex)
                connection.Execute("DELETE FROM Facts WHERE Topic = ? COLLATE NOCASE", data);

            app.Notify(who.Session, msgDeleted);
            logger.Information("{User} deleted factoid for topic {Topic}", who.Name, data);
            return true;
        }

        bool cmdGetFact(VPServices app, Avatar<Vector3> who, string data)
        {
            var fact = getFact(data);

            // Undefined topics
            if (fact == null)
            {
                app.Warn(who.Session, msgNonExistant);
                return true;
            }
            
            // Alias topics
            if ( fact.Description.StartsWith("@") )
            {
                var aliasTopic = fact.Description.Substring(1);
                var alias      = getFact(aliasTopic);
                
                if (alias == null)
                {
                    app.Warn(who.Session, msgBrokenAlias, aliasTopic, data);
                    return true;
                }

                app.NotifyAll(msgFact, alias.Topic, alias.Description);
                return true;
            }

            app.NotifyAll(msgFact, fact.Topic, fact.Description);
            return true;
        }
        #endregion

        #region Web routes
        string webListFacts(VPServices app, string data)
        {
            lock (app.DataMutex)
            {
                var listing = "# Factoids:\n";
                var list    = connection.Query<sqlFact>("SELECT * FROM Facts ORDER BY Topic ASC;");

                foreach ( var fact in list )
                    listing +=
    $@"## {fact.Topic} is {fact.Description}

    * **By:** #{fact.WhoID}
    * **When:** {fact.When}
    * **Locked:** {fact.Locked}

    ";

                return app.MarkdownParser.Transform(listing);
            }
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
