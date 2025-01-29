using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NMAP;

public class AsyncScanner : IPScanner
{
    public Task Scan(IPAddress[] ipAddrs, int[] ports)
    {
        var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

        return Parallel.ForEachAsync(ipAddrs, options, async (ipAddr, _) =>
        {
            var pingStatus = await PingAddrAsync(ipAddr);
            if (pingStatus != IPStatus.Success)
                return;

            await Task.WhenAll(ports.Select(port => CheckPortAsync(ipAddr, port)));
        });
    }

    private static async Task<IPStatus> PingAddrAsync(IPAddress ipAddr, int timeout = 3000)
    {
        await Console.Out.WriteLineAsync($"Pinging {ipAddr}");
        
        using var ping = new Ping();
        var reply = await ping.SendPingAsync(ipAddr, timeout);
        await Console.Out.WriteLineAsync($"Pinged {ipAddr}: {reply.Status}");
        
        return reply.Status;
    }

    private static async Task CheckPortAsync(IPAddress ipAddr, int port, int timeout = 3000)
    {
        using var tcpClient = new TcpClient();
        await Console.Out.WriteLineAsync($"Checking {ipAddr}:{port}");

        var portStatus = await tcpClient.ConnectWithTimeoutAsync(ipAddr, port, timeout);
        await Console.Out.WriteLineAsync($"Checked {ipAddr}:{port} - {portStatus}");
    }
}