using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RagChatbotSystem.Presentation.Pages.Account
{
    public class RegisterModel : PageModel
    {
        public IActionResult OnGet()
        {
            TempData["ErrorMessage"] = "Tài khoản Sinh viên và Giảng viên sẽ do Admin cấp. Vui lòng liên hệ Admin nếu bạn cần tài khoản.";
            return RedirectToPage("/Account/Login");
        }

        public IActionResult OnPost()
        {
            TempData["ErrorMessage"] = "Tài khoản Sinh viên và Giảng viên sẽ do Admin cấp. Vui lòng liên hệ Admin nếu bạn cần tài khoản.";
            return RedirectToPage("/Account/Login");
        }
    }
}
