using MarkdownSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VPServices
{
    public partial class VPServices : IDisposable
    {
        public HttpListener Server = new HttpListener();
        public Markdown MarkdownParser = new Markdown();
        public string PublicUrl;

        /// <summary>
        /// Global list of all routes registered
        /// </summary>
        public SortedSet<WebRoute> Routes = new SortedSet<WebRoute>();

        const string HTML_WHOLE =
@"<!DOCTYPE html>
{0}
{1}
</html>";

        const string HTML_HEAD =
@"<head>
    <title>{0}</title>
    <style type='text/css'> body {{ font-family: sans-serif; }} </style>
</head>";

        const string HTML_BODY =
@"<body>
    {0}
    <hr />
    <em>VPServices bot currently running on <strong>{1}</strong>, generated on <strong>{2}</strong></em>
</body>";

        const string HTML_NOTHANDLED =
@"This bot does not handle route {0}. See <a href='{1}'>here</a> for route reference.";

        public void SetupWeb()
        {
            PublicUrl = WebSettings.Get("PublicUrl");
            Server.Prefixes.Add(WebSettings.Get("Prefix"));
            Server.Start();
            Task.Factory.StartNew(() =>
            {
                while (Server.IsListening)
                {
                    try { processContext(Server.GetContext()); }
                    catch (Exception e) { e.LogFullStackTrace(); }
                }
            });

            Log.Info("Web server", "Listening on {0}", WebSettings.Get("Prefix"));
        }

        void processContext(HttpListenerContext ctx)
        {
            // Ignore favicon requests
            if (ctx.Request.RawUrl.Contains("favicon.ico"))
            {
                ctx.Response.Close();
                return;
            }

            Log.Fine("Web server", "Request for {0} by {1}", ctx.Request.RawUrl, ctx.Request.RemoteEndPoint);
            string response = null;
            var intercept = ctx.Request.RawUrl.Split(new[] { '/' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var targetRoute = intercept.Length >= 1 ? intercept[0] : null;
            var data = intercept.Length == 2 ? intercept[1] : null;

            if (targetRoute == null)
            {
                // Generate command listing
                response = string.Format(HTML_WHOLE,
                    string.Format(HTML_HEAD, "VPServices web interface"),
                    string.Format(HTML_BODY, mdGenerateRouteListing(), World, DateTime.Now));
            } else {
                // Search for route
                foreach (var rt in Routes)
                    if (TRegex.IsMatch(targetRoute, rt.Regex) || rt.Name.Equals(targetRoute, StringComparison.CurrentCultureIgnoreCase))
                    {
                        Log.Debug("Web server", "Routing to {0}", rt.Name);
                        response = string.Format(HTML_WHOLE,
                            string.Format(HTML_HEAD, "VPServices: " + rt.Name),
                            string.Format(HTML_BODY, rt.Handler(this, data), World, DateTime.Now));

                        break;
                    }

                // 404 handler
                if (response == null)
                {
                    response = string.Format(HTML_WHOLE,
                            string.Format(HTML_HEAD, "Route not handled"),
                            string.Format(HTML_BODY,
                                string.Format(HTML_NOTHANDLED, targetRoute, PublicUrl),
                            World, DateTime.Now));

                    ctx.Response.StatusCode = 404;
                    ctx.Response.StatusDescription = "Unhandled route";
                }
            }

            using (var outStream = new StreamWriter(ctx.Response.OutputStream))
            {
                try
                {
                    outStream.Write(response);
                    outStream.Flush();
                    ctx.Response.AddHeader("Content-Type", "Content-Type:text/html; charset=utf-8");
                    ctx.Response.Close();
                }
                catch { Log.Info("Web server", "Discarding response for closed connection {0}", ctx.Request.RemoteEndPoint); }
            }
        }

        /// <summary>
        /// Generates a markdown-formatted listing of registered routes
        /// </summary>
        string mdGenerateRouteListing()
        {
            string listing = "# Web services available:\n";

            foreach (var route in Routes)
            {
                listing += string.Format(
@"## [{0}]({1}{2})

* **Regex:** {3}
* *{4}*

", route.Name, PublicUrl, route.Name, route.Regex, route.Help);
            }

            return MarkdownParser.Transform(listing);
        }
    }

    public delegate string WebHandler(VPServices serv, string data);

    /// <summary>
    /// Defines a web route
    /// </summary>
    public class WebRoute : IComparable<WebRoute>
    {
        /// <summary>
        /// Canonical route name
        /// </summary>
        public string Name;
        /// <summary>
        /// Regex pattern that matches this route
        /// </summary>
        public string Regex;
        /// <summary>
        /// Handler to call when this route is requested
        /// </summary>
        public WebHandler Handler;
        /// <summary>
        /// Help string for this route
        /// </summary>
        public string Help;

        public WebRoute(string name, string rgx, WebHandler handler, string help)
        {
            Name = name;
            Regex = rgx;
            Handler = handler;
            Help = help;
        }

        public int CompareTo(WebRoute other) { return this.Name.CompareTo(other.Name); }
    }
}
