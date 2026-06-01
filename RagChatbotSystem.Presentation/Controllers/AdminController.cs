using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagChatbotSystem.Business.Interfaces;

namespace RagChatbotSystem.Presentation.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUserService _userService;
        private readonly IDatasetService _datasetService;

        public AdminController(IUserService userService, IDatasetService datasetService)
        {
            _userService = userService;
            _datasetService = datasetService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetUsersAsync();
            var datasets = await _datasetService.GetDatasetsAsync();

            ViewBag.Users = users;
            ViewBag.Datasets = datasets;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDataset(Guid id, bool approve)
        {
            var success = await _datasetService.ApproveDatasetAsync(id, approve);
            if (!success)
            {
                return NotFound();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Permissions(Guid id)
        {
            var dataset = await _datasetService.GetDatasetAsync(id);
            if (dataset == null)
            {
                return NotFound();
            }

            var permittedUsers = await _datasetService.GetPermittedUsersAsync(id);
            var allUsers = await _userService.GetUsersAsync();

            // Lọc ra các user chưa được phân quyền truy cập dataset này (ngoại trừ chính người tạo và Admin)
            var availableUsers = allUsers
                .Where(u => u.Role != "Admin" && u.UserId != dataset.CreatedBy && !permittedUsers.Any(pu => pu.UserId == u.UserId))
                .ToList();

            ViewBag.Dataset = dataset;
            ViewBag.PermittedUsers = permittedUsers;
            ViewBag.AvailableUsers = availableUsers;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantPermission(Guid datasetId, Guid userId)
        {
            await _datasetService.GrantPermissionAsync(datasetId, userId);
            return RedirectToAction(nameof(Permissions), new { id = datasetId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokePermission(Guid datasetId, Guid userId)
        {
            await _datasetService.RevokePermissionAsync(datasetId, userId);
            return RedirectToAction(nameof(Permissions), new { id = datasetId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(Guid userId, bool approve)
        {
            var success = await _userService.ApproveUserAsync(userId, approve);
            if (!success)
            {
                return NotFound();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
