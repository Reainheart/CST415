// PRSTestClientProgram.cs
// Authors:
//   Noah Etchemedy - Test design, test case implementation
//   ChatGPT (OpenAI) - Task-based PRSClient enhancements, troubleshooting support

// Description: Test client for validating PRS Server behavior using UDP sockets.
//              Includes test cases for port request, keep-alive, timeouts, and error handling.

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using PRSLib;

namespace PRSTestClient
{
    class PRSTestClientProgram
    {
        static void Main(string[] args)
        {
            string serverIP = "127.0.0.1";
            ushort serverPort = 30000;
            EndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            Console.WriteLine("Starting PRS Test Client...\n");

            ushort reservedPort1 = 0;
            if (TestCase1_RequestPort(socket, serverEndPoint, out reservedPort1))
            {
                TestCase2_LookupPort(socket, serverEndPoint, reservedPort1);
                TestCase3_KeepAlive(socket, serverEndPoint, reservedPort1);
                TestCase4_ClosePort(socket, serverEndPoint, reservedPort1);
                TestCase7_LookupNonExistent(socket, serverEndPoint);
                TestCase8_CloseNonExistent(socket, serverEndPoint);
                TestCase9_RequestDuplicateService(socket, serverEndPoint);
                TestCase10_KeepAliveMultipleTimes(socket, serverEndPoint);
                TestCase11_StopKeepAliveThenTimeout();
                TestCase12_MultipleClientReservations();
                TestCase13_MultipleClientsWithKeepAlive();

            }

            TestCase5_Timeout(socket, serverEndPoint);
            TestCase6_StopServer(socket, serverEndPoint);

            socket.Close();
            Console.WriteLine("All tests completed.\n");
        }

        static bool TestCase1_RequestPort(Socket socket, EndPoint serverEP, out ushort reservedPort)
        {
            Console.WriteLine("Running TC1: Request a port for 'TestService1'");
            PRSMessage req = new PRSMessage(PRSMessage.MESSAGE_TYPE.REQUEST_PORT, "TestService1", 0, PRSMessage.STATUS.SUCCESS);
            req.SendMessage(socket, serverEP);
            PRSMessage resp = PRSMessage.ReceiveMessage(socket, ref serverEP);
            bool passed = (resp.Status == PRSMessage.STATUS.SUCCESS);
            reservedPort = resp.Port;
            Console.WriteLine($"TC1 {(passed ? "PASSED" : "FAILED")} - Received port {reservedPort}\n");
            return passed;
        }

        static void TestCase2_LookupPort(Socket socket, EndPoint serverEP, ushort expectedPort)
        {
            Console.WriteLine("Running TC2: Lookup the port for 'TestService1'");
            PRSMessage lookup = new PRSMessage(PRSMessage.MESSAGE_TYPE.LOOKUP_PORT, "TestService1", 0, PRSMessage.STATUS.SUCCESS);
            lookup.SendMessage(socket, serverEP);
            PRSMessage resp = PRSMessage.ReceiveMessage(socket, ref serverEP);
            bool passed = (resp.Status == PRSMessage.STATUS.SUCCESS && resp.Port == expectedPort);
            Console.WriteLine($"TC2 {(passed ? "PASSED" : "FAILED")} - Lookup port was {resp.Port}\n");
        }

        static void TestCase3_KeepAlive(Socket socket, EndPoint serverEP, ushort port)
        {
            Console.WriteLine("Running TC3: Send KEEP_ALIVE for 'TestService1'");
            PRSMessage keepAlive = new PRSMessage(PRSMessage.MESSAGE_TYPE.KEEP_ALIVE, "TestService1", port, PRSMessage.STATUS.SUCCESS);
            keepAlive.SendMessage(socket, serverEP);
            PRSMessage resp = PRSMessage.ReceiveMessage(socket, ref serverEP);
            bool passed = (resp.Status == PRSMessage.STATUS.SUCCESS);
            Console.WriteLine($"TC3 {(passed ? "PASSED" : "FAILED")}\n");
        }

        static void TestCase4_ClosePort(Socket socket, EndPoint serverEP, ushort port)
        {
            Console.WriteLine("Running TC4: Close port for 'TestService1'");
            PRSMessage closeMsg = new PRSMessage(PRSMessage.MESSAGE_TYPE.CLOSE_PORT, "TestService1", port, PRSMessage.STATUS.SUCCESS);
            closeMsg.SendMessage(socket, serverEP);
            PRSMessage resp = PRSMessage.ReceiveMessage(socket, ref serverEP);
            bool passed = (resp.Status == PRSMessage.STATUS.SUCCESS);
            Console.WriteLine($"TC4 {(passed ? "PASSED" : "FAILED")}\n");
        }

