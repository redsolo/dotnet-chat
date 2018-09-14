using System;
using System.Linq;
using System.Net.NetworkInformation;

namespace DotnetChat
{
    public static class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();

            if (IsServerRunning(options))
            {
                Console.WriteLine("Starting client (exit with enter or CTRL+C)");
                var client = new Client(options);
                client.Connect();
                Console.CancelKeyPress += (sender, eventArgs) => client.Shutdown();
                var line = Console.ReadLine();
                while (!string.IsNullOrEmpty(line))
                {
                    client.Send(line);
                    line = Console.ReadLine();
                }
                client.Shutdown();
            }
            else
            {
                Console.WriteLine("Starting server (exit with CTRL+C)");
                var server = new Server(options);
                Console.CancelKeyPress += (sender, eventArgs) => server.Shutdown();
                server.Listen();
            }
        }
        
        private static bool IsServerRunning(Options options)
        {
            return IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(l => l.Port == options.Port);
        }
    }
}
