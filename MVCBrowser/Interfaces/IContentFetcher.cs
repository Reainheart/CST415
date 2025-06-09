using SDBrowser;

namespace MVCBrowser.Interfaces
{
    public interface IContentFetcher
    {
        public void AddProtocol(string name, IProtocolClient client);
        public void Close();
        public string Fetch(string type, string address, string resourceName);

    }
}