        static void TestCase5_Timeout(Socket socket, EndPoint serverEP)
        {
            Console.WriteLine("Running TC5: Request 'TestService2' and wait for timeout...");
            PRSMessage req = new PRSMessage(PRSMessage.MESSAGE_TYPE.REQUEST_PORT, "TestService2", 0, PRSMessage.STATUS.SUCCESS);
            req.SendMessage(socket, serverEP);
            PRSMessage resp = PRSMessage.ReceiveMessage(socket, ref serverEP);

            if (resp.Status == PRSMessage.STATUS.SUCCESS)
            {
                Console.WriteLine("Waiting 12 seconds (assuming timeout = 10s)...");
                Thread.Sleep(12000);

                PRSMessage lookup = new PRSMessage(PRSMessage.MESSAGE_TYPE.LOOKUP_PORT, "TestService2", 0, PRSMessage.STATUS.SUCCESS);
                lookup.SendMessage(socket, serverEP);
                PRSMessage lookupResp = PRSMessage.ReceiveMessage(socket, ref serverEP);

                bool passed = (lookupResp.Status == PRSMessage.STATUS.SERVICE_NOT_FOUND);
                Console.WriteLine($"TC5 {(passed ? "PASSED" : "FAILED")} - Lookup after timeout: {lookupResp.Status}\n");
            }
            else
            {
                Console.WriteLine("TC5 FAILED - Couldn't reserve port for TestService2.\n");
            }
        }

        static void TestCase6_StopServer(Socket socket, EndPoint serverEP)
        {
            Console.WriteLine("Running TC6: Sending STOP to server");
            PRSMessage stopMsg = new PRSMessage(PRSMessage.MESSAGE_TYPE.STOP, "TestServiceAdmin", 0, PRSMessage.STATUS.SUCCESS);
            stopMsg.SendMessage(socket, serverEP);
            PRSMessage resp = PRSMessage.ReceiveMessage(socket, ref serverEP);
            bool passed = (resp.Status == PRSMessage.STATUS.SUCCESS);
            Console.WriteLine($"TC6 {(passed ? "PASSED" : "FAILED")} - Server responded with {resp.Status}\n");
        }

        static void TestCase7_LookupNonExistent(Socket socket, EndPoint serverEP)
        {
            Console.WriteLine("Running TC7: Lookup non-existent service 'NoSuchService'");
            PRSMessage msg = new PRSMessage(PRSMessage.MESSAGE_TYPE.LOOKUP_PORT, "NoSuchService", 0, PRSMessage.STATUS.SUCCESS);
            msg.SendMessage(socket, serverEP);
            PRSMessage resp = PRSMessage.ReceiveMessage(socket, ref serverEP);
            bool passed = (resp.Status == PRSMessage.STATUS.SERVICE_NOT_FOUND);
            Console.WriteLine($"TC7 {(passed ? "PASSED" : "FAILED")} - Response: {resp.Status}\n");
        }

        static void TestCase8_CloseNonExistent(Socket socket, EndPoint serverEP)
        {
            Console.WriteLine("Running TC8: Close non-existent service 'GhostService'");
            PRSMessage msg = new PRSMessage(PRSMessage.MESSAGE_TYPE.CLOSE_PORT, "GhostService", 12345, PRSMessage.STATUS.SUCCESS);
            msg.SendMessage(socket, serverEP);
            PRSMessage resp = PRSMessage.ReceiveMessage(socket, ref serverEP);
            bool passed = (resp.Status == PRSMessage.STATUS.SERVICE_NOT_FOUND);
            Console.WriteLine($"TC8 {(passed ? "PASSED" : "FAILED")} - Response: {resp.Status}\n");
        }
        static void TestCase9_RequestDuplicateService(Socket socket, EndPoint serverEP)
        {
            Console.WriteLine("Running TC9: Request same service name twice 'RepeatService'");
            // First request - should succeed
            PRSMessage first = new PRSMessage(PRSMessage.MESSAGE_TYPE.REQUEST_PORT, "RepeatService", 0, PRSMessage.STATUS.SUCCESS);
            first.SendMessage(socket, serverEP);
            PRSMessage firstResp = PRSMessage.ReceiveMessage(socket, ref serverEP);

            if (firstResp.Status != PRSMessage.STATUS.SUCCESS)
            {
                Console.WriteLine("TC9 FAILED - Could not reserve port initially.\n");
                return;
            }

            // Second request - should fail
            PRSMessage second = new PRSMessage(PRSMessage.MESSAGE_TYPE.REQUEST_PORT, "RepeatService", 0, PRSMessage.STATUS.SUCCESS);
            second.SendMessage(socket, serverEP);
            PRSMessage secondResp = PRSMessage.ReceiveMessage(socket, ref serverEP);

            bool passed = secondResp.Status == PRSMessage.STATUS.SERVICE_IN_USE;
            Console.WriteLine($"TC9 {(passed ? "PASSED" : "FAILED")} - Second request status: {secondResp.Status}\n");

            // Clean up
            PRSMessage cleanup = new PRSMessage(PRSMessage.MESSAGE_TYPE.CLOSE_PORT, "RepeatService", firstResp.Port, PRSMessage.STATUS.SUCCESS);
            cleanup.SendMessage(socket, serverEP);
            PRSMessage cleanupResp = PRSMessage.ReceiveMessage(socket, ref serverEP);
        }

