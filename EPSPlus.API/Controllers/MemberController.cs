using EPSPlus.Application.Interfaces;
using EPSPlus.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EPSPlus.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberController : ControllerBase
    {
        private readonly IMemberService _memberService;
        private readonly ILogger<MemberController> _logger;

        public MemberController(IMemberService memberService, ILogger<MemberController> logger)
        {
            _memberService = memberService;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Member>> GetMemberById(Guid id)
        {
            _logger.LogInformation("Fetching member with ID {MemberId}", id);

            var member = await _memberService.GetMemberByIdAsync(id);
            if (member == null)
            {
                _logger.LogWarning("Member with ID {MemberId} not found", id);
                return NotFound();
            }

            return Ok(member);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Member>>> GetAllMembers()
        {
            _logger.LogInformation("Fetching all active members");
            var members = await _memberService.GetAllMembersAsync();
            return Ok(members);
        }

        [HttpPost]
        public async Task<ActionResult<Member>> CreateMember(Member member)
        {
            string fullName = $"{member.FirstName} {member.LastName}";
            _logger.LogInformation("Creating new member: {MemberFullName}", fullName);

            try
            {
                var createdMember = await _memberService.CreateMemberAsync(member);
                _logger.LogInformation("Member {MemberFullName} created successfully", fullName);
                return CreatedAtAction(nameof(GetMemberById), new { id = createdMember.Id }, createdMember);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error creating member: {MemberFullName}", fullName);
                return BadRequest(ex.Message);  // Return the error message to the client
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMember(Guid id, Member member)
        {
            if (id != member.Id)
            {
                _logger.LogWarning("Mismatch between request ID and member ID");
                return BadRequest();
            }

            try
            {
                await _memberService.UpdateMemberAsync(member);
                _logger.LogInformation("Updated member with ID {MemberId}", id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error updating member with ID {MemberId}", id);
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMember(Guid id)
        {
            _logger.LogInformation("Deleting member with ID {MemberId}", id);

            try
            {
                await _memberService.SoftDeleteMemberAsync(id);
                _logger.LogInformation("Member with ID {MemberId} deleted (soft delete)", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting member with ID {MemberId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
