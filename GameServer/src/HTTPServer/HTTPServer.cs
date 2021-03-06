using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Logginf;
using SimpleHttpServer;
using SimpleHttpServer.Models;

namespace FoolOnlineServer.HTTPServer {
	public class HTTPServer : HttpServer {
		private static HTTPServer _instance;

	    public static int Port;

		private HTTPServer(int port, List<Route> routes) : base(port, routes) { }


		public static void StartServer(int port)
		{
		    Port = port;

			log4net.Config.XmlConfigurator.Configure();

			_instance = new HTTPServer(port, Routes.GET);
			Thread thread = new Thread(_instance.Listen);
			thread.Start();

			Log.WriteLine("HTTP server is started on port " + port, _instance);
		}
	}
}