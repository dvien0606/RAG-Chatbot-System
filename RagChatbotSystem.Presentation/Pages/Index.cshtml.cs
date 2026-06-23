using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using RagChatbotSystem.Business.DTOs;
using RagChatbotSystem.Business.Interfaces;
using RagChatbotSystem.Presentation.Helpers;

namespace RagChatbotSystem.Presentation.Pages
{
    [Authorize]
    [RequestSizeLimit(52428800)]
    public class IndexModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IDatasetService _datasetService;
        private readonly IDocumentService _documentService;
        private readonly IChatSessionService _chatSessionService;
        private readonly IChatService _chatService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IUserService userService,
            IDatasetService datasetService,
            IDocumentService documentService,
            IChatSessionService chatSessionService,
            IChatService chatService,
            ILogger<IndexModel> logger)
        {
            _userService = userService;
            _datasetService = datasetService;
            _documentService = documentService;
            _chatSessionService = chatSessionService;
            _chatService = chatService;
            _logger = logger;
        }

        public Guid SelectedUserId { get; set; }
        public Guid? SelectedDatasetId { get; set; }
        public Guid? SelectedSessionId { get; set; }
        public Guid? SelectedMessageId { get; set; }

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public UserDto SelectedUser { get; set; } = null!;
        public IReadOnlyList<DatasetDto> Datasets { get; set; } = new List<DatasetDto>();
        public DatasetDto? SelectedDataset { get; set; }
        public IReadOnlyList<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
        public IReadOnlyList<ChatSessionDto> Sessions { get; set; } = new List<ChatSessionDto>();
        public ChatSessionDto? SelectedSession { get; set; }
        public IReadOnlyList<ChatMessageDto> MessageHistory { get; set; } = new List<ChatMessageDto>();
        public IReadOnlyList<CitationDto> Citations { get; set; } = new List<CitationDto>();

        public async Task<IActionResult> OnGetAsync(
            Guid? datasetId = null, 
            Guid? sessionId = null, 
            Guid? citationId = null, 
            string? error = null, 
            string? success = null)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var currentUserId))
            {
                return RedirectToPage("/Account/Login");
            }

            var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "Student";
            if (currentUserRole == "Admin")
            {
                return RedirectToPage("/Admin/Index");
            }

            if (string.Equals(User.FindFirstValue("MustChangePassword"), "True", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToPage("/Account/ChangePassword");
            }

            SelectedUserId = currentUserId;
            SelectedDatasetId = datasetId;
            SelectedSessionId = sessionId;
            SelectedMessageId = citationId;
            ErrorMessage = error;
            SuccessMessage = success;

            try
            {
                var user = await _userService.GetUserAsync(currentUserId);
                if (user == null)
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    TempData["ErrorMessage"] = "Phiên làm việc hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToPage("/Account/Login");
                }
                SelectedUser = user;

                Datasets = await _datasetService.GetDatasetsForUserAsync(currentUserId, currentUserRole);

                if (datasetId.HasValue)
                {
                    SelectedDataset = await _datasetService.GetDatasetAsync(datasetId.Value);
                    if (SelectedDataset != null)
                    {
                        var hasAccess = Datasets.Any(d => d.DatasetId == datasetId.Value);
                        if (!hasAccess)
                        {
                            return RedirectToPage(new { error = "Bạn không có quyền truy cập môn học này." });
                        }

                        Documents = await _documentService.GetDocumentsByDatasetAsync(datasetId.Value);
                        Sessions = await _chatSessionService.GetSessionsAsync(currentUserId, datasetId.Value);
                    }
                }

                if (sessionId.HasValue)
                {
                    SelectedSession = await _chatSessionService.GetSessionAsync(sessionId.Value);
                    if (SelectedSession != null)
                    {
                        if (SelectedSession.UserId != currentUserId)
                        {
                            return RedirectToPage(new { datasetId, error = "Không có quyền xem phiên trò chuyện này." });
                        }

                        MessageHistory = await _chatSessionService.GetMessageHistoryAsync(sessionId.Value);
                    }
                }

                if (citationId.HasValue)
                {
                    Citations = await _chatSessionService.GetCitationsAsync(citationId.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải dữ liệu workspace.");
                ErrorMessage = $"Lỗi hệ thống: {ex.Message}";
            }

            return Page();
        }

        // Tạo phòng chat mới
        public async Task<IActionResult> OnPostCreateSessionAsync(Guid datasetId, string? title)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var currentUserId))
            {
                return Challenge();
            }

            try
            {
                var role = User.FindFirstValue(ClaimTypes.Role) ?? "Student";
                var allowedDatasets = await _datasetService.GetDatasetsForUserAsync(currentUserId, role);
                if (!allowedDatasets.Any(d => d.DatasetId == datasetId))
                {
                    return RedirectToPage(new { error = "Bạn không có quyền truy cập môn học này." });
                }

                var session = await _chatSessionService.CreateSessionAsync(new CreateChatSessionRequest(currentUserId, datasetId, title));
                return RedirectToPage(new { datasetId, sessionId = session.SessionId, success = "Khởi tạo phòng chat mới thành công!" });
            }
            catch (Exception ex)
            {
                return RedirectToPage(new { datasetId, error = $"Không thể khởi tạo phòng chat: {ex.Message}" });
            }
        }

        // Tạo Subject mới (Dành cho Giáo viên tự tạo hoặc quản lý nếu được phân công, hoặc do Admin)
        public async Task<IActionResult> OnPostCreateDatasetAsync(string name, string? description, bool isPublic)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var currentUserId))
            {
                return Challenge();
            }

            try
            {
                var request = new CreateDatasetRequest(name, description, currentUserId, isPublic);
                var dataset = await _datasetService.CreateDatasetAsync(request);
                return RedirectToPage(new { datasetId = dataset.DatasetId, success = $"Đã tạo môn học '{dataset.Name}' thành công." });
            }
            catch (Exception ex)
            {
                return RedirectToPage(new { error = $"Tạo môn học thất bại: {ex.Message}" });
            }
        }

        // Xóa Subject
        public async Task<IActionResult> OnPostDeleteDatasetAsync(Guid id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var currentUserId))
            {
                return Challenge();
            }

            try
            {
                var deleted = await _datasetService.DeleteDatasetAsync(id);
                return RedirectToPage(new { success = deleted ? "Xóa môn học thành công." : "Không tìm thấy môn học." });
            }
            catch (Exception ex)
            {
                return RedirectToPage(new { error = $"Xóa môn học thất bại: {ex.Message}" });
            }
        }

        // Tải lên tài liệu
        public async Task<IActionResult> OnPostUploadAsync(Guid datasetId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return RedirectToPage(new { datasetId, error = "Vui lòng chọn tệp để tải lên." });
            }

            if (file.Length > 52428800)
            {
                return RedirectToPage(new { datasetId, error = "Kích thước tệp vượt quá giới hạn (tối đa 50MB)." });
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Student";
            if (!Guid.TryParse(userIdString, out var currentUserId))
            {
                return Challenge();
            }

            var dataset = await _datasetService.GetDatasetAsync(datasetId);
            if (dataset == null)
            {
                return NotFound();
            }

            if (!await _datasetService.CanManageDatasetAsync(currentUserId, userRole, datasetId))
            {
                return RedirectToPage(new { datasetId, error = "Bạn chỉ có quyền tải tài liệu lên môn học được gán." });
            }

            try
            {
                using var stream = file.OpenReadStream();
                var doc = await _documentService.UploadDocumentAsync(datasetId, currentUserId, stream, file.FileName, file.Length);
                
                // Chạy bất đồng bộ tiến trình phân tách và vector hóa
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _documentService.ProcessUploadedDocumentAsync(doc.DocumentId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi xử lý tài liệu background cho DocumentId {DocId}", doc.DocumentId);
                    }
                });

                return RedirectToPage(new { datasetId, success = $"Đang tải lên và xử lý tài liệu '{file.FileName}'." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tải lên tài liệu thất bại.");
                return RedirectToPage(new { datasetId, error = $"Tải tài liệu thất bại: {ex.Message}" });
            }
        }

        // Xóa tài liệu
        public async Task<IActionResult> OnPostDeleteDocumentAsync(Guid datasetId, Guid documentId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Student";
            if (!Guid.TryParse(userIdString, out var currentUserId))
            {
                return Challenge();
            }

            var document = await _documentService.GetDocumentAsync(documentId);
            if (document == null)
            {
                return RedirectToPage(new { datasetId, error = "Không tìm thấy tài liệu cần xóa." });
            }

            if (!await _datasetService.CanManageDatasetAsync(currentUserId, userRole, document.DatasetId))
            {
                return RedirectToPage(new { datasetId, error = "Bạn chỉ có quyền xóa tài liệu môn học được gán." });
            }

            try
            {
                var deleted = await _documentService.DeleteDocumentAsync(documentId);
                return RedirectToPage(new { datasetId, success = deleted ? "Xóa tài liệu thành công." : "Xóa tài liệu thất bại." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Xóa tài liệu thất bại.");
                return RedirectToPage(new { datasetId, error = $"Xóa tài liệu thất bại: {ex.Message}" });
            }
        }

        // AJAX: Gửi tin nhắn và bắt đầu stream
        public async Task<IActionResult> OnPostSendMessageAjaxAsync(Guid datasetId, Guid sessionId, string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return new BadRequestObjectResult(new { error = "Câu hỏi không được để trống." });
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var currentUserId))
            {
                return new UnauthorizedResult();
            }

            try
            {
                var session = await _chatSessionService.GetSessionAsync(sessionId);
                if (session == null || session.UserId != currentUserId)
                {
                    return new ForbidResult();
                }

                // Thực hiện sinh câu trả lời (luồng stream SignalR tự động đẩy các chunk qua RealtimeService)
                var response = await _chatService.SendChatMessageAsync(sessionId, question, HttpContext.RequestAborted);
                return new JsonResult(new
                {
                    userMessage = new
                    {
                        messageId = response.UserMessage.MessageId,
                        content = response.UserMessage.Content,
                        role = response.UserMessage.Role,
                        createdAt = VietnamTime.Format(response.UserMessage.CreatedAt, "HH:mm")
                    },
                    assistantMessage = new
                    {
                        messageId = response.AssistantMessage.MessageId,
                        content = response.AssistantMessage.Content,
                        role = response.AssistantMessage.Role,
                        createdAt = VietnamTime.Format(response.AssistantMessage.CreatedAt, "HH:mm")
                    },
                    citations = response.Citations.Select(c => new
                    {
                        citationId = c.CitationId,
                        fileName = c.FileName,
                        pageNumber = c.PageNumber,
                        quoteText = c.QuoteText,
                        sourceLabel = c.SourceLabel
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi AJAX gửi tin nhắn.");
                return new StatusCodeResult(500);
            }
        }

        // AJAX: Lấy danh sách nguồn trích dẫn
        public async Task<IActionResult> OnGetGetCitationsAsync(Guid messageId)
        {
            try
            {
                var citations = await _chatSessionService.GetCitationsAsync(messageId);
                return new JsonResult(citations.Select(c => new
                {
                    citationId = c.CitationId,
                    fileName = c.FileName,
                    pageNumber = c.PageNumber,
                    quoteText = c.QuoteText,
                    sourceLabel = c.SourceLabel
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lấy nguồn trích dẫn cho message {MessageId}", messageId);
                return new StatusCodeResult(500);
            }
        }

        // AJAX: Lấy phân đoạn tài liệu
        public async Task<IActionResult> OnGetGetChunksAsync(Guid documentId)
        {
            try
            {
                var chunks = await _documentService.GetDocumentChunksAsync(documentId);
                return new JsonResult(chunks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lấy danh sách phân đoạn của tài liệu {DocId}", documentId);
                return new StatusCodeResult(500);
            }
        }
    }
}
