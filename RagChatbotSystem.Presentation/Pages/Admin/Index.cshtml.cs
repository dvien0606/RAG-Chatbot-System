using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using RagChatbotSystem.Business.DTOs;
using RagChatbotSystem.Business.Interfaces;

namespace RagChatbotSystem.Presentation.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IDatasetService _datasetService;
        private readonly IConfiguration _configuration;

        public IndexModel(IUserService userService, IDatasetService datasetService, IConfiguration configuration)
        {
            _userService = userService;
            _datasetService = datasetService;
            _configuration = configuration;
        }

        public IReadOnlyList<UserDto> Users { get; set; } = new List<UserDto>();
        public IReadOnlyList<UserDto> Teachers { get; set; } = new List<UserDto>();
        public IReadOnlyList<DatasetDto> Datasets { get; set; } = new List<DatasetDto>();
        public IReadOnlyList<TeacherSubjectAssignmentDto> Assignments { get; set; } = new List<TeacherSubjectAssignmentDto>();
        public IReadOnlyList<DatasetDto> UnassignedDatasets { get; set; } = new List<DatasetDto>();

        public string? AdminSuccess { get; set; }
        public string? AdminError { get; set; }
        public string? ImportErrors { get; set; }

        public async Task OnGetAsync()
        {
            await PopulateDashboardDataAsync();
        }

        public async Task<IActionResult> OnPostImportStudentsAsync(IFormFile studentsFile)
        {
            if (studentsFile == null || studentsFile.Length == 0)
            {
                TempData["AdminError"] = "Vui lòng chọn một file XLSX để nhập danh sách sinh viên.";
                return RedirectToPage();
            }

            var adminUserId = GetCurrentUserId();
            try
            {
                if (!IsSmtpConfigured())
                {
                    TempData["AdminError"] = "Chưa cấu hình SMTP Email. Vui lòng cấu hình SMTP trước khi nhập sinh viên.";
                    return RedirectToPage();
                }

                await using var stream = studentsFile.OpenReadStream();
                var result = await _userService.ImportStudentsFromXlsxAsync(stream, adminUserId);
                TempData["AdminSuccess"] = $"Nhập sinh viên hoàn tất. Đã tạo {result.CreatedCount}/{result.TotalRows} tài khoản. Thất bại {result.FailedCount}.";
                if (result.FailedCount > 0)
                {
                    TempData["ImportErrors"] = string.Join(" | ", result.Rows.Where(r => !r.Success).Take(8).Select(r => $"Dòng {r.RowNumber}: {r.ErrorMessage}"));
                }
            }
            catch (Exception ex)
            {
                TempData["AdminError"] = $"Nhập sinh viên thất bại: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCreateTeacherAsync(string fullName, string email, Guid[] datasetIds)
        {
            var adminUserId = GetCurrentUserId();
            try
            {
                if (!IsSmtpConfigured())
                {
                    TempData["AdminError"] = "Chưa cấu hình SMTP Email. Vui lòng cấu hình SMTP trước khi cấp tài khoản giảng viên.";
                    return RedirectToPage();
                }

                var provisioned = await _userService.CreateTeacherByAdminAsync(
                    new AdminCreateTeacherRequest(fullName, email, datasetIds),
                    adminUserId);

                TempData["AdminSuccess"] = $"Đã tạo tài khoản giảng viên và gửi email đến {provisioned.Email}. Tên đăng nhập: {provisioned.Username}.";
            }
            catch (Exception ex)
            {
                TempData["AdminError"] = $"Tạo giảng viên thất bại: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostApproveDatasetAsync(Guid id, bool approve)
        {
            var success = await _datasetService.ApproveDatasetAsync(id, approve);
            if (!success)
            {
                return NotFound();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAssignTeacherAsync(Guid datasetId, Guid teacherId)
        {
            try
            {
                await _datasetService.AssignTeacherToDatasetAsync(datasetId, teacherId, GetCurrentUserId());
                TempData["AdminSuccess"] = "Gán giảng viên phụ trách môn học thành công.";
            }
            catch (Exception ex)
            {
                TempData["AdminError"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUnassignTeacherAsync(Guid datasetId)
        {
            await _datasetService.UnassignTeacherFromDatasetAsync(datasetId);
            TempData["AdminSuccess"] = "Đã thu hồi quyền phụ trách môn học.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostApproveUserAsync(Guid userId, bool approve)
        {
            var success = await _userService.ApproveUserAsync(userId, approve);
            if (!success)
            {
                return NotFound();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCreateDatasetAsync(string name, string? description, bool isPublic)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["AdminError"] = "Tên môn học bắt buộc nhập.";
                return RedirectToPage();
            }

            var adminUserId = GetCurrentUserId();
            try
            {
                var request = new CreateDatasetRequest(name, description, adminUserId, isPublic);
                var dataset = await _datasetService.CreateDatasetAsync(request);
                TempData["AdminSuccess"] = $"Đã tạo môn học '{dataset.Name}' thành công.";
            }
            catch (Exception ex)
            {
                TempData["AdminError"] = ex.Message;
            }

            return RedirectToPage();
        }

        private async Task PopulateDashboardDataAsync()
        {
            Users = await _userService.GetUsersAsync();
            Teachers = Users.Where(u => u.Role == "Teacher" && u.IsApproved).ToList();
            Datasets = await _datasetService.GetDatasetsAsync();
            Assignments = await _datasetService.GetTeacherAssignmentsAsync();
            UnassignedDatasets = Datasets.Where(d => d.AssignedTeacherId == null).ToList();

            if (TempData["AdminSuccess"] != null)
                AdminSuccess = TempData["AdminSuccess"] as string;

            if (TempData["AdminError"] != null)
                AdminError = TempData["AdminError"] as string;

            if (TempData["ImportErrors"] != null)
                ImportErrors = TempData["ImportErrors"] as string;
        }

        private Guid GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(value, out var userId))
            {
                throw new InvalidOperationException("Phiên đăng nhập quản trị không hợp lệ.");
            }

            return userId;
        }

        private bool IsSmtpConfigured()
        {
            return !string.IsNullOrWhiteSpace(_configuration["Smtp:Host"]) &&
                   !string.IsNullOrWhiteSpace(_configuration["Smtp:Username"]) &&
                   !string.IsNullOrWhiteSpace(_configuration["Smtp:Password"]) &&
                   !string.IsNullOrWhiteSpace(_configuration["Smtp:FromEmail"]);
        }
    }
}
