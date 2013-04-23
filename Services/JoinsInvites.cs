using System;
using System.Collections.Generic;
using VP;

namespace VPServices.Services
{
    /// <summary>
    /// Handles joins and invites between users
    /// </summary>
    public class JoinsInvites : IService
    {
        List<JoinInvite> requests = new List<JoinInvite>();

        public string Name { get { return "Joins & invites"; } }
        public void Init(VPServices app, Instance bot)
        {
            app.Commands.AddRange(new[] {
                new Command("Join", "^jo(in)?$", (s, w, d) => { onRequest(s, w, d, false); },
                @"Sends a join request to target user in the format: `!join *who*`"),
                new Command("Invite", "^inv(ite)?$", (s, w, d) => { onRequest(s, w, d, true); },
                @"Sends an invite request to the target user in the format: `!invite *who*`"),
                new Command("Accept join/invite", "^(yes|accept)$", (s, w, d) => { onResponse(s, w, true); },
                @"Accepts a pending join or invite request"),
                new Command("Reject join/invite", "^(no|reject|deny)$", (s, w, d) => { onResponse(s, w, true); },
                @"Rejects a pending join or invite request"),
            });
        }

        public void Dispose() { }

        void onRequest(VPServices serv, Avatar who, string forWhom, bool invite)
        {
            // Ignore if self
            if (who.Name.Equals(forWhom, StringComparison.CurrentCultureIgnoreCase))
                return;

            // Ignore if not nearby
            var whomUser = serv.GetUser(forWhom);
            if (whomUser == null) return;

            // Ignore if bot
            if (whomUser.IsBot) return;

            // Reject if requestee is pending
            if (!isRequestee(who.Session).Equals(JoinInvite.Nobody))
            {
                serv.Bot.Say("{0}: You already have a pending request", who);
                Log.Info(Name, "Rejecting request by {0} as they already have one pending", who);
                return;
            }

            // Reject if requester is pending
            if (!isRequested(forWhom).Equals(JoinInvite.Nobody))
            {
                serv.Bot.Say("{0}: That person already has a pending request", who);
                Log.Info(Name, "Rejecting request for {0} as they already have one pending", forWhom);
                return;
            }

            serv.Bot.Say("{0}: {1} would like to {2} you; respond with !yes or !no",
                whomUser.Name,
                who.Name,
                invite ? "invite" : "join");

            requests.Add(new JoinInvite
            {
                By = who.Session,
                Who = forWhom.ToLower(),
                When = DateTime.Now,
                Invite = invite
            });
        }

        void onResponse(VPServices serv, Avatar from, bool yes)
        {
            var req = isRequested(from.Name);
            // Reject non-requested
            if (req.Equals(JoinInvite.Nobody)) return;

            requests.Remove(req);
            if (!yes) return;

            var who = serv.GetUser(from.Session);
            var by = serv.GetUser(req.By);

            if (by == null) return;
            if (who == null)
            {
                serv.Bot.Say("{0}: That person is no longer here", by.Name);
                Log.Info(Name, "Rejecting response for {0} as they have left", by.Name);
                return;
            }

            var target = req.Invite ? by.Position : who.Position;
            serv.Bot.Avatars.Teleport(
                req.Invite ? who.Session : by.Session,
                "", new Vector3(target.X, target.Y, target.Z),
                0, 0);
        }

        /// <summary>
        /// Check if given name has been requested somewhere
        /// </summary>
        JoinInvite isRequested(string who)
        {
            requests.RemoveAll(timedOut);
            foreach (var req in requests)
                if (req.Who == who.ToLower())
                    return req;

            return JoinInvite.Nobody;
        }

        /// <summary>
        /// Check if given session has made a request
        /// </summary>
        JoinInvite isRequestee(int who)
        {
            requests.RemoveAll(timedOut);
            foreach (var req in requests)
                if (req.By == who)
                    return req;

            return JoinInvite.Nobody;
        }

        /// <summary>
        /// Predicate for checking timed out entries
        /// </summary>
        bool timedOut(JoinInvite i) { return DateTime.Now.Subtract(i.When).TotalSeconds > 60; }
    }

    public struct JoinInvite
    {
        public static JoinInvite Nobody = new JoinInvite();

        public string   Who;
        public int      By;
        public bool     Invite;
        public DateTime When;
    }
}
