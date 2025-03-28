using EPSPlus.Application.Interfaces;
using EPSPlus.Application.Services;
using EPSPlus.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EPSPlus.Tests.Services
{
    public class ContributionServiceTests
    {
        private readonly ContributionService _service;
        private readonly Mock<IContributionRepository> _contributionRepoMock;
        private readonly Mock<IMemberRepository> _memberRepoMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IMemoryCache> _cacheMock;
        private readonly Mock<ILogger<ContributionService>> _loggerMock;
        private readonly Mock<ICacheEntry> _cacheEntryMock;

        public ContributionServiceTests()
        {
            _contributionRepoMock = new Mock<IContributionRepository>();
            _memberRepoMock = new Mock<IMemberRepository>();
            _emailServiceMock = new Mock<IEmailService>();
            _cacheMock = new Mock<IMemoryCache>();
            _loggerMock = new Mock<ILogger<ContributionService>>();
            _cacheEntryMock = new Mock<ICacheEntry>();

            // Fix: Ensure CreateEntry is mocked to prevent null reference errors
            _cacheMock.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(_cacheEntryMock.Object);

            _service = new ContributionService(
                _contributionRepoMock.Object,
                _memberRepoMock.Object,
                _emailServiceMock.Object,
                _cacheMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task GetContributionsByMember_ReturnsContributions()
        {
            // Arrange
            var memberId = Guid.NewGuid();
            var contributions = new List<Contribution>
            {
                new Contribution { Id = Guid.NewGuid(), MemberId = memberId, Amount = 100, ContributionDate = DateTime.UtcNow },
                new Contribution { Id = Guid.NewGuid(), MemberId = memberId, Amount = 200, ContributionDate = DateTime.UtcNow }
            };

            _contributionRepoMock.Setup(repo => repo.GetAllByMemberIdAsync(memberId))
                .ReturnsAsync(contributions);

            // Fix: Ensure TryGetValue is properly mocked
            object cacheEntry = null;
            _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheEntry))
                .Returns(false);

            // Act
            var result = await _service.GetContributionsByMember(memberId);

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetContributionById_ReturnsContribution()
        {
            // Arrange
            var contributionId = Guid.NewGuid();
            var contribution = new Contribution { Id = contributionId, Amount = 100 };

            _contributionRepoMock.Setup(repo => repo.GetByIdAsync(contributionId))
                .ReturnsAsync(contribution);

            // Fix: Properly mock cache TryGetValue
            object cacheEntry = null;
            _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheEntry))
                .Returns(false);

            // Act
            var result = await _service.GetContributionById(contributionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(contributionId, result.Id);
        }

        [Fact]
        public async Task AddContribution_InvalidAmount_ReturnsErrorMessage()
        {
            // Arrange
            var contribution = new Contribution { Id = Guid.NewGuid(), Amount = 0 };

            // Act
            var result = await _service.AddContribution(contribution);

            // Assert
            Assert.Equal("Contribution amount must be greater than zero.", result);
        }

        [Fact]
        public async Task AddContribution_ValidAmount_ReturnsSuccessMessage()
        {
            // Arrange
            var contribution = new Contribution { Id = Guid.NewGuid(), Amount = 100 };

            _contributionRepoMock.Setup(repo => repo.AddAsync(contribution))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.AddContribution(contribution);

            // Assert
            Assert.Equal("Contribution successfully added.", result);
        }

        [Fact]
        public async Task IsEligibleForBenefit_Eligible_ReturnsTrue()
        {
            // Arrange
            var memberId = Guid.NewGuid();
            var contributions = new List<Contribution>
            {
                new Contribution { ContributionDate = DateTime.UtcNow.AddMonths(-10) },
                new Contribution { ContributionDate = DateTime.UtcNow.AddMonths(-5) }
            };

            _contributionRepoMock.Setup(repo => repo.GetAllByMemberIdAsync(memberId))
                .ReturnsAsync(contributions);

            // Fix: Properly mock cache TryGetValue
            object cacheEntry = null;
            _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheEntry))
                .Returns(false);

            // Act
            var result = await _service.IsEligibleForBenefit(memberId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ProcessContributionAsync_InvalidMember_ReturnsErrorMessage()
        {
            // Arrange
            var contribution = new Contribution { Id = Guid.NewGuid(), MemberId = Guid.NewGuid(), Amount = 100 };

            _memberRepoMock.Setup(repo => repo.GetByIdAsync(contribution.MemberId))
                .ReturnsAsync((Member)null);

            // Act
            var result = await _service.ProcessContributionAsync(contribution);

            // Assert
            Assert.False(result.success);
            Assert.Equal("Member not found.", result.errorMessage);
        }

        [Fact]
        public async Task ProcessContributionAsync_ValidContribution_ReturnsSuccess()
        {
            // Arrange
            var contribution = new Contribution { Id = Guid.NewGuid(), MemberId = Guid.NewGuid(), Amount = 100, Type = ContributionType.Monthly };
            var member = new Member { Id = contribution.MemberId, FirstName = "John", LastName = "Doe" };

            _memberRepoMock.Setup(repo => repo.GetByIdAsync(contribution.MemberId))
                .ReturnsAsync(member);

            _contributionRepoMock.Setup(repo => repo.AddAsync(contribution))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.ProcessContributionAsync(contribution);

            // Assert
            Assert.True(result.success);
            Assert.Equal(string.Empty, result.errorMessage);
        }

        [Fact]
        public async Task CalculateTotalContributionsAsync_ReturnsCorrectTotal()
        {
            // Arrange
            var memberId = Guid.NewGuid();
            var contributions = new List<Contribution>
            {
                new Contribution { MemberId = memberId, Amount = 100, Type = ContributionType.Monthly },
                new Contribution { MemberId = memberId, Amount = 200, Type = ContributionType.Voluntary }
            };

            _contributionRepoMock.Setup(repo => repo.GetAllByMemberIdAsync(memberId))
                .ReturnsAsync(contributions);

            // Fix: Properly mock cache TryGetValue
            object cacheEntry = null;
            _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheEntry))
                .Returns(false);

            // Act
            var total = await _service.CalculateTotalContributionsAsync(memberId);

            // Assert
            Assert.Equal(300, total);
        }
    }
}
