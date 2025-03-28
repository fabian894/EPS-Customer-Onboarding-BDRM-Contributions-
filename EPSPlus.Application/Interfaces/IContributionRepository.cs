using EPSPlus.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EPSPlus.Application.Interfaces
{
    public interface IContributionRepository
    {
        Task<IEnumerable<Contribution>> GetAllByMemberIdAsync(Guid memberId);
        Task<Contribution?> GetByIdAsync(Guid id);
        Task AddAsync(Contribution contribution);
        Task UpdateContributionStatusAsync(Guid contributionId, ContributionStatus status);
        Task DeleteAsync(Guid id);

        Task<bool> CheckMemberExistsAsync(Guid memberId);

        Task<IEnumerable<Contribution>> GetAllContributionsAsync();
        Task<IEnumerable<Contribution>> GetFailedContributionsAsync();
        Task<IEnumerable<Member>> GetAllMembersAsync();
        Task UpdateAsync(Contribution contribution);
    }
}
