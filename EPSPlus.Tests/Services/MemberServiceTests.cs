using EPSPlus.Application.Interfaces;
using EPSPlus.Application.Services;
using EPSPlus.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace EPSPlus.Tests.Services
{
    public class MemberServiceTests
    {
        private readonly Mock<IMemberRepository> _mockMemberRepository;
        private readonly Mock<ILogger<MemberService>> _mockLogger;
        private readonly MemberService _memberService;

        public MemberServiceTests()
        {
            _mockMemberRepository = new Mock<IMemberRepository>();
            _mockLogger = new Mock<ILogger<MemberService>>();
            _memberService = new MemberService(_mockMemberRepository.Object, _mockLogger.Object);
        }

        #region CreateMemberAsync Tests

        [Fact]
        public async Task CreateMemberAsync_ValidMember_CreatesMember()
        {
            // Arrange
            var member = new Member
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = DateTime.UtcNow.AddYears(-25) // Correct way to set age
            };

            _mockMemberRepository.Setup(repo => repo.AddAsync(It.IsAny<Member>())).Returns(Task.CompletedTask);

            // Act
            var result = await _memberService.CreateMemberAsync(member);

            // Assert
            _mockMemberRepository.Verify(repo => repo.AddAsync(It.Is<Member>(m => m == member)), Times.Once);
            Assert.Equal(member, result);
        }

        [Fact]
        public async Task CreateMemberAsync_InvalidAge_ThrowsException()
        {
            // Arrange
            var member = new Member
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = DateTime.UtcNow.AddYears(-17) // Underage
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await _memberService.CreateMemberAsync(member));
        }

        #endregion

        #region GetMemberByIdAsync Tests

        [Fact]
        public async Task GetMemberByIdAsync_MemberFound_ReturnsMember()
        {
            // Arrange
            var memberId = Guid.NewGuid();
            var expectedMember = new Member
            {
                Id = memberId,
                FirstName = "Jane",
                LastName = "Doe",
                DateOfBirth = DateTime.UtcNow.AddYears(-30)
            };

            _mockMemberRepository.Setup(repo => repo.GetByIdAsync(memberId)).ReturnsAsync(expectedMember);

            // Act
            var result = await _memberService.GetMemberByIdAsync(memberId);

            // Assert
            Assert.Equal(expectedMember, result);
        }

        [Fact]
        public async Task GetMemberByIdAsync_MemberNotFound_ReturnsNull()
        {
            // Arrange
            var memberId = Guid.NewGuid();

            _mockMemberRepository.Setup(repo => repo.GetByIdAsync(memberId)).ReturnsAsync((Member)null);

            // Act
            var result = await _memberService.GetMemberByIdAsync(memberId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region UpdateMemberAsync Tests

        [Fact]
        public async Task UpdateMemberAsync_ValidMember_UpdatesMember()
        {
            // Arrange
            var member = new Member
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = DateTime.UtcNow.AddYears(-28)
            };

            _mockMemberRepository.Setup(repo => repo.UpdateAsync(It.IsAny<Member>())).Returns(Task.CompletedTask);

            // Act
            await _memberService.UpdateMemberAsync(member);

            // Assert
            _mockMemberRepository.Verify(repo => repo.UpdateAsync(It.Is<Member>(m => m == member)), Times.Once);
        }

        [Fact]
        public async Task UpdateMemberAsync_InvalidAge_ThrowsException()
        {
            // Arrange
            var member = new Member
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = DateTime.UtcNow.AddYears(-17) // Invalid age
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await _memberService.UpdateMemberAsync(member));
        }

        #endregion
    }
}
