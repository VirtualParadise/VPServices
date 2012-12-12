﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VP;

namespace VPServices.Services
{
    class Telegram
    {
        public string To;
        public string From;
        public string Message;
        public bool Sent;

        public static Telegram FromString(string dat)
        {
            if (dat == "" || !dat.Contains(","))
                return null;

            var parts = dat.Split(new[] { "," }, StringSplitOptions.None);
            return new Telegram
            {
                To = parts[0],
                From = parts[1],
                Message = parts[2].Replace("%COMMA", ","),
                Sent = bool.Parse(parts[3]),
            };
        }

        public string Save()
        {
            return string.Join(",",
                To,
                From,
                Message.Replace(",", "%COMMA"),
                Sent);
        }
    }

    class Telegrams
    {
        const string TGRAMS = "Telegrams.dat";
        const string TBLOCKED = "DoNotTelegram.dat";

        List<Telegram> StoredGrams = new List<Telegram>();
        List<string> DoNotTelegram = new List<string>();

        public Telegrams()
        {
            // Load all saved telegrams
            if (File.Exists(TGRAMS))
                foreach (var tgram in File.ReadAllLines(TGRAMS))
                    StoredGrams.Add(Telegram.FromString(tgram));

            // Load block list
            if (File.Exists(TBLOCKED))
                DoNotTelegram = new List<string>(File.ReadAllLines(TBLOCKED));
            
            VPServices.Bot.Comms.Chat += checkTelegrams;
        }

        public void SaveTelegrams()
        {
            File.WriteAllLines(TGRAMS,
                from t in StoredGrams
                select t.Save()
                , Encoding.UTF8);
        }

        public void OnCommand(Instance bot, Chat chat, string data)
        {
            var matches = Regex.Match(data, "^(.+?): (.+?)$", RegexOptions.IgnoreCase);
            if (!matches.Success) return;
            
            var target = matches.Groups[1].Value.ToLower().Trim();
            var msg = matches.Groups[2].Value.Trim();

            if (DoNotTelegram.Contains(target))
            {
                bot.Comms.Say("{0}: {1} is not accepting telegrams.", chat.Name, target);
                return;
            }

            StoredGrams.Add(new Telegram { From = chat.Name, To = target, Message = msg });
            bot.Comms.Say("{0}: Saved for {1}", chat.Name, target);
            Console.WriteLine("Telegram by {0} recorded for {1}.", chat.Name, target);
            SaveTelegrams();
        }

        public void Block(Instance bot, string name)
        {
            if (DoNotTelegram.Contains(name))
            {
                bot.Comms.Say("{0}: You are now accepting telegrams.", name);
                DoNotTelegram.Remove(name);
            }
            else
            {
                bot.Comms.Say("{0}: You are now blocking telegrams.", name);
                DoNotTelegram.Add(name);
            }

            File.WriteAllLines(TBLOCKED, DoNotTelegram, Encoding.UTF8);
        }

        void checkTelegrams(Instance bot, Chat chat)
        {
            if (DoNotTelegram.Contains(chat.Name.ToLower())) return;

            bool sentOne = false;
            foreach (var tg in StoredGrams)
                if (!tg.Sent && chat.Name.ToLower() == tg.To)
                {
                    bot.Comms.Say("{0}: You have a telegram from {1}: {2}", chat.Name, tg.From, tg.Message);
                    tg.Sent = true;
                    sentOne = true;
                    SaveTelegrams();
                }

            if (sentOne) bot.Comms.Say("(Say '!telegram <recepient>: <message>' to respond or !blocktelegrams to prevent further)");
        }
    }
}
