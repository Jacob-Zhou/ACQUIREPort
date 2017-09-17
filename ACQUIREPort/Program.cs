using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace ACQUIREPort
{
	class Program
	{
		static void Main(string[] args)
		{
			PortServer server = new PortServer();
			server.start();
			while (true)
			{
				Thread.Sleep(1000);
			}
		}
	}
}
