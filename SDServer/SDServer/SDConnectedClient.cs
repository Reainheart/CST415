// SDConnectedClient.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 

using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.IO;

namespace SDServer
{
    class SDConnectedClient(Socket clientSocket, SessionTable sessionTable)
    {
        // represents a single connected sd client
        // each client will have its own socket and thread while its connected
        // client is given it's socket from the SDServer when the server accepts the connection
        // this class creates it's own thread
        // the client's thread will process messages on the client's socket until it disconnects
        // NOTE: an sd client can connect/send messages/disconnect many times over it's lifetime

        private Socket ClientSocket = clientSocket;// save the client's socket
        
        // at this time, there is no stream, reader, write or thread
        private NetworkStream? Stream = null;
        private StreamReader? Reader = null;
        private StreamWriter? Writer = null;
        private Thread? ClientThread = null;

        private SessionTable SessionTable = sessionTable;      // save the server's session table
        private ulong sessionId = 0;                // session id for this session, once opened or resumed

        public void Start()
        {
            // TODO: SDConnectedClient.Start()

            // called by the main thread to start the clientThread and process messages for the client
            ClientThread = new Thread(() => ThreadProc(this));
            // create and start the clientThread, pass in a reference to this class instance as a parameter

            ClientThread.Start();
        }
        /// <summary>
        /// the procedure for the clientThread
        /// when this method returns, the clientThread will exit
        /// </summary>
        /// <param name="param"></param>
        private static void ThreadProc(Object param)
        {
            // the param is a SDConnectedClient instance
            SDConnectedClient client = (SDConnectedClient)param;

            // start processing messages with the Run() method
            client.Run();
        }

        private async void Run()
        {
            // TODO: SDConnectedClient.Run()

            // this method is executed on the clientThread

            try
            {
                // create network stream, reader and writer over the socket
                Stream = new NetworkStream(ClientSocket);
                Reader = new StreamReader(Stream);
                Writer = new StreamWriter(Stream) { AutoFlush = true};

                // process client requests
                bool done = false;
                while (!done)
                {
                    // receive a message from the client
                    string? msg = Reader?.ReadLine();
                    if (msg == null)
                    {
                        // no message means the client disconnected
                        // remember that the client will connect and disconnect as desired
                        Console.WriteLine("[" + ClientThread?.ManagedThreadId.ToString() + "] " + "Client disconnected, closing connection.");
                        done = true; // exit the loop
                    }
                    else
                    {
                        // handle the message
                        Console.WriteLine("[" + ClientThread?.ManagedThreadId.ToString() + "] " + "Received message from client: " + msg);

                        switch (msg)
                        {
                            case "open":
                                HandleOpen();
                                break;

                            case "resume":
                                HandleResume();
                                break;

                            case "close":
                                HandleClose();
                                break;

                            case "get":
                                HandleGet();
                                break;

                            case "post":
                                HandlePost();
                                break;

                            default:
                                {
                                    // error handling for an invalid message
                                    Console.WriteLine("[" + ClientThread.ManagedThreadId.ToString() + "] " + "Received invalid message from client: " + msg);
                                    // this client is too broken to waste our time on!
                                    SendError("Invalid message: " + msg);
                                    done = true; // exit the loop
                                }
                                break;
                        }
                    }
                }
            }
            catch (SocketException se)
            {
                Console.WriteLine("[" + ClientThread.ManagedThreadId.ToString() + "] " + "Error on client socket, closing connection: " + se.Message);
            }
            catch (IOException ioe)
            {
                Console.WriteLine("[" + ClientThread.ManagedThreadId.ToString() + "] " + "IO Error on client socket, closing connection: " + ioe.Message);
            }

            // close the client's writer, reader, network stream and socket
            Writer?.Close();
            Reader?.Close();
            Stream?.Close();
            ClientSocket.Disconnect(false);
            ClientSocket.Close();
        }

        private void HandleOpen()
        {
            // TODO: SDConnectedClient.HandleOpen()

            // handle an "open" request from the client

            // if no session currently open, then...
            if (sessionId == 0)
            {
                try
                {
                    // ask the SessionTable to open a new session and save the session ID
                    sessionId =  SessionTable.OpenSession();
                    // send accepted message, with the new session's ID, to the client
                    SendAccepted(sessionId);
                }
                catch (SessionException se)
                {
                    SendError(se.Message);
                }
                catch (Exception ex)
                {
                    SendError(ex.Message);
                }
            }
            else
            {
                // error!  the client already has a session open!
                SendError("Session already open!");
            }
        }

        private void HandleResume()
        {
            // TODO: SDConnectedClient.HandleResume()

            // handle a "resume" request from the client
            ulong resumeSessionId = ulong.Parse(Reader?.ReadLine()); // initialize sessionId to 0
            // get the sessionId that the client just asked us to resume

            try
            {
                // if we don't have a session open currently for this client...
                if (sessionId == 0)
                {
                    // try to resume the session in the session table
                    // if success, remember the session that we're now using and send accepted to client
                    SessionTable.ResumeSession(resumeSessionId);
                    sessionId = resumeSessionId;
                    SendAccepted(resumeSessionId);

                    // if failed to resume session, send rejectetd to client

                }
                else
                {
                    // error! we already have a session open
                    SendError("Session already open, cannot resume!");
                }
            }
            catch (SessionException se)
            {
                SendError(se.Message);
            }
            catch (Exception ex)
            {
                SendError(ex.Message);
            }
        }

