using EPSPlus.Application.DTOs;
using EPSPlus.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPSPlus.Application.Interfaces
{
    public interface IContributionJobService
    {
        Task ValidateContributions();
        Task RetryFailedContributions();
        Task UpdateBenefitEligibility();
    }

}
