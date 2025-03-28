using EPSPlus.Application.Interfaces;
using EPSPlus.Domain.Entities;
using EPSPlus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EPSPlus.Infrastructure.Repositories
{
    public class MemberRepository : IMemberRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MemberRepository> _logger;
        private readonly IMemoryCache _cache;

        public MemberRepository(ApplicationDbContext context, ILogger<MemberRepository> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        public async Task<Member?> GetByIdAsync(Guid id)
        {
            // Try to get member from cache
            if (_cache.TryGetValue($"Member_{id}", out Member cachedMember))
            {
                _logger.LogInformation("Retrieved member {MemberId} from cache.", id);
                return cachedMember;
            }

            _logger.LogInformation("Retrieving member by ID: {MemberId} from database", id);
            var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == id);

            if (member != null)
            {
                // Cache the member
                _cache.Set($"Member_{id}", member, TimeSpan.FromMinutes(10)); // Set cache for 10 minutes
                _logger.LogInformation("Member {MemberId} cached for subsequent requests.", id);
            }

            return member;
        }

        public async Task<IEnumerable<Member>> GetAllAsync()
        {
            const string cacheKey = "ActiveMembers";

            // Try to get the active members list from cache
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Member> cachedMembers))
            {
                _logger.LogInformation("Retrieved active members from cache.");
                return cachedMembers;
            }

            _logger.LogInformation("Retrieving all active members from database.");
            var members = await _context.Members.Where(m => m.IsActive).ToListAsync();

            // Cache the active members list
            _cache.Set(cacheKey, members, TimeSpan.FromMinutes(10)); // Cache for 10 minutes
            _logger.LogInformation("Active members cached for subsequent requests.");

            return members;
        }

        public async Task AddAsync(Member member)
        {
            string fullName = $"{member.FirstName} {member.LastName}";
            _logger.LogInformation("Adding new member: {MemberFullName}", fullName);

            // Check if member with the same ID already exists
            var existingMember = await _context.Members
                .FirstOrDefaultAsync(m => m.Id == member.Id);

            if (existingMember != null)
            {
                // If a member with the same ID exists, throw an exception or handle accordingly
                _logger.LogWarning("Member with ID {MemberId} already exists", member.Id);
                throw new InvalidOperationException("A member with this ID already exists.");
            }

            // Add the new member
            await _context.Members.AddAsync(member);
            await _context.SaveChangesAsync();

            // Clear the cache since the members list might have changed
            _cache.Remove("ActiveMembers");

            _logger.LogInformation("Successfully added member: {MemberFullName}", fullName);
        }

        public async Task UpdateAsync(Member member)
        {
            string fullName = $"{member.FirstName} {member.LastName}";
            _logger.LogInformation("Updating member {MemberFullName} with ID: {MemberId}", fullName, member.Id);

            _context.Members.Update(member);
            await _context.SaveChangesAsync();

            // Clear the cache since the members list might have changed
            _cache.Remove("ActiveMembers");
            _cache.Remove($"Member_{member.Id}"); // Invalidate the cache for the updated member

            _logger.LogInformation("Member {MemberFullName} with ID {MemberId} updated successfully", fullName, member.Id);
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            _logger.LogInformation("Attempting to soft delete member with ID: {MemberId}", id);

            var member = await GetByIdAsync(id);
            if (member != null)
            {
                member.IsActive = false;
                await UpdateAsync(member);

                // Clear the cache since the members list might have changed
                _cache.Remove("ActiveMembers");
                _cache.Remove($"Member_{id}"); // Invalidate the cache for the deleted member

                _logger.LogInformation("Successfully soft deleted member with ID: {MemberId}", id);
            }
            else
            {
                _logger.LogWarning("Member with ID {MemberId} not found for deletion", id);
            }
        }
    }
}
