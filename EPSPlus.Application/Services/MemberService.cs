using EPSPlus.Application.Interfaces;
using EPSPlus.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EPSPlus.Application.Services
{
    public class MemberService : IMemberService
    {
        private readonly IMemberRepository _memberRepository;
        private readonly ILogger<MemberService> _logger;

        public MemberService(IMemberRepository memberRepository, ILogger<MemberService> logger)
        {
            _memberRepository = memberRepository;
            _logger = logger;
        }

        public async Task<Member?> GetMemberByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Fetching member with ID {MemberId}", id);
                var member = await _memberRepository.GetByIdAsync(id);
                if (member == null)
                {
                    _logger.LogWarning("Member with ID {MemberId} not found", id);
                }
                return member;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member with ID {MemberId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Member>> GetAllMembersAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all active members.");
                return await _memberRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching members.");
                throw;
            }
        }

        public async Task<Member> CreateMemberAsync(Member member)
        {
            try
            {
                if (!member.IsValidAge())
                {
                    throw new InvalidOperationException("Member must be between 18 and 70 years old.");
                }

                await _memberRepository.AddAsync(member);
                return member;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating member.");
                throw;
            }
        }

        public async Task UpdateMemberAsync(Member member)
        {
            try
            {
                if (!member.IsValidAge())
                {
                    throw new InvalidOperationException("Member must be between 18 and 70 years old.");
                }

                await _memberRepository.UpdateAsync(member);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating member with ID {MemberId}", member.Id);
                throw;
            }
        }

        public async Task SoftDeleteMemberAsync(Guid id)
        {
            try
            {
                await _memberRepository.SoftDeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting member with ID {MemberId}", id);
                throw;
            }
        }
    }
}
