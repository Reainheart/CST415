//
// FileTransferClient.cs
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
using System.Net;
using System.Net.Sockets;
using System.IO;
using PRSLib;

namespace FTClient
{
    class FileTransferClient
    {
        private string ftServerAddress;
        private ushort ftServerPort;
        private string ftClientServiceName;
        private string prsAddress;
        private ushort prsPort;

        bool connected;
        Socket clientSocket;
        NetworkStream stream;
        StreamReader reader;
        StreamWriter writer;

        public FileTransferClient(string ftServerAddress, string prsAddress, ushort prsPort, string ftClientServiceName)
        {
            // Save server address and PRS details
            this.ftServerAddress = ftServerAddress;
            this.prsAddress = prsAddress;
            this.prsPort = prsPort;
            this.ftClientServiceName = ftClientServiceName;

            connected = false;
        }

        public void Connect()
        {
            if (!connected)
            {
                try
                {

                    // Look up port from PRS
                    EndPoint prsEP = new IPEndPoint(IPAddress.Parse(prsAddress), prsPort);
                    Socket prsSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                    PRSMessage request = new PRSMessage(PRSMessage.MESSAGE_TYPE.LOOKUP_PORT, ftClientServiceName, 0, PRSMessage.STATUS.SUCCESS);
                    request.SendMessage(prsSocket, prsEP);

                    PRSMessage response = PRSMessage.ReceiveMessage(prsSocket, ref prsEP);
                    prsSocket.Close();

                    if (response.Status != PRSMessage.STATUS.SUCCESS) 
                        throw new Exception("PRS lookup failed for service: " + ftClientServiceName);

                    ftServerPort = response.Port;

                    // Connect to FT server using retrieved port
                    clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    clientSocket.Connect(new IPEndPoint(IPAddress.Parse(ftServerAddress), ftServerPort));

                    stream = new NetworkStream(clientSocket);
                    reader = new StreamReader(stream, Encoding.ASCII);
                    writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

                    connected = true;
                    Console.WriteLine($"Connected to FT Server on port {ftServerPort}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Connection failed: " + ex.Message);
                }
            }
        }

        public void Disconnect()
        {
            if (connected)
            {
                try
                {
                    SendExit();
                    writer.Close();
                    reader.Close();
                    stream.Close();
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error disconnecting: " + ex.Message);
                }

                connected = false;
                Console.WriteLine("Disconnected from FT Server.");
            }
        }

        public void GetDirectory(string directoryName)
        {
            if (connected)
            {
                SendGet(directoryName);
                while (ReceiveFile(directoryName)) { }
            }
        }

        #region implementation

        private void SendGet(string directoryName)
        {
            writer.WriteLine($"get {directoryName}");
        }

        private void SendExit()
        {
            writer.WriteLine("exit");
        }

        private void SendInvalidMessage()
        {
            writer.WriteLine("badcommand");
        }

        private bool ReceiveFile(string directoryName)
        {

            BinaryReader binReader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            try
            {
                string fileName = binReader.ReadString();
                int fileLength = binReader.ReadInt32();
                byte[] fileContents = binReader.ReadBytes(fileLength);

                string dirPath = Path.Combine(Directory.GetCurrentDirectory(), directoryName);
                Directory.CreateDirectory(dirPath);
                string localFilePath = Path.Combine(dirPath, fileName);
                File.WriteAllBytes(localFilePath, fileContents);

                Console.WriteLine($"Received file: {fileName} ({fileLength} bytes)");
                return true;
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        #endregion
    }
}
