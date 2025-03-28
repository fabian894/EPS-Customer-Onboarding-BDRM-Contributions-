using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EPSPlus.Domain.Entities;

public class Member
{

    //[JsonIgnore]
    public Guid Id { get; set; } = Guid.NewGuid(); // Unique ID
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public bool IsActive { get; set; } = true; // Soft delete flag
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Business Rule: Age must be between 18 and 70
    public int Age => DateTime.UtcNow.Year - DateOfBirth.Year;

    public bool IsValidAge() => Age >= 18 && Age <= 70;
}
