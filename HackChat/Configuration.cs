using System.Collections.Generic;

namespace HackChat;

public class Configuration
{
    /// <summary>
    ///     Порт на котором слушает наше приложение и ищет в сети чаты с таким же портом
    /// </summary>
    public int Port { get; set; } = Chat.DefaultPort;
    
    /// <summary>
    ///     Ip адрес на котором будет слушать наш чат
    /// </summary>
    public string BindIp { get; set; } = "0.0.0.0";
    
    /// <summary>
    ///     Диапазон ip адресов которые будут опрашиваться для поиска других клиентов
    /// </summary>
    public List<string> IpList { get; set; } = CreateDefault();

    private static List<string> CreateDefault()
    {
        var defaultSearchRange = new List<string>();
        defaultSearchRange.AddRange(IpRangeParser.Parse("127.0.0.1-127.0.0.255"));
        defaultSearchRange.AddRange(IpRangeParser.Parse("192.168.0.1-192.168.1.255"));
        return defaultSearchRange;
    }
}