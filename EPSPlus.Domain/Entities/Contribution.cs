using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EPSPlus.Domain.Entities
{
    public class Contribution
    {
        [Key]
        [JsonIgnore]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid MemberId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime ContributionDate { get; set; } = DateTime.UtcNow;

        [Required]
        public ContributionType Type { get; set; }  // Monthly or Voluntary

        [Required]
        public ContributionStatus Status { get; set; } = ContributionStatus.Pending;

        // Navigation Property
        [ForeignKey("MemberId")]
        [JsonIgnore]
        public Member? Member { get; set; }
    }

    public enum ContributionType
    {
        Monthly = 1,
        Voluntary = 2
    }

    public enum ContributionStatus
    {
        Pending = 1,
        Success = 2,
        Failed = 3
    }
}
