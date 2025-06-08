// Noah Etchemendy
// CST 415
// Spring 2025
// 
namespace MVCBrowser.Models
{
    public class IndexBrowserModel
    {
        public string Protocol { get; set; } = "SD";
        public string Address { get; set; } = "127.0.0.1";
        public string SearchName { get; set; } = "/foo/bar";
        public string? ResultMessage { get; set; }
    }
}
