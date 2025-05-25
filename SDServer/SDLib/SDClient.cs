using System.Net.Sockets;
using System.Text;

namespace SDLib
{
    public class SDClient : IDisposable
    {
        private readonly TcpClient client;
        private readonly StreamReader reader;
        private readonly StreamWriter writer;

        public SDClient(string host, int port)
        {
            client = new TcpClient();
            client.Connect(host, port);

            var stream = client.GetStream();
            reader = new StreamReader(stream, Encoding.UTF8);
            writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
        }

        public async Task<ulong> OpenSessionAsync()
        {
            await writer.WriteLineAsync("OPEN");
            string response = await reader.ReadLineAsync() ?? throw new IOException("No response from server");

            if (response.StartsWith("OK "))
            {
                return ulong.Parse(response.Substring(3));
            }

            throw new Exception($"Server error: {response}");
        }

        public async Task PutAsync(ulong sessionId, string key, string value)
        {
            await writer.WriteLineAsync($"PUT {sessionId} {key} {value}");
            string response = await reader.ReadLineAsync() ?? throw new IOException("No response from server");

            if (!response.StartsWith("OK"))
                throw new Exception($"Server error: {response}");
        }

        public async Task<string?> GetAsync(ulong sessionId, string key)
        {
            await writer.WriteLineAsync($"GET {sessionId} {key}");
            string response = await reader.ReadLineAsync() ?? throw new IOException("No response from server");

            if (response == "OK")
                return null;

            if (response.StartsWith("OK "))
                return response.Substring(3);

            throw new Exception($"Server error: {response}");
        }

        public async Task CloseSessionAsync(ulong sessionId)
        {
            await writer.WriteLineAsync($"CLOSE {sessionId}");
            string response = await reader.ReadLineAsync() ?? throw new IOException("No response from server");

            if (!response.StartsWith("OK"))
                throw new Exception($"Server error: {response}");
        }

        public void Dispose()
        {
            client?.Close();
        }
    }
}
