using EPSPlus.Application.Interfaces;
using EPSPlus.Application.Services;
using EPSPlus.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EPSPlus.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContributionController : ControllerBase
    {
        private readonly IContributionRepository _contributionRepository;
        private readonly IContributionService _contributionService;
        private readonly ILogger<ContributionController> _logger;

        public ContributionController(
            IContributionRepository contributionRepository,
            IContributionService contributionService,
            ILogger<ContributionController> logger)
        {
            _contributionRepository = contributionRepository;
            _contributionService = contributionService;
            _logger = logger;
        }

        // GET: api/contribution/member/{memberId}
        [HttpGet("member/{memberId}")]
        public async Task<ActionResult<IEnumerable<Contribution>>> GetContributionsByMember(Guid memberId)
        {
            _logger.LogInformation("Fetching contributions for Member ID: {MemberId}", memberId);

            var contributions = await _contributionRepository.GetAllByMemberIdAsync(memberId);
            return Ok(contributions);
        }

        // GET: api/contribution/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Contribution>> GetContributionById(Guid id)
        {
            _logger.LogInformation("Fetching contribution with ID: {ContributionId}", id);

            var contribution = await _contributionRepository.GetByIdAsync(id);

            if (contribution == null)
            {
                _logger.LogWarning("Contribution with ID {ContributionId} not found.", id);
                return NotFound();
            }

            return Ok(contribution);
        }

        // POST: api/contribution
        [HttpPost]
        public async Task<ActionResult> AddContribution([FromBody] Contribution contribution)
        {
            _logger.LogInformation("Attempting to add a new contribution: {Contribution}", contribution);

            if (contribution.Amount <= 0)
            {
                _logger.LogWarning("Invalid contribution amount: {Amount}", contribution.Amount);
                return BadRequest("Contribution amount must be greater than zero.");
            }

            try
            {
               
                var (paymentSuccessful, errorMessage) = await _contributionService.ProcessContributionAsync(contribution);
                var status = paymentSuccessful ? ContributionStatus.Success : ContributionStatus.Failed;
                await _contributionRepository.AddAsync(contribution);

                await _contributionRepository.UpdateContributionStatusAsync(contribution.Id, status);
                _logger.LogInformation("Contribution added successfully: {ContributionId}", contribution.Id);

                if (!paymentSuccessful)
                {
                    _logger.LogWarning("Contribution processing failed: {ErrorMessage}", errorMessage);
                    return BadRequest(errorMessage ?? "Contribution processing failed.");
                }

                _logger.LogInformation("Contribution processed successfully for ID: {ContributionId}", contribution.Id);
                return CreatedAtAction(nameof(GetContributionById), new { id = contribution.Id }, contribution);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Error while processing contribution: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/contribution/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContribution(Guid id)
        {
            _logger.LogInformation("Deleting contribution with ID: {ContributionId}", id);

            await _contributionRepository.DeleteAsync(id);

            _logger.LogInformation("Contribution with ID {ContributionId} deleted successfully.", id);
            return NoContent();
        }

        // GET api/contribution/check-eligibility/{memberId}
        [HttpGet("check-eligibility/{memberId}")]
        public async Task<ActionResult> CheckEligibility(Guid memberId)
        {
            _logger.LogInformation("Checking eligibility for Member ID: {MemberId}", memberId);

            var isEligible = await _contributionService.IsEligibleForBenefit(memberId);

            if (isEligible)
            {
                _logger.LogInformation("Member {MemberId} is eligible for benefits.", memberId);
                return Ok("Member is eligible for benefits.");
            }
            else
            {
                _logger.LogWarning("Member {MemberId} is not eligible for benefits.", memberId);
                return BadRequest("Member is not eligible for benefits. Please contribute for at least 6 months.");
            }
        }

        // GET api/contribution/total/{memberId}
        [HttpGet("total/{memberId}")]
        public async Task<IActionResult> GetTotalContributions(Guid memberId)
        {
            _logger.LogInformation("Fetching total contributions for Member ID: {MemberId}", memberId);

            try
            {
                var totalContributions = await _contributionService.CalculateTotalContributionsAsync(memberId);
                return Ok(new { totalContributions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching total contributions for Member ID: {MemberId}", memberId);
                return StatusCode(500, new { message = "An error occurred while retrieving total contributions.", details = ex.Message });
            }
        }

        [HttpGet("statement/{memberId}")]
        public async Task<IActionResult> GetContributionStatement(Guid memberId, DateTime startDate, DateTime endDate)
        {
            _logger.LogInformation("Generating contribution statement for Member ID: {MemberId} from {StartDate} to {EndDate}", memberId, startDate, endDate);

            try
            {
                var statement = await _contributionService.GenerateStatementAsync(memberId, startDate, endDate);
                return Ok(statement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating contribution statement for Member ID: {MemberId}", memberId);
                return StatusCode(500, new { message = "An error occurred while generating the statement.", details = ex.Message });
            }
        }

        [HttpGet("statement/{memberId}/pdf")]
        public async Task<IActionResult> GetContributionStatementPdf(Guid memberId, DateTime startDate, DateTime endDate)
        {
            _logger.LogInformation("Generating PDF statement for Member ID: {MemberId} from {StartDate} to {EndDate}", memberId, startDate, endDate);

            try
            {
                var pdfBytes = await _contributionService.GenerateStatementPdfAsync(memberId, startDate, endDate);
                return File(pdfBytes, "application/pdf", "ContributionStatement.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating contribution statement PDF for Member ID: {MemberId}", memberId);
                return StatusCode(500, new { message = "An error occurred while generating the statement.", details = ex.Message });
            }
        }
    }
}
