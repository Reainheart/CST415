// ContentFetcher.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 
// Noah Etchemendy
// CST 415
// Spring 2025
// 
using MVCBrowser.Interfaces;
using System;
using System.Collections.Generic;


namespace SDBrowser
{
    class ContentFetcher : IContentFetcher
    {
        public Dictionary<string, IProtocolClient> Protocols = new Dictionary<string, IProtocolClient>();
        public void Close()
        {
            // close each protocol client
            foreach (var client in Protocols.Values)
            {
                client.Close();
            }
        }

        public void AddProtocol(string name, IProtocolClient client)
        {
            // save the protocol client under the given name
            Protocols[name] = client; 
        }

        public string Fetch(string type, string address, string resourceName)
        {
            // check if type is null or empty
            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(resourceName))
            {
                throw new ArgumentException("Type, address, and resource name cannot be empty.");
            }

            // retrieve the correct protocol client for the requested protocol
            // watch out for invalid type
            if (!Protocols.TryGetValue(type, out IProtocolClient protocolClient))
            {
                throw new ArgumentException($"Unknown protocol type: {type}");
            }

            // get the content from the protocol client, using the given IP address and resource name
            // return the content
            return protocolClient.GetDocument(address, resourceName);
        }
    }
}
