using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TicketApi.Application.DTOs;
using TicketApi.Domain.Entities;
using TicketApi.Domain.Enums;
using TicketApi.Infrastructure.Persistence;
using TicketApi.Infrastructure.Repositories;

namespace TicketApi.Tests;

public class TicketRepositoryTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Query_filters_by_status_and_user()
    {
        await using var db = CreateDb();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        db.Users.AddRange(
            new AppUser { Id = userA, Email = "a@t.com", FullName = "A", PasswordHash = "x", Role = Roles.User },
            new AppUser { Id = userB, Email = "b@t.com", FullName = "B", PasswordHash = "x", Role = Roles.User });
        db.Tickets.AddRange(
            new Ticket { Id = Guid.NewGuid(), Title = "Mine Open", Status = TicketStatus.Open, CreatedByUserId = userA },
            new Ticket { Id = Guid.NewGuid(), Title = "Mine Closed", Status = TicketStatus.Closed, CreatedByUserId = userA },
            new Ticket { Id = Guid.NewGuid(), Title = "Other", Status = TicketStatus.Open, CreatedByUserId = userB });
        await db.SaveChangesAsync();

        var repo = new TicketRepository(db);
        var page = await repo.QueryAsync(new TicketQuery(1, 10, TicketStatus.Open, null, null), userA);

        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle(t => t.Title == "Mine Open");
    }
}
