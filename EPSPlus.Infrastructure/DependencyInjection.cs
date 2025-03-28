using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPSPlus.Application.Interfaces;
using EPSPlus.Application.Services;
using EPSPlus.Infrastructure.Persistence;
using EPSPlus.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EPSPlus.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IContributionRepository, ContributionRepository>();
        services.AddScoped<IContributionService, ContributionService>();
        services.AddScoped<IContributionJobService, ContributionJobService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IRedisCacheService, RedisCacheService>();
        services.AddScoped<IMemberService, MemberService>();






        return services;
    }
}

