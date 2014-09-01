using System;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace TinyHttpServer
{
    public class Client
	{
		protected Socket Remote;
		protected Server Server;

        protected Request Request;

		public Client(Socket remote, Server server)
		{
			Remote = remote;
			Server = server;
            Request = new Request(remote);
		}

		public void Start()
		{
			var thread = new Thread(ProcessRequest);
			thread.IsBackground = true;
			thread.Start();
		}

		private void ProcessRequest()
		{
            try
            {
                ParseRequest();
                if (!ProcessByUserHandler()) ProcessByStdHandler();
            }
            catch(Exception e)
            {
                if (Request.Writer != null) ProcessByErrorHandler(e);
            }
            finally
            {
                if (Request.Writer != null) Request.Writer.Flush();
                if (Request.Stream != null) Request.Stream.Close();
                if (Remote != null) Remote.Close();
            }
		}

        protected virtual void StdAnswer(int i, string reason, string body)
	    {
	        Request.Writer.WriteLine("HTTP/1.1 {0} {1}", i, reason);
            Request.Writer.WriteLine();
            Request.Writer.WriteLine(body);
	    }

        protected virtual void ProcessByStdHandler()
	    {
            StdAnswer(404, "Not Found", "Page Not Found");
	    }

        protected virtual void ProcessByErrorHandler(Exception e)
        {
            StdAnswer(500, "Internal Server Error", e.ToString());
        }

        protected virtual bool ProcessByUserHandler()
	    {
            return false;
	    }

        protected virtual void ParseRequest()
	    {
	        ProcessRequestHeader();
	        ProcessRequestData();
	    }

        protected virtual void ProcessRequestHeader()
	    {
	        ProcessRequestFirstLine();
	        ProcessRequestPostParams();
	        ParseUrl();
	    }

        protected virtual void ProcessRequestPostParams()
	    {
	        Regex hdr = new Regex(@"^([^:]+)[:]\s*(.*)$", RegexOptions.IgnoreCase);
	        while (true)
	        {
	            var ws = GetNetxLine(Request.Reader);
	            if (string.IsNullOrEmpty(ws)) break;
	            var m = hdr.Match(ws);
	            if (!m.Success) continue;
	            var key = m.Groups[1].Value.Trim().ToLower();
	            var value = m.Groups[2].Value.Trim();
	            if (!SpecialParse(key, value)) Request.Param[key] = value;
	        }
	    }

        protected virtual void ProcessRequestFirstLine()
	    {
	        var headline = GetNetxLine(Request.Reader);
	        if (string.IsNullOrEmpty(headline)) throw new ArgumentException("wrong request");
	        var parts = headline.Split(new[] {' '}, 3, StringSplitOptions.RemoveEmptyEntries);
	        if (parts.Length < 2) throw new ArgumentException("wrong request");
	        Request.Method = parts[0].ToUpper();
	        Request.Url = parts[1];
	        Request.Std = parts.Length == 3 ? parts[2] : string.Empty;
	    }

	    protected virtual void ProcessRequestData()
	    {
	        SkipRestRequestData(Request.Reader);
	    }

	    protected void SkipRestRequestData(StreamReader reader)
	    {
            if (!Request.Stream.DataAvailable) return;
	        while (reader.Read()!=-1) {}
	    }

	    protected virtual void ParseUrl()
	    {
            var url = Request.Url;
	        string host = string.Empty;
	        if (url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase))
            {
                url = url.Substring(7);
                var idx = url.IndexOf('/');
                host = idx == -1 ? url : url.Substring(0, idx);
                url = idx == -1 ? "/" : url.Substring(idx);
            }
            var pidx = url.IndexOf('?');
            if (pidx != -1)
            {
                var query = url.Substring(pidx + 1);
                url = url.Substring(0, pidx);
                if (!string.IsNullOrEmpty(query))
                {
                    var pairs = HttpUtility.ParseQueryString(query);
                    foreach (var key in pairs.AllKeys) 
                        if (!Request.Param.ContainsKey(key)) 
                            Request.Param[key] = pairs[key];
                }
            }
            Request.Url = url;
            if (host != string.Empty) Request.Param["host"] = host;
	    }

	    protected virtual bool SpecialParse(string key, string value)
	    {
            return false;
	    }

	    private static string GetNetxLine(StreamReader reader)
	    {
	        return (reader.ReadLine() ?? string.Empty).TrimEnd();
	    }
	}
}