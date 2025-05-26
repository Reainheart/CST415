// SDClient.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace SDClient
{
    class SimpleDocumentClient (string SDServerAddress, ushort SDServerPort)
    {
        private string SDServerAddress = SDServerAddress;
        private ushort SDServerPort = SDServerPort;
        private bool connected = false;
        private ulong sessionID = 0;
        private Socket? clientSocket = null;
        private NetworkStream? stream = null;
        private StreamReader? reader = null;
        private StreamWriter? writer = null;

        public ulong SessionID { get { return sessionID; } set { sessionID = value; } }

        public void Connect()
        {
            // TODO: SDClient.Connect()

            ValidateDisconnected();

            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // create a client socket and connect to the SD Server's IP address and port
            clientSocket.Connect(IPAddress.Parse(SDServerAddress), SDServerPort);

            // establish the network stream, reader and writer
            stream = new NetworkStream(clientSocket);
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);

            // now connected
            connected = true;
            Console.WriteLine($"Connected to SD Server at {SDServerAddress}:{SDServerPort}");
        }

        public void Disconnect()
        {
            // TODO: SDClient.Disconnect()

            ValidateConnected();

            // close writer, reader and stream
            stream?.Close();
            writer?.Close();
            reader?.Close();

            // disconnect and close socket
            clientSocket?.Disconnect(false);

            // now disconnected
            connected = false;
            Console.WriteLine($"Disconnected from SD Server at {SDServerAddress}:{SDServerPort}");
        }

        public void OpenSession()
        {
            // TODO: SDClient.OpenSession()

            ValidateConnected();

            // send open command to server
            SendOpen();
            try
            {
                // receive server's response, hopefully with a new session id
                sessionID = ReceiveSessionResponse(); // read the server's response
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to open session with SD server: " + ex.Message);
            }


        }

        public void ResumeSession(ulong trySessionID)
        {
            // TODO: SDClient.ResumeSession()
            if (trySessionID == 0)
            {
                throw new Exception("Cannot resume session with ID 0.");
            }

            ValidateConnected();

            // send resume session to the server
            SendResume(trySessionID);

            // receive server's response, hopefully confirming our sessionId
            ulong returnedID = ReceiveSessionResponse();

            // check if the returned session ID matches the one we tried to resume
            if (returnedID != trySessionID)
            {
                throw new Exception($"Server returned session ID {returnedID}, expected {trySessionID}.");
            }
            else
            {
                // save opened session
                sessionID =  returnedID; // update the session ID
                Console.WriteLine($"Session resumed with ID {returnedID}.");
            }

            
        }

        public void CloseSession(ulong sESSION_ID)
        {
            // TODO: SDClient.CloseSession()

            ValidateConnected();

            // send close session to the server
            SendClose(sessionID);

            // no session open
            sessionID = 0;
        }

        public string GetDocument(string documentName)
        {
            // TODO: SDClient.GetDocument()

            ValidateConnected();

            // send get to the server
            SendGet(documentName);

            // get the server's response
            string documentContent = ReceiveGetResponse();
            
            return documentContent;
        }

        public void PostDocument(string documentName, string documentContents)
        {
            // TODO: SDClient.PostDocument()

            ValidateConnected();

            // send the document to the server
            SendPost(documentName, documentContents);

            // get the server's response
            ReceivePostResponse();
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

            // send open message to SD server
            writer?.WriteLine("open");
            writer?.Flush(); // ensure the message is sent immediately
            Console.WriteLine("Sent open session request to SD server.");
        }

        private void SendClose(ulong sessionId)
        {
            // TODO: SDClient.SendClose()

            // send close message to SD server
            writer.WriteLine("close\n" + sessionId);
            writer.Flush(); // ensure the message is sent immediately
            Console.WriteLine($"Sent close session request for session ID {sessionId} to SD server.");
        }

        private void SendResume(ulong sessionId)
        {
            // TODO: SDClient.SendResume()

            // send resume message to SD server
            writer.WriteLine("resume\n" + sessionId +"\n");
            writer.Flush(); // ensure the message is sent immediately
        }

        private ulong ReceiveSessionResponse()
        {
            // TODO: SDClient.ReceiveSessionResponse()

            // get SD server's response to our last session request (open or resume)
            string? line = reader?.ReadLine();
            if (line == "accepted")
            {
                // yay, server accepted our session!
                // get the sessionID
                line = reader?.ReadLine();
                if (line == null)
                {
                    throw new Exception("Expected session ID after 'accepted' response, but got null.");
                }
                return ulong.Parse(line);
            }
            else if (line == "rejected")
            {
                // boo, server rejected us!
                throw new Exception("Server rejected connection attempt");
            }
            else if (line == "error")
            {
                // boo, server sent us an error!
                line = reader?.ReadLine(); // read the error messa ge
                throw new Exception(line);
            }
            else
            {
                throw new Exception("Expected to receive a valid session response, instead got... " + line);
            }
        }

        private void SendPost(string documentName, string documentContents)
        {
            // TODO: SDClient.SendPost()

            // send post message to SD erer, including document name, length and contents

        }

        private void SendGet(string documentName)
        {
            // TODO: SDClient.SendGet()

            // send get message to SD server

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
            // TODO: SDClient.ReceiveGetResponse()

            // get server's response to our last get request and return the content received
            string line = reader.ReadLine();
            if (line == "success")
            {
                // yay, server accepted our request!

                // read the document name, content length and content

                // return the content
                return "TODO";
            }
            else if (line == "error")
            {
                // boo, server sent us an error!
                throw new Exception("TODO");
            }
            else
            {
                throw new Exception("Expected to receive a valid get response, instead got... " + line);
            }
        }

        private string ReceiveDocumentContent(int length)
        {
            // TODO: SDClient.ReceiveDocumentContent()

            // read from the reader until we've received the expected number of characters
            // accumulate the characters into a string and return those when we received enough

            return "TODO";
        }
    }
}
