﻿// FTServer.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 
// Noah Etchemendy
// CST 415
// Spring 2025

using System;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using PRSLib;

namespace FTServer
{
    class FTServer
    {
        // represents the server and it's logic
        // the server uses the main program thread to listen and accept connections from client
        // when the server accepts a client connection, it will create the client's socket and thread

        private ushort listeningPort;
        private int clientBacklog;

        public FTServer(ushort listeningPort, int clientBacklog)
        {
            this.listeningPort = listeningPort;
            this.clientBacklog = clientBacklog;
        }

        public void Start()
        {

            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, listeningPort));
            serverSocket.Listen(clientBacklog);

            Console.WriteLine($"[FTServer] Listening for client connections on port {listeningPort}...");

            bool done = false;
            while (!done)
            {
                try
                {
                    // Accept incoming client connection (blocking)
                    Socket clientSocket = serverSocket.Accept();
                    Console.WriteLine($"[FTServer] Accepted connection from {clientSocket.RemoteEndPoint}");

                    // Step 3: Handle client in a new thread
                    Thread clientThread = new Thread(() => HandleClient(clientSocket));
                    clientThread.Start();

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error while accepting and starting client: " + ex.Message);
                    Console.WriteLine("Waiting for 5 seconds and trying again...");
                    Thread.Sleep(5000);
                }
            }

            // close socket and quit
            serverSocket.Close();
        }

        private void HandleClient(Socket clientSocket)
        {
            Console.WriteLine($"[FTServer] Accepted connection from {clientSocket.RemoteEndPoint}");
            FTConnectedClient client = new FTConnectedClient(clientSocket);
            client.Start();
            Console.WriteLine("[FTServer] Client thread started.");
        }

    }
}
