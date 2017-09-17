using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace ACQUIREPort
{
	public class RoomInfomation
	{
		public RoomInfomation() { }

		public RoomInfomation(string uid, int port, string name, bool needPassword, string password, int maxPlayerCount, int playerCount)
		{
			Uid = uid;
			this.port = port;
			this.name = name;
			this.needPassword = needPassword;
			this.password = password;
			this.maxPlayerCount = maxPlayerCount;
			this.playerCount = playerCount;
		}

		public string Uid { get; set; }
		public Int32 port { get; set; }
		public string name { get; set; }
		public bool needPassword { get; set; }
		public string password { get; set; }
		public int maxPlayerCount { get; set; }
		public int playerCount { get; set; }
	}

	public class ServerResponse
	{
		public int port { get; set; }
		public string password { get; set; }
	}

	class PortServer
	{
		private HttpListener httpListener = new HttpListener();
		private Thread listenerThread;
		private Dictionary<string, RoomInfomation> rooms = new Dictionary<string, RoomInfomation>();
		private bool isStart = false;

		public bool IsStart
		{
			get
			{
				return isStart;
			}
		}

		public void start()
		{
			isStart = true;
			httpListener.Prefixes.Add("http://*:56000/");
			httpListener.Start();
			listenerThread = new Thread(() =>
			{
				while (isStart)
				{
					HttpListenerContext context = httpListener.GetContext();
					Console.WriteLine(context.Request.RawUrl);
					Console.WriteLine(context.Request.HttpMethod);
					if (context.Request.RawUrl.ToLower() == "/aquire?require=create")
					{
						var istream = context.Request.InputStream;
						StreamReader SR = new StreamReader(istream);
						for (int port = 56001; port < 63000; port++)
						{
							if (!isListened(port))
							{
								var roomInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(SR.ReadLine());
								Console.WriteLine(roomInfo);
								rooms[port.ToString()] = new RoomInfomation(port.ToString(), port, roomInfo["name"], bool.Parse(roomInfo["needPassword"]), roomInfo["password"], int.Parse(roomInfo["maxPlayerCount"]), 1);
								Process myPro = new Process();
								myPro.StartInfo.FileName = "cmd.exe";
								myPro.StartInfo.UseShellExecute = false;
								myPro.StartInfo.RedirectStandardInput = true;
								myPro.StartInfo.RedirectStandardOutput = true;
								myPro.StartInfo.RedirectStandardError = true;
								myPro.StartInfo.CreateNoWindow = true;
								myPro.Start();
								myPro.StandardInput.WriteLine("ACQUIREServer {0} {1}", port.ToString(), int.Parse(roomInfo["maxPlayerCount"]));
								myPro.StandardInput.AutoFlush = true;
								context.Response.StatusCode = (int)HttpStatusCode.OK;
								using (StreamWriter writer = new StreamWriter(context.Response.OutputStream))
								{
									writer.WriteLine(JsonConvert.SerializeObject(new { port = port, password = roomInfo["password"] }));
								}
								break;
							}
						}
					}
					else if (context.Request.RawUrl.ToLower() == "/aquire?require=select")
					{
						var istream = context.Request.InputStream;
						StreamReader SR = new StreamReader(istream);
						int port = int.Parse(SR.ReadLine());
						lock (rooms)
						{
							using (StreamWriter writer = new StreamWriter(context.Response.OutputStream))
							{
								if (rooms[port.ToString()].playerCount < rooms[port.ToString()].maxPlayerCount)
								{
									context.Response.StatusCode = (int)HttpStatusCode.OK;
									rooms[port.ToString()].playerCount++;
									writer.Write(true);
								}
								else
								{
									context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
									rooms.Remove(port.ToString());
									writer.Write(false);
								}
							}
						}
					}
					else if (context.Request.RawUrl.ToLower() == "/aquire?require=search")
					{
						var roomsStr = JsonConvert.SerializeObject(rooms.Values);
						context.Response.StatusCode = (int)HttpStatusCode.OK;
						using (StreamWriter writer = new StreamWriter(context.Response.OutputStream))
						{
							writer.WriteLine(roomsStr);
						}
					}
				}
			});
			listenerThread.Start();
		}

		private bool isListened(int port)
		{
			IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
			IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();

			return (ipEndPoints.Where(e => e.Port == port).Count() > 0);
		}

		public void stop()
		{
			isStart = false;
			listenerThread.Abort();
		}
	}
}
