using SDServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SDLib
{
    public class SDService
    {
        // represents the server and it's logic
        // the server uses the main program thread to listen and accept connections from client
        // when the server accepts a client connection, it will create the client's socket and thread

        private ushort listeningPort;
        private int clientBacklog;
        private SessionTable sessionTable;

        public SDService(ushort listeningPort, int clientBacklog)
        {
            this.listeningPort = listeningPort;
            this.clientBacklog = clientBacklog;

            // initially empty session table
            sessionTable = new SessionTable();
        }


        public void Start()
        {
            TcpListener listener = new TcpListener(System.Net.IPAddress.Any, listeningPort);
            listener.Start(clientBacklog);
            Console.WriteLine($"SD Server listening on port {listeningPort}...");

            while (true)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();  // blocking call
                    Console.WriteLine($"Accepted connection from {client.Client.RemoteEndPoint}");

                    // Start a new task to handle the client
                    Task.Run(() => HandleClientAsync(client));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error while accepting client: " + ex.Message);
                    Thread.Sleep(5000);  // recoverable failure, wait before retrying
                }
            }
        }
        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Console.WriteLine($"Received: {request}");

                // Simulated logic
                ulong sessionId = await sessionTable.OpenSessionAsync();
                Console.WriteLine($"Opened session {sessionId}");

                // Respond to client (example message)
                string response = $"Session {sessionId} opened.";
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in client handler: " + ex.Message);
            }
            finally
            {
                client.Close();
            }
        }

    }
}
