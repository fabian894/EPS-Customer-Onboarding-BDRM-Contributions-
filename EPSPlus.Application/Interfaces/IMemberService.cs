using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPSPlus.Domain.Entities;

namespace EPSPlus.Application.Interfaces
{
    public interface IMemberService
    {
        Task<Member?> GetMemberByIdAsync(Guid id);
        Task<IEnumerable<Member>> GetAllMembersAsync();
        Task<Member> CreateMemberAsync(Member member);
        Task UpdateMemberAsync(Member member);
        Task SoftDeleteMemberAsync(Guid id);
    }
}

