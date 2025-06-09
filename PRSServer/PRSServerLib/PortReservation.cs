using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRSServer
{
    public class PortReservation
    {
        private ushort port;
        private bool available;
        private string serviceName;
        private DateTime lastAlive;

        public PortReservation(ushort port)
        {
            this.port = port;
            available = true;
            serviceName = string.Empty;
            lastAlive = DateTime.MinValue; // Not alive until reserved
        }


        public string ServiceName => serviceName;
        public ushort Port => port;
        public bool Available => available;

        public bool Expired(int timeout)
        {
            // return true if timeout seconds have elapsed since lastAlive

            return (DateTime.Now - lastAlive).TotalSeconds > timeout;
        }

        public void Reserve(string serviceName)
        {
            if (available)
            {
                this.serviceName = serviceName;
                available = false;
                lastAlive = DateTime.Now;
            }
        }

        public void KeepAlive()
        {
            if (!available)
            {
                lastAlive = DateTime.Now;
            }
        }

        public void Close()
        {
            serviceName = string.Empty;
            available = true;
            lastAlive = DateTime.MinValue;
        }
    }

}
