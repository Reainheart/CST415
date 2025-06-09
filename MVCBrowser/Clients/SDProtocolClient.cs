// SDProtocolClient.cs
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
using System.Collections.Generic;
using System.Text;
using PRSLib;
using System.Net;
using System.Net.Sockets;
using System.IO;
using SDLib;

namespace SDBrowser
{
    // implements IProtocolClient
    // uses the SD protcol
    // keeps track of sessions for each SD Server
    //  appropriately opens or resumes a session
    //  closes the session when the protocol client is closed
    // retrieves a single requested "document" by name
    // TODO: consider how this class could be implemented in terms of the SDClient class in SDClient project

    class SDProtocolClient : IProtocolClient
    {
        // represents an open session for a single SD Server
        private class SDSession
        {
            public SDClient client;
            public ulong sessionId;

            public SDSession(string ipAddr, ushort port, ulong sessionId)
            {
                client = new SDClient(ipAddr, port);
                this.sessionId = sessionId;
            }
        }

        private string prsIP;
        private ushort prsPort;
        private Dictionary<string, SDSession> sessions;     // server IP address --> session  info on the SD server


        public SDProtocolClient(string prsIP, ushort prsPort)
        {
            // TODO: SDProtocolClient.SDProtocolClient()

            // save the PRS server's IP address and port
            // will be used later to lookup the port for the SD Server when needed
            this.prsIP = prsIP;
            this.prsPort = prsPort;

            // initially empty dictionary of sessions
            sessions = new Dictionary<string, SDSession>();
        }


        /// <summary>
        /// retrieve requested document from the specified server
        /// manage the session with the SD Server
        /// opening or resuming as needed
        /// connect to and disconnect from the server w/in this method
        /// </summary>
        /// <param name="serverIP"></param>
        /// <param name="documentName"></param>
        /// <returns></returns>
        public string GetDocument(string serverIP, string documentName)
        {
            // TODO: SDProtocolClient.GetDocument()

            // make sure we have valid parameters
            // serverIP is the SD Server's IP address
            // documentName is the name of a docoument on the SD Server
            // both should not be empty
            if (string.IsNullOrWhiteSpace(serverIP) || string.IsNullOrWhiteSpace(documentName))
            {
                throw new ArgumentException("Server IP and document name cannot be empty.");
            }

            // contact the PRS and lookup port for "SD Server"
            PRSClient prsClient = new PRSClient(prsIP, prsPort);
            PRSMessage pRSMessage = prsClient.LookUpPort("Simple Document (SD) Service"); // connect to the PRS server

            // check if the PRS server returned a valid port for the SD Server
            if (pRSMessage == null || pRSMessage.Port == 0)
            {
                throw new InvalidOperationException("Failed to retrieve port for SD Server from PRS.");
            }

            // connect to SD server by ipAddr and port
            // use OpenOrResumeSession() to ensure session is handled correctly

            SDSession session = OpenOrResumeSession(serverIP, pRSMessage.Port);

            // send get message to server for requested document
            // get the server's response
            string output = session.client.GetDocument(documentName);

            // close writer, reader and network stream
            // disconnect from server and close the socket
            session.client.Disconnect();

            // return the content
            return output;
        }

        public void Close()
        {
            // TODO: SDProtocolClient.Close()

            // close each open session with the various servers

            // for each session...
            // connect to the SD Server's IP address and port
            // create network stream, reader and writer
            // send the close for this sessionId
            // close writer, reader and network stream
            // disconnect from server and close the socket

        }

        private SDSession OpenOrResumeSession(string ipAddr, ushort port)
        {
            // open or resume a session
            // check if we already have a session for this server
            if (!sessions.ContainsKey(ipAddr))
            {
                // no session exists, so create a new one
                sessions[ipAddr] = new SDSession(ipAddr, port, 0); // sessionId starts at 0
                
                sessions[ipAddr].client.Connect(); // connect to the SD Server
                sessions[ipAddr].client.OpenSession(); // initialize sessionId to 0
                sessions[ipAddr].sessionId = sessions[ipAddr].client.SessionID; // get the sessionId from the client
            }
            else
            {

                sessions[ipAddr].client.Connect(); // connect to the SD Server
                sessions[ipAddr].client.ResumeSession(sessions[ipAddr].sessionId); // initialize sessionId to 0
            }

            // keep the socket open and return it
            return sessions[ipAddr];
        }

        private static void SendOpen(StreamWriter writer)
        {
            // TODO: SDProtocolClient.SendOpen()

            // send open message to SD Server

        }

        private static void SendResume(StreamWriter writer, ulong sessionId)
        {
            // TODO: SDProtocolClient.SendResume()

            // send resume message to SD Server

        }

        private static ulong ReceiveSessionResponse(StreamReader reader)
        {
            // TODO: SDProtocolClient.ReceiveSessionResponse()

            // get server's response to our session request

            // if accepted...
            // yay, server accepted our session!
            // get and return the sessionID


            // if rejected
            // boo, server rejected us!


            // if error
            // boo, server sent us an error!

            // handle invalid response to our session request

            return 0;
        }

        private static void SendClose(StreamWriter writer, ulong sessionId)
        {
            // TODO: SDProtocolClient.SendClose()

            // send close message to SD Server

        }

        private static void SendGet(StreamWriter writer, string documentName)
        {
            // TODO: SDProtocolClient.SendGet()

            // send get message to SD Server

        }

        private static string ReceiveGetResponse(StreamReader reader)
        {
            // TODO: SDProtocolClient.ReceiveGetResponse()

            // get server's response to our get request and return the content received

            // if success...
            // yay, server accepted our request!
            // read the document name, content length and content
            // return the content

            // if error
            // boo, server sent us an error!

            // handle invalid response to our session request

            return "TODO";
        }

        private static string ReceiveDocument(StreamReader reader, int length)
        {
            // TODO: SDProtocolClient.ReceiveDocument()

            // read from the reader until we've received the expected number of characters
            // accumulate the characters into a string and return those when we got enough


            return "TODO";
        }
    }
}
