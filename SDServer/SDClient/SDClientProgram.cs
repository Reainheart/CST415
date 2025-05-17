using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using PRSLib;

namespace SDClient
{
    class SDClientProgram
    {
        private static void Usage()
        {
            /*
                -prs <PRS IP address>:<PRS port>
                -s <SD server IP address>
		        -o | -r <session id> | -c <session id>
                [-get <document> | -post <document>]
            */
            Console.WriteLine("Usage: SDClient [-prs <PRS IP>:<PRS port>] [-s <SD Server IP>]");
            Console.WriteLine("\t-o | -r <session id> | -c <session id>");
            Console.WriteLine("\t[-get <document> | -post <document>]");
        }

        static void Main(string[] args)
        {
            // TODO: SDClientProgram.Main()

            // defaults
            string PRSSERVER_IPADDRESS = "127.0.0.1";
            ushort PSRSERVER_PORT = 30000;
            string SDSERVICE_NAME = "SD Server";
            string SDSERVER_IPADDRESS = "127.0.0.1";
            ushort SDSERVER_PORT = 40000;
            string SESSION_CMD = null;
            ulong SESSION_ID = 0;
            string DOCUMENT_CMD = null;
            string DOCUMENT_NAME = null;

            // process the command line arguments
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-prs":
                        string[] prsParts = args[++i].Split(':');
                        PRSSERVER_IPADDRESS = prsParts[0];
                        PSRSERVER_PORT = ushort.Parse(prsParts[1]);
                        break;
                    case "-s":
                        SDSERVER_IPADDRESS = args[++i];
                        break;
                    case "-o":
                        SESSION_CMD = "-o";
                        break;
                    case "-r":
                    case "-c":
                        SESSION_CMD = args[i];
                        SESSION_ID = ulong.Parse(args[++i]);
                        break;
                    case "-get":
                    case "-post":
                        DOCUMENT_CMD = args[i];
                        DOCUMENT_NAME = args[++i];
                        break;
                    default:
                        Usage();
                        return;
                }
            }


            Console.WriteLine("PRS Address: " + PRSSERVER_IPADDRESS);
            Console.WriteLine("PRS Port: " + PSRSERVER_PORT);
            Console.WriteLine("SD Server Address: " + SDSERVER_IPADDRESS);
            Console.WriteLine("Session Command: " + SESSION_CMD);
            Console.WriteLine("Session Id: " + SESSION_ID);
            Console.WriteLine("Document Command: " + DOCUMENT_CMD);
            Console.WriteLine("Document Name: " + DOCUMENT_NAME);

            try
            {
                // contact the PRS and lookup port for "SD Server"
                PRSClient prsClient = new PRSClient(PRSSERVER_IPADDRESS, PSRSERVER_PORT);
                PRSMessage response  = prsClient.LookUpPort(SDSERVICE_NAME);
                
                if (response.Status != PRSMessage.STATUS.SUCCESS)
                    throw new Exception("PRS lookup failed for service: " + SDSERVICE_NAME);

                SDSERVER_PORT = response.Port;

                // create an SDClient to use in talking to the server
                SDClient client = new SDClient(SDSERVER_IPADDRESS, SDSERVER_PORT);
                client.Connect();

                // send session command to server
                if (SESSION_CMD == "-o")
                {
                    client.OpenSession();
                }
                else if (SESSION_CMD == "-r")
                {
                    client.ResumeSession(SESSION_ID);
                }
                else if (SESSION_CMD == "-c")
                {
                    client.ResumeSession(SESSION_ID); // Must resume to close it
                    client.CloseSession();
                }


                if (DOCUMENT_CMD == "-post")
                {
                    string documentContents = Console.In.ReadToEnd();
                    client.PostDocument(DOCUMENT_NAME, documentContents);
                }
                else if (DOCUMENT_CMD == "-get")
                {
                    string documentContents = client.GetDocument(DOCUMENT_NAME);
                    Console.Write(documentContents); // Write to stdout
                }


                // disconnect from the server
                client.Disconnect();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            // wait for a keypress from the user before closing the console window
            // NOTE: the following commented out as they cannot be used when redirecting input to post a file
            //Console.WriteLine("Press Enter to exit");
            //Console.ReadKey();
        }
    }
}
