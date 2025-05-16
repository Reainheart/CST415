// FTClientProgram.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using PRSLib;

namespace FTClient
{
    class FTClientProgram
    {
        private static void Usage()
        {
            /*
                -prs <PRS IP address>:<PRS port>
                -s <file transfer server IP address>
                -d <directory requested>
            */
            Console.WriteLine("Usage: FTClient -d <directory> [-prs <PRS IP>:<PRS port>] [-s <FT Server IP>]");
        }

        static void Main(string[] args)
        {
            // TODO: FTClientProgram.Main()

            // defaults
            string PRSSERVER_IPADDRESS = "127.0.0.1";
            ushort PSRSERVER_PORT = 30000;
            string FTSERVICE_NAME = "FT Server";
            string FTSERVER_IPADDRESS = "127.0.0.1";
            ushort FTSERVER_PORT = 40000;
            string DIRECTORY_NAME = null;

            // process the command line arguments
            Console.WriteLine("PRS Address: " + PRSSERVER_IPADDRESS);
            Console.WriteLine("PRS Port: " + PSRSERVER_PORT);
            Console.WriteLine("FT Server Address: " + FTSERVER_IPADDRESS);
            Console.WriteLine("Directory: " + DIRECTORY_NAME);

            try
            {
                // check for command line arguments
                if (args.Length == 0)
                {
                    Usage();
                    return;
                }

                // parse the command line arguments
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-prs")
                    {
                        string[] parts = args[i + 1].Split(':');
                        if (parts.Length == 2)
                        {
                            PRSSERVER_IPADDRESS = parts[0];
                            if (!ushort.TryParse(parts[1], out PSRSERVER_PORT))
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
                    else if (args[i] == "-s")
                    {
                        FTSERVER_IPADDRESS = args[i + 1];
                    }
                    else if (args[i] == "-d")
                    {
                        DIRECTORY_NAME = args[i + 1];
                    }
                }
                // check if the directory exists
                if (!Directory.Exists(DIRECTORY_NAME))
                {
                    Console.WriteLine("Directory " + DIRECTORY_NAME + " does not exist.");
                    return;
                }
                // check if the directory is empty
                if (Directory.GetFiles(DIRECTORY_NAME).Length == 0)
                {
                    Console.WriteLine("Directory " + DIRECTORY_NAME + " is empty.");
                    return;
                }

                // create a socket to connect to the PRS server
                Socket prsSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                // create an endpoint for the PRS server
                EndPoint prsEP = new IPEndPoint(IPAddress.Parse(PRSSERVER_IPADDRESS), PSRSERVER_PORT);


                // contact the PRS and lookup port for "FT Server"
                PRSMessage sendFTMessage = new PRSMessage(PRSMessage.MESSAGE_TYPE.LOOKUP_PORT, FTSERVICE_NAME, 0, PRSMessage.STATUS.SUCCESS);
                // wait for a response from the server
                PRSMessage response = PRSMessage.ReceiveMessage(prsSocket, ref prsEP);

                if (response.Status != PRSMessage.STATUS.SUCCESS)
                {
                    Console.WriteLine("Failed to get response from server. Status: " + response.Status);
                    return;
                }
                else
                {
                    FTSERVER_PORT = response.Port;
                    Console.WriteLine("Received port from server: " + FTSERVER_PORT);
                }

                // create an FTClient and connect it to the server
                FTClient fTClient = new FTClient(FTSERVER_IPADDRESS, PRSSERVER_IPADDRESS, PSRSERVER_PORT, FTSERVICE_NAME);
                fTClient.Connect();
                // get the contents of the specified directory
                string[] files = Directory.GetFiles(DIRECTORY_NAME);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Socket error: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            catch (IOException ex)
            {
                Console.WriteLine("IO error: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            // wait for a keypress from the user before closing the console window
            Console.WriteLine("Press Enter to exit");
            Console.ReadKey();
        }
    }
}
