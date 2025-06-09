using PRSLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRSServer
{
    /// <summary>
    /// represents a PRS Server, keeps all state and processes messages accordingly
    /// </summary>
    public class PRS
    {
        
        // server attribues
        private ushort startingClientPort;
        private ushort endingClientPort;
        private int keepAliveTimeout;
        private int numPorts;
        private PortReservation[] ports;
        private bool stopped;

        public PRS(ushort ServicePort, ushort startingClientPort, ushort endingClientPort, int kEEP_ALIVE_TIMEOUT)
        {
            // TODO: PRS.PRS()

            // save parameters
            this.startingClientPort = startingClientPort;
            this.endingClientPort = endingClientPort;
            this.keepAliveTimeout = kEEP_ALIVE_TIMEOUT;

            // initialize to not stopped
            this.stopped = false;

            // initialize port reservations
            numPorts = endingClientPort - startingClientPort + 1;
            ports = new PortReservation[numPorts];
            for (ushort i = 0; i < numPorts; i++)
            {
                ports[i] = (new PortReservation((ushort)(startingClientPort + i)));
            }
        }

        public bool Stopped { get { return stopped; } }

        private void CheckForExpiredPorts()
        {
            // Go through each port and expire it if necessary
            foreach (PortReservation port in ports)
            {
                if (!port.Available && port.Expired(keepAliveTimeout))
                {
                    Console.WriteLine($"[INFO] Expiring service '{port.ServiceName}' on port {port.Port} due to timeout.");
                    port.Close();
                }
            }
        }

        private PRSMessage RequestAvaliablePort(string serviceName)
        {
            // client has requested the lowest available port, so find it!
            for (int i = 0; i < numPorts; i++)
            {
                // if found an avialable port, reserve it and send SUCCESS
                if (ports[i].Available)
                {
                    // found an available port, reserve it
                    ports[i].Reserve(serviceName);
                    return new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, serviceName, ports[i].Port, PRSMessage.STATUS.SUCCESS);
                }
            }
            // else, none available, send ALL_PORTS_BUSY
            return new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, serviceName, 0, PRSMessage.STATUS.ALL_PORTS_BUSY);
        }

        private PRSMessage RequestPort(string serviceName)
        {
            // Check if this service is already registered
            for (int i = 0; i < numPorts; i++)
            {
                if (!ports[i].Available && ports[i].ServiceName == serviceName)
                {
                    // Service already using a port — reject the request
                    return new PRSMessage(
                        PRSMessage.MESSAGE_TYPE.RESPONSE,
                        serviceName,
                        ports[i].Port,
                        PRSMessage.STATUS.SERVICE_IN_USE
                    );
                }
            }

            return RequestAvaliablePort(serviceName);
        }

        public PRSMessage HandleMessage(PRSMessage msg)
        {
            // TODO: PRS.HandleMessage()
            CheckForExpiredPorts();
            // handle one message and return a response
            //Console.WriteLine($"{DateTime.Now}: Received {msg.MsgType} from {msg.ServiceName} on port {msg.Port}");

            PRSMessage response = null;

            switch (msg.MsgType)
            {
                case PRSMessage.MESSAGE_TYPE.REQUEST_PORT:
                    {
                        // check for expired ports and send requested report
                        CheckForExpiredPorts();
                        response = RequestPort(msg.ServiceName);
                    }
                    break;

                case PRSMessage.MESSAGE_TYPE.KEEP_ALIVE:
                    {
                        // client has requested that we keep their port alive
                        // find the port
                        var portEntry = ports.FirstOrDefault(p => p.Port == msg.Port);

                        if (portEntry != null && portEntry.Available)
                        {
                            // port is available, send SERVICE_NOT_FOUND
                            response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SERVICE_NOT_FOUND);
                        }
                        else if (portEntry != null && portEntry.ServiceName == msg.ServiceName)
                        {
                            // port is not available, keep it alive and send SUCCESS
                            portEntry.KeepAlive();
                            response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SUCCESS);
                        }
                        else
                        {
                            // port not found at all, send SERVICE_NOT_FOUND
                            response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SERVICE_NOT_FOUND);
                        }
                        break;

                    }

                case PRSMessage.MESSAGE_TYPE.CLOSE_PORT:
                    {
                        // client has requested that we close their port, and make it available for others!
                        // find the port
                        var portEntry = ports.FirstOrDefault(p => p.Port == msg.Port);

                        if (portEntry == null)
                        {
                            return new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SERVICE_NOT_FOUND);
                        }

                        if (portEntry.Available || portEntry.Port == 0 || portEntry == null)
                        {
                            // port doesn't exist or is already available — send SERVICE_NOT_FOUND
                            response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SERVICE_NOT_FOUND);
                        }
                        else if (portEntry.ServiceName == msg.ServiceName)
                        {
                            // port is in use, close it and send SUCCESS
                            portEntry.Close();
                            response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SUCCESS);
                        }
                        else
                        {
                            // port is in use — and send SUCCESS
                            portEntry.Close();
                            response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SUCCESS);
                        }

                    }
                    break;

                case PRSMessage.MESSAGE_TYPE.LOOKUP_PORT:
                    {
                        // client wants to know the reserved port number for a named service
                        // find the port
                        foreach (PortReservation port in ports)
                        {
                            if (port.ServiceName == msg.ServiceName)
                            {
                                // found the port, send it back
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, port.Port, PRSMessage.STATUS.SUCCESS);
                                break;
                            }
                        }
                        // if found, send port number back
                        // else, SERVICE_NOT_FOUND
                        if (response == null)
                        {
                            response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, 0, PRSMessage.STATUS.SERVICE_NOT_FOUND);
                        }
                    }
                    break;

                case PRSMessage.MESSAGE_TYPE.STOP:
                    {
                        // client is telling us to close the appliation down
                        // stop the PRS and return SUCCESS
                        stopped = true;

                        response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, 0, PRSMessage.STATUS.SUCCESS);
                    }
                    break;
            }

            return response;
        }

    }

}
