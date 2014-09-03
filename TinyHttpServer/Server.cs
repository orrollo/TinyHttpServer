using System;
using System.Collections.Generic;
using System.Reflection;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TinyHttpServer
{
	public class Server
	{
        public readonly Dictionary<string,RequestHandler> Handlers = new Dictionary<string, RequestHandler>();

		public int Port { get; protected set; }

		protected Thread serverThread;
		protected Socket socket;
		
        protected ManualResetEvent stopServer;
        protected ManualResetEvent nextIncoming;

		public Server(int port)
		{
			Port = port;
            RegisterHandlers();
		}

	    private void RegisterHandlers()
	    {
	        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
	        foreach (var assembly in assemblies)
	        {
	            var types = assembly.GetTypes();
	            foreach (var type in types)
	            {
	                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
	                foreach (var method in methods)
	                {
	                    var attributes = method.GetCustomAttributes(typeof (RequestHandlerAttribute), false);
	                    foreach (var attribute in attributes)
	                    {
                            var attr = (RequestHandlerAttribute)attribute;
	                        var path = attr.Path;
	                        var handler = (RequestHandler) Delegate.CreateDelegate(typeof (RequestHandler), method);
	                        Handlers[path] = handler;
	                    }
	                }
	            }
	        }
	    }

	    public void Start()
		{
			if (serverThread != null) 
				throw new InvalidOperationException("server already started");
			RestartSocket();
			stopServer = new ManualResetEvent(false);
            nextIncoming = new ManualResetEvent(true);
			serverThread = new Thread(MainServerLoop);
			serverThread.IsBackground = true;
			serverThread.Start();
		}

		public void Stop()
		{
			if (serverThread == null) throw new InvalidOperationException("server not started");
			stopServer.Set();
			serverThread.Join();
			serverThread = null;
			CloseSocket();
		}

		private void MainServerLoop()
		{
			try
			{
				socket.Listen(30);
				while (!stopServer.WaitOne(0, false))
				{
                    if (nextIncoming.WaitOne(0))
                    {
                        nextIncoming.Reset();
                        socket.BeginAccept(OnIncome, socket);
                    }
                    else Thread.Sleep(50);
				}
			}
			finally
			{
				CloseSocket();
				stopServer.Set();
			}
		}

        private void OnIncome(IAsyncResult ar)
        {
            Socket listener = null;
            try
            {
                listener = (Socket)ar.AsyncState;
                var remoteSocket = listener.EndAccept(ar);
                var thread = new Client(remoteSocket, this);
                thread.Start();
            }
            catch (Exception e)
            {
                if (listener != null) listener.Close();
                stopServer.Set();
            }
            finally
            {
                nextIncoming.Set();
            }
        }

	    private void RestartSocket()
		{
			CloseSocket();
			InitSocket();
		}

		private void InitSocket()
		{
            //var isV6 = Socket.OSSupportsIPv6;
            var isV6 = false;
			var af = isV6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
			var bnd = isV6 ? IPAddress.IPv6Any : IPAddress.Any;
			socket = new Socket(af, SocketType.Stream, ProtocolType.Tcp);
			socket.Bind(new IPEndPoint(bnd, Port));
		}

		private void CloseSocket()
		{
			if (socket == null) return;
			socket.Close();
			socket = null;
		}
	}

    [AttributeUsage(AttributeTargets.Method,AllowMultiple = true,Inherited = false)]
    public class RequestHandlerAttribute
    {
        public string Path { get; protected set; }
        public RequestHandlerAttribute(string key) { Path = key; }
    }
}
