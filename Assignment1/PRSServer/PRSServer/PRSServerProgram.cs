// PRSServerProgram.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using PRSLib;

namespace PRSServer
{
    class PRSServerProgram
    {
        class PRS
        {
            // represents a PRS Server, keeps all state and processes messages accordingly

            class PortReservation
            {
                private ushort port;
                private bool available;
                private string serviceName;
                private DateTime lastAlive;

                public PortReservation(ushort port)
                {
                    this.port = port;
                    available = true;
                    serviceName = string.Empty;
                    lastAlive = DateTime.MinValue; // Not alive until reserved
                }


                public string ServiceName => serviceName;
                public ushort Port => port;
                public bool Available => available;

                public bool Expired(int timeout)
                {
                    // return true if timeout seconds have elapsed since lastAlive

                    return (DateTime.Now - lastAlive).TotalSeconds > timeout;
                }

                public void Reserve(string serviceName)
                {
                    if (available)
                    {
                        this.serviceName = serviceName;
                        available = false;
                        lastAlive = DateTime.Now;
                    }
                }

                public void KeepAlive()
                {
                    if (!available)
                    {
                        lastAlive = DateTime.Now;
                    }
                }

                public void Close()
                {
                    serviceName = string.Empty;
                    available = true;
                    lastAlive = DateTime.MinValue;
                }
            }

            // server attribues
            private ushort startingClientPort;
            private ushort endingClientPort;
            private int keepAliveTimeout;
            private int numPorts;
            private PortReservation[] ports;
            private bool stopped;

            public PRS(ushort ServicePort, ushort startingClientPort, ushort endingClientPort, int kEEP_ALIVE_TIMEOUT)
            {
                // TODO: PRS.PRS()

                // save parameters
                this.startingClientPort = startingClientPort;
                this.endingClientPort = endingClientPort;
                this.keepAliveTimeout = kEEP_ALIVE_TIMEOUT;

                // initialize to not stopped
                this.stopped = false;

                // initialize port reservations
                numPorts = endingClientPort - startingClientPort + 1;
                ports = new PortReservation[numPorts];
                for (ushort i = 0; i < numPorts; i++)
                {
                    ports[i] = (new PortReservation((ushort)(startingClientPort+i)));
                }
            }

            public bool Stopped { get { return stopped; } }

            private void CheckForExpiredPorts()
            {
                // Go through each port and expire it if necessary
                foreach (PortReservation port in ports)
                {
                    if (!port.Available && port.Expired(keepAliveTimeout))
                    {
                        Console.WriteLine($"[INFO] Expiring service '{port.ServiceName}' on port {port.Port} due to timeout.");
                        port.Close();
                    }
                }
            }

            private PRSMessage RequestAvaliablePort(string serviceName)
            {
                // client has requested the lowest available port, so find it!
                for (int i = 0; i < numPorts; i++)
                {
                    // if found an avialable port, reserve it and send SUCCESS
                    if (ports[i].Available)
                    {
                        // found an available port, reserve it
                        ports[i].Reserve(serviceName);
                        return new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, serviceName, ports[i].Port, PRSMessage.STATUS.SUCCESS);
                    }
                }
                // else, none available, send ALL_PORTS_BUSY
                return new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, serviceName, 0, PRSMessage.STATUS.ALL_PORTS_BUSY);
            }

            private PRSMessage RequestPort(string serviceName)
            {
                // Check if this service is already registered
                for (int i = 0; i < numPorts; i++)
                {
                    if (!ports[i].Available && ports[i].ServiceName == serviceName)
                    {
                        // Service already using a port — reject the request
                        return new PRSMessage(
                            PRSMessage.MESSAGE_TYPE.RESPONSE,
                            serviceName,
                            ports[i].Port,
                            PRSMessage.STATUS.SERVICE_IN_USE
                        );
                    }
                }

                return RequestAvaliablePort(serviceName);
            }

