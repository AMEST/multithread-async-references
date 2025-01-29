using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HackChat;

public class Chat
{
    public const int DefaultPort = 31337; // Стандартный порт нашего чата
    private readonly Configuration _configuration; // Конфигурации для запуска чата (диапазон поиска, порт и ip для прослушки)
    private readonly IPAddress[] _ips; //Список IP адресов которые будут сканироваться для поиска других участников чата

    private readonly ConcurrentDictionary<IPEndPoint, (TcpClient Client, NetworkStream Stream)>
        _connections = new(); // Словарь для хранения активных соединений

    private readonly TcpListener _tcpListener;// TCP-слушатель для приема входящих соединений

    public Chat(Configuration configuration)
    {
        _configuration = configuration;
        // Инициализация TCP-слушателя с привязкой к выбранному Ip и Port
        _tcpListener = new TcpListener(IPAddress.Parse(configuration.BindIp), configuration.Port);

        // Создаем список локальных ip чтобы не подключатся к самим себе
        // Если слушаем на всех ip, то в список добавляем все локальные Ip V4 
        // Если слушаем на конкретном ip, то добавляем в список исключений только себя
        var localIps = configuration.BindIp == "0.0.0.0"
            ? Extensions.GetLocalIps()
            : [configuration.BindIp];
        // Собираем список IP адресов за исключением локальных и создаем объекты IPAddress
        _ips = _configuration.IpList.Where(x => !localIps.Contains(x)).Select(IPAddress.Parse).ToArray();
    }

    public void Start()
    {
        // Запускаем "Поток" обнаружения узлов
        Task.Run(DiscoverLoop);
        // Отдельный "поток" пользовательского ввода
        Task.Run(() =>
        {
            while (Console.ReadLine() is { } line) // Читаем ввод из консоли 
                Task.Run(() => BroadcastAsync(line)); // Отправляем всем остальным клиентам
        });
        // Поток обработки подклчюений
        Task.Run(() =>
        {
            // Запускаем наш Tcp слушатель и указываем, очень большой размер очереди ожидающих подключений
            _tcpListener.Start(100500);
            ConsoleHelper.ColoredWrite($"[Start listening on {_configuration.BindIp}:{_configuration.Port}]",
                ConsoleColor.Green);
            while (true) // В бесконечном цикле
            {
                var tcpClient = _tcpListener.AcceptTcpClient(); // Ждем новое подключение
                Task.Run(() => ProcessClientAsync(tcpClient)); // Запускаем обработку в отдельном "потоке" и возвращаемся ждать подключения
            }
        });
    }

    private async Task BroadcastAsync(string message)
    {
        // Пребразуем наше сообщение в массив байт
        var messageBytes = Encoding.UTF8.GetBytes(message + Environment.NewLine);
        // Отправляем их в нескольких потоках подключенным клиентам
        await Parallel.ForEachAsync(_connections, async (connection, token) =>
        {
            try
            {
                await connection.Value.Stream.WriteAsync(messageBytes, token); 
            }
            catch { /* ignore */ }
        });
    }

    private async void DiscoverLoop()
    {
        // Бесконечный цикл поиска новых узлов
        while (true)
        {
            try { await Discover(); }catch { /* ignored */ }
            await Task.Delay(TimeSpan.FromSeconds(3).AddRandomSecond(2, 13)); // Пауза с небольшой долей случайности (разносим паузы поиска у всех)
        }
    }

