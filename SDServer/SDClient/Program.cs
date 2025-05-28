using PRSLib;
using SDClient;

// defaults
string PRSSERVER_IPADDRESS = "127.0.0.1";
ushort PSRSERVER_PORT = 30000;
string SERVICE_NAME = "Simple Document (SD) Service";
string SDSERVER_IPADDRESS = "127.0.0.1";
ushort SDSERVER_PORT = 40000;
string SESSION_CMD = "";
ulong SESSION_ID = 0;
string DOCUMENT_CMD = null;
string DOCUMENT_NAME = null;
string DOCUMENT_CONTENT = null;
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
    else if (args[i] == "-o")
    {
        SESSION_CMD = "-o"; // open new session
    }
    else if (args[i] == "-r" && i + 1 < args.Length)
    {
        SESSION_CMD = "-r"; // resume existing session
        SESSION_ID = ulong.Parse(args[++i]);
    }
    else if (args[i] == "-c")
    {
        SESSION_CMD = "-c"; // close session
        if (i + 1 < args.Length)
        {
            SESSION_ID = ulong.Parse(args[++i]);
        }
        else
        {
            Console.WriteLine("Error: -c requires a session ID argument.");
            return;
        }
    }
    else if (args[i] == "-get" && i + 1 < args.Length)
    {
        DOCUMENT_CMD = "-get"; // get document
        DOCUMENT_NAME = args[++i];
    }
    else if (args[i] == "-post" && i + 2 < args.Length)
    {
        DOCUMENT_CMD = "-post"; // post document
        DOCUMENT_NAME = args[++i];
        DOCUMENT_CONTENT = args[++i];
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
        sdClient.CloseSession();
    }

    // send document request to server
    if (DOCUMENT_CMD == "-post")
    {
        // read the document contents from stdin
        //string documentContent = Console.In.ReadToEnd(); // read until EOF
        // send the document to the server
        sdClient.PostDocument(DOCUMENT_NAME, DOCUMENT_CONTENT);
    }
    else if (DOCUMENT_CMD == "-get")
    {
        // get document from the server
        string document = sdClient.GetDocument(DOCUMENT_NAME);
        // print out the received document
        Console.WriteLine("Received Document: " + DOCUMENT_NAME);
        Console.WriteLine(document);
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