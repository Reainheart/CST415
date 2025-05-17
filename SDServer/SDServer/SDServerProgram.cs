// SDServerProgram.cs
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
using PRSLib;

namespace SDServer
{
    class SDServerProgram
    {
        private static void Usage()
        {
            Console.WriteLine("Usage: SDServer -prs <PRS IP address>:<PRS port>");
        }

        static void Main(string[] args)
        {
            // defaults
            ushort SDSERVER_PORT = 40000;
            int CLIENT_BACKLOG = 5;
            string PRS_ADDRESS = "127.0.0.1";
            ushort PRS_PORT = 30000;
            string SERVICE_NAME = "SD Server";

            // --- Parse command-line arguments ---
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-prs" && i + 1 < args.Length)
                    {
                        string[] parts = args[++i].Split(':');
                        if (parts.Length == 2)
                        {
                            PRS_ADDRESS = parts[0];
                            PRS_PORT = ushort.Parse(parts[1]);
                        }
                        else
                        {
                            throw new ArgumentException("Invalid PRS address format.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Unknown argument: " + args[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error parsing command line: " + ex.Message);
                Usage();
                return;
            }

            Console.WriteLine("PRS Address: " + PRS_ADDRESS);
            Console.WriteLine("PRS Port: " + PRS_PORT);

            PRSClient prsClient = null;

            try
            {
                // --- Contact PRS and get port ---
                prsClient = new PRSClient(PRS_ADDRESS, PRS_PORT);
                SDSERVER_PORT = prsClient.RequestPort(SERVICE_NAME);
                prsClient.KeepPortAlive(SERVICE_NAME, SDSERVER_PORT);

                Console.WriteLine("SD Server acquired port: " + SDSERVER_PORT);

                // --- Start SD Server ---
                SDServer server = new SDServer(SDSERVER_PORT, CLIENT_BACKLOG);
                server.Start();

                // --- Return port to PRS ---
                prsClient.ClosePort(SERVICE_NAME);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("Press Enter to exit");
            Console.ReadKey();
        }

    }
}
