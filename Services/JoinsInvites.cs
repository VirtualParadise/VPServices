using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VP;

namespace VPServices.Services
{
    public struct JoinInvite
    {
        public static JoinInvite Nobody = new JoinInvite();

        public string Who;
        public string By;
        public bool Invite;
        public DateTime When;
    }

    public class JoinsInvites
    {
        List<JoinInvite> requests = new List<JoinInvite>();

        public void OnRequest(string who, string forWhom, bool invite) {
            if (VPServices.UserManager[forWhom] == null) return;

            if (!IsRequestee(who).Equals(JoinInvite.Nobody))
            {
                VPServices.Bot.Comms.Say("{0}: You already have a pending request", who);
                return;
            }

            if (!IsRequested(forWhom).Equals(JoinInvite.Nobody))
            {
                VPServices.Bot.Comms.Say("{0}: That person already has a pending request", who);
                return;
            }

            VPServices.Bot.Comms.Say(
                "{0}: {1} would like to {2} you; respond with !yes or !no",
                forWhom,
                who,
                invite ? "invite" : "join");

            requests.Add(new JoinInvite
            {
                By = who.ToLower(),
                Who = forWhom.ToLower(),
                When = DateTime.Now,
                Invite = invite
            });
        }

        public void OnResponse(string from, bool yes)
        {

            var req = IsRequested(from);
            // Reject non-requested
            if (req.Equals(JoinInvite.Nobody)) return;

            requests.Remove(req);
            if (!yes) return;

            var who = VPServices.UserManager[req.Who];
            var by = VPServices.UserManager[req.By];

            if (by == null) return;
            if (who == null)
            {
                VPServices.Bot.Comms.Say("{0}: That person is no longer here", by.Name);
                return;
            }

            var target = req.Invite ? by.LastLocation : who.LastLocation;
            VPServices.Bot.World.TeleportAvatar(
                req.Invite ? who.Session : by.Session,
                "",
                new Vector3
                {
                    X = target.X,
                    Y = target.Y,
                    Z = target.Z
                },
                0, 0);
        }

        /// <summary>
        /// Check if given name has been requested somewhere
        /// </summary>
        public JoinInvite IsRequested(string who)
        {
            requests.RemoveAll(timedOut);
            foreach (var req in requests)
                if (req.Who == who.ToLower())
                    return req;

            return JoinInvite.Nobody;
        }

        /// <summary>
        /// Check if given name has made a request
        /// </summary>
        public JoinInvite IsRequestee(string who)
        {
            requests.RemoveAll(timedOut);
            foreach (var req in requests)
                if (req.By == who.ToLower())
                    return req;

            return JoinInvite.Nobody;
        }

        bool timedOut(JoinInvite i)
        {
            return DateTime.Now.Subtract(i.When).TotalSeconds > 60;
        }
    }
}