        private void HandleClose()
        {
            // TODO: SDConnectedClient.HandleClose()

            // handle a "close" request from the client
            ulong closeSessionId = ulong.Parse(Reader?.ReadLine());
            // get the sessionId that the client just asked us to close
            SessionTable.ResumeSession(closeSessionId);

            try
            {
                // close the session in the session table
                SessionTable.CloseSession(closeSessionId);

                // send closed message back to client
                Writer.Write("closed\n" + closeSessionId + "\n");
                // record that this client no longer has an open session

            }
            catch (SessionException se)
            {
                SendError(se.Message);
            }
            catch (Exception ex)
            {
                SendError(ex.Message);
            }
        }

        private void HandleGet()
        {
            // TODO: SDConnectedClient.HandleGet()

            // handle a "get" request from the client

            // if the client has a session open
            if (sessionId != 0)
            {
                try
                {
                    // get the document name from the client
                    string documentName = Reader?.ReadLine() ?? "";
                    // get the document content from the session table
                    string documentContents = SessionTable.GetSessionValue(sessionId, documentName);

                    // send success and document to the client
                    if (documentContents.Length > 0)
                        SendSuccess(documentName, documentContents);
                    throw new Exception("Document was empty or does not exist");
                }
                catch (SessionException se)
                {
                    SendError(se.Message);
                }
                catch (Exception ex)
                {
                    SendError(ex.Message);
                }
            }
            else
            {
                // error, cannot post without a session

            }
        }

        private void HandlePost()
        {
            // TODO: SDConnectedClient.HandlePost()

            // handle a "post" request from the client

            // if the client has a session open
            if (sessionId != 0)
            {
                try
                {
                    // get the document name, content length and contents from the client
                    string documentName = Reader?.ReadLine() ?? "";
                    int documentLength = int.Parse(Reader?.ReadLine() ?? "0");
                    string documentContent = ReceiveDocument(documentLength);
                    // put the document into the session
                    SessionTable.PutSessionValue(sessionId, documentName, documentContent);

                    // send success to the client
                    SendSuccess();
                }
                catch (SessionException se)
                {
                    SendError(se.Message);
                }
                catch (Exception ex)
                {
                    SendError(ex.Message);
                }
            }
            else
            {
                // error, cannot post without a session

            }
        }

        private void SendAccepted(ulong sessionId)
        {
            // TODO: SDConnectedClient.SendAccepted()

            // send accepted message to SD client, including session id of now open session

            Writer?.Write("accepted\n" + sessionId.ToString() + "\n");
            Writer?.Flush(); // ensure the message is sent immediately
            Console.WriteLine($"Sent accepted session request for session ID {sessionId} to SD client.");
        }

        private void SendRejected(string reason)
        {
            // TODO: SDConnectedClient.SendRejected()

            // send rejected message to SD client, including reason for rejection
            Writer?.WriteLine("rejected\n" + reason + "\n");
            Console.WriteLine($"Sent rejected session request for {reason}.");
        }

        private void SendClosed(ulong sessionId)
        {
            // TODO: SDConnectedClient.SendClosed()

            // send closed message to SD client, including session id that was just closed
            Writer.Write("closed\n" + sessionId + "\n");
            Console.WriteLine($"Sent close session request for session ID {sessionId} to SD client."); 
        }

        private void SendSuccess()
        {
            // TODO: SDConnectedClient.SendSuccess()

            // send sucess message to SD client, with no further info
            // NOTE: in response to a post request
            Writer.Write("success\n");
            Writer.Flush();
        }

        private void SendSuccess(string documentName, string documentContent)
        {
            // TODO: SDConnectedClient.SendSuccess(documentName, documentContent)

            // send success message to SD client, including retrieved document name, length and content
            // NOTE: in response to a get request
            Writer.Write($"success\n{documentName}\n{documentContent.Length}\n{documentContent}\n");
            Writer.Flush();

        }

        private void SendError(string errorString)
        {
            // TODO: SDConnectedClient.SendError()

            // send error message to SD client, including error string
            Writer.Write($"error\n{errorString}\n");
        }

        private string ReceiveDocument(int length)
        {
            // TODO: SDConnectedClient.ReceiveDocument()

            // receive a document from the SD client, of expected length
            // NOTE: as part of processing a post request

            // read from the reader until we've received the expected number of characters
            // accumulate the characters into a string and return those when we got enough

            StringBuilder sb = new StringBuilder(length);
            int charToRead = length;
            while (charToRead > 0)
            {
                // read from the reader
                char[] buffer = new char[charToRead];
                int charsRead = Reader?.Read(buffer, 0, charToRead) ?? 0;
                if (charsRead == 0)
                {
                    // no more characters to read, break out of the loop
                    break;
                }
                sb.Append(buffer, 0, charsRead);
                charToRead -= charsRead;
            }
            return sb.ToString();
        }
    }
}
