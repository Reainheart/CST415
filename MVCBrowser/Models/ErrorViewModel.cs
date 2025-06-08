// Noah Etchemendy
// CST 415
// Spring 2025
// 
namespace MVCBrowser.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
