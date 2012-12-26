using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VP;

namespace VPServ.Services
{
    public class KickBans
    {
        public Dictionary<string, DateTime> Kicked = new Dictionary<string, DateTime>();
        public Dictionary<string, DateTime> Banned = new Dictionary<string, DateTime>();

        DateTime lastPetition = DateTime.MinValue;
        Dictionary<string, bool> votes = new Dictionary<string, bool>();
        string petitioningFor;
        int majority;

        public void OnPetition(string who, string target) {
            if (VPServices.UserManager[target] == null) return;

            checkExpired();
            if (lastPetition != DateTime.MinValue)
            {
                VPServices.Bot.Say("{0}: There is an ongoing petition", who);
                return;
            }

            if (VPServices.UserManager.UniqueUsers < 4)
            {
                VPServices.Bot.Say("{0}: At least four unique non-bot users required for petition", who);
                return;
            }

            lastPetition = DateTime.Now;
            petitioningFor = target.ToLower();
            votes.Clear();
            majority = (int) ((float)VPServices.UserManager.UniqueUsers * 0.6);
            Console.WriteLine("Beginning petition to moderate {0} by {1}", target, who);
            VPServices.Bot.Say(
                "Petitioning moderation for {0}; vote to !kickvote or !banvote or neither; at least {1} votes (out of {2} unique users) required",
                target, majority, VPServices.UserManager.UniqueUsers);
        }

        public void OnVote(string from, bool ban)
        {
            // Reject during no petitions
            if (lastPetition == DateTime.MinValue) return;

            // Remove and ignore expired petition
            if (checkExpired())
            {
                VPServices.Bot.Say("{0}: Sorry, the petition has timed out.", from);
                return;
            }

            votes[from.ToLower()] = ban;
            Console.WriteLine("Recorded {0} vote from {1}", ban ? "ban" : "kick", from);

            if (votes.Count >= majority) doModeration();
        }

        bool checkExpired()
        {
            if (DateTime.Now.Subtract(lastPetition).TotalSeconds > 120)
            {
                lastPetition = DateTime.MinValue;
                return true;
            }
            else return false;
        }

        public bool IsKickBanned(string name)
        {
            var who = name.ToLower();
            foreach (var kick in Kicked)
                if (kick.Key == who)
                {
                    if (DateTime.Now.Subtract(kick.Value).TotalMinutes > 5)
                    {
                        Console.WriteLine("Removing expired kick for {0}", name);
                        Kicked.Remove(who);
                        break;
                    }
                    else return true;
                }

            foreach (var ban in Banned)
                if (ban.Key == who)
                {
                    if (DateTime.Now.Subtract(ban.Value).TotalHours > 24)
                    {
                        Console.WriteLine("Removing expired ban for {0}", name);
                        Banned.Remove(who);
                        break;
                    }
                    else return true;
                }

            return false;
        }

        public void Eject(int session)
        {
            Console.WriteLine("Ejecting session {0}", session);
            VPServices.Bot.Avatars.Teleport(
                session,
                "void", Vector3.Zero, 0, 0);
        }

        public void Eject(string target)
        {
            var user = VPServices.UserManager[target];
            Eject(user.Session);
        }

        void doModeration()
        {
            int kicks = 0;
            int bans = 0;

            foreach (var vote in votes)
                if (vote.Value) bans++;
                else kicks++;

            if (kicks >= bans)
            {
                VPServices.Bot.Say("Kicking {0} out for five minutes", petitioningFor);
                Console.WriteLine("Kicking {0} for five minutes", petitioningFor);
                Kicked.Add(petitioningFor, DateTime.Now);
            }
            else
            {
                VPServices.Bot.Say("Banning {0} for 24 hours", petitioningFor);
                Console.WriteLine("Banning {0} for 24 hours", petitioningFor);
                Banned.Add(petitioningFor, DateTime.Now);
            }

            Eject(petitioningFor);
            lastPetition = DateTime.MinValue;
        }
    }
}
