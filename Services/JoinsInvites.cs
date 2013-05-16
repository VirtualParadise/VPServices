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
        const string msgRequest          = "{0} would like to {1} you; please respond with !yes or !no";
        const string msgRequestSent      = "Request sent; waiting for {0} to accept...";
        const string msgRequestRejected  = "Your request has been rejected";
        const string msgNoRequests       = "You have no requests to respond to (perhaps it has timed out?)";
        const string msgJoined           = "You are being joined by {0}";
        const string msgInvited          = "You are being invited by {0}";
        const string msgSelf             = "You cannot request a join or invite to yourself";
        const string msgNotPresent       = "Requester is no longer present";
        const string msgPendingRequester = "You already have a pending request";
        const string msgPendingRequestee = "That user already has a pending request";

        List<JoinInvite> requests = new List<JoinInvite>();

        public string Name { get { return "Joins & invites"; } }
        public void Init(VPServices app, Instance bot)
        {
            app.Commands.AddRange(new[] {
                new Command
                (
                    "Request: Join", "^jo(in)?$",
                    (s, w, d) => { return onRequest(s, w, d, false); },
                    @"Sends a join request to target user",
                    @"!join `target`"
                ),

                new Command
                (
                    "Request: Invite", "^inv(ite)?$",
                    (s, w, d) => { return onRequest(s, w, d, true); },
                    @"Sends an invite request to the target user",
                    @"!invite `target`"
                ),

                new Command
                (
                    "Request: Accept", "^(yes|accept)$",
                    (s, w, d) => { return onResponse(s, w, true); },
                    @"Accepts a pending join or invite request",
                    @"!yes"
                ),

                new Command
                (
                    "Request: Reject", "^(no|reject|deny)$",
                    (s, w, d) => { return onResponse(s, w, true); },
                    @"Rejects a pending join or invite request",
                    @"!no"
                ),
            });
        }

        public void Migrate(VPServices app, int target) {  }

        public void Dispose() { }

        #region Command handlers
        bool onRequest(VPServices app, Avatar source, string targetName, bool invite)
        {
            // Ignore if self
            if ( source.Name.IEquals(targetName) )
            {
                app.Warn(source.Session, msgSelf);
                return true;
            }

            // Reject if source has request
            if ( !isRequestee(source.Session).Equals(JoinInvite.Nobody) )
            {
                app.Warn(source.Session, msgPendingRequester);
                return Log.Info(Name, "Rejecting request by {0} as they already have one pending", source);
            }

            // Reject if target has request
            if ( !isRequested(targetName).Equals(JoinInvite.Nobody) )
            {
                app.Warn(source.Session, msgPendingRequestee);
                return Log.Info(Name, "Rejecting request by {0} as they already have one pending", source);
            }

            // Ignore if no such users found
            var action  = invite ? "invite" : "join";
            var targets = app.GetUsers(targetName);
            if ( targets.Length <= 0 )
            {
                app.Warn(source.Session, msgNotPresent);
                return true;
            }

            // Request all sessions of given name
            foreach (var target in targets)
                app.Notify(target.Session, msgRequest, source.Name, action);

            app.Notify(source.Session, msgRequestSent, targetName);
            requests.Add(new JoinInvite
            {
                By     = source.Session,
                Who    = targetName.ToLower(),
                When   = DateTime.Now,
                Invite = invite
            });

            return true;
        }

        bool onResponse(VPServices app, Avatar targetAv, bool yes)
        {
            var sourceReq = isRequested(targetAv.Name);

            // Reject non-requested
            if ( sourceReq.Equals(JoinInvite.Nobody) )
            {
                app.Warn(targetAv.Session, msgNoRequests);
                return true;
            }

            requests.Remove(sourceReq);
            // Rejected requests
            if ( !yes )
            {
                app.Notify(sourceReq.By, msgRequestRejected);
                return true;
            }

            var target = app.GetUser(targetAv.Session);
            var source = app.GetUser(sourceReq.By);

            // Reject phantom users
            if ( target == null )
                return true;

            // Reject if source has gone away
            if ( source == null )
            {
                app.Warn(targetAv.Session, msgNotPresent);
                return Log.Info(Name, "Rejecting response by {0} as they have left", source.Name);
            }

            var targetPos     = sourceReq.Invite ? source.Position : target.Position;
            var targetSession = sourceReq.Invite ? target.Session : source.Session;
            var targetMsg     = sourceReq.Invite ? msgInvited : msgJoined;
            app.Notify(target.Session, targetMsg, source.Name);
            app.Bot.Avatars.Teleport(targetSession, "", new Vector3(targetPos.X, targetPos.Y, targetPos.Z), 0, 0);
            return true;
        } 
        #endregion

        #region Request checking logic
        /// <summary>
        /// Check if given name has been requested somewhere
        /// </summary>
        JoinInvite isRequested(string who)
        {
            requests.RemoveAll(timedOut);
            foreach ( var req in requests )
                if ( req.Who == who.ToLower() )
                    return req;

            return JoinInvite.Nobody;
        }

        /// <summary>
        /// Check if given session has made a request
        /// </summary>
        JoinInvite isRequestee(int who)
        {
            requests.RemoveAll(timedOut);
            foreach ( var req in requests )
                if ( req.By == who )
                    return req;

            return JoinInvite.Nobody;
        }

        /// <summary>
        /// Predicate for checking timed out entries
        /// </summary>
        bool timedOut(JoinInvite i) { return DateTime.Now.Subtract(i.When).TotalSeconds > 60; } 
        #endregion
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
