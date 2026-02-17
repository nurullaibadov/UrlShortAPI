using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Application.Common.Models;
using UrlShrt.Application.DTOs.ApiKey;
using UrlShrt.Application.Interfaces;
using UrlShrt.Domain.Entities;
using UrlShrt.Domain.Interfaces.Repositories;

namespace UrlShrt.Infrastructure.Services.AppServices
{
    public class ApiKeyService : IApiKeyService
    {
        private readonly IApiKeyRepository _apiKeyRepo;
        private readonly IMapper _mapper;

        public ApiKeyService(IApiKeyRepository apiKeyRepo, IMapper mapper)
        {
            _apiKeyRepo = apiKeyRepo;
            _mapper = mapper;
        }

        public async Task<ApiResponse<ApiKeyDto>> CreateAsync(string userId, CreateApiKeyDto dto, CancellationToken ct = default)
        {
            // Max 10 API keys per user
            var existing = await _apiKeyRepo.GetByUserIdAsync(userId, ct);
            if (existing.Count() >= 10)
                return ApiResponse<ApiKeyDto>.Fail("Maximum of 10 API keys allowed per account.", 400);

            var rawKey = GenerateApiKey();

            var apiKey = new ApiKey
            {
                Key = rawKey,
                Name = dto.Name,
                UserId = userId,
                IsActive = true,
                ExpiresAt = dto.ExpiresAt,
                RateLimit = dto.RateLimit
            };

            await _apiKeyRepo.AddAsync(apiKey, ct);

            var responseDto = _mapper.Map<ApiKeyDto>(apiKey);
            responseDto.Key = rawKey; // Return full key ONLY on creation

            return ApiResponse<ApiKeyDto>.Created(responseDto, "API key created. Save this key — it will not be shown again.");
        }

        public async Task<ApiResponse<List<ApiKeyDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
        {
            var keys = await _apiKeyRepo.GetByUserIdAsync(userId, ct);
            return ApiResponse<List<ApiKeyDto>>.Ok(_mapper.Map<List<ApiKeyDto>>(keys));
        }

        public async Task<ApiResponse<bool>> RevokeAsync(Guid id, string userId, CancellationToken ct = default)
        {
            var key = await _apiKeyRepo.GetByIdAsync(id, ct);
            if (key is null) return ApiResponse<bool>.NotFound("API key not found.");
            if (key.UserId != userId) return ApiResponse<bool>.Forbidden("Access denied.");

            key.IsActive = false;
            await _apiKeyRepo.UpdateAsync(key, ct);

            return ApiResponse<bool>.Ok(true, "API key revoked.");
        }

        public async Task<ApiResponse<bool>> ValidateAsync(string apiKey, CancellationToken ct = default)
        {
            var key = await _apiKeyRepo.GetByKeyAsync(apiKey, ct);
            if (key is null || !key.IsActive)
                return ApiResponse<bool>.Unauthorized("Invalid API key.");

            if (key.ExpiresAt.HasValue && key.ExpiresAt.Value < DateTime.UtcNow)
                return ApiResponse<bool>.Unauthorized("API key has expired.");

            await _apiKeyRepo.IncrementRequestCountAsync(apiKey, ct);
            return ApiResponse<bool>.Ok(true);
        }

        private static string GenerateApiKey()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return "sk-" + Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")[..40];
        }
    }
}
