using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetChat
{
    public class Client
    {
        private const int MessageBufferSize = 4096;
        private readonly Options options;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly TcpClient tcpClient;

        public Client(Options options)
        {
            this.options = options;
            cancellationTokenSource = new CancellationTokenSource();
            tcpClient = new TcpClient();
        }

        public void Connect()
        {
            tcpClient.Connect(options.Hostname, options.Port);
            Task.Run(async () =>
            {
                var bytes = new byte[MessageBufferSize];
                try
                {
                    while (!cancellationTokenSource.IsCancellationRequested)
                    {
                        var count = await tcpClient.GetStream().ReadAsync(bytes, 0, bytes.Length, cancellationTokenSource.Token);
                        Console.WriteLine(Encoding.UTF8.GetString(bytes, 0, count));
                    }
                }
                catch (OperationCanceledException) { }
            });
        }

        public void Shutdown()
        {
            cancellationTokenSource.Cancel();
            tcpClient.Dispose();
        }

        public void Send(string line)
        {
            var bytes = Encoding.UTF8.GetBytes(line);
            tcpClient.GetStream().Write(bytes, 0, bytes.Length);
        }
    }
}
