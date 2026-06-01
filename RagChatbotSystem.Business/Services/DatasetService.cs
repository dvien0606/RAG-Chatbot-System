using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RagChatbotSystem.Business.DTOs;
using RagChatbotSystem.Business.Interfaces;
using RagChatbotSystem.DataAccess.Repositories;
using RagChatbotSystem.DataAccess.Models;

namespace RagChatbotSystem.Business.Services
{
    public class DatasetService : IDatasetService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Dataset> _datasetRepository;
        private readonly IGenericRepository<User> _userRepository;

        public DatasetService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _datasetRepository = _unitOfWork.Repository<Dataset>();
            _userRepository = _unitOfWork.Repository<User>();
        }

        public async Task<IReadOnlyList<DatasetDto>> GetDatasetsAsync(Guid? createdBy = null, CancellationToken cancellationToken = default)
        {
            var query = _datasetRepository.GetQueryable().AsNoTracking();

            if (createdBy.HasValue)
            {
                query = query.Where(d => d.CreatedBy == createdBy.Value);
            }

            return await query
                .OrderByDescending(d => d.UpdatedAt)
                .Select(d => new DatasetDto(
                    d.DatasetId,
                    d.Name,
                    d.Description,
                    d.CreatedBy,
                    d.CreatedAt,
                    d.UpdatedAt,
                    d.Documents.Count(),
                    d.IsPublic,
                    d.IsApproved))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<DatasetDto>> GetDatasetsForUserAsync(Guid userId, string role, CancellationToken cancellationToken = default)
        {
            var query = _datasetRepository.GetQueryable().AsNoTracking();

            if (role != "Admin")
            {
                query = query.Where(d =>
                    (d.IsPublic && d.IsApproved) ||
                    d.CreatedBy == userId ||
                    d.DatasetPermissions.Any(dp => dp.UserId == userId)
                );
            }

            return await query
                .OrderByDescending(d => d.UpdatedAt)
                .Select(d => new DatasetDto(
                    d.DatasetId,
                    d.Name,
                    d.Description,
                    d.CreatedBy,
                    d.CreatedAt,
                    d.UpdatedAt,
                    d.Documents.Count(),
                    d.IsPublic,
                    d.IsApproved))
                .ToListAsync(cancellationToken);
        }

        public async Task<DatasetDto?> GetDatasetAsync(Guid datasetId, CancellationToken cancellationToken = default)
        {
            return await _datasetRepository.GetQueryable()
                .AsNoTracking()
                .Where(d => d.DatasetId == datasetId)
                .Select(d => new DatasetDto(
                    d.DatasetId,
                    d.Name,
                    d.Description,
                    d.CreatedBy,
                    d.CreatedAt,
                    d.UpdatedAt,
                    d.Documents.Count(),
                    d.IsPublic,
                    d.IsApproved))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<DatasetDto> CreateDatasetAsync(CreateDatasetRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Dataset name is required.", nameof(request));
            }

            var creatorExists = await _userRepository.GetQueryable().AnyAsync(u => u.UserId == request.CreatedBy, cancellationToken);
            if (!creatorExists)
            {
                throw new InvalidOperationException("Creator user was not found.");
            }

            var now = DateTime.UtcNow;
            var dataset = new Dataset
            {
                DatasetId = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                CreatedBy = request.CreatedBy,
                CreatedAt = now,
                UpdatedAt = now,
                IsPublic = request.IsPublic,
                IsApproved = true // Giáo viên/Admin tải lên mặc định sẽ tự động duyệt ngay
            };

            await _datasetRepository.AddAsync(dataset, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ToDto(dataset);
        }

        public async Task<bool> UpdateDatasetAsync(Guid datasetId, string name, string? description, bool isPublic, CancellationToken cancellationToken = default)
        {
            var dataset = await _datasetRepository.GetByIdAsync(datasetId, cancellationToken);
            if (dataset == null)
            {
                return false;
            }

            dataset.Name = name.Trim();
            dataset.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            dataset.IsPublic = isPublic;
            dataset.UpdatedAt = DateTime.UtcNow;

            _datasetRepository.Update(dataset);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> DeleteDatasetAsync(Guid datasetId, CancellationToken cancellationToken = default)
        {
            var dataset = await _datasetRepository.GetByIdAsync(datasetId, cancellationToken);
            if (dataset == null)
            {
                return false;
            }

            _datasetRepository.Delete(dataset);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> ApproveDatasetAsync(Guid datasetId, bool approve, CancellationToken cancellationToken = default)
        {
            var dataset = await _datasetRepository.GetByIdAsync(datasetId, cancellationToken);
            if (dataset == null)
            {
                return false;
            }

            dataset.IsApproved = approve;
            dataset.UpdatedAt = DateTime.UtcNow;

            _datasetRepository.Update(dataset);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> GrantPermissionAsync(Guid datasetId, Guid userId, CancellationToken cancellationToken = default)
        {
            var permissionRepo = _unitOfWork.Repository<DatasetPermission>();
            var exists = await permissionRepo.GetQueryable()
                .AnyAsync(dp => dp.DatasetId == datasetId && dp.UserId == userId, cancellationToken);

            if (!exists)
            {
                var permission = new DatasetPermission
                {
                    PermissionId = Guid.NewGuid(),
                    DatasetId = datasetId,
                    UserId = userId,
                    GrantedAt = DateTime.UtcNow
                };
                await permissionRepo.AddAsync(permission, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return true;
        }

        public async Task<bool> RevokePermissionAsync(Guid datasetId, Guid userId, CancellationToken cancellationToken = default)
        {
            var permissionRepo = _unitOfWork.Repository<DatasetPermission>();
            var permission = await permissionRepo.GetQueryable()
                .FirstOrDefaultAsync(dp => dp.DatasetId == datasetId && dp.UserId == userId, cancellationToken);

            if (permission != null)
            {
                permissionRepo.Delete(permission);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return true;
            }

            return false;
        }

        public async Task<IReadOnlyList<UserDto>> GetPermittedUsersAsync(Guid datasetId, CancellationToken cancellationToken = default)
        {
            var permissionRepo = _unitOfWork.Repository<DatasetPermission>();
            return await permissionRepo.GetQueryable()
                .AsNoTracking()
                .Where(dp => dp.DatasetId == datasetId)
                .OrderBy(dp => dp.User.FullName)
                .Select(dp => new UserDto(
                    dp.User.UserId,
                    dp.User.FullName,
                    dp.User.Email,
                    dp.User.Role,
                    dp.User.CreatedAt,
                    dp.User.IsApproved))
                .ToListAsync(cancellationToken);
        }

        private static DatasetDto ToDto(Dataset dataset)
        {
            return new DatasetDto(
                dataset.DatasetId,
                dataset.Name,
                dataset.Description,
                dataset.CreatedBy,
                dataset.CreatedAt,
                dataset.UpdatedAt,
                dataset.Documents.Count,
                dataset.IsPublic,
                dataset.IsApproved);
        }
    }
}

