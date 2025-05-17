using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace FTTestClientProgram
{
    [TestClass]
    public sealed class FTServerTests
    {
        private const string ServerIP = "127.0.0.1";
        private const int ServerPort = 40000;

        [TestMethod]
        public void Test_ConnectToFTServer()
        {
            using (TcpClient client = new TcpClient())
            {
                client.Connect(ServerIP, ServerPort);
                Assert.IsTrue(client.Connected, "Should connect to FTServer successfully.");
            }
        }
    }
}
 