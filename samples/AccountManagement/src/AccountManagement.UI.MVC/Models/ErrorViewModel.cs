namespace AccountManagement.UI.MVC.Models;

public class ErrorViewModel
{
   public string RequestId { get; init; } = string.Empty;

   public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}