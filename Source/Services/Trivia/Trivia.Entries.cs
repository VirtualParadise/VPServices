using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VPServices.Services
{
    public partial class Trivia : ServiceBase
    {
        const string configTrivia = "Trivia";
        const string fileDatabase = "TriviaQuickfire.csv";
        const string keyDatabase  = "Database";

        TriviaEntry[] entries;
        TriviaEntry   entryInPlay;

        void loadTrivia()
        {
            var config   = app.Settings.Configs[configTrivia];
            var fileName = config.Get(keyDatabase, fileDatabase);

            if ( !File.Exists(fileName) )
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

            entries = shuffleEntries(list);
            reader.Dispose();
            file  .Dispose();
            Log.Debug(tag, "Loaded trivia database '{0}', {1} entries", fileName, entries.Length);
        }

        /// <summary>
        /// Shuffles a list of trivia entries
        /// </summary>
        TriviaEntry[] shuffleEntries(List<TriviaEntry> list)
        {
            var idx = 0;
            var arr = new TriviaEntry[list.Count];

            while (list.Count > 0)
            {
                var listIdx = VPServices.Rand.Next(list.Count);
                var item    = list[listIdx];
                arr[idx]    = item;

                list.Remove(item);
                idx++;
            }

            Log.Debug(tag, "Shuffled {0} entries into random order", arr.Length);
            return arr;
        }

        /// <summary>
        /// Marks all entries as unused for the session
        /// </summary>
        void markEntriesUnused(string category)
        {
            var query = from   e in entries
                        where  e.Category.ToLower().Contains(category)
                        select e;

            foreach (var entry in query)
                entry.Used = false;
        }

        /// <summary>
        /// Fetches a random entry of the specified category
        /// </summary>
        TriviaEntry fetchEntry(string category = "")
        {
            // First, collect entries of the specified category
            var catSearch = category.ToLower().Trim();
            var query     = from   e in entries
                            where  e.Category.ToLower().Contains(catSearch)
                            select e;

            if (query.Count() < 1)
                return null;

        pickEntry:
            // Then pick out an entry that has not been used
            query = from    e in query
                    where  !e.Used
                    select  e;

            if (query.Count() < 1)
            {
                // No more unused entries in specified category; mark all as unused and
                // repeat search
                markEntriesUnused(category);
                Log.Debug(tag, "No more unused trivia entries for category '{0}'; marking all as unused", category);
                app.WarnAll("Out of entries for that query; marking all entries as unused and starting over");
                goto pickEntry;
            }

            return query.First();
        }
    }

    class TriviaEntry
    {
        public bool   Used;

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