            public PRSMessage HandleMessage(PRSMessage msg)
            {
                // TODO: PRS.HandleMessage()
                CheckForExpiredPorts();
                // handle one message and return a response
                Console.WriteLine($"{DateTime.Now}: Received {msg.MsgType} from {msg.ServiceName} on port {msg.Port}");

                PRSMessage response = null;

                switch (msg.MsgType)
                {
                    case PRSMessage.MESSAGE_TYPE.REQUEST_PORT:
                        {
                            // check for expired ports and send requested report
                            CheckForExpiredPorts();
                            response = RequestPort(msg.ServiceName);
                        }
                        break;

                    case PRSMessage.MESSAGE_TYPE.KEEP_ALIVE:
                        {
                            // client has requested that we keep their port alive
                            // find the port
                            var portEntry = ports.FirstOrDefault(p => p.Port == msg.Port);

                            if (portEntry != null && portEntry.Available)
                            {
                                // port is available, send SERVICE_NOT_FOUND
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SERVICE_NOT_FOUND);
                            }
                            else if (portEntry != null && portEntry.ServiceName == msg.ServiceName)
                            {
                                // port is not available, keep it alive and send SUCCESS
                                portEntry.KeepAlive();
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SUCCESS);
                            }
                            else
                            {
                                // port not found at all, send SERVICE_NOT_FOUND
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SERVICE_NOT_FOUND);
                            }
                            break;

                        }

                    case PRSMessage.MESSAGE_TYPE.CLOSE_PORT:
                        {
                            // client has requested that we close their port, and make it available for others!
                            // find the port
                            var portEntry = ports.FirstOrDefault(p => p.Port == msg.Port);

                            if (portEntry == null)
                            {
                                return new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SERVICE_NOT_FOUND);
                            }

                            if (portEntry.Available || portEntry.Port == 0 || portEntry == null)
                            {
                                // port doesn't exist or is already available — send SERVICE_NOT_FOUND
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SERVICE_NOT_FOUND);
                            }
                            else if (portEntry.ServiceName == msg.ServiceName)
                            {
                                // port is in use, close it and send SUCCESS
                                portEntry.Close();
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SUCCESS);
                            }
                            else
                            {
                                // port is in use — and send SUCCESS
                                portEntry.Close();
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SUCCESS);
                            }

                        }
                        break;

                    case PRSMessage.MESSAGE_TYPE.LOOKUP_PORT:
                        {
                            // client wants to know the reserved port number for a named service
                            // find the port
                            foreach (PortReservation port in ports)
                            {
                                if (port.ServiceName == msg.ServiceName)
                                {
                                    // found the port, send it back
                                    response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, port.Port, PRSMessage.STATUS.SUCCESS);
                                    break;
                                }
                            }
                            // if found, send port number back
                            // else, SERVICE_NOT_FOUND
                            if (response == null)
                            {
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, 0, PRSMessage.STATUS.SERVICE_NOT_FOUND);
                            }
                        }
                        break;

                    case PRSMessage.MESSAGE_TYPE.STOP:
                        {
                            // client is telling us to close the appliation down
                            // stop the PRS and return SUCCESS
                            stopped = true;

                            response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, 0, PRSMessage.STATUS.SUCCESS);
                        }
                        break;
                }

                return response;
            }

        }

        static void Usage()
        {
            Console.WriteLine("usage: PRSServer [options]");
            Console.WriteLine("\t-p < service port >");
            Console.WriteLine("\t-s < starting client port number >");
            Console.WriteLine("\t-e < ending client port number >");
            Console.WriteLine("\t-t < keep alive time in seconds >");
        }

        static void Main(string[] args)
        {
            // TODO: PRSServerProgram.Main()

            // defaults
            ushort SERVER_PORT = 30000;
            ushort STARTING_CLIENT_PORT = 40000;
            ushort ENDING_CLIENT_PORT = 40099;
            int KEEP_ALIVE_TIMEOUT = 10;

            // process command options
            // -p < service port >
            // -s < starting client port number >
            // -e < ending client port number >
            // -t < keep alive time in seconds >
            // Process command options
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-p":
                        SERVER_PORT = ushort.Parse(args[++i]);
                        break;
                    case "-s":
                        STARTING_CLIENT_PORT = ushort.Parse(args[++i]);
                        break;
                    case "-e":
                        ENDING_CLIENT_PORT = ushort.Parse(args[++i]);
                        break;
                    case "-t":
                        KEEP_ALIVE_TIMEOUT = int.Parse(args[++i]);
                        break;
                }
            }

            // check for valid STARTING_CLIENT_PORT and ENDING_CLIENT_PORT
            if (STARTING_CLIENT_PORT > ENDING_CLIENT_PORT)
            {
                Console.WriteLine("Error: Starting client port must be less than or equal to ending client port.");
                return;
            }
            // initialize the PRS server
            PRS prs = new PRS(SERVER_PORT, STARTING_CLIENT_PORT, ENDING_CLIENT_PORT, KEEP_ALIVE_TIMEOUT);

            // create the socket for receiving messages at the server
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            EndPoint serverEndPoint = new IPEndPoint(IPAddress.Any, SERVER_PORT);


            

            // bind the listening socket to the PRS server port
            serverSocket.Bind(serverEndPoint);
            Console.WriteLine($"PRS server listening on port {SERVER_PORT}");
            
            //
            // Process client messages
            //

            while (!prs.Stopped)
            {
                try
                {
                    // receive a message from a client
                    PRSMessage message = PRSMessage.ReceiveMessage(serverSocket, ref serverEndPoint);

                    // let the PRS handle the message
                    PRSMessage response = prs.HandleMessage(message);

                    // send response message back to client

                    if (response != null)
                    {
                        // send the response message back to the client
                        response.SendMessage(serverSocket, serverEndPoint);
                    }   
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling message: {ex.Message}");

                    // attempt to send a UNDEFINED_ERROR response to the client, if we know who that was
                    if (serverEndPoint != null)
                    {
                        try
                        {
                            PRSMessage errorResponse = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, "", SERVER_PORT, PRSMessage.STATUS.UNDEFINED_ERROR);
                            errorResponse.SendMessage(serverSocket, serverEndPoint);
                        }
                        catch (Exception sendEx)
                        {
                            Console.WriteLine($"Failed to send error response: {sendEx.Message}");
                        }
                    }
                }
            }

            // close the listening socket
            serverSocket.Close();
            // wait for a keypress from the user before closing the console window
            Console.WriteLine("Press Enter to exit");
            Console.ReadKey();
        }
    }
}
