using System;
using System.Net.Sockets;
using System.Threading;

namespace TinyHttpServer
{
	public class Client
	{
		protected Socket Remote;
		protected Server Server;

		public Client(Socket remote, Server server)
		{
			Remote = remote;
			Server = server;
		}

		public void Start()
		{
			var thread = new Thread(ProcessRequest);
			thread.IsBackground = true;
			thread.Start();
		}

		private void ProcessRequest()
		{
			ReadRequestData();
			//var handler = FindRegisteredHandler();
			//if (handler == null) handler = DefaultErrorHandler;
			//handler();
		}

		private void ReadRequestData()
		{
			throw new NotImplementedException();
		}
	}
}