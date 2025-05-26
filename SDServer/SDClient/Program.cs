using PRSLib;
using SDClient;

// defaults
string PRSSERVER_IPADDRESS = "127.0.0.1";
ushort PSRSERVER_PORT = 30000;
string SERVICE_NAME = "Simple Document (SD) Service";
string SDSERVER_IPADDRESS = "127.0.0.1";
ushort SDSERVER_PORT = 40000;
string SESSION_CMD = "-r";
ulong SESSION_ID = 1;
string DOCUMENT_CMD = null;
string DOCUMENT_NAME = null;

// process the command line arguments to get the PRS ip address and PRS port number
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "-prs" && i + 1 < args.Length)
    {
        var parts = args[++i].Split(':');
        PRSSERVER_IPADDRESS = parts[0];
        PSRSERVER_PORT = ushort.Parse(parts[1]);
    }
    else if (args[i] == "-sd" && i + 1 < args.Length)
    {
        var parts = args[++i].Split(':');
        SDSERVER_IPADDRESS = parts[0];
        SDSERVER_PORT = ushort.Parse(parts[1]);
    }
    else if (args[i] == "-s" && i + 1 < args.Length)
    {
        SESSION_CMD = args[++i];
    }
    else if (args[i] == "-id" && i + 1 < args.Length)
    {
        SESSION_ID = ulong.Parse(args[++i]);
    }
    else if (args[i] == "-d" && i + 1 < args.Length)
    {
        DOCUMENT_CMD = args[++i];
        DOCUMENT_NAME = args[++i];
    }
}
Console.WriteLine("PRS Address: " + PRSSERVER_IPADDRESS);
Console.WriteLine("PRS Port: " + PSRSERVER_PORT);

// process the command line arguments


Console.WriteLine("SD Server Address: " + SDSERVER_IPADDRESS);
Console.WriteLine("Session Command: " + SESSION_CMD);
Console.WriteLine("Session Id: " + SESSION_ID);
Console.WriteLine("Document Command: " + DOCUMENT_CMD);
Console.WriteLine("Document Name: " + DOCUMENT_NAME);

try
{
    // contact the PRS and lookup port for "SD Server"
    PRSClient PRS = new PRSClient(PRSSERVER_IPADDRESS, PSRSERVER_PORT);
    Console.WriteLine($"Connecting to PRS at {PRSSERVER_IPADDRESS}:{PSRSERVER_PORT}...");
    // create an SDClient to use in talking to the server
    SDSERVER_PORT = PRS.LookUpPort(SERVICE_NAME).Port;
    Console.WriteLine($"Received port {SDSERVER_PORT} for service '{SERVICE_NAME}'");
    SimpleDocumentClient sdClient = new SimpleDocumentClient(SDSERVER_IPADDRESS, SDSERVER_PORT);
    sdClient.Connect();

    // send session command to server
    if (SESSION_CMD == "-o")
    {
        // open new session
        sdClient.OpenSession();

    }
    else if (SESSION_CMD == "-r")
    {
        // resume existing session
        sdClient.ResumeSession(SESSION_ID);
    }
    else if (SESSION_CMD == "-c")
    {
        // close existing session
        sdClient.CloseSession(SESSION_ID);
    }

    // send document request to server
    if (DOCUMENT_CMD == "-post")
    {
        // read the document contents from stdin

        // send the document to the server

    }
    else if (DOCUMENT_CMD == "-get")
    {
        // get document from the server

        // print out the received document

    }

    // disconnect from the server

}
catch (Exception ex)
{
    Console.WriteLine("Error " + ex.Message);
    Console.WriteLine(ex.StackTrace);
}

// end of command line argument processing

// wait for a keypress from the user before closing the console window
// NOTE: the following commented out as they cannot be used when redirecting input to post a file
//Console.WriteLine("Press Enter to exit");
//Console.ReadKey();