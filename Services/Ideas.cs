using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using VP;

namespace VPServ.Services
{
    public class Ideas : IService
    {
        public const string FileIdeas = "Ideas.dat";

        List<Idea> storedIdeas = new List<Idea>();

        public string Name { get { return "Ideas"; } }
        public void Init(VPServ app, Instance bot)
        {
            // Load all saved Ideas
            if (File.Exists(FileIdeas))
                foreach (var Idea in File.ReadAllLines(FileIdeas))
                    storedIdeas.Add(new Idea(Idea));

            app.Commands.AddRange(new[] {
                new Command("Add Idea", "^(addidea|ai)$", cmdAddIdea,
                @"Adds an attributed and datestamped idea in the format: `!addidea *what*`"),

                new Command("Complete Idea", "^(com(plete)?idea|ci|done)$", cmdDoneIdea,
                @"Marks an idea done of the specified ID in the format: `!comidea *id*`"),

                new Command("List Ideas", "^(listideas?|li|ideas?list)$", (s,a,d) => { s.Bot.Say(s.PublicUrl + "Ideas"); },
                @"Prints the URL to a listing of ideas to chat", 60),

                new Command("Idea", "^i(dea)?$", cmdIdea,
                @"Spins the idea wheel and outputs a random idea", 5),
            });

            app.Routes.Add(new WebRoute("Ideas", "^(list)ideas?$", webListIdeas,
                @"Provides a list of ideas known by the system"));
        }

        public void Dispose()
        {
            saveIdeas();
            storedIdeas.Clear();
        }

        void saveIdeas()
        {
            File.WriteAllLines(FileIdeas,
                from t in storedIdeas
                select t.ToString(), Encoding.UTF8);
        }

        void cmdAddIdea(VPServ serv, Avatar who, string data)
        {
            var what = data.Trim();

            storedIdeas.Add( new Idea(who.Name, what) );
            saveIdeas();

            serv.Bot.Say("{0}: Saved", who.Name);
            Log.Info(Name, "Saved a idea for {0}: {1}", who.Name, what);
        }

        void cmdDoneIdea(VPServ serv, Avatar who, string data)
        {
            uint id;
            if ( !uint.TryParse(data, out id) )
                 return;

            var idea = getIdea(id);
            if ( idea.ID == 0 )
            {
                serv.Bot.Say("{0}: Idea does not exist", who.Name);
                return;
            }
            else if ( idea.Done )
                return;
            else
                idea.Done = true;

            saveIdeas();
            serv.Bot.Say("{0}: Idea marked as done", who.Name);
            Log.Info(Name, "Marked {0} idea as done for {1}", id, who.Name);
        }

        void cmdIdea(VPServ serv, Avatar who, string data)
        {
            try
            {
                var idea   = from i in storedIdeas
                             where !i.Done
                             select i;

                var random = idea
                    .Skip(VPServ.Rand.Next(0, storedIdeas.Count))
                    .Take(1)
                    .Single();

                serv.Bot.Say("From {0} (#{1}): {2}", random.Who, random.ID, random.What);
            }
            catch
            {
                serv.Bot.Say("{0}: I am unable to fetch an idea (perhaps they are all done?)", who.Name);
            }
        }

        Idea getIdea(uint id)
        {
            foreach (var idea in storedIdeas)
                if (idea.ID == id) return idea;

            return null;
        }

        string webListIdeas(VPServ serv, string data)
        {
            string listing = "# Ideas available:\n";

            foreach (var idea in storedIdeas)
            {
                var done = idea.Done
                    ? "*DONE*"
                    : "";

                listing += string.Format(
@"## Idea ID {0} {1}
### *{2}*
* **By:** {3}
* **When:** {4}

", idea.ID, done, idea.What, idea.Who, idea.When);
            }

            return serv.MarkdownParser.Transform(listing);
        }
    }

    class Idea
    {
        static uint        nextID = 1;
        public static uint NextID
        {
            get { return nextID++; }
        }

        public uint     ID;
        public string   Who;
        public string   What;
        public DateTime When;
        public bool     Done;

        public Idea(string who, string what)
        {
            Who  = who;
            What = what;
            When = DateTime.Now;
            ID   = NextID;
            Done = false;
        }

        /// <summary>
        /// Creates a Idea from a CSV string
        /// </summary>
        public Idea(string csv)
        {
            if (csv == "" || !csv.Contains(","))
                throw new ArgumentNullException();

            var parts = csv.SplitCSV(true).ToArray();
            Who  = parts[0];
            What = parts[1];
            When = DateTime.Parse(parts[2]);
            Done = bool    .Parse(parts[3]);
            ID   = NextID;
        }

        /// <summary>
        /// Formats the Idea to a CSV string
        /// </summary>
        public override string ToString()
        {
            return TBXString.PartsToCSV(Who, What, When, Done);
        }
    }
}
