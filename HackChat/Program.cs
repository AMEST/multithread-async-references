using System;
using System.Threading;

namespace HackChat;

static class Program
{
	static void Main(string[] args)
	{
		var config = TryParseConfiguration(args);
		var chat = new Chat(config);
		chat.Start();

		Thread.Sleep(-1);

		GC.KeepAlive(chat);
	}

	/// <summary>
	///		Простая парсилка аргументов запуска чата
	/// </summary>
	/// <remarks>
	///		Первый аргумент обязательно порт чата, второй и третий могут быть либо адресс для tcp слушателя либо диапазон ip адресов
	/// </remarks>
	private static Configuration TryParseConfiguration(string[] args)
	{
		var config = new Configuration();
		if (args.Length == 0)
			return config;
		for (var i = 0; i < args.Length; i++)
		{
			if (i == 0 && int.TryParse(args[0], out var port))
			{
				config.Port = port;
				continue;
			}

			if (args[i].Contains('-'))
				config.IpList = IpRangeParser.Parse(args[i]);
			else
				config.BindIp = args[i];
		}

		return config;
	}

}