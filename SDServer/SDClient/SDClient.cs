// SDClient.cs
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
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace SDClient
{
    class SDClient
    {
        private string sdServerAddress;
        private ushort sdServerPort;
        private bool connected;
        private ulong sessionID;
        Socket clientSocket;
        NetworkStream stream;
        StreamReader reader;
        StreamWriter writer;

        public SDClient(string sdServerAddress, ushort sdServerPort)
        {
            // TODO: SDClient.SDClient()

            // save server address/port
            this.sdServerAddress = sdServerAddress;
            this.sdServerPort = sdServerPort;
            // initialize to not connected to server
            connected = false;

            // initialize session ID to 0, meaning we have no session open
            // no session open at this time

            sessionID = 0;
        }

        public ulong SessionID { get { return sessionID; } set { sessionID = value; } }

        public void Connect()
        {
            // TODO: SDClient.Connect()

            ValidateDisconnected();

            // create a client socket and connect to the FT Server's IP address and port
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(IPAddress.Parse(sdServerAddress), sdServerPort);

            // establish the network stream, reader and writer
            stream = new NetworkStream(clientSocket);
            reader = new StreamReader(stream, Encoding.ASCII);
            writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

            // now connected
            connected = true;
            Console.WriteLine("Connected to SD server.");
        }

        public void Disconnect()
        {
            // TODO: SDClient.Disconnect()

            ValidateConnected(); 

            // close writer, reader and stream
            writer?.Close();
            reader?.Close();
            stream?.Close();

            // disconnect and close socket
            clientSocket?.Shutdown(SocketShutdown.Both);
            clientSocket?.Close();

            // now disconnected
            connected = false;
            Console.WriteLine("Disconnected from SD server.");
         }

        public void OpenSession()
        {
            // TODO: SDClient.OpenSession()

            ValidateConnected();

            // send open command to server
            SendOpen();

            // receive server's response, hopefully with a new session id
            sessionID = ReceiveSessionResponse();
            Console.WriteLine($"Opened session with ID: {sessionID}");
        }

        public void ResumeSession(ulong trySessionID)
        {
            // TODO: SDClient.ResumeSession()

            ValidateConnected();

            // send resume session to the server
            SendResume(trySessionID);

            // receive server's response, hopefully confirming our sessionId
            ulong confirmedSessionID = ReceiveSessionResponse();

            // verify that we received the same session ID that we requested
            if (confirmedSessionID != trySessionID)
                throw new Exception($"Server returned a different session ID. Expected {trySessionID}, got {confirmedSessionID}");

            // save opened session
            sessionID = confirmedSessionID;
            Console.WriteLine($"Resumed session with ID: {sessionID}");
        }

        public void CloseSession()
        {
            // TODO: SDClient.CloseSession()

            ValidateConnected();

            // send close session to the server
            SendClose(sessionID);

            // no session open
            sessionID = 0;
            Console.WriteLine("Closed session.");
        }

        public string GetDocument(string documentName)
        {
            // TODO: SDClient.GetDocument()

            ValidateConnected();

            // send get to the server
            
            // get the server's response
            return "TODO";
        }

        public void PostDocument(string documentName, string documentContents)
        {
            // TODO: SDClient.PostDocument()

            ValidateConnected();

            // send the document to the server
            
            // get the server's response
            
        }

        private void ValidateConnected()
        {
            if (!connected)
                throw new Exception("Connot perform action. Not connected to server!");
        }

        private void ValidateDisconnected()
        {
            if (connected)
                throw new Exception("Connot perform action. Already connected to server!");
        }

        private void SendOpen()
        {
            // TODO: SDClient.SendOpen()
            writer.WriteLine("open");
            // send open message to SD server

        }

        private void SendClose(ulong sessionId)
        {
            // TODO: SDClient.SendClose()
            writer.WriteLine("close");
            // send close message to SD server
            writer.WriteLine(sessionId.ToString());
        }

        private void SendResume(ulong sessionId)
        {
            // TODO: SDClient.SendResume()

            // send resume message to SD server
            writer.WriteLine("resume");
            writer.WriteLine(sessionId.ToString());
        }

        private ulong ReceiveSessionResponse()
        {
            string line = reader.ReadLine();
            if (line == "accepted")
            {
                string idLine = reader.ReadLine();
                if (!ulong.TryParse(idLine, out ulong sessionId))
                    throw new Exception("Invalid session ID received.");

                return sessionId;
            }
            else if (line == "rejected")
            {
                throw new Exception("Session rejected by server.");
            }
            else if (line.StartsWith("error"))
            {
                throw new Exception("Server error: " + line);
            }
            else
            {
                throw new Exception("Unexpected session response: " + line);
            }
        }

        private void SendPost(string documentName, string documentContents)
        {
            // TODO: SDClient.SendPost()

            // send post message to SD erer, including document name, length and contents
            SendPost(documentName, documentContents);
            ReceivePostResponse();
        }

        private void SendGet(string documentName)
        {
            // TODO: SDClient.SendGet()
            // send get message to SD server
            writer.WriteLine("get");
            writer.WriteLine(documentName);

        }

        private void ReceivePostResponse()
        {
            // TODO: SDClient.ReceivePostResponse()

            // get server's response to our last post request
            string line = reader.ReadLine();
            if (line == "success")
            {
                // yay, server accepted our request!
                
            }
            else if (line == "error")
            {
                // boo, server sent us an error!
                throw new Exception("TODO");
            }
            else
            {
                throw new Exception("Expected to receive a valid post response, instead got... " + line);
            }
        }

        private string ReceiveGetResponse()
        {
            string line = reader.ReadLine();

            if (line == "success")
            {
                string receivedDocName = reader.ReadLine();
                string lengthLine = reader.ReadLine();
                if (!int.TryParse(lengthLine, out int contentLength))
                    throw new Exception("Invalid content length from server.");

                return ReceiveDocumentContent(contentLength);
            }
            else if (line.StartsWith("error"))
            {
                throw new Exception("Server error: " + line);
            }
            else
            {
                throw new Exception("Unexpected response: " + line);
            }
        }


        private string ReceiveDocumentContent(int length)
        {
            byte[] buffer = new byte[length];
            int bytesRead = 0;

            while (bytesRead < length)
            {
                int chunk = stream.Read(buffer, bytesRead, length - bytesRead);
                if (chunk == 0)
                    throw new IOException("Unexpected disconnect during document download.");

                bytesRead += chunk;
            }

            return Encoding.UTF8.GetString(buffer);
        }
    }
}
