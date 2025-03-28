using EPSPlus.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPSPlus.Application.DTOs
{
    public class ContributionStatementDto
    {
        public Guid MemberId { get; set; }
        public string? MemberName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalContributions { get; set; }
        public List<Contribution>? Contributions { get; set; }
        public string? EligibleBenefits { get; set; }
    }

}
