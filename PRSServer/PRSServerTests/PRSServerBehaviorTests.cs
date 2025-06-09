namespace PRSServerTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using PRSLib;


    [TestClass]
    public sealed class PRSServerBehaviorTests
    {
        private static EndPoint _serverEndPoint;
        private static Socket _socket;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            _serverEndPoint = new IPEndPoint(IPAddress.Parse("192.168.0.204"), 30000);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            _socket?.Close();
        }

        [TestMethod]
        public void TC1_RequestPort_Succeeds()
        {
            var req = new PRSMessage(PRSMessage.MESSAGE_TYPE.REQUEST_PORT, "TestService1", 0, PRSMessage.STATUS.SUCCESS);
            req.SendMessage(_socket, _serverEndPoint);

            var resp = PRSMessage.ReceiveMessage(_socket, ref _serverEndPoint);

            Assert.AreEqual(PRSMessage.STATUS.SUCCESS, resp.Status);
            Assert.IsTrue(resp.Port > 0);
        }

        [TestMethod]
        public void TC2_LookupPort_ReturnsExpected()
        {
            var lookup = new PRSMessage(PRSMessage.MESSAGE_TYPE.LOOKUP_PORT, "TestService1", 0, PRSMessage.STATUS.SUCCESS);
            lookup.SendMessage(_socket, _serverEndPoint);
            var resp = PRSMessage.ReceiveMessage(_socket, ref _serverEndPoint);

            Assert.AreEqual(PRSMessage.STATUS.SUCCESS, resp.Status);
            Assert.IsTrue(resp.Port > 0);
        }

        [TestMethod]
        public void TC3_KeepAlive_Succeeds()
        {
            ushort port = GetPort("TestService3");
            var keepAlive = new PRSMessage(PRSMessage.MESSAGE_TYPE.KEEP_ALIVE, "TestService3", port, PRSMessage.STATUS.SUCCESS);
            keepAlive.SendMessage(_socket, _serverEndPoint);
            var resp = PRSMessage.ReceiveMessage(_socket, ref _serverEndPoint);

            Assert.AreEqual(PRSMessage.STATUS.SUCCESS, resp.Status);
        }

        [TestMethod]
        public void TC4_ClosePort_Succeeds()
        {
            ushort port = GetPort("TestService4");
            var close = new PRSMessage(PRSMessage.MESSAGE_TYPE.CLOSE_PORT, "TestService4", port, PRSMessage.STATUS.SUCCESS);
            close.SendMessage(_socket, _serverEndPoint);
            var resp = PRSMessage.ReceiveMessage(_socket, ref _serverEndPoint);

            Assert.AreEqual(PRSMessage.STATUS.SUCCESS, resp.Status);
        }

        [TestMethod]
        public void TC5_Timeout_RemovesService()
        {
            var req = new PRSMessage(PRSMessage.MESSAGE_TYPE.REQUEST_PORT, "TestService5", 0, PRSMessage.STATUS.SUCCESS);
            req.SendMessage(_socket, _serverEndPoint);
            var resp = PRSMessage.ReceiveMessage(_socket, ref _serverEndPoint);
            Assert.AreEqual(PRSMessage.STATUS.SUCCESS, resp.Status);

            Thread.Sleep(12000);

            var lookup = new PRSMessage(PRSMessage.MESSAGE_TYPE.LOOKUP_PORT, "TestService5", 0, PRSMessage.STATUS.SUCCESS);
            lookup.SendMessage(_socket, _serverEndPoint);
            var lookupResp = PRSMessage.ReceiveMessage(_socket, ref _serverEndPoint);

            Assert.AreEqual(PRSMessage.STATUS.SERVICE_NOT_FOUND, lookupResp.Status);
        }

        //[TestMethod]
        //public void TC6_StopServer_Succeeds()
        //{
        //    var stop = new PRSMessage(PRSMessage.MESSAGE_TYPE.STOP, "TestServiceAdmin", 0, PRSMessage.STATUS.SUCCESS);
        //    stop.SendMessage(_socket, _serverEndPoint);
        //    var resp = PRSMessage.ReceiveMessage(_socket, ref _serverEndPoint);

        //    Assert.AreEqual(PRSMessage.STATUS.SUCCESS, resp.Status);
        //}

        [TestMethod]
        public void TC7_LookupNonExistent_Fails()
        {
            var msg = new PRSMessage(PRSMessage.MESSAGE_TYPE.LOOKUP_PORT, "NoSuchService", 0, PRSMessage.STATUS.SUCCESS);
            msg.SendMessage(_socket, _serverEndPoint);
            var resp = PRSMessage.ReceiveMessage(_socket, ref _serverEndPoint);

            Assert.AreEqual(PRSMessage.STATUS.SERVICE_NOT_FOUND, resp.Status);
        }

        [TestMethod]
        public void TC8_CloseNonExistent_Fails()
        {
            var msg = new PRSMessage(PRSMessage.MESSAGE_TYPE.CLOSE_PORT, "GhostService", 12345, PRSMessage.STATUS.SUCCESS);
            msg.SendMessage(_socket, _serverEndPoint);
            var resp = PRSMessage.ReceiveMessage(_socket, ref _serverEndPoint);

            Assert.AreEqual(PRSMessage.STATUS.SERVICE_NOT_FOUND, resp.Status);
        }

        private ushort GetPort(string serviceName)
        {
            var req = new PRSMessage(PRSMessage.MESSAGE_TYPE.REQUEST_PORT, serviceName, 0, PRSMessage.STATUS.SUCCESS);
            req.SendMessage(_socket, _serverEndPoint);
            var resp = PRSMessage.ReceiveMessage(_socket, ref _serverEndPoint);
            Assert.AreEqual(PRSMessage.STATUS.SUCCESS, resp.Status);
            return resp.Port;
        }
    }

