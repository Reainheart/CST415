// HomeController.cs
// Noah Etchemendy
// CST 415
// Spring 2025
// 
using FTLib;
using Microsoft.AspNetCore.Mvc;
using MVCBrowser.Interfaces;
using MVCBrowser.Models;
using SDBrowser;
using SDLib;
using System.Diagnostics;

namespace MVCBrowser.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IContentFetcher _ContentFetcher;

        public HomeController(ILogger<HomeController> logger, IContentFetcher fetcher)
        {
            _ContentFetcher = fetcher ?? throw new ArgumentNullException(nameof(fetcher));
            _ContentFetcher.AddProtocol("SD", new SDProtocolClient("127.0.0.1", 30000)); // PRS IP and port should be set here
            _ContentFetcher.AddProtocol("FT", new FTProtocolClient("127.0.0.1", 30000));
            _logger = logger;
        }


        public IActionResult Index()
        {
            IndexBrowserModel model = new();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(IndexBrowserModel model)
        {
            // Do stuff with model.Protocol, model.Address, model.SearchName


            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(model.Address) || string.IsNullOrWhiteSpace(model.SearchName))
                {
                    model.ResultMessage = "Address and Search Name cannot be empty.";
                    return View(model);
                }

                model.ResultMessage = _ContentFetcher.Fetch(model.Protocol, model.Address, model.SearchName);
            }
            catch (Exception ex)
            {
                model.ResultMessage = $"Error: {ex.Message}";
                return View(model);
            }
            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
