// SDConnectedClient.cs
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
using System.Threading;
using System.Net.Sockets;
using System.IO;

namespace SDServer
{
    class SDConnectedClient
    {
        // represents a single connected sd client
        // each client will have its own socket and thread while its connected
        // client is given it's socket from the SDServer when the server accepts the connection
        // this class creates it's own thread
        // the client's thread will process messages on the client's socket until it disconnects
        // NOTE: an sd client can connect/send messages/disconnect many times over it's lifetime

        private Socket clientSocket;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;
        private Thread clientThread;
        private SessionTable sessionTable;      // server's session table
        private ulong sessionId;                // session id for this session, once opened or resumed

        public SDConnectedClient(Socket clientSocket, SessionTable sessionTable)
        {
            // Save the client's socket
            this.clientSocket = clientSocket;

            // at this time, there is no stream, reader, write or thread
            stream = null;
            reader = null;
            writer = null;
            clientThread = null;

            // save the server's session table
            this.sessionTable = sessionTable;

            // at this time, ther eis no session open
            sessionId = 0;
        }

        public void Start()
        {
            // TODO: SDConnectedClient.Start()

            // called by the main thread to start the clientThread and process messages for the client
            clientThread = new Thread(ThreadProc);
            // create and start the clientThread, pass in a reference to this class instance as a parameter
            clientThread.Start(this);
        }

        private static void ThreadProc(Object param)
        {
            // TODO: SDConnectedClient.ThreadProc()

            // the procedure for the clientThread
            // when this method returns, the clientThread will exit
            SDConnectedClient client = (SDConnectedClient)param;

            // the param is a SDConnectedClient instance
            // start processing messages with the Run() method
            client.Run();

        }

        private void Run()
        {
            try
            {
                // Create network stream, reader, and writer over the socket
                stream = new NetworkStream(clientSocket);
                reader = new StreamReader(stream, Encoding.UTF8);
                writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                bool done = false;
                while (!done)
                {
                    // Receive a message from the client
                    string msg = reader.ReadLine();
                    if (msg == null)
                    {
                        // Client disconnected
                        Console.WriteLine("[" + clientThread.ManagedThreadId + "] Client disconnected.");
                        done = true;
                    }
                    else
                    {
                        Console.WriteLine("[" + clientThread.ManagedThreadId + "] Received: " + msg);
                        // Handle the message
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
                                // Invalid command — disconnect the client
                                SendError("Invalid message received.");
                                done = true;
                                break;
                        }
                    }
                }
            }
            catch (SocketException se)
            {
                Console.WriteLine("[" + clientThread.ManagedThreadId + "] Socket error: " + se.Message);
            }
            catch (IOException ioe)
            {
                Console.WriteLine("[" + clientThread.ManagedThreadId + "] IO error: " + ioe.Message);
            }

            // Clean up
            if (writer != null) writer.Close();
            if (reader != null) reader.Close();
            if (stream != null) stream.Close();
            if (clientSocket != null) clientSocket.Close();

            Console.WriteLine("[" + clientThread.ManagedThreadId + "] Connection closed.");
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
                    
                    // send accepted message, with the new session's ID, to the client
                    
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

            // get the sessionId that the client just asked us to resume
            
            try
            {
                // if we don't have a session open currently for this client...
                if (sessionId == 0)
                {
                    // try to resume the session in the session table
                    // if success, remember the session that we're now using and send accepted to client
                    
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

            // get the sessionId that the client just asked us to close
            
            try
            {
                // close the session in the session table
                // send closed message back to client
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
                    
                    // get the document content from the session table
                    
                    // send success and document to the client
                    
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
                    
                    // put the document into the session
                    
                    // send success to the client
                    
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
            
        }

        private void SendRejected(string reason)
        {
            // TODO: SDConnectedClient.SendRejected()

            // send rejected message to SD client, including reason for rejection
            
        }

        private void SendClosed(ulong sessionId)
        {
            // TODO: SDConnectedClient.SendClosed()

            // send closed message to SD client, including session id that was just closed
            
        }

        private void SendSuccess()
        {
            // TODO: SDConnectedClient.SendSuccess()

            // send sucess message to SD client, with no further info
            // NOTE: in response to a post request
            
        }

        private void SendSuccess(string documentName, string documentContent)
        {
            // TODO: SDConnectedClient.SendSuccess(documentName, documentContent)

            // send success message to SD client, including retrieved document name, length and content
            // NOTE: in response to a get request
            
        }

        private void SendError(string errorString)
        {
            // TODO: SDConnectedClient.SendError()

            // send error message to SD client, including error string
            
        }

        private string ReceiveDocument(int length)
        {
            // TODO: SDConnectedClient.ReceiveDocument()

            // receive a document from the SD client, of expected length
            // NOTE: as part of processing a post request

            // read from the reader until we've received the expected number of characters
            // accumulate the characters into a string and return those when we got enough
            
            return "TODO";
        }
    }
}
