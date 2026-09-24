using FluentAssertions;
using Moq;
using TicketApi.Application.Common;
using TicketApi.Application.DTOs;
using TicketApi.Application.Interfaces;
using TicketApi.Application.Services;
using TicketApi.Domain.Entities;
using TicketApi.Domain.Enums;

namespace TicketApi.Tests;

public class TicketServiceTests
{
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<INotificationRepository> _notes = new();
    private readonly Mock<IEmailNotifier> _email = new();
    private readonly Mock<IReplyFileStore> _files = new();
    private readonly TicketService _sut;

    public TicketServiceTests()
    {
        _users.Setup(u => u.ListByRoleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AppUser>());
        _notes.Setup(n => n.AddRangeAsync(It.IsAny<IReadOnlyList<AppNotification>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _email.Setup(e => e.NotifyAsync(
                It.IsAny<IReadOnlyList<EmailRecipient>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _files.Setup(f => f.SaveAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sut = new TicketService(_tickets.Object, _users.Object, _notes.Object, _email.Object, _files.Object);
    }

    [Fact]
    public async Task Create_sets_open_status()
    {
        Ticket? saved = null;
        _tickets.Setup(r => r.AddAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
            .Callback<Ticket, CancellationToken>((t, _) => saved = t)
            .Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var result = await _sut.CreateAsync(userId, Roles.User, new CreateTicketRequest("Bug", "desc", TicketPriority.High));

        result.Status.Should().Be(TicketStatus.Open);
        result.Title.Should().Be("Bug");
        result.Attachments.Should().BeEmpty();
        saved!.CreatedByUserId.Should().Be(userId);
    }

    [Fact]
    public async Task Create_stores_attachment()
    {
        Ticket? saved = null;
        _tickets.Setup(r => r.AddAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
            .Callback<Ticket, CancellationToken>((t, _) => saved = t)
            .Returns(Task.CompletedTask);

        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 };
        var result = await _sut.CreateAsync(
            Guid.NewGuid(),
            Roles.User,
            new CreateTicketRequest("Ekran", "**kalın** hata", TicketPriority.Low),
            new[] { new ReplyFileUpload("ekran.png", "image/png", png) });

        result.Attachments.Should().ContainSingle(a => a.FileName == "ekran.png" && a.ContentType == "image/png");
        saved!.Attachments.Should().ContainSingle();
        _files.Verify(f => f.SaveAsync(It.IsAny<string>(), png, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_rejects_admin()
    {
        var act = async () => await _sut.CreateAsync(
            Guid.NewGuid(), Roles.Admin, new CreateTicketRequest("Bug", "desc", TicketPriority.Low));

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Delete_rejects_user()
    {
        var ticket = new Ticket { Id = Guid.NewGuid(), Title = "T", CreatedByUserId = Guid.NewGuid() };
        _tickets.Setup(r => r.GetByIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        var act = async () => await _sut.DeleteAsync(ticket.Id, ticket.CreatedByUserId, Roles.User);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task ChangeStatus_allows_open_to_in_progress()
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "T",
            Status = TicketStatus.Open,
            CreatedByUserId = Guid.NewGuid()
        };
        _tickets.Setup(r => r.GetByIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _tickets.Setup(r => r.UpdateAsync(ticket, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.ChangeStatusAsync(
            ticket.Id, Guid.NewGuid(), Roles.Admin, new ChangeStatusRequest(TicketStatus.InProgress));

        result.Status.Should().Be(TicketStatus.InProgress);
    }

    [Fact]
    public async Task ChangeStatus_rejects_non_admin()
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "T",
            Status = TicketStatus.Open,
            CreatedByUserId = Guid.NewGuid()
        };
        _tickets.Setup(r => r.GetByIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        var act = async () => await _sut.ChangeStatusAsync(
            ticket.Id, ticket.CreatedByUserId, Roles.User, new ChangeStatusRequest(TicketStatus.InProgress));

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.Code.Should().Be("FORBIDDEN");
    }

    [Fact]
    public async Task AddReply_saves_message_for_creator()
    {
        var userId = Guid.NewGuid();
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "T",
            Status = TicketStatus.Open,
            CreatedByUserId = userId
        };
        _tickets.Setup(r => r.GetByIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _users.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppUser { Id = userId, FullName = "Ada", Email = "a@t.com", Role = Roles.User });
        _tickets.Setup(r => r.AddReplyAsync(It.IsAny<TicketReply>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _tickets.Setup(r => r.UpdateAsync(ticket, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.AddReplyAsync(
            ticket.Id, userId, Roles.User, new CreateTicketReplyRequest("  Bakıyorum  "));

        result.Body.Should().Be("Bakıyorum");
        result.AuthorName.Should().Be("Ada");
        result.TicketId.Should().Be(ticket.Id);
    }

    [Fact]
    public async Task AddReply_forbidden_for_other_user()
    {
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        var ticket = new Ticket { Id = Guid.NewGuid(), Title = "T", CreatedByUserId = owner };
        _tickets.Setup(r => r.GetByIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        var act = async () => await _sut.AddReplyAsync(
            ticket.Id, other, Roles.User, new CreateTicketReplyRequest("merhaba"));

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task ChangeStatus_rejects_closed_to_open()
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "T",
            Status = TicketStatus.Closed,
            CreatedByUserId = Guid.NewGuid()
        };
        _tickets.Setup(r => r.GetByIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        var act = async () => await _sut.ChangeStatusAsync(
            ticket.Id, ticket.CreatedByUserId, Roles.Admin, new ChangeStatusRequest(TicketStatus.Open));

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.Code.Should().Be("INVALID_STATUS_TRANSITION");
    }

    [Fact]
    public async Task User_reply_notifies_admins()
    {
        var userId = Guid.NewGuid();
        var admin = new AppUser { Id = Guid.NewGuid(), FullName = "System Admin", Email = "admin@t.com", Role = Roles.Admin };
        var ticket = new Ticket { Id = Guid.NewGuid(), Title = "Safari", CreatedByUserId = userId };
        _tickets.Setup(r => r.GetByIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _users.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppUser { Id = userId, FullName = "Ada", Email = "a@t.com", Role = Roles.User });
        _users.Setup(r => r.ListByRoleAsync(Roles.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AppUser> { admin });
        _tickets.Setup(r => r.AddReplyAsync(It.IsAny<TicketReply>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _tickets.Setup(r => r.UpdateAsync(ticket, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _sut.AddReplyAsync(ticket.Id, userId, Roles.User, new CreateTicketReplyRequest("bakıyorum"));

        _notes.Verify(n => n.AddRangeAsync(
            It.Is<IReadOnlyList<AppNotification>>(list =>
                list.Count == 1
                && list[0].RecipientUserId == admin.Id
                && list[0].Message == "Ada cevap verdi"),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Admin_reply_notifies_ticket_owner()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var ticket = new Ticket { Id = Guid.NewGuid(), Title = "Safari", CreatedByUserId = ownerId };
        _tickets.Setup(r => r.GetByIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _users.Setup(r => r.GetByIdAsync(adminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppUser { Id = adminId, FullName = "System Admin", Email = "admin@t.com", Role = Roles.Admin });
        _users.Setup(r => r.GetByIdAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppUser { Id = ownerId, FullName = "Ada", Email = "a@t.com", Role = Roles.User });
        _tickets.Setup(r => r.AddReplyAsync(It.IsAny<TicketReply>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _tickets.Setup(r => r.UpdateAsync(ticket, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _sut.AddReplyAsync(ticket.Id, adminId, Roles.Admin, new CreateTicketReplyRequest("çözüldü"));

        _notes.Verify(n => n.AddRangeAsync(
            It.Is<IReadOnlyList<AppNotification>>(list =>
                list.Count == 1
                && list[0].RecipientUserId == ownerId
                && list[0].Message == "Admin cevap verdi"),
            It.IsAny<CancellationToken>()));
    }
}
