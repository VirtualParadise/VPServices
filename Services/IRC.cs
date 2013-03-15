using System;
using System.Collections.Generic;
using VP;
using IrcDotNet;

namespace VPServ.Services
{
    /// <summary>
    /// Provides a VP <> IRC bridge
    /// </summary>
    class IRC : IService
    {
        IrcClient irc;
        
        public string Name { get { return "IRC"; } }
        public void   Init (VPServ app, Instance bot)
        {
            // Comment out return to enable IRC
            return;

                irc = new IrcClient();
            var reg = new IrcUserRegistrationInfo
            {
                NickName = "VPBlizzard",
                RealName = "Roy Curtis",
                UserName = "VPBlizzard",
                Password = "###"
            };

            irc.Error                += (o,e) => { Log.Warn(Name, e.Error.ToString()); };
            irc.ErrorMessageReceived += (o,e) => { Log.Warn(Name, "IRC error: {0}", e.Message); };
            irc.ProtocolError        += (o,e) => { Log.Warn(Name, "Protocol error: {0} {1}", e.Code, e.Message); };
            irc.RawMessageReceived   += (o,e) => { Log.Warn(Name, "Protocol message: {0}", e.RawContent); };
            irc.RawMessageReceived   += irc_RawMessageReceived;

            irc.Connect("irc.ablivion.net", 6667, false, reg);
            irc.Registered += irc_Connected;
            bot.Chat       += bot_Chat;
        }

        void bot_Chat(Instance sender, Chat chat)
        {
            if (!irc.IsConnected)
                return;

            var msg = string.Format("PRIVMSG #vp :{0}: {1}", chat.Name, chat.Message);
            irc.SendRawMessage(msg);
        }

        void irc_Connected(object sender, EventArgs e)
        {
            irc.Channels.Join("#vp");
        }

        void irc_RawMessageReceived(object sender, IrcRawMessageEventArgs e)
        {
            if (e.Message.Parameters[0] == "#vp")
            {
                if      ( e.Message.Command.Equals("PRIVMSG", StringComparison.CurrentCultureIgnoreCase) )
                    VPServ.Instance.Bot.Say("/me {0}:\t{1}", e.Message.Source.Name, e.Message.Parameters[1]);
                else if ( e.Message.Command.Equals("JOIN", StringComparison.CurrentCultureIgnoreCase) )
                    VPServ.Instance.Bot.Say("/me *** {0} has joined #vp", e.Message.Source.Name);
                else if ( e.Message.Command.Equals("PART", StringComparison.CurrentCultureIgnoreCase) )
                    VPServ.Instance.Bot.Say("/me *** {0} has left #vp", e.Message.Source.Name);
            }
            else if ( e.Message.Command.Equals("QUIT", StringComparison.CurrentCultureIgnoreCase) )
                VPServ.Instance.Bot.Say("/me *** {0} has quit ({1})", e.Message.Source.Name, e.Message.Parameters[0]);
            
        }

        public void Dispose()
        {
            
        }
    }
}