        static void TestCase10_KeepAliveMultipleTimes(Socket socket, EndPoint serverEP)
        {
            Console.WriteLine("Running TC10: KEEP_ALIVE extends service lifetime multiple times");

            PRSMessage req = new PRSMessage(PRSMessage.MESSAGE_TYPE.REQUEST_PORT, "KeepAliveExtendService", 0, PRSMessage.STATUS.SUCCESS);
            req.SendMessage(socket, serverEP);
            PRSMessage resp = PRSMessage.ReceiveMessage(socket, ref serverEP);

            if (resp.Status != PRSMessage.STATUS.SUCCESS)
            {
                Console.WriteLine("TC10 FAILED - Could not reserve port.\n");
                return;
            }

            ushort port = resp.Port;

            for (int i = 0; i < 3; i++)
            {
                Thread.Sleep(4000); // shorter than timeout
                PRSMessage keepAlive = new PRSMessage(PRSMessage.MESSAGE_TYPE.KEEP_ALIVE, "KeepAliveExtendService", port, PRSMessage.STATUS.SUCCESS);
                keepAlive.SendMessage(socket, serverEP);
                PRSMessage keepResp = PRSMessage.ReceiveMessage(socket, ref serverEP);

                if (keepResp.Status != PRSMessage.STATUS.SUCCESS)
                {
                    Console.WriteLine($"TC10 FAILED - KeepAlive failed at iteration {i + 1}.\n");
                    return;
                }
            }

            // Wait less than full timeout to verify it’s still alive
            Thread.Sleep(5000);
            PRSMessage lookup = new PRSMessage(PRSMessage.MESSAGE_TYPE.LOOKUP_PORT, "KeepAliveExtendService", 0, PRSMessage.STATUS.SUCCESS);
            lookup.SendMessage(socket, serverEP);
            PRSMessage lookupResp = PRSMessage.ReceiveMessage(socket, ref serverEP);

            bool passed = (lookupResp.Status == PRSMessage.STATUS.SUCCESS && lookupResp.Port == port);
            Console.WriteLine($"TC10 {(passed ? "PASSED" : "FAILED")} - Lookup result after multiple keep-alives: {lookupResp.Status}\n");

            // Cleanup
            PRSMessage cleanup = new PRSMessage(PRSMessage.MESSAGE_TYPE.CLOSE_PORT, "KeepAliveExtendService", port, PRSMessage.STATUS.SUCCESS);
            cleanup.SendMessage(socket, serverEP);
            PRSMessage cleanupResp = PRSMessage.ReceiveMessage(socket, ref serverEP);
        }

        static void TestCase11_StopKeepAliveThenTimeout()
        {
            Console.WriteLine("Running TC11: Start keep-alive, stop it, and ensure timeout");

            PRSClient client = new PRSClient("127.0.0.1", 30000, "KeepAliveDropTest");

            ushort port = client.RequestPort("KeepAliveDropTest");
            client.KeepPortAlive("KeepAliveDropTest", port);

            Console.WriteLine("Keep-alive running. Letting it run for 6 seconds...");
            Thread.Sleep(6000); // allow at least 1 keep-alive cycle

            Console.WriteLine("Stopping keep-alive...");
            client.StopKeepAlive();

            Console.WriteLine("Waiting for timeout (12 seconds)...");
            Thread.Sleep(12000); // should exceed timeout period

            // Try lookup with raw socket
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            EndPoint serverEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 30000);
            PRSMessage lookup = new PRSMessage(PRSMessage.MESSAGE_TYPE.LOOKUP_PORT, "KeepAliveDropTest", 0, PRSMessage.STATUS.SUCCESS);
            lookup.SendMessage(socket, serverEP);
            PRSMessage resp = PRSMessage.ReceiveMessage(socket, ref serverEP);

