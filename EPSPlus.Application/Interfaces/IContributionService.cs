using EPSPlus.Application.DTOs;
using EPSPlus.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPSPlus.Application.Interfaces
{
    public interface IContributionService
    {
        Task<IEnumerable<Contribution>> GetContributionsByMember(Guid memberId);
        Task<Contribution?> GetContributionById(Guid id);
        Task<string> AddContribution(Contribution contribution);
        Task<bool> IsEligibleForBenefit(Guid memberId);
        Task<(bool success, string errorMessage)> ProcessContributionAsync(Contribution contribution);
        Task<decimal> CalculateTotalContributionsAsync(Guid memberId);
        Task<ContributionStatementDto> GenerateStatementAsync(Guid memberId, DateTime startDate, DateTime endDate);
        Task<byte[]> GenerateStatementPdfAsync(Guid memberId, DateTime startDate, DateTime endDate);
    }
}
