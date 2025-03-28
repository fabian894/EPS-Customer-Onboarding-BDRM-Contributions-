using EPSPlus.API;
using EPSPlus.Domain.Entities;
using EPSPlus.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public class MemberControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly ApplicationDbContext _dbContext;

    public MemberControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();

        // Use an in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;

        _dbContext = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateMember_ShouldReturn_CreatedMember()
    {
        // Arrange
        var newMember = new Member
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "1234567890",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        var json = JsonConvert.SerializeObject(newMember);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Member", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var createdMember = JsonConvert.DeserializeObject<Member>(responseBody);

        // Assert
        Assert.NotNull(createdMember);
        Assert.Equal("John", createdMember.FirstName);
    }

    [Fact]
    public async Task GetMemberById_ShouldReturn_Member()
    {
        // Arrange: Add a member to the in-memory DB
        var member = new Member
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane.doe@example.com",
            PhoneNumber = "1234567890",
            DateOfBirth = new DateTime(1995, 5, 5)
        };

        _dbContext.Members.Add(member);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/Member/{member.Id}");
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var fetchedMember = JsonConvert.DeserializeObject<Member>(responseBody);

        // Assert
        Assert.NotNull(fetchedMember);
        Assert.Equal("Jane", fetchedMember.FirstName);
    }

    [Fact]
    public async Task GetAllMembers_ShouldReturn_MemberList()
    {
        // Act
        var response = await _client.GetAsync("/api/Member");
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var members = JsonConvert.DeserializeObject<List<Member>>(responseBody);

        // Assert
        Assert.NotNull(members);
        Assert.True(members.Count >= 0);
    }

    [Fact]
    public async Task UpdateMember_ShouldReturn_NoContent()
    {
        // Arrange: Add a member
        var member = new Member
        {
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice.smith@example.com",
            PhoneNumber = "1234567890",
            DateOfBirth = new DateTime(1988, 10, 10)
        };

        _dbContext.Members.Add(member);
        await _dbContext.SaveChangesAsync();

        // Modify member data
        member.FirstName = "AliceUpdated";

        var json = JsonConvert.SerializeObject(member);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/api/Member/{member.Id}", content);
        response.EnsureSuccessStatusCode();

        // Assert
        var updatedMember = await _dbContext.Members.FindAsync(member.Id);
        Assert.NotNull(updatedMember);
        Assert.Equal("AliceUpdated", updatedMember.FirstName);
    }

    [Fact]
    public async Task DeleteMember_ShouldReturn_NoContent()
    {
        // Arrange: Add a member
        var member = new Member
        {
            FirstName = "Bob",
            LastName = "Brown",
            Email = "bob.brown@example.com",
            PhoneNumber = "1234567890",
            DateOfBirth = new DateTime(1985, 12, 12)
        };

        _dbContext.Members.Add(member);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/Member/{member.Id}");
        response.EnsureSuccessStatusCode();

        // Assert
        var deletedMember = await _dbContext.Members.FindAsync(member.Id);
        // Member should be soft deleted
        Assert.False(deletedMember.IsActive); 
    }
}
