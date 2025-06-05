using Mocks;
using SDLib;
using System;
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
            var client = new SDTestClient();
            await client.ConnectAsync(ServerIp, ServerPort);

            var sessionId = await client.OpenSessionAsync();
            Assert.IsTrue(sessionId > 0);

            await client.CloseSession(sessionId);
            client.Disconnect();
        }

        [TestMethod]
        public async Task ResumeSessionTest()
        {
            var client = new SDTestClient();
            await client.ConnectAsync(ServerIp, ServerPort);

            var sessionId = await client.OpenSessionAsync();
            client.Disconnect();
            await Task.Delay(1000); // Simulate some time passing
            await client.ConnectAsync(ServerIp, ServerPort);
            Assert.IsTrue(await client.ResumeSessionAsync(sessionId) == sessionId);

            await client.CloseSession(sessionId);
            client.Disconnect();
        }

        [TestMethod]
        public async Task PostAndGetSessionValueTest()
        {
            var client = new SDTestClient();
            await client.ConnectAsync(ServerIp, ServerPort);

            var sessionId = await client.OpenSessionAsync();
            await client.PostSessionValue("username.env", "noah");
           
            //await client.ResumeSessionAsync(sessionId);
            var value = await client.GetSessionValue("username.env");

            Assert.AreEqual("success\nusername.env\n4\nnoah", value);
            //await client.CloseSession(sessionId);
            
        }

        [TestMethod]
        public async Task InvalidSessionResumeTest()
        {
            var client = new SDTestClient();
            await client.ConnectAsync(ServerIp, ServerPort);

            var result = await client.ResumeSessionAsync(99999999); // Random invalid session
            Assert.IsFalse(result < 1);

            client.Disconnect();
        }
    }
}