            bool passed = (resp.Status == PRSMessage.STATUS.SERVICE_NOT_FOUND);
            Console.WriteLine($"TC11 {(passed ? "PASSED" : "FAILED")} - Lookup after stopping keep-alive: {resp.Status}\n");

            socket.Close();
        }

        static void TestCase12_MultipleClientReservations()
        {
            Console.WriteLine("Running TC12: Multiple clients reserving unique services concurrently");

            PRSClient clientA = new PRSClient("127.0.0.1", 30000, "ClientAService");
            PRSClient clientB = new PRSClient("127.0.0.1", 30000, "ClientBService");

            ushort portA = clientA.RequestPort("ClientAService");
            ushort portB = clientB.RequestPort("ClientBService");

            bool portsValid = (portA != 0 && portB != 0 && portA != portB);

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            EndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 30000);

            // Lookup both to verify
            PRSMessage lookupA = new PRSMessage(PRSMessage.MESSAGE_TYPE.LOOKUP_PORT, "ClientAService", 0, PRSMessage.STATUS.SUCCESS);
            lookupA.SendMessage(socket, ep);
            PRSMessage respA = PRSMessage.ReceiveMessage(socket, ref ep);

            PRSMessage lookupB = new PRSMessage(PRSMessage.MESSAGE_TYPE.LOOKUP_PORT, "ClientBService", 0, PRSMessage.STATUS.SUCCESS);
            lookupB.SendMessage(socket, ep);
            PRSMessage respB = PRSMessage.ReceiveMessage(socket, ref ep);

            bool passed = portsValid &&
                          respA.Status == PRSMessage.STATUS.SUCCESS && respA.Port == portA &&
                          respB.Status == PRSMessage.STATUS.SUCCESS && respB.Port == portB;

            Console.WriteLine($"TC12 {(passed ? "PASSED" : "FAILED")} - ClientA={portA}, ClientB={portB}\n");

            // Cleanup
            clientA.ClosePort("ClientAService", portA);
            clientB.ClosePort("ClientBService", portB);
            socket.Close();
        }

        static void TestCase13_MultipleClientsWithKeepAlive()
        {
            Console.WriteLine("Running TC13: Two clients keeping services alive independently");

            PRSClient client1 = new PRSClient("127.0.0.1", 30000, "AliveService1");
            PRSClient client2 = new PRSClient("127.0.0.1", 30000, "AliveService2");

            ushort port1 = client1.RequestPort("AliveService1");
            ushort port2 = client2.RequestPort("AliveService2");

            client1.KeepPortAlive("AliveService1", port1);
            client2.KeepPortAlive("AliveService2", port2);

            Console.WriteLine("Both keep-alives running. Waiting 12 seconds...");
            Thread.Sleep(12000); // long enough to exceed timeout

            // Check both are still alive
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            EndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 30000);

            PRSMessage lookup1 = new PRSMessage(PRSMessage.MESSAGE_TYPE.LOOKUP_PORT, "AliveService1", 0, PRSMessage.STATUS.SUCCESS);
            lookup1.SendMessage(socket, ep);
            PRSMessage resp1 = PRSMessage.ReceiveMessage(socket, ref ep);

            PRSMessage lookup2 = new PRSMessage(PRSMessage.MESSAGE_TYPE.LOOKUP_PORT, "AliveService2", 0, PRSMessage.STATUS.SUCCESS);
            lookup2.SendMessage(socket, ep);
            PRSMessage resp2 = PRSMessage.ReceiveMessage(socket, ref ep);

            bool passed = (resp1.Status == PRSMessage.STATUS.SUCCESS && resp2.Status == PRSMessage.STATUS.SUCCESS);

            Console.WriteLine($"TC13 {(passed ? "PASSED" : "FAILED")} - Both services still alive\n");

            client1.StopKeepAlive();
            client2.StopKeepAlive();
            client1.ClosePort("AliveService1", port1);
            client2.ClosePort("AliveService2", port2);
            socket.Close();
        }


    }
}
