// SD Server Implementation - .NET 9 Console App
// Author: Noah Etchemendy
// Attribution: Based on specifications provided in a university-level assignment prompt.
// Note: Requires PRS server from Assignment 1

using PRSLib;
using SDLib;

// defaults
ushort SDSERVER_PORT = 40000;
int CLIENT_BACKLOG = 5;
string FT_ADDRESS = "127.0.0.1";
ushort FT_PORT = 30001;
string PRS_ADDRESS = "127.0.0.1";
ushort PRS_PORT = 30000;
string SERVICE_NAME = "Simple Document (SD) Service";

// process the command line arguments to get the PRS ip address and PRS port number
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "-prs" && i + 1 < args.Length)
    {
        var parts = args[++i].Split(':');
        PRS_ADDRESS = parts[0];
        PRS_PORT = ushort.Parse(parts[1]);
    }
}

Console.WriteLine("PRS Address: " + PRS_ADDRESS);
Console.WriteLine("PRS Port: " + PRS_PORT);

PRSClient PRS = new PRSClient(PRS_ADDRESS, PRS_PORT);
Console.WriteLine($"Connecting to PRS at {PRS_ADDRESS}:{PRS_PORT}...");

SDSERVER_PORT = PRS.RequestPort(SERVICE_NAME);
Console.WriteLine($"Received port {SDSERVER_PORT} for service '{SERVICE_NAME}'");
PRS.KeepPortAlive(SERVICE_NAME, SDSERVER_PORT);

try
{
    // instantiate SD server and start it running
    Console.WriteLine("Starting SD Server...");
    SimpleDocumentService sdService = new(SDSERVER_PORT, CLIENT_BACKLOG);
    sdService.Start();

    Console.WriteLine($"SD Server started on port {SDSERVER_PORT} with backlog {CLIENT_BACKLOG}");

    // tell the PRS that it can have it's port back, we don't need it anymore
    PRS.ClosePort(SERVICE_NAME, SDSERVER_PORT);
}
catch (Exception ex)
{
    Console.WriteLine("Error " + ex.Message);
    Console.WriteLine(ex.StackTrace);
}

// wait for a keypress from the user before closing the console window
Console.WriteLine("Press Enter to exit");
Console.ReadKey();