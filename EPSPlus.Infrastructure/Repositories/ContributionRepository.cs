using EPSPlus.Application.Interfaces;
using EPSPlus.Domain.Entities;
using EPSPlus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EPSPlus.Infrastructure.Repositories
{
    public class ContributionRepository : IContributionRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContributionRepository> _logger;
        private readonly IMemoryCache _cache;

        public ContributionRepository(ApplicationDbContext context, ILogger<ContributionRepository> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        public async Task<IEnumerable<Contribution>> GetAllByMemberIdAsync(Guid memberId)
        {
            string cacheKey = $"contributions_{memberId}";

            _logger.LogInformation("Checking cache for MemberId: {MemberId}", memberId);

            if (_cache.TryGetValue(cacheKey, out IEnumerable<Contribution> cachedContributions))
            {
                _logger.LogInformation("Returning cached contributions for MemberId: {MemberId}", memberId);
                return cachedContributions;
            }

            _logger.LogInformation("Fetching contributions from DB for MemberId: {MemberId}", memberId);

            var contributions = await _context.Contributions
                .Where(c => c.MemberId == memberId)
                .ToListAsync();

            _logger.LogInformation("Fetched {Count} contributions from DB for MemberId: {MemberId}", contributions.Count, memberId);

            // Store in Cache for 10 minutes
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

            _cache.Set(cacheKey, contributions, cacheOptions);

            return contributions;
        }

        public async Task<Contribution?> GetByIdAsync(Guid id)
        {
            string cacheKey = $"contribution_{id}";

            _logger.LogInformation("Checking cache for ContributionId: {ContributionId}", id);

            if (_cache.TryGetValue(cacheKey, out Contribution? cachedContribution))
            {
                _logger.LogInformation("Returning cached contribution for ContributionId: {ContributionId}", id);
                return cachedContribution;
            }

            _logger.LogInformation("Fetching contribution from DB for ContributionId: {ContributionId}", id);

            var contribution = await _context.Contributions.FindAsync(id);

            if (contribution == null)
            {
                _logger.LogWarning("Contribution with Id: {ContributionId} not found", id);
                return null;
            }

            // Store in Cache for 10 minutes
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

            _cache.Set(cacheKey, contribution, cacheOptions);

            return contribution;
        }

        public async Task AddAsync(Contribution contribution)
        {
            _logger.LogInformation("Attempting to add contribution for MemberId: {MemberId}", contribution.MemberId);

            var memberExists = await _context.Members.AnyAsync(m => m.Id == contribution.MemberId);
            if (!memberExists)
            {
                _logger.LogWarning("The specified MemberId: {MemberId} does not exist", contribution.MemberId);
                throw new ArgumentException("The specified member does not exist.");
            }

            contribution.Status = ContributionStatus.Pending;
            _context.Contributions.Add(contribution);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully added contribution with Id: {ContributionId} for MemberId: {MemberId}", contribution.Id, contribution.MemberId);
        }

        public async Task UpdateContributionStatusAsync(Guid contributionId, ContributionStatus status)
        {
            _logger.LogInformation("Updating status of contribution with Id: {ContributionId} to Status: {Status}", contributionId, status);

            var contribution = await _context.Contributions.FindAsync(contributionId);
            if (contribution == null)
            {
                _logger.LogWarning("Contribution with Id: {ContributionId} not found", contributionId);
                throw new ArgumentException("Contribution not found.");
            }

            contribution.Status = status;
            _context.Contributions.Update(contribution);
            await _context.SaveChangesAsync();

            // Remove the outdated cache entry after update
            string cacheKey = $"contribution_{contributionId}";
            _cache.Remove(cacheKey);

            _logger.LogInformation("Successfully updated status of contribution with Id: {ContributionId} to Status: {Status}");

            // refresh the cache with the updated contribution
            _cache.Set(cacheKey, contribution, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });
        }

        public async Task DeleteAsync(Guid id)
        {
            _logger.LogInformation("Attempting to delete contribution with Id: {ContributionId}", id);

            var contribution = await _context.Contributions.FindAsync(id);
            if (contribution != null)
            {
                _context.Contributions.Remove(contribution);
                await _context.SaveChangesAsync();

                // Invalidate cache after successful deletion
                string cacheKey = $"contribution_{id}";
                _cache.Remove(cacheKey);

                _logger.LogInformation("Successfully deleted contribution with Id: {ContributionId} and invalidated cache", id);
            }
            else
            {
                _logger.LogWarning("Contribution with Id: {ContributionId} not found for deletion", id);
            }
        }

        public async Task<bool> CheckMemberExistsAsync(Guid memberId)
        {
            _logger.LogInformation("Checking if MemberId: {MemberId} exists", memberId);

            var memberExists = await _context.Members.AnyAsync(m => m.Id == memberId);

            if (memberExists)
            {
                _logger.LogInformation("MemberId: {MemberId} exists.", memberId);
            }
            else
            {
                _logger.LogWarning("MemberId: {MemberId} does not exist.", memberId);
            }

            return memberExists;
        }

        public async Task<IEnumerable<Contribution>> GetAllContributionsAsync()
        {
            _logger.LogInformation("Fetching all contributions");

            var contributions = await _context.Contributions.ToListAsync();

            _logger.LogInformation("Fetched {Count} contributions", contributions.Count);
            return contributions;
        }

        public async Task<IEnumerable<Contribution>> GetFailedContributionsAsync()
        {
            _logger.LogInformation("Fetching all failed contributions");

            var failedContributions = await _context.Contributions.Where(c => c.Status == ContributionStatus.Failed).ToListAsync();

            _logger.LogInformation("Fetched {Count} failed contributions", failedContributions.Count);
            return failedContributions;
        }

        public async Task<IEnumerable<Member>> GetAllMembersAsync()
        {
            _logger.LogInformation("Fetching all members");

            var members = await _context.Members.ToListAsync();

            _logger.LogInformation("Fetched {Count} members", members.Count);
            return members;
        }

        public async Task UpdateAsync(Contribution contribution)
        {
            _logger.LogInformation("Updating contribution with Id: {ContributionId}", contribution.Id);

            _context.Contributions.Update(contribution);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully updated contribution with Id: {ContributionId}", contribution.Id);
        }
    }
}
