using System;
using System.Collections.Generic;
using EPSPlus.Application.Interfaces;
using EPSPlus.Domain.Entities;
using Hangfire;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace EPSPlus.Application.Services
{
    public class ContributionJobService : IContributionJobService
    {
        private readonly IContributionRepository _contributionRepository;
        private readonly IContributionService _contributionService;
        private readonly ILogger<ContributionJobService> _logger;
        private readonly IEmailService _emailService;

        public ContributionJobService(IContributionRepository contributionRepository, ILogger<ContributionJobService> logger, IContributionService contributionService, IEmailService emailService)
        {
            _contributionRepository = contributionRepository;
            _logger = logger;
            _contributionService = contributionService;
            _emailService = emailService;
        }

        // Job to validate contributions
        [AutomaticRetry(Attempts = 3)]
        public async Task ValidateContributions()
        {
            _logger.LogInformation("Running contribution validation job...");

            var contributions = await _contributionRepository.GetAllContributionsAsync();

            foreach (var contribution in contributions)
            {
                if (contribution.Amount <= 0)
                {
                    _logger.LogWarning($"Invalid contribution detected! ID: {contribution.Id}, Amount: {contribution.Amount}");
                }
                else
                {
                    _logger.LogInformation($"Valid contribution found! ID: {contribution.Id}, Amount: {contribution.Amount}");
                }
            }

            _logger.LogInformation("Contribution validation job completed.");
        }

        public async Task RetryFailedContributions()
        {
            _logger.LogInformation("Retrying failed contributions...");

            var failedContributions = await _contributionRepository.GetFailedContributionsAsync();

            foreach (var contribution in failedContributions)
            {
                try
                {
                    contribution.Status = ContributionStatus.Pending;
                    await _contributionRepository.UpdateAsync(contribution);
                    _logger.LogInformation($"Retried contribution {contribution.Id} successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error retrying contribution {contribution.Id}: {ex.Message}");

                    // Send Email Notification for Failed Transaction
                    string subject = $"Transaction Failed - Contribution ID {contribution.Id}";
                    string body = $"Dear User,<br><br>Your contribution with ID {contribution.Id} has failed due to an error.<br>Please contact support for assistance.<br><br>Best Regards,<br>EPSPlus Team";
                    await _emailService.SendEmailAsync("stehen894@gmail.com", subject, body);

                    _logger.LogInformation($"Email notification sent for failed transaction {contribution.Id}.");
                }
            }

            _logger.LogInformation("Retry job for failed contributions completed.");
        }

        public async Task UpdateBenefitEligibility()
        {
            _logger.LogInformation("Updating benefit eligibility...");

            var members = await _contributionRepository.GetAllMembersAsync();

            foreach (var member in members)
            {
                bool isEligible = await _contributionService.IsEligibleForBenefit(member.Id);
                _logger.LogInformation($"Member {member.Id}: Eligibility - {isEligible}");
            }

            _logger.LogInformation("Benefit eligibility update completed.");
        }
    }
}
