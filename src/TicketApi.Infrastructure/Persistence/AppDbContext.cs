using Microsoft.EntityFrameworkCore;
using TicketApi.Domain.Entities;

namespace TicketApi.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketReply> TicketReplies => Set<TicketReply>();
    public DbSet<ReplyAttachment> ReplyAttachments => Set<ReplyAttachment>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<AppNotification> Notifications => Set<AppNotification>();
    public DbSet<SmtpSettings> SmtpSettings => Set<SmtpSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.Role).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Ticket>(e =>
        {
            e.ToTable("tickets");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(4000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(30);
            e.HasOne(x => x.CreatedByUser)
                .WithMany(u => u.CreatedTickets)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AssignedToUser)
                .WithMany()
                .HasForeignKey(x => x.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.Priority);
            e.HasIndex(x => x.CreatedAtUtc);
        });

        modelBuilder.Entity<TicketReply>(e =>
        {
            e.ToTable("ticket_replies");
            e.HasKey(x => x.Id);
            e.Property(x => x.Body).HasMaxLength(4000).IsRequired();
            e.HasOne(x => x.Ticket)
                .WithMany(t => t.Replies)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Author)
                .WithMany()
                .HasForeignKey(x => x.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.TicketId);
            e.HasIndex(x => x.CreatedAtUtc);
        });

        modelBuilder.Entity<TicketAttachment>(e =>
        {
            e.ToTable("ticket_attachments");
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.StoredName).HasMaxLength(200).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Ticket)
                .WithMany(t => t.Attachments)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.TicketId);
        });

        modelBuilder.Entity<ReplyAttachment>(e =>
        {
            e.ToTable("reply_attachments");
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.StoredName).HasMaxLength(200).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Reply)
                .WithMany(r => r.Attachments)
                .HasForeignKey(x => x.ReplyId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.ReplyId);
        });

        modelBuilder.Entity<AppNotification>(e =>
        {
            e.ToTable("notifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Message).HasMaxLength(300).IsRequired();
            e.HasOne(x => x.Recipient)
                .WithMany()
                .HasForeignKey(x => x.RecipientUserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Ticket>()
                .WithMany()
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.RecipientUserId, x.IsRead });
            e.HasIndex(x => x.CreatedAtUtc);
        });

        modelBuilder.Entity<SmtpSettings>(e =>
        {
            e.ToTable("smtp_settings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Host).HasMaxLength(200);
            e.Property(x => x.Username).HasMaxLength(200);
            e.Property(x => x.Password).HasMaxLength(500);
            e.Property(x => x.FromEmail).HasMaxLength(200);
            e.Property(x => x.FromName).HasMaxLength(200);
        });
    }
}
