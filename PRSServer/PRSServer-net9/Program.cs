using PRSLib;
using PRSServer;
using System.Net;
using System.Net.Sockets;
static void Usage()
{
    Console.WriteLine("usage: PRSServer [options]");
    Console.WriteLine("\t-p < service port >");
    Console.WriteLine("\t-s < starting client port number >");
    Console.WriteLine("\t-e < ending client port number >");
    Console.WriteLine("\t-t < keep alive time in seconds >");
}

// defaults
ushort SERVER_PORT = 30000;
ushort STARTING_CLIENT_PORT = 40000;
ushort ENDING_CLIENT_PORT = 40099;
int KEEP_ALIVE_TIMEOUT = 10;

// process command options
// -p < service port >
// -s < starting client port number >
// -e < ending client port number >
// -t < keep alive time in seconds >

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "-p":
            SERVER_PORT = ushort.Parse(args[++i]);
            break;
        case "-s":
            STARTING_CLIENT_PORT = ushort.Parse(args[++i]);
            break;
        case "-e":
            ENDING_CLIENT_PORT = ushort.Parse(args[++i]);
            break;
        case "-t":
            KEEP_ALIVE_TIMEOUT = int.Parse(args[++i]);
            break;
        default:
            Usage();
            return;
    }
}

// check for valid STARTING_CLIENT_PORT and ENDING_CLIENT_PORT
if (STARTING_CLIENT_PORT > ENDING_CLIENT_PORT)
{
    Console.WriteLine("Error: Starting client port must be less than or equal to ending client port.");
    return;
}
// initialize the PRS server
PRS prs = new PRS(SERVER_PORT, STARTING_CLIENT_PORT, ENDING_CLIENT_PORT, KEEP_ALIVE_TIMEOUT);

// create the socket for receiving messages at the server
Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
EndPoint serverEndPoint = new IPEndPoint(IPAddress.Any, SERVER_PORT);




// bind the listening socket to the PRS server port
serverSocket.Bind(serverEndPoint);
Console.WriteLine($"PRS server listening on port {SERVER_PORT}");

//
// Process client messages
//

while (!prs.Stopped)
{
    try
    {
        // receive a message from a client
        PRSMessage message = PRSMessage.ReceiveMessage(serverSocket, ref serverEndPoint);

        // let the PRS handle the message
        PRSMessage response = prs.HandleMessage(message);

        // send response message back to client

        if (response != null)
        {
            // send the response message back to the client
            response.SendMessage(serverSocket, serverEndPoint);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error handling message: {ex.Message}");

        // attempt to send a UNDEFINED_ERROR response to the client, if we know who that was
        if (serverEndPoint != null)
        {
            try
            {
                PRSMessage errorResponse = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, "", SERVER_PORT, PRSMessage.STATUS.UNDEFINED_ERROR);
                errorResponse.SendMessage(serverSocket, serverEndPoint);
            }
            catch (Exception sendEx)
            {
                Console.WriteLine($"Failed to send error response: {sendEx.Message}");
            }
        }
    }
}

// close the listening socket
serverSocket.Close();
// wait for a keypress from the user before closing the console window
Console.WriteLine("Press Enter to exit");
Console.ReadKey();