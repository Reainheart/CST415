﻿// SDConnectedClient.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 
// Noah Etchemendy
// CST 415
// Spring 2025
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

        private void Run()
        {
            try
            {
                Stream = new NetworkStream(ClientSocket);
                Reader = new StreamReader(Stream);
                Writer = new StreamWriter(Stream) { AutoFlush = true };

                bool done = false;
                while (!done)
                {
                    string? msg = Reader?.ReadLine();
                    if (string.IsNullOrEmpty(msg))
                    {
                        Console.WriteLine("[" + ClientThread?.ManagedThreadId.ToString() + "] " + "Client disconnected, closing connection.");
                        done = true;
                    }
                    else
                    {
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
                                // Let HandleGet read the document name line
                                HandleGet();
                                break;

                            case "post":
                                // Let HandlePost read session ID, document name, length, etc.
                                HandlePost();
                                break;

                            default:
                                Console.WriteLine("[" + ClientThread.ManagedThreadId.ToString() + "] " + "Received invalid message from client: " + msg);
                                SendError("Invalid message: " + msg);
                                done = true;
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

            try
            {
                Stream?.Dispose();
                ClientSocket?.Shutdown(SocketShutdown.Both);
                ClientSocket?.Close();
            }
            catch { }
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
            if (!ulong.TryParse(Reader?.ReadLine(), out ulong resumeSessionId))
            {
                SendError("Invalid session ID.");
                return;
            }



            // get the sessionId that the client just asked us to resume

            try
            {
                // if we don't have a session open currently for this client...
                if (sessionId == 0)
                {
                    // try to resume the session in the session table
                    // if success, remember the session that we're now using and send accepted to client
                    if (SessionTable.ResumeSession(resumeSessionId))
                    {
                        // session resumed successfully
                        sessionId = resumeSessionId;
                        SendAccepted(resumeSessionId);
                    }
                    else
                    {
                        // session resume failed, send rejected to client
                        SendRejected("Failed to resume session: " + resumeSessionId);
                        return;
                    }
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
                throw; // rethrow the exception to be handled by the caller
            }
            catch (Exception ex)
            {
                SendError(ex.Message);
                throw; // rethrow the exception to be handled by the caller
            }
        }

        // handle a "close" request from the client
        private void HandleClose()
        {
            // get the sessionId that the client just asked us to close
            if (!ulong.TryParse(Reader?.ReadLine(), out ulong resumeSessionId))
            {
                SendError("Invalid session ID.");
                return;
            }

            try
            {
                // close the session in the session table
                SessionTable.CloseSession(sessionId);
                SendClosed(sessionId);

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
            if (sessionId != 0)
            {
                try
                {
                    string documentName = Reader?.ReadLine() ?? "";
                    if (string.IsNullOrEmpty(documentName))
                    {
                        SendError("No document name provided.");
                        return;
                    }

                    string? documentContents = SessionTable?.GetSessionValue(sessionId, documentName);
                    if (string.IsNullOrEmpty(documentContents))
                    {
                        SendError("Document was empty or does not exist");
                        return;
                    }

                    SendSuccess(documentName, documentContents);
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
                SendError("No session open. Cannot perform GET.");
            }
        }


        // Client sends:
        // 2. document name (string)
        // 3. document length (int)
        // 4. document content (string of exact length)

        private void HandlePost()
        {

            string documentName = Reader?.ReadLine() ?? "";
            if (string.IsNullOrEmpty(documentName))
            {
                SendError("Missing document name.");
                return;
            }

            if (!int.TryParse(Reader?.ReadLine(), out int documentLength))
            {
                SendError("Invalid document length.");
                return;
            }

            string documentContent = ReceiveDocument(documentLength);

            try
            {
                if (documentName.StartsWith("/"))
                {
                    // Save to file
                    string filePath = Path.Combine(Environment.CurrentDirectory, documentName.TrimStart('/'));
                    File.WriteAllText(filePath, documentContent);
                }
                else
                {
                    // Save to session variable
                    SessionTable.PutSessionValue(sessionId , documentName, documentContent);
                }

                SendSuccess();
            }
            catch (Exception ex)
            {
                SendError("Post failed: " + ex.Message);
            }
        }


        private void SendAccepted(ulong sessionId)
        {
            // TODO: SDConnectedClient.SendAccepted()

            // send accepted message to SD client, including session id of now open session

            Writer?.WriteLine("accepted");
            Writer?.WriteLine(sessionId.ToString());


            Console.WriteLine($"Sent accepted session request for session ID {sessionId} to SD client.");
        }

        private void SendRejected(string reason)
        {
            // TODO: SDConnectedClient.SendRejected()

            // send rejected message to SD client, including reason for rejection
            Writer?.WriteLine("rejected");
            Writer?.WriteLine(reason);

            Console.WriteLine($"Sent rejected session request for {reason}.");
        }

        private void SendClosed(ulong sessionId)
        {
            // TODO: SDConnectedClient.SendClosed()

            // send closed message to SD client, including session id that was just closed
            Writer?.WriteLine("closed");
            Writer?.WriteLine(sessionId.ToString());

            Console.WriteLine($"Sent close session request for session ID {sessionId} to SD client."); 
        }

        private void SendSuccess()
        {
            // TODO: SDConnectedClient.SendSuccess()

            // send sucess message to SD client, with no further info
            // NOTE: in response to a post request
            Writer?.WriteLine("success");
            Console.WriteLine("Sent success message to SD client for post request.");
        }

        private void SendSuccess(string documentName, string documentContent)
        {
            // TODO: SDConnectedClient.SendSuccess(documentName, documentContent)

            // send success message to SD client, including retrieved document name, length and content
            // NOTE: in response to a get request
            Writer?.WriteLine("success");
            Writer?.WriteLine(documentName);
            Writer?.WriteLine(documentContent.Length);
            Writer?.Write(documentContent);
            Console.WriteLine($"Sent success message to SD client for get request: {documentName} (length: {documentContent.Length})");
        }

        private void SendError(string errorString)
        {
            // TODO: SDConnectedClient.SendError()

            // send error message to SD client, including error string
            Writer?.WriteLine($"error");
            Writer?.WriteLine(errorString);
            Console.WriteLine($"Sent error message to SD client: {errorString}");

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
