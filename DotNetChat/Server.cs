using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetChat
{
    public class Server
    {
        private const int MessageBufferSize = 4096;
        private readonly Options options;
        private readonly CancellationTokenSource cancellationTokenSource;

        public Server(Options options)
        {
            this.options = options;
            cancellationTokenSource = new CancellationTokenSource();
        }

        public List<TcpClient> Clients { get; } = new List<TcpClient>();

        public void Listen()
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Any, options.Port);
            tcpListener.Start();
            using (cancellationTokenSource.Token.Register(() => tcpListener.Stop()))
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    var client = tcpListener.AcceptTcpClient();
                    lock (Clients)
                    {
                        Clients.Add(client);
                    }
                    Task.Run(async () => { await ListenForClientMessagesAsync(client); }, cancellationTokenSource.Token);
                }
            }
        }

        private async Task ListenForClientMessagesAsync(TcpClient client)
        {
            Console.WriteLine("Client connected");
            await BroadcastToAllExcept(client, "Client connected");
            try
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    var bytes = new byte[MessageBufferSize];
                    var readCount = await client.GetStream().ReadAsync(bytes, 0, bytes.Length, cancellationTokenSource.Token);
                    if (readCount == 0) break;
                    var line = Encoding.UTF8.GetString(bytes, 0, readCount);
                    Console.WriteLine($"Received {line}  ({readCount})");
                    await BroadcastToAllExcept(client, line);
                }
            }
            catch (IOException)
            {
            }

            Console.WriteLine("Client disconnected");
            await BroadcastToAllExcept(client, "Client disconnected");
            lock (Clients)
            {
                Clients.Remove(client);
            }

            client.Dispose();
        }

        private async Task BroadcastToAllExcept(TcpClient excludedClient, string message)
        {
            TcpClient[] clients;
            lock (Clients)
            {
                clients = Clients.Except(new[] {excludedClient}).ToArray();
            }
            var bytes = Encoding.UTF8.GetBytes(message);
            foreach (var client in clients)
            {
                try
                {
                    await client.GetStream().WriteAsync(bytes, 0, bytes.Length, cancellationTokenSource.Token);
                }
                catch (IOException)
                {
                }
            }
        }

        public void Shutdown()
        {
            cancellationTokenSource.Cancel();
        }
    }
}
