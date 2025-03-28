using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPSPlus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EPSPlus.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Member> Members { get; set; }
    public DbSet<Contribution> Contributions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Member>().HasKey(m => m.Id);
        modelBuilder.Entity<Member>().Property(m => m.FirstName).IsRequired().HasMaxLength(50);
        modelBuilder.Entity<Member>().Property(m => m.LastName).IsRequired().HasMaxLength(50);
        modelBuilder.Entity<Member>().Property(m => m.Email).IsRequired();
        modelBuilder.Entity<Member>().Property(m => m.DateOfBirth).IsRequired();
        modelBuilder.Entity<Contribution>()
        .Property(c => c.Amount)
        .HasColumnType("decimal(18,2)");
    }
}

