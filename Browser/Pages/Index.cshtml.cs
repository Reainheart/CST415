using FTLib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SDLib; // assuming your SDClient namespace

namespace Browser.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        // Clients for the two protocols
        private static FTClient? ftClient;
        private static SDClient? sdClient;

        // Config - you can inject these or read from config in a real app
        // Use these to match your FTClient constructor
        private const string FTServerAddress = "127.0.0.1";
        private const string PRSAddress = "127.0.0.1";
        private const ushort PRSPort = 30000;
        private const string FTServiceName = "FTClient";
        private const string SDServerAddress = "127.0.0.1";
        private const ushort SDServerPort = 23456;
        public string? ResultMessage { get; set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            // Initialize clients if null
            if (ftClient == null)
            {
                ftClient = new FTClient(FTServerAddress, PRSAddress, PRSPort, FTServiceName);
            }
            if (sdClient == null)
            {
                sdClient = new SDClient(SDServerAddress, SDServerPort);
            }
        }
        public IActionResult OnPostSend(string protocol, string address, string searchName)
        {
            try
            {
                if (protocol == "FT")
                {
                    ftClient ??= new FTClient(address, "127.0.0.1", 30000, "FTClient");
                    ftClient.Connect();
                    ftClient.GetDirectory(searchName.Trim('/'));
                    ResultMessage = $"Downloaded directory '{searchName}' from FT server.";
                }
                else if (protocol == "SD")
                {
                    sdClient ??= new SDClient(address, 40000);
                    sdClient.Connect();
                    string content = sdClient.GetDocument(searchName);
                    ResultMessage = $"SD Server returned: {content}";
                }
            }
            catch (Exception ex)
            {
                ResultMessage = $"Error: {ex.Message}";
            }



            return Page(); // don't redirect, so the result shows
        }

    }
}
