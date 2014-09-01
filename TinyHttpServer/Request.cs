using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace TinyHttpServer
{
    public class Request
    {
        public NetworkStream Stream;

        public StreamReader Reader;
        public StreamWriter Writer;

        public string Method;
        public string Url;
        public string Std;

        public Dictionary<string, string> Param = new Dictionary<string, string>();

        public Request(Socket remote)
        {
            Stream = new NetworkStream(remote);
            Reader = new StreamReader(Stream);
            Writer = new StreamWriter(Stream);
        }
    }
}