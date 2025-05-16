// FTServer.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 

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
            // TODO: FTServer.Start()

            // create a listening socket for clients to connect
            Socket listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // bind to the listening port
            listeningSocket.Bind(new IPEndPoint(IPAddress.Any, listeningPort));

            // set the socket to listen for incoming connections
            listeningSocket.Listen(clientBacklog);
            Console.WriteLine("FTServer listening on port " + listeningPort);
            Console.WriteLine("Waiting for incoming connections...");

            // create a socket to connect to the PRS server
            

            // create an endpoint for the PRS server

            // contact the PRS and lookup port for "FT Server"


            // create an endpoint for the FT Server

            // create a socket to listen for incoming connections

            // create a socket to connect to the FT Server

            // create an endpoint for the FT Server



            // bind to the FT Server port
            // set the socket to listen

            //bool done = false;
            //while (!done)
            {
                try
                {
                    // accept a client connection
                    
                    // instantiate connected client to process messages
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error while accepting and starting client: " + ex.Message);
                    Console.WriteLine("Waiting for 5 seconds and trying again...");
                    Thread.Sleep(5000);
                }
            }

            // close socket and quit
            
        }
    }
}
