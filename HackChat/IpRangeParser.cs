using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace HackChat;

/// <summary>
///  Парсер (максимально простой, но не оптимальный) для преобразования ip range (string) в массив адресов
/// </summary>
public static class IpRangeParser
{
    public static List<string> Parse(string ipRange)
    {
        var ipAddresses = new List<string>();

        // Разделяем диапазон на начальный и конечный IP-адреса
        var parts = ipRange.Split('-');
        if (parts.Length != 2)
            throw new ArgumentException("Invalid IP range format");

        // Берем ip адрес начала диапазона
        var startIp = IPAddress.Parse(parts[0]);
        // И ip адрес конца диапазона
        var endIp = IPAddress.Parse(parts[1]);

        // Берем их представление в массиве байт
        var startIpBytes = startIp.GetAddressBytes();
        var endIpBytes = endIp.GetAddressBytes();

        if (startIpBytes.Length != 4 || endIpBytes.Length != 4)
            throw new ArgumentException("IP addresses must be IPv4");

        // Делаем копию адреса начала диапазона для модификации
        var currentIpBytes = (byte[])startIpBytes.Clone();

        // Перебираем адреса и добавляем в массив пока не достигнем конца диапазона
        while (true)
        {
            // Добавляем в массив адресов ip4 адрес из текущего набора байт
            ipAddresses.Add(new IPAddress(currentIpBytes).ToString());
            // перебираем последний байт пока не дойдем до конца 
            if (currentIpBytes[3] < 255)
                currentIpBytes[3]++;
            else if (currentIpBytes[2] < 255) // если дошли до конца 4 байта, увеличиваем 3 байт и обнуляем 4
            {
                currentIpBytes[3] = 0;
                currentIpBytes[2]++;
            }
            else if (currentIpBytes[1] < 255) // Если дошли до конца 3 байта, обнуляем 3 и 4, увеличиваем 2
            {
                currentIpBytes[3] = 0;
                currentIpBytes[2] = 0;
                currentIpBytes[1]++;
            }
            else if (currentIpBytes[0] < 255) // Если дошли до конца 2 байта, обнуляем 2, 3 и 4, увеличиваем 1
            {
                currentIpBytes[3] = 0;
                currentIpBytes[2] = 0;
                currentIpBytes[1] = 0;
                currentIpBytes[0]++;
            }
            else
                break;
            // Текущий набор байтов адреса равен набору байтов конца диапазона, если нет, то продолжаем перебор
            if (!currentIpBytes.SequenceEqual(endIpBytes)) continue;
            // Добавляем последний адрес и выходим из цикла
            ipAddresses.Add(new IPAddress(currentIpBytes).ToString());
            break;
        }

        return ipAddresses;
    }
}