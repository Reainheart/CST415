using PRSLib;
string prsAddress = "127.0.0.1";
ushort prsPort = 0;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "-prs" && i + 1 < args.Length)
    {
        var parts = args[++i].Split(':');
        prsAddress = parts[0];
        prsPort = ushort.Parse(parts[1]);
    }
}

PRSClient PRS = new PRSClient(prsAddress, prsPort);
Console.WriteLine($"Connecting to PRS at {prsAddress}:{prsPort}...");