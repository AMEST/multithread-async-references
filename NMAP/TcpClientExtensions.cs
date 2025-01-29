using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NMAP
{
    public static class TcpClientExtensions
    {
        public static Task ConnectWithTimeout(this TcpClient tcpClient, IPAddress ipAddr, int port, int timeout = 3000)
        {
            var connectTask = tcpClient.ConnectAsync(ipAddr, port);
            Task.WaitAny(connectTask, Task.Delay(timeout));
            return connectTask;
        }

        public static async Task<PortStatus> ConnectWithTimeoutAsync(this TcpClient tcpClient, IPAddress ipAddr, int port, int timeout = 3000)
        {
            var connectTask = tcpClient.ConnectAsync(ipAddr, port);

            await Task.WhenAny(connectTask, Task.Delay(timeout));

            return DecodePortStatus(connectTask);
        }

        private static PortStatus DecodePortStatus(Task connectTask)
        {
            return connectTask.Status switch
            {
                TaskStatus.RanToCompletion => PortStatus.OPEN,
                TaskStatus.Faulted => PortStatus.CLOSED,
                _ => PortStatus.FILTERED
            };
        }

    }
}