using FTLib;

namespace FTTestClientProgram;

[TestClass]
public sealed class FTClientTests
{
    private const string FT_SERVER_IP = "127.0.0.1";
    private const string PRS_IP = "127.0.0.1";
    private const ushort PRS_PORT = 30000;
    private const string FT_SERVICE_NAME = "FTServer";

    [TestMethod]
    public void Connect_Disconnect_ShouldSucceed()
    {
        FTClient client = new FTClient(FT_SERVER_IP, PRS_IP, PRS_PORT, FT_SERVICE_NAME);
        client.Connect();
        Assert.IsTrue(client.connected, "Client should connect successfully to FTServer.");

        client.Disconnect();
        Assert.IsFalse(client.connected, "Client should disconnect successfully from FTServer.");
    }

    [TestMethod]
    public void GetDirectory_ShouldDownloadFiles()
    {
        string testDir = "TestFiles";
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), testDir);

        if (Directory.Exists(filePath))
            Directory.Delete(filePath, true); // Ensure clean test state

        FTClient client = new FTClient(FT_SERVER_IP, PRS_IP, PRS_PORT, FT_SERVICE_NAME);
        client.Connect();

        client.GetDirectory(testDir);
        client.Disconnect();

        Assert.IsTrue(Directory.Exists(filePath), $"Directory '{testDir}' should exist after retrieval.");
        Assert.IsTrue(Directory.GetFiles(filePath).Length > 0, $"Directory '{testDir}' should contain files.");
    }

    [TestMethod]
    public void InvalidCommand_ShouldNotCrash()
    {
        FTClient client = new FTClient(FT_SERVER_IP, PRS_IP, PRS_PORT, FT_SERVICE_NAME);
        client.Connect();

        try
        {
            // Send an invalid message directly via reflection (or expose it if testable)
            typeof(FTClient)
                .GetMethod("SendInvalidMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(client, null);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Unexpected exception on invalid command: {ex.Message}");
        }

        client.Disconnect();
    }

}
