// FTServerProgram.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 

using PRSLib;
using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;


namespace FTServer
{
    class FTServerProgram
    {
        private static void Usage()
        {
            Console.WriteLine("Usage: FTServer -prs <PRS IP address>:<PRS port>");
        }

        static void Main(string[] args)
        {
            // TODO: FTServerProgram.Main()

            // defaults
            ushort FTSERVER_PORT = 40000;
            int CLIENT_BACKLOG = 5;
            string PRS_ADDRESS = "127.0.0.1";
            ushort PRS_PORT = 30000;
            string SERVICE_NAME = "FT Server";

            // Parse command line args for PRS IP and port
            if (args.Length == 2 && args[0] == "-prs")
            {
                string[] parts = args[1].Split(':');
                if (parts.Length == 2)
                {
                    PRS_ADDRESS = parts[0];
                    if (!ushort.TryParse(parts[1], out PRS_PORT))
                    {
                        Usage();
                        return;
                    }
                }
                else
                {
                    Usage();
                    return;
                }
            }

            Console.WriteLine("PRS Address: " + PRS_ADDRESS);
            Console.WriteLine("PRS Port: " + PRS_PORT);


            Socket prsSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            EndPoint prsEP = new IPEndPoint(IPAddress.Parse(PRS_ADDRESS), PRS_PORT);
            
            try
            {
                PRSMessage requestPort = new PRSMessage(PRSMessage.MESSAGE_TYPE.REQUEST_PORT, SERVICE_NAME, FTSERVER_PORT, PRSMessage.STATUS.SUCCESS);
                requestPort.SendMessage(prsSocket, prsEP);
                requestPort = PRSMessage.ReceiveMessage(prsSocket, ref prsEP);
                if (requestPort.Status != PRSMessage.STATUS.SUCCESS)
                {
                    Console.Write($"{requestPort.Status} : {requestPort.MsgType} : {requestPort.ServiceName}:{requestPort.Port} ");
                }else
                {
                    FTSERVER_PORT = requestPort.Port;
                    Thread keepAliveThread = new Thread(() => SendKeepAlive(prsSocket, prsEP, SERVICE_NAME, FTSERVER_PORT));
                    keepAliveThread.IsBackground = true;
                    keepAliveThread.Start();


                    FTServer server = new FTServer(FTSERVER_PORT, CLIENT_BACKLOG);
                    server.Start();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                prsSocket?.Close();
            }

            Console.WriteLine("Press Enter to exit");
            Console.ReadKey();
        }

        private static void SendKeepAlive(Socket prsSocket, EndPoint prsEP, string serviceName, ushort port)
        {
            while (true)
            {
                try
                {
                    PRSMessage keepAlive = new PRSMessage(
                        PRSMessage.MESSAGE_TYPE.KEEP_ALIVE,
                        serviceName,
                        port,
                        PRSMessage.STATUS.SUCCESS
                    );
                    keepAlive.SendMessage(prsSocket, prsEP);
                    Console.WriteLine($"[KeepAlive] Sent KEEP_ALIVE for {serviceName}:{port}");

                    Thread.Sleep(5000); // wait 5 seconds
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[KeepAlive] Error: " + ex.Message);
                }
            }
        }

    }
}
