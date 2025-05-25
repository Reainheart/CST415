
using PRSLib;
using FTLib;
using SDLib;



// defaults
string SD_ADDRESS = "127.0.0.1";
ushort SDSERVER_PORT = 40000;
int CLIENT_BACKLOG = 5;
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

SDSERVER_PORT = PRS.LookUpPort(SERVICE_NAME).Port;
Console.WriteLine($"Received port {SDSERVER_PORT} for service '{SERVICE_NAME}'");

try
{
    // instantiate SD server and start it running
    Console.WriteLine("Starting SD Client...");
    SDClient sdClient = new SDClient(SD_ADDRESS, CLIENT_BACKLOG);
    

    // tell the PRS that it can have it's port back, we don't need it anymore

}
catch (Exception ex)
{
    Console.WriteLine("Error " + ex.Message);
    Console.WriteLine(ex.StackTrace);
}

// wait for a keypress from the user before closing the console window
Console.WriteLine("Press Enter to exit");
Console.ReadKey();
