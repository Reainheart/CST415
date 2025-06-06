
using SDLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDServerTests
{
    [TestClass]
    public class SDClientCommandTests
    {
        private const string ServerIp = "127.0.0.1";
        private const ushort ServerPort = 40000;
        private SimpleDocumentService? _server;

        [TestInitialize]
        public void Init()
        {
            _server = new SimpleDocumentService(ServerPort, 10);
            Task.Run(() => _server.Start());
            Task.Delay(500).Wait(); // give it a moment to start
        }

        [TestCleanup]
        public void Cleanup()
        {
            _server?.Stop();
        }

        [TestMethod]
        public async Task OpenAndCloseSessionTest()
        {
            var client = new SimpleDocumentClient(ServerIp, ServerPort);
            client.Connect();
            client.OpenSession();
            Assert.IsTrue(client.SessionID > 0);

            client.CloseSession();

            Assert.IsTrue(client.SessionID == 0);
            client.Disconnect();
        }

        [TestMethod]
        public async Task ResumeSessionTest()
        {
            var client = new SimpleDocumentClient(ServerIp, ServerPort);
            client.Connect();
            client.OpenSession();
            ulong sessionId = client.SessionID;
            client.Disconnect();

            client.Connect(); // Reconnect to the server

            client.ResumeSession(sessionId);

            Assert.IsTrue(client.SessionID == sessionId);

            client.CloseSession();

            Assert.IsTrue(client.SessionID == 0);

        }



        [TestMethod]
        public async Task PostAndGetSessionValueTest()
        {
            var client = new SimpleDocumentClient(ServerIp, ServerPort);
            string docName = "testDoc";
            string docContent = "This is a test document.\nLine 2.\nLine 3.";
            client.Connect();

            client.OpenSession();
            Console.WriteLine($"Opened session ID: {client.SessionID}");

            client.PostDocument(docName, docContent);
            Console.WriteLine($"Posted document '{docName}'.");

            string received = client.GetDocument(docName);
            Console.WriteLine($"Retrieved document '{docName}':");
            Console.WriteLine(received);

            client.CloseSession();
            client.Disconnect();
        }


        [TestMethod]
        public async Task InvalidSessionResumeTest()
        {
            var client = new SimpleDocumentClient(ServerIp, ServerPort);
            client.Connect();
            client.OpenSession();
            ulong sessionId = client.SessionID;
            client.Disconnect();

            client.Connect(); // Reconnect to the server

            client.ResumeSession(sessionId);

            Assert.ThrowsException<Exception>(() =>
            {
                client.ResumeSession(99999999);
            });

            client.CloseSession();

            Assert.IsTrue(client.SessionID == 0);
 
            client.Disconnect();
        }

    }
}
