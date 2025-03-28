using EPSPlus.Application.DTOs;
using EPSPlus.Application.Interfaces;
using EPSPlus.Domain.Entities;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPSPlus.Application.Services
{
    public class ContributionService : IContributionService
    {
        private readonly IContributionRepository _contributionRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ContributionService> _logger;

        public ContributionService(IContributionRepository contributionRepository, IMemberRepository memberRepository, IEmailService emailService, IMemoryCache cache, ILogger<ContributionService> logger)
        {
            _contributionRepository = contributionRepository;
            _memberRepository = memberRepository;
            _emailService = emailService;
            _cache = cache;
            _logger = logger;

        }

        public async Task<IEnumerable<Contribution>> GetContributionsByMember(Guid memberId)
        {
            string cacheKey = $"contributions_{memberId}";
            if (!_cache.TryGetValue(cacheKey, out IEnumerable<Contribution> contributions))
            {
                _logger.LogInformation($"Fetching contributions from database for Member ID: {memberId}");
                contributions = await _contributionRepository.GetAllByMemberIdAsync(memberId);

                // Cache contributions for 10 minutes
                _cache.Set(cacheKey, contributions, TimeSpan.FromMinutes(10));
            }
            else
            {
                _logger.LogInformation($"Fetching contributions from cache for Member ID: {memberId}");
            }

            return contributions;
        }

        public async Task<Contribution?> GetContributionById(Guid id)
        {
            string cacheKey = $"contribution_{id}";
            if (!_cache.TryGetValue(cacheKey, out Contribution contribution))
            {
                _logger.LogInformation($"Fetching contribution from database for ID: {id}");
                contribution = await _contributionRepository.GetByIdAsync(id);

                if (contribution != null)
                {
                    _cache.Set(cacheKey, contribution, TimeSpan.FromMinutes(10));
                }
            }
            else
            {
                _logger.LogInformation($"Fetching contribution from cache for ID: {id}");
            }

            return contribution;
        }

        public async Task<string> AddContribution(Contribution contribution)
        {
            _logger.LogInformation("Attempting to add a contribution with amount: {Amount} for Member: {MemberId}",
                contribution.Amount, contribution.MemberId);

            if (contribution.Amount <= 0)
            {
                _logger.LogWarning("Contribution amount must be greater than zero for Member: {MemberId}", contribution.MemberId);
                return "Contribution amount must be greater than zero.";
            }

            await _contributionRepository.AddAsync(contribution);
            _logger.LogInformation("Successfully added contribution with amount: {Amount} for Member: {MemberId}",
                contribution.Amount, contribution.MemberId);

            // Invalidate cache for related data
            string totalContributionsCacheKey = $"total_contributions_{contribution.MemberId}";
            string eligibilityCacheKey = $"eligibility_{contribution.MemberId}";
            string contributionsCacheKey = $"contributions_{contribution.MemberId}";

            _logger.LogInformation("Invalidating cache for Member: {MemberId}", contribution.MemberId);

            _cache.Remove(totalContributionsCacheKey);
            _cache.Remove(eligibilityCacheKey);
            _cache.Remove(contributionsCacheKey);

            return "Contribution successfully added.";
        }

        public async Task<bool> IsEligibleForBenefit(Guid memberId)
        {
            string cacheKey = $"eligibility_{memberId}";

            if (!_cache.TryGetValue(cacheKey, out bool isEligible))
            {
                _logger.LogInformation($"Checking eligibility for Member ID: {memberId}");

                // Get all contributions by the member where status is Success (2)
                var contributions = await _contributionRepository
                    .GetAllByMemberIdAsync(memberId);

                // Filter out contributions that are not successful
                var successfulContributions = contributions
                    .Where(c => c.Status == ContributionStatus.Success)
                    .ToList();

                if (successfulContributions == null || !successfulContributions.Any())
                {
                    _cache.Set(cacheKey, false, TimeSpan.FromMinutes(10)); // Cache negative result to avoid repeated queries
                    return false;
                }

                // Find the earliest contribution date (from successful contributions)
                var firstContributionDate = successfulContributions
                    .OrderBy(c => c.ContributionDate)
                    .First().ContributionDate;

                // Calculate the number of months since the first successful contribution
                var monthsContributed = DateTime.UtcNow.Month - firstContributionDate.Month +
                                        12 * (DateTime.UtcNow.Year - firstContributionDate.Year);

                // Check eligibility based on the number of months contributed
                isEligible = monthsContributed >= EligibilityRules.MinimumContributionMonths;

                // Cache the eligibility status for 10 minutes
                _cache.Set(cacheKey, isEligible, TimeSpan.FromMinutes(10));
            }
            else
            {
                _logger.LogInformation($"Fetching eligibility from cache for Member ID: {memberId}");
            }

            return isEligible;
        }

        public async Task<(bool success, string errorMessage)> ProcessContributionAsync(Contribution contribution)
        {
            try
            {
                _logger.LogInformation("Processing contribution for Member: {MemberId} with Amount: {Amount} and Type: {Type}",
                    contribution.MemberId, contribution.Amount, contribution.Type);

                var member = await _memberRepository.GetByIdAsync(contribution.MemberId);
                if (member == null)
                {
                    _logger.LogError("Member not found for Contribution: {ContributionId}", contribution.Id);
                    throw new ArgumentException("Member not found.");
                }

                if (contribution.Type == ContributionType.Monthly)
                {
                    await HandleMonthlyContributionAsync(contribution, member);
                }
                else if (contribution.Type == ContributionType.Voluntary)
                {
                    await HandleVoluntaryContributionAsync(contribution, member);
                }

                bool paymentSuccessful = await SimulatePaymentProcessing(contribution);

                if (!paymentSuccessful)
                {
                    _logger.LogError("Payment processing failed for Contribution: {ContributionId} and Member: {MemberId}",
                        contribution.Id, contribution.MemberId);

                    return (false, "Payment processing failed.");
                }

                _logger.LogInformation("Contribution processed successfully for Member: {MemberId} with Amount: {Amount}",
                    contribution.MemberId, contribution.Amount);

                // Invalidate Cache After Successful Contribution
                string totalContributionsCacheKey = $"total_contributions_{contribution.MemberId}";
                string eligibilityCacheKey = $"eligibility_{contribution.MemberId}";
                string contributionsCacheKey = $"contributions_{contribution.MemberId}";

                _logger.LogInformation("Invalidating cache for Member: {MemberId}", contribution.MemberId);

                _cache.Remove(totalContributionsCacheKey);
                _cache.Remove(eligibilityCacheKey);
                _cache.Remove(contributionsCacheKey);

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing contribution for Member: {MemberId} with Amount: {Amount}",
                    contribution.MemberId, contribution.Amount);
                return (false, ex.Message);
            }
        }

        private async Task<bool> SimulatePaymentProcessing(Contribution contribution)
        {
            await Task.Delay(500); // Simulate some delay
            return true; // Simulate successful payment
        }

        private async Task HandleMonthlyContributionAsync(Contribution contribution, Member member)
        {
            var contributions = await _contributionRepository
                .GetAllByMemberIdAsync(contribution.MemberId);

            var lastMonthlyContribution = contributions
                .Where(c => c.Type == ContributionType.Monthly && c.Status != ContributionStatus.Failed) // Exclude failed contributions
                .OrderByDescending(c => c.ContributionDate)
                .FirstOrDefault();

            if (lastMonthlyContribution != null && lastMonthlyContribution.ContributionDate.Month == DateTime.UtcNow.Month)
            {
                throw new ArgumentException("Member has already made a monthly contribution this month.");
            }
        }

        private async Task HandleVoluntaryContributionAsync(Contribution contribution, Member member)
        {
            if (contribution.Amount <= 0)
            {
                throw new ArgumentException("Voluntary contribution amount must be greater than 0.");
            }

            if (contribution.Amount > 1000)
            {
                throw new ArgumentException("Voluntary contribution cannot exceed 1000.");
            }
        }

        public async Task<decimal> CalculateTotalContributionsAsync(Guid memberId)
        {
            string cacheKey = $"total_contributions_{memberId}";

            if (!_cache.TryGetValue(cacheKey, out decimal totalContributions))
            {
                _logger.LogInformation($"Fetching total contributions from DB for Member ID: {memberId}");

                var contributions = await _contributionRepository.GetAllByMemberIdAsync(memberId);

                // Filter out successful contributions only
                var successfulContributions = contributions
                    .Where(c => c.Status == ContributionStatus.Success)
                    .ToList();

                // Calculate total for Monthly contributions
                var totalMonthly = successfulContributions
                    .Where(c => c.Type == ContributionType.Monthly)
                    .Sum(c => c.Amount);

                // Calculate total for Voluntary contributions
                var totalVoluntary = successfulContributions
                    .Where(c => c.Type == ContributionType.Voluntary)
                    .Sum(c => c.Amount);

                // Calculate the total contributions (only successful ones)
                totalContributions = totalMonthly + totalVoluntary;

                // Cache the result for 10 minutes
                _cache.Set(cacheKey, totalContributions, TimeSpan.FromMinutes(10));
            }
            else
            {
                _logger.LogInformation($"Fetching total contributions from cache for Member ID: {memberId}");
            }

            return totalContributions;
        }

        public async Task<ContributionStatementDto> GenerateStatementAsync(Guid memberId, DateTime startDate, DateTime endDate)
        {
            // Fetch all contributions for the member
            var contributions = await _contributionRepository.GetAllByMemberIdAsync(memberId);
            if (contributions == null || !contributions.Any())
            {
                throw new InvalidOperationException("No contributions found for the given member.");
            }

            // Filter contributions within the date range
            var filteredContributions = contributions
                .Where(c => c.ContributionDate >= startDate && c.ContributionDate <= endDate)
                .ToList();

            // If no contributions are within the date range, return an empty result
            if (!filteredContributions.Any())
            {
                throw new InvalidOperationException("No contributions found for the given date range.");
            }

            // Fetch member details
            var member = await _memberRepository.GetByIdAsync(memberId);
            if (member == null)
            {
                throw new InvalidOperationException("Member not found.");
            }

            // Calculate the total contributions in the date range
            decimal totalContributions = filteredContributions.Sum(c => c.Amount);

            // Check if the member is eligible for any benefits (you can reuse the IsEligibleForBenefit method here)
            bool isEligibleForBenefit = await IsEligibleForBenefit(memberId);

            // Prepare the statement DTO
            var statement = new ContributionStatementDto
            {
                MemberId = memberId,
                MemberName = $"{member.FirstName} {member.LastName}",
                StartDate = startDate,
                EndDate = endDate,
                TotalContributions = totalContributions,
                Contributions = filteredContributions,
                EligibleBenefits = isEligibleForBenefit ? "Eligible for benefits" : "Not eligible for benefits"
            };

            return statement;
        }

        public async Task<byte[]> GenerateStatementPdfAsync(Guid memberId, DateTime startDate, DateTime endDate)
        {
            _logger.LogInformation($"Generating PDF statement for Member ID: {memberId} from {startDate.ToShortDateString()} to {endDate.ToShortDateString()}");

            // Fetch all contributions for the member
            var contributions = await _contributionRepository.GetAllByMemberIdAsync(memberId);
            if (contributions == null || !contributions.Any())
            {
                _logger.LogWarning($"No contributions found for Member ID: {memberId}.");
                throw new InvalidOperationException("No contributions found for the given member.");
            }

            _logger.LogInformation($"Found {contributions.Count()} contributions for Member ID: {memberId}");

            // Filter contributions within the date range and only successful ones
            var filteredContributions = contributions
                .Where(c => c.ContributionDate >= startDate && c.ContributionDate <= endDate && c.Status == ContributionStatus.Success)
                .ToList();

            if (!filteredContributions.Any())
            {
                _logger.LogWarning($"No successful contributions found for Member ID: {memberId} within the date range {startDate.ToShortDateString()} - {endDate.ToShortDateString()}.");
                throw new InvalidOperationException("No successful contributions found for the given date range.");
            }

            _logger.LogInformation($"Found {filteredContributions.Count} successful contributions within the date range.");

            // Fetch member details
            var member = await _memberRepository.GetByIdAsync(memberId);
            if (member == null)
            {
                _logger.LogError($"Member not found for ID: {memberId}");
                throw new InvalidOperationException("Member not found.");
            }

            _logger.LogInformation($"Found member details for Member ID: {memberId}, Name: {member.FirstName} {member.LastName}");

            // Calculate the total contributions (only successful ones)
            decimal totalContributions = filteredContributions.Sum(c => c.Amount);
            _logger.LogInformation($"Total Successful Contributions for Member ID: {memberId} within the date range: {totalContributions:C}");

            // Check eligibility for benefits
            bool isEligibleForBenefit = await IsEligibleForBenefit(memberId);
            _logger.LogInformation($"Member ID: {memberId} Eligibility for benefits: {isEligibleForBenefit}");

            // Prepare the statement DTO
            var statement = new ContributionStatementDto
            {
                MemberId = memberId,
                MemberName = $"{member.FirstName} {member.LastName}",
                StartDate = startDate,
                EndDate = endDate,
                TotalContributions = totalContributions,
                Contributions = filteredContributions,
                EligibleBenefits = isEligibleForBenefit ? "Eligible for benefits" : "Not eligible for benefits"
            };

            _logger.LogInformation($"Statement DTO created for Member ID: {memberId}");

            // Create the PDF document
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    var writer = new PdfWriter(memoryStream);
                    var pdfDocument = new PdfDocument(writer);
                    var document = new Document(pdfDocument);

                    // Add Title
                    document.Add(new Paragraph("Contribution Statement").SimulateBold().SetFontSize(20).SetTextAlignment(TextAlignment.CENTER));

                    // Add Member Details
                    document.Add(new Paragraph($"Member: {statement.MemberName}"));
                    document.Add(new Paragraph($"Member ID: {statement.MemberId}"));
                    document.Add(new Paragraph($"Date Range: {statement.StartDate.ToShortDateString()} - {statement.EndDate.ToShortDateString()}"));
                    document.Add(new Paragraph($"Total Contributions: {statement.TotalContributions:C}"));
                    document.Add(new Paragraph($"Eligible Benefits: {statement.EligibleBenefits}"));

                    // Add Contributions Table
                    var table = new Table(3); // 3 columns: Date, Type, Amount
                    table.AddHeaderCell("Contribution Date");
                    table.AddHeaderCell("Type");
                    table.AddHeaderCell("Amount");

                    foreach (var contribution in statement.Contributions)
                    {
                        table.AddCell(contribution.ContributionDate.ToShortDateString());
                        table.AddCell(contribution.Type.ToString());
                        table.AddCell(contribution.Amount.ToString("C"));
                    }

                    document.Add(table);

                    // Finalize the document
                    document.Close();

                    _logger.LogInformation($"PDF statement generated successfully for Member ID: {memberId}");

                    // Return the PDF as byte array
                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred while generating PDF statement for Member ID: {memberId}: {ex.Message}");
                throw new InvalidOperationException("An error occurred while generating the PDF statement.");
            }
        }
    }
}
