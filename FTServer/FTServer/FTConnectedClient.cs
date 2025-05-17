// FTConnectedClient.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 
// Noah Etchemendy
// CST 415
// Spring 2025


using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.IO;

namespace FTServer
{
    class FTConnectedClient
    {
        // represents a single connected ft client, that wants directory contents from the server
        // each client will have its own socket and thread
        // client is given it's socket from the FTServer when the server accepts the connection
        // the client class creates it's own thread
        // the client's thread will process messages on the client's socket

        private Socket clientSocket;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;
        private Thread clientThread;

        public FTConnectedClient(Socket clientSocket)
        {
            // TODO: FTConnectedClient.FTConnectedClient()

            // save the client's socket
            this.clientSocket = clientSocket;
            // at this time, there is no stream, reader, write or thread

        }

        public void Start()
        {
            // TODO: FTConnectedClient.Start()

            // called by the main thread to start the clientThread and process messages for the client
            clientThread = new Thread(ThreadProc);
            // create and start the clientThread, pass in a reference to this class instance as a parameter
            clientThread.Start(this);

        }

        private static void ThreadProc(Object param)
        {
            // TODO: FTConnectedClient.ThreadProc()

            // the procedure for the clientThread
            // when this method returns, the clientThread will exit
            FTConnectedClient client = (FTConnectedClient)param;
            // the param is a FTConnectedClient instance
            // start processing messages with the Run() method
            client.Run();

        }

        private void Run()
        {
            // TODO: FTConnectedClient.Run()

            // this method is executed on the clientThread

            try
            {
                // create network stream, reader and writer over the socket
                stream = new NetworkStream(clientSocket);
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream) { AutoFlush = true };
                // process client requests
                bool done = false;
                while (!done)
                {
                    // receive a message from the client
                     string message = reader.ReadLine();
                    if (message == null)
                    {
                        // Client disconnected 
                        break;
                    }

                    if (message.StartsWith("get "))
                    {
                        string directory = message.Substring(4).Trim();
                        if (!Directory.Exists(directory))
                        {
                            SendError("Directory does not exist.");
                            continue;
                        }

                        string[] files = Directory.GetFiles(directory, "*.txt");
                        foreach (string file in files)
                        {
                            string fileName = Path.GetFileName(file);
                            byte[] fileBytes = File.ReadAllBytes(file);
                            SendFileName(fileName, fileBytes.Length);
                            stream.Write(fileBytes, 0, fileBytes.Length);
                        }

                        SendDone();
                    }

                    if (message.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    {
                        done = true;
                    }

                    else // invalid message
                    {
                        // error handling for an invalid message
                        SendError("Invalid command.");

                        // this client is too broken to waste our time on!
                        // quite processing messages and disconnect
                        break;

                    }
                }
            }
            catch (SocketException se)
            {
                Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Error on client socket, closing connection: " + se.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Unhandled exception: " + ex.Message);
            }
            // close the client's writer, reader, network stream and socket
            finally
            {
                writer?.Close();
                reader?.Close();
                stream?.Close();
                clientSocket?.Close();
            }

        }

        private void SendFileName(string fileName, int fileLength)
        {
            writer.WriteLine(fileName);        // match FTClient expectation
            writer.WriteLine(fileLength.ToString());
        }

        private void SendDone()
        {
            writer.WriteLine("done");
        }

        private void SendError(string errorMessage)
        {
            writer.WriteLine($"error {errorMessage}");
        }
    }
}
