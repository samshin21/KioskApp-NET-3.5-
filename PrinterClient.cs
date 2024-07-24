using System;
using System.Net.Sockets;
using System.Text;

namespace ThermalPrinterNetworkExample
{
    public class PrinterClient : IDisposable
    {
        private readonly string printerIpAddress;
        private readonly int printerPort;
        private TcpClient client;
        private NetworkStream stream;

        public PrinterClient(string ipAddress, int port)
        {
            printerIpAddress = ipAddress;
            printerPort = port;
        }

        public void Connect()
        {
            client = new TcpClient();
            client.Connect(printerIpAddress, printerPort);
            stream = client.GetStream();
        }

        public void WriteString(string data)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(data);
            stream.Write(bytes, 0, bytes.Length);
        }

        public void WriteBytes(params byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }

        public void Dispose()
        {
            stream?.Close();
            client?.Close();
        }
    }
}
