using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbotSystem.Business.DTOs;
using RagChatbotSystem.Business.Interfaces;

namespace RagChatbotSystem.Presentation.Pages.Documents
{
    [Authorize]
    public class PreviewModel : PageModel
    {
        private readonly IDocumentService _documentService;

        public PreviewModel(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        public DocumentPreviewDto Preview { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(Guid documentId)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return Challenge();
            }

            try
            {
                var preview = await _documentService.GetDocumentPreviewAsync(documentId, userId, userRole);
                if (preview == null)
                {
                    return NotFound();
                }

                Preview = preview;
                return Page();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}
