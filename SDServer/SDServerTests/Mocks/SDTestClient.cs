using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Mocks
{
    public class SDTestClient
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;

        public async Task ConnectAsync(string host, int port)
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(host, port);
            _stream = _tcpClient.GetStream();
        }

        private async Task<string> SendCommandAsync(string command)
        {
            var buffer = Encoding.UTF8.GetBytes(command );
            await _stream.WriteAsync(buffer, 0, buffer.Length);

            byte[] responseBuffer = new byte[4096];
            int bytesRead = await _stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
           return Encoding.UTF8.GetString(responseBuffer, 0, bytesRead).Trim();
        }

        public async Task<ulong> OpenSessionAsync()
        {
            string response = await SendCommandAsync("open\n");
            if (string.IsNullOrEmpty(response))
                throw new Exception("No response from server when opening session.");

            if (response.StartsWith("accepted", StringComparison.OrdinalIgnoreCase))
            {
                response = response.Remove(0, response.IndexOf('\n')+1);

                // Try to parse the response as a ulong session ID
                if (ulong.TryParse(response, out var sessionId))
                    return sessionId;
            }

            throw new Exception("Invalid session ID returned: " + response);
        }

        public async Task<string> GetSessionValue(string key)
        {
            string response = await SendCommandAsync($"get\n{key}\n");
            return response;
        }

        public async Task PostSessionValue(string fileName, string value)
        {
            string response = await SendCommandAsync($"post\n{fileName}\n{value.Length}\n{value}");
            if (!response.StartsWith("success", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Failed to put session value: " + response);
        }

        public async Task CloseSession(ulong sessionId)
        {
            string response = await SendCommandAsync($"close\n{sessionId}\n");
            if (response.StartsWith("closed", StringComparison.OrdinalIgnoreCase))
            {
                response = response.Remove(0, response.IndexOf('\n') + 1);
            }
        }
        public async Task<ulong> ResumeSessionAsync(ulong sessionId)
        {
            string response = await SendCommandAsync($"resume\n{sessionId}\n");
            if (string.IsNullOrEmpty(response))
                throw new Exception("No response from server when resuming session.");
            if (response.StartsWith("accepted", StringComparison.OrdinalIgnoreCase))
                return sessionId; // Session resumed successfully, return the same ID

            if (response.Equals("not found", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Session not found: " + sessionId);
            
            throw new Exception("Failed to resume session: " + response);
        }

        public void Disconnect()
        {
            _stream?.Dispose();
            _tcpClient?.Close();
        }
    }
}
