//-----------------------------------------------------------------------------
// PRSClient.cs
//
// Authors: Noah Etchemedy, ChatGPT (OpenAI)

// Course: CST 415 - Programming Languages and Systems
// Project: Port Reservation Server Client Library
// Date: [05/17/2025]
//
// Description:
// A client library to interact with the Port Reservation Server (PRS).
// Supports port requests, keep-alive signaling, and clean port closure
// over UDP using a task-based asynchronous model.
//
//-----------------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PRSLib
{
    public class PRSClient
    {
        private EndPoint prsEndpoint;
        private Socket socket;
        private CancellationTokenSource cancellationTokenSource;
        private Task keepAliveTask;
        private readonly object socketLock = new object();

        public PRSClient(string prsAddress, ushort prsPort)
        {
            prsAddress = prsAddress ?? throw new ArgumentNullException(nameof(prsAddress));
            prsEndpoint = new IPEndPoint(IPAddress.Parse(prsAddress), prsPort);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
        }

        public ushort RequestPort(string serviceName)
        {
            PRSMessage request = new PRSMessage(PRSMessage.MESSAGE_TYPE.REQUEST_PORT, serviceName, 0, PRSMessage.STATUS.SUCCESS);

            lock (socketLock)
            {
                request.SendMessage(socket, prsEndpoint);
                PRSMessage response = PRSMessage.ReceiveMessage(socket, ref prsEndpoint);

                if (response.Status == PRSMessage.STATUS.SUCCESS)
                    return response.Port;
                else
                    throw new Exception("Failed to get port from PRS.");
            }
        }

        public void KeepPortAlive(string serviceName, ushort port)
        {
            if (keepAliveTask != null && !keepAliveTask.IsCompleted)
                throw new InvalidOperationException("Keep-alive task is already running.");

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            keepAliveTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var keepAlive = new PRSMessage(PRSMessage.MESSAGE_TYPE.KEEP_ALIVE, serviceName, port, PRSMessage.STATUS.SUCCESS);
                        lock (socketLock)
                        {
                            keepAlive.SendMessage(socket, prsEndpoint);
                            keepAlive = PRSMessage.ReceiveMessage(socket, ref prsEndpoint);
                        }

                        if (keepAlive.Status != PRSMessage.STATUS.SUCCESS)
                            Console.WriteLine($"[KeepAlive] Error: {keepAlive.Status}");

                        await Task.Delay(5000, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[KeepAlive] Error: " + ex.Message);
                    }
                }
            }, token);
        }

        public void StopKeepAlive()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();

                try
                {
                    keepAliveTask?.Wait();
                }
                catch (AggregateException ex)
                {
                    ex.Handle(e => e is TaskCanceledException);
                }

                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
                keepAliveTask = null;
            }
        }

        public void ClosePort(string serviceName, ushort port)
        {
            var closePort = new PRSMessage(PRSMessage.MESSAGE_TYPE.CLOSE_PORT, serviceName, port, PRSMessage.STATUS.SUCCESS);

            lock (socketLock)
            {
                closePort.SendMessage(socket, prsEndpoint);
                var response = PRSMessage.ReceiveMessage(socket, ref prsEndpoint);

                if (response.Status != PRSMessage.STATUS.SUCCESS)
                    throw new Exception("Failed to close port.");
            }

            StopKeepAlive();
            socket.Close();
            socket = null;
            prsEndpoint = null;
        }
    }
}