    private async Task Discover()
    {
        await Parallel.ForEachAsync(_ips, async (ip, token) =>
        {
            // Пропускаем тех, кто уже подключен (при работае локально, может быть ситуация, что подключение прошло по "ipv6 ::ffff:127.0.0.1", обработаем и это
            if (_connections.Keys.Any(x => x.Address.ToString().Split(":").Last() == ip.ToString()))
                return;

            // Проверяем, доступен ли узел (для 127.0.0.x всегда будет Success)
            var ping = await ip.PingAsync();
            if (ping != IPStatus.Success)
                return;

            // Создаем Tcp клиента и привязываем его к конкретному ip (интерфейсу).
            // Исправляет ситуацию запуска на одном хосте экземпляров на разных LoopBack адресах
            var tcpClient = new TcpClient(new IPEndPoint(IPAddress.Parse(_configuration.BindIp), GetFreePort()));
            var connectionTask = await tcpClient.ConnectAsync(ip, _configuration.Port, 1000); // Коннектимся с тайиаутом, чтобы не ждать все вечно
            if (connectionTask.Status != TaskStatus.RanToCompletion)
            {
                tcpClient.Dispose(); // Закрываем нашего клиента, если коннект не удался
                return;
            }

            // Делаем повторную проверку, вдруг мы уже подключились или к нам подключились
            if (_connections.Keys.Any(x => x.Address.ToString().Split(":").Last() == ip.ToString()))
            {
                tcpClient.Dispose();
                return;
            }

            var endpoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
            ConsoleHelper.ColoredWrite($"[{endpoint}] Founded {endpoint.Address} via {ip}",
                ConsoleColor.Yellow);
            Task.Run(() => ProcessClientAsync(tcpClient)); // Запускаем обработку соединения в отдельном "потоке"
        });
    }

    /// <summary>
    ///     Обработка подключение (прослушка приходящих сообщений, открытие/закрытие потока чтения)
    /// </summary>
    private async Task ProcessClientAsync(TcpClient tcpClient)
    {
        IPEndPoint endpoint = null;
        // Пытаемся определить удаленное подключение (ip:port)
        try { endpoint = tcpClient.Client.RemoteEndPoint as IPEndPoint; }catch { /* ignored */ }

        ConsoleHelper.ColoredWrite($"[{endpoint}] connected", ConsoleColor.Green);
        try
        {
            // оборачиваем нашего клиента в using, чтобы после завершения или ошибки, автоматичеки закрыть соединение, network stream и высвободить ресурсы
            using (tcpClient) 
            {
                var stream = tcpClient.GetStream(); // Получаем сетевой поток
                _connections.TryAdd(endpoint, (tcpClient, stream)); // Добавляем в список клиентов новое соединение
                await ReadLinesToConsoleAsync(stream); // Запускаем обработку входящих данных (поулчение и вывод в консоль)
            }
        }
        catch { /* ignored */ }
        // Если мы вышли, значит произошел Disconnect/Ошибка, удаляем подключение из списка
        _connections.TryRemove(endpoint, out _);

        ConsoleHelper.ColoredWrite($"[{endpoint}] disconnected", ConsoleColor.DarkRed);
    }

    /// <summary>
    ///     Обработка входящего потока и вывод в консоль
    /// </summary>
    /// <param name="stream"></param>
    private static async Task ReadLinesToConsoleAsync(Stream stream)
    {
        using var sr = new StreamReader(stream);
        while (await sr.ReadLineAsync() is { } line) // Читаем асинхронно из потока (если ничего нет, просто ждем I/O)
            await Console.Out.WriteLineAsync($"[{((NetworkStream)stream).Socket.RemoteEndPoint}] {line}");
    }
        
    /// <summary>
    ///     Метод получения свободного порта
    /// </summary>
    private static int GetFreePort()
    {
        // Запускаем TcpListener на 0 порту, тогда система сама выберет нам доступный порт
        var listener = new TcpListener(IPAddress.Loopback, 0);
        try
        {
            listener.Start(); // Запускаем, в этот момент нам выдается порт
            return ((IPEndPoint)listener.LocalEndpoint).Port; // Получаем порт и отдаем его
        }
        finally
        {
            listener.Stop(); // Как только мы вышли из метода (return) выполняется блок finally и останавливается слушатель с освобождением порта
        }
    }
}