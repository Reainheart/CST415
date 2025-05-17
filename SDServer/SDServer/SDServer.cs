// SDServer.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 
// Noah Etchemedy
// CST 415
// Fall 2025
// 
using System;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace SDServer
{
    class SDServer
    {
        // represents the server and it's logic
        // the server uses the main program thread to listen and accept connections from client
        // when the server accepts a client connection, it will create the client's socket and thread

        private ushort listeningPort;
        private int clientBacklog;
        private SessionTable sessionTable;

        public SDServer(ushort listeningPort, int clientBacklog)
        {
            this.listeningPort = listeningPort;
            this.clientBacklog = clientBacklog;

            // initially empty session table
            sessionTable = new SessionTable();
        }

        public void Start()
        {
            // Create a listening socket
            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                // Bind to any local IP at the specified port
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, listeningPort);
                listenSocket.Bind(endPoint);

                // Listen for incoming connections
                listenSocket.Listen(clientBacklog);
                Console.WriteLine("SD Server is listening on port " + listeningPort);

                bool done = false;
                while (!done)
                {
                    try
                    {
                        // Accept an incoming client connection
                        Socket clientSocket = listenSocket.Accept();
                        Console.WriteLine("Client connected: " + clientSocket.RemoteEndPoint);

                        // Create an SDConnectedClient for handling the client
                        SDConnectedClient client = new SDConnectedClient(clientSocket, sessionTable);
                        client.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error while accepting and starting client: " + ex.Message);
                        Console.WriteLine("Waiting for 5 seconds and trying again...");
                        Thread.Sleep(5000);
                    }
                }

                // Close the listening socket (unreachable in current loop design, but included for safety)
                listenSocket.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal error: " + ex.Message);
            }
        }

    }
}
