using SDServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SDLib
{
    // represents the server and it's logic
    // the server uses the main program thread to listen and accept connections from client
    // when the server accepts a client connection, it will create the client's socket and thread
    public class SimpleDocumentService
    {
        private readonly ushort _listeningPort;
        private readonly int _clientBacklog;
        private readonly SessionTable _sessionTable = new SessionTable();
        private Socket? _listener;

        public SimpleDocumentService(ushort listeningPort, int clientBacklog)
        {
            _listeningPort = listeningPort;
            _clientBacklog = clientBacklog;
        }

        public void Start()
        {
            // Initialize a TCP socket
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(new IPEndPoint(IPAddress.Any, _listeningPort));
            _listener.Listen(_clientBacklog);

            Console.WriteLine($"SD Server listening (via Socket) on port {_listeningPort}...");

            while (true)
            {
                try
                {
                    // Accept an incoming connection (blocking)
                    Socket clientSocket = _listener.Accept();
                    Console.WriteLine($"Accepted connection from {((IPEndPoint)clientSocket.RemoteEndPoint).Address}:{((IPEndPoint)clientSocket.RemoteEndPoint).Port}");

                    var clientHandler = new SDConnectedClient(clientSocket, _sessionTable);
                    clientHandler.Start();
                }
                catch (SocketException se)
                {
                    Console.WriteLine($"Socket error accepting client: {se.Message}");
                    Thread.Sleep(5000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while accepting client: {ex.Message}");
                    Thread.Sleep(5000);
                }
            }
        }

        public void Stop()
        {
            Console.WriteLine("Stopping SD Server...");
            try
            {
                _listener?.Shutdown(SocketShutdown.Both);
                _listener?.Close();
                _listener = null;
            }
            catch { }

            _sessionTable.Dispose();
        }

    }
}
