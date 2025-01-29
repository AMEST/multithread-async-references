using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace HackChat
{
	public static class Extensions
	{
		/// <summary>
		///		Подключение к адресу с таймаутом
		/// </summary>
		public static async Task<Task> ConnectAsync(this TcpClient tcpClient, IPAddress ipAddr, int port, int timeout = 3000)
		{
			var connectTask = tcpClient.ConnectAsync(ipAddr, port);
			await Task.WhenAny(connectTask, Task.Delay(timeout));
			return connectTask;
		}

		/// <summary>
		///		Метод запускающий ping удаленного адреса
		/// </summary>
		public static async Task<IPStatus> PingAsync(this IPAddress ipAddr)
		{
			if (ipAddr.ToString().StartsWith("127.0.0."))
				return IPStatus.Success;
			
			using var ping = new Ping();
			var reply = await ping.SendPingAsync(ipAddr, TimeSpan.FromSeconds(5));
			return reply.Status;
		}

		/// <summary>
		///		Получение локальных ip адресов на интерфейсах
		/// </summary>
		public static string[] GetLocalIps()
		{
			var hostName = Dns.GetHostName();
			var resolve = Dns.GetHostEntry(hostName);
			return resolve.AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork)
				.Select(x => x.ToString()).ToArray();
		}
		
		public static TimeSpan AddRandomSecond(this TimeSpan time, int min = 0, int max = 5)
		{
			return time + TimeSpan.FromMilliseconds(Random.Shared.NextInt64(min * 1000, max * 1000));
		}

	}

	public static class ConsoleHelper
	{
		public static void ColoredWrite(string msg, ConsoleColor color)
		{
			lock(Console.Out)
			{
				Console.ForegroundColor = color;
				Console.Out.WriteLine(msg);
				Console.ResetColor();
			}
		}
	}
}