// HomeController.cs
// Noah Etchemendy
// CST 415
// Spring 2025
// 
using FTLib;
using Microsoft.AspNetCore.Mvc;
using MVCBrowser.Models;
using SDLib;
using System.Diagnostics;

namespace MVCBrowser.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
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
                // Validate protocol
                if (model.Protocol != "SD" && model.Protocol != "FT")
                {
                    model.ResultMessage = "Invalid protocol selected.";
                    return View(model);
                }

                // Initialize clients based on selected protocol
                if (model.Protocol == "SD")
                {
                    // Initialize SDClient and perform search
                    SimpleDocumentClient sdClient = new(model.Address, 40000);
                    sdClient.Connect();
                    string content = sdClient.GetDocument(model.SearchName.Trim('/'));
                    model.ResultMessage = $"SD Server returned: {content}";
                }
                else if (model.Protocol == "FT")
                {
                    // Initialize FTClient and perform search
                    FTClient ftClient = new(model.Address, "127.0.0.1", 30000, "FTClient");
                    ftClient.Connect();
                    ftClient.GetDirectory(model.SearchName.Trim('/'));
                    model.ResultMessage = $"Downloaded directory '{model.SearchName}' from FT server at {model.Address}.";
                }
                else
                {
                    model.ResultMessage = "Search complete!"; // or your result
                }
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
