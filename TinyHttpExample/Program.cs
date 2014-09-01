using System;
using System.Collections.Generic;
using System.Text;
using TinyHttpServer;

namespace TinyHttpExample
{
	class Program
	{
		static void Main(string[] args)
		{
            Server srv = new Server(5080);
            srv.Start();
            Console.ReadLine();
            srv.Stop();
		}
	}
}
