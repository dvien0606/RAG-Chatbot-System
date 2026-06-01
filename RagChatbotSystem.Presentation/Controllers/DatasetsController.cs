using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagChatbotSystem.Business.DTOs;
using RagChatbotSystem.Business.Interfaces;

namespace RagChatbotSystem.Presentation.Controllers
{
    [Authorize]
    public class DatasetsController : Controller
    {
        private readonly IDatasetService _datasetService;

        public DatasetsController(IDatasetService datasetService)
        {
            _datasetService = datasetService;
        }

        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name, string? description, bool isPublic)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return RedirectToAction("Index", "Home", new { error = "Tên Dataset không được để trống." });
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var currentUserId))
            {
                return Challenge();
            }

            try
            {
                var request = new CreateDatasetRequest(name, description, currentUserId, isPublic);
                var dataset = await _datasetService.CreateDatasetAsync(request);
                return RedirectToAction("Index", "Home", new { datasetId = dataset.DatasetId, success = $"Tạo Dataset '{name}' thành công!" });
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Home", new { error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, string name, string? description, bool isPublic)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return RedirectToAction("Index", "Home", new { datasetId = id, error = "Tên Dataset không được để trống." });
            }

            var dataset = await _datasetService.GetDatasetAsync(id);
            if (dataset == null)
            {
                return NotFound();
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole != "Admin" && dataset.CreatedBy.ToString() != userIdString)
            {
                return RedirectToAction("Index", "Home", new { datasetId = id, error = "Bạn chỉ có thể chỉnh sửa học liệu do chính mình tạo." });
            }

            try
            {
                await _datasetService.UpdateDatasetAsync(id, name, description, isPublic);
                return RedirectToAction("Index", "Home", new { datasetId = id, success = "Cập nhật thông tin Dataset thành công!" });
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Home", new { datasetId = id, error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var dataset = await _datasetService.GetDatasetAsync(id);
            if (dataset == null)
            {
                return NotFound();
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole != "Admin" && dataset.CreatedBy.ToString() != userIdString)
            {
                return RedirectToAction("Index", "Home", new { datasetId = id, error = "Bạn chỉ có thể xóa học liệu do chính mình tạo." });
            }

            try
            {
                await _datasetService.DeleteDatasetAsync(id);
                return RedirectToAction("Index", "Home", new { success = "Xóa học liệu thành công!" });
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Home", new { datasetId = id, error = ex.Message });
            }
        }
    }
}
