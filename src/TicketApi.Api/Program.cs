using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TicketApi.Api.Middleware;
using TicketApi.Application;
using TicketApi.Domain.Entities;
using TicketApi.Infrastructure;
using TicketApi.Infrastructure.Identity;
using TicketApi.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "http://localhost:5173",
                "http://127.0.0.1:3000",
                "http://127.0.0.1:3001",
                "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ticket API",
        Version = "v1",
        Description = "JWT-secured ticket management API with Admin/User roles and status workflow."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? "change-me-to-a-long-random-secret-at-least-32-chars!!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "TicketApi",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "TicketApi",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await EnsureRepliesTableAsync(db);
    await EnsureNotificationTablesAsync(db);
    await EnsureTicketAttachmentsTableAsync(db);

    await SeedAsync(scope.ServiceProvider);
    await SyncSmtpFromEnvironmentAsync(scope.ServiceProvider);
}

app.Run();

static async Task EnsureRepliesTableAsync(AppDbContext db)
{
    var provider = db.Database.ProviderName ?? string.Empty;
    if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ticket_replies (
                "Id" uuid NOT NULL,
                "TicketId" uuid NOT NULL,
                "AuthorUserId" uuid NOT NULL,
                "Body" character varying(4000) NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_ticket_replies" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_ticket_replies_tickets_TicketId" FOREIGN KEY ("TicketId") REFERENCES tickets ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_ticket_replies_users_AuthorUserId" FOREIGN KEY ("AuthorUserId") REFERENCES users ("Id") ON DELETE RESTRICT
            );
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_ticket_replies_TicketId" ON ticket_replies ("TicketId");
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_ticket_replies_CreatedAtUtc" ON ticket_replies ("CreatedAtUtc");
            """);
        return;
    }

    if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ticket_replies (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ticket_replies" PRIMARY KEY,
                "TicketId" TEXT NOT NULL,
                "AuthorUserId" TEXT NOT NULL,
                "Body" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_ticket_replies_tickets_TicketId" FOREIGN KEY ("TicketId") REFERENCES "tickets" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_ticket_replies_users_AuthorUserId" FOREIGN KEY ("AuthorUserId") REFERENCES "users" ("Id") ON DELETE RESTRICT
            );
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_ticket_replies_TicketId" ON ticket_replies ("TicketId");
            """);
    }
}

static async Task EnsureTicketAttachmentsTableAsync(AppDbContext db)
{
    var provider = db.Database.ProviderName ?? string.Empty;
    if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ticket_attachments (
                "Id" uuid NOT NULL,
                "TicketId" uuid NOT NULL,
                "FileName" character varying(260) NOT NULL,
                "StoredName" character varying(200) NOT NULL,
                "ContentType" character varying(100) NOT NULL,
                "SizeBytes" bigint NOT NULL,
                CONSTRAINT "PK_ticket_attachments" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_ticket_attachments_tickets_TicketId" FOREIGN KEY ("TicketId") REFERENCES tickets ("Id") ON DELETE CASCADE
            );
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_ticket_attachments_TicketId" ON ticket_attachments ("TicketId");
            """);
        return;
    }

    if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ticket_attachments (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ticket_attachments" PRIMARY KEY,
                "TicketId" TEXT NOT NULL,
                "FileName" TEXT NOT NULL,
                "StoredName" TEXT NOT NULL,
                "ContentType" TEXT NOT NULL,
                "SizeBytes" INTEGER NOT NULL,
                CONSTRAINT "FK_ticket_attachments_tickets_TicketId" FOREIGN KEY ("TicketId") REFERENCES "tickets" ("Id") ON DELETE CASCADE
            );
            """);
    }
}

static async Task EnsureNotificationTablesAsync(AppDbContext db)
{
    var provider = db.Database.ProviderName ?? string.Empty;
    if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS reply_attachments (
                "Id" uuid NOT NULL,
                "ReplyId" uuid NOT NULL,
                "FileName" character varying(260) NOT NULL,
                "StoredName" character varying(200) NOT NULL,
                "ContentType" character varying(100) NOT NULL,
                "SizeBytes" bigint NOT NULL,
                CONSTRAINT "PK_reply_attachments" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_reply_attachments_ticket_replies_ReplyId" FOREIGN KEY ("ReplyId") REFERENCES ticket_replies ("Id") ON DELETE CASCADE
            );
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_reply_attachments_ReplyId" ON reply_attachments ("ReplyId");
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS notifications (
                "Id" uuid NOT NULL,
                "RecipientUserId" uuid NOT NULL,
                "TicketId" uuid NOT NULL,
                "Message" character varying(300) NOT NULL,
                "IsRead" boolean NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_notifications" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_notifications_users_RecipientUserId" FOREIGN KEY ("RecipientUserId") REFERENCES users ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_notifications_tickets_TicketId" FOREIGN KEY ("TicketId") REFERENCES tickets ("Id") ON DELETE CASCADE
            );
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_notifications_RecipientUserId_IsRead" ON notifications ("RecipientUserId", "IsRead");
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS smtp_settings (
                "Id" uuid NOT NULL,
                "Host" character varying(200) NOT NULL,
                "Port" integer NOT NULL,
                "Username" character varying(200) NOT NULL,
                "Password" character varying(500) NOT NULL,
                "FromEmail" character varying(200) NOT NULL,
                "FromName" character varying(200) NOT NULL,
                "EnableSsl" boolean NOT NULL,
                "Enabled" boolean NOT NULL,
                CONSTRAINT "PK_smtp_settings" PRIMARY KEY ("Id")
            );
            """);
        return;
    }

    if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS reply_attachments (
                "Id" TEXT NOT NULL CONSTRAINT "PK_reply_attachments" PRIMARY KEY,
                "ReplyId" TEXT NOT NULL,
                "FileName" TEXT NOT NULL,
                "StoredName" TEXT NOT NULL,
                "ContentType" TEXT NOT NULL,
                "SizeBytes" INTEGER NOT NULL,
                CONSTRAINT "FK_reply_attachments_ticket_replies_ReplyId" FOREIGN KEY ("ReplyId") REFERENCES ticket_replies ("Id") ON DELETE CASCADE
            );
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS notifications (
                "Id" TEXT NOT NULL CONSTRAINT "PK_notifications" PRIMARY KEY,
                "RecipientUserId" TEXT NOT NULL,
                "TicketId" TEXT NOT NULL,
                "Message" TEXT NOT NULL,
                "IsRead" INTEGER NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_notifications_users_RecipientUserId" FOREIGN KEY ("RecipientUserId") REFERENCES users ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_notifications_tickets_TicketId" FOREIGN KEY ("TicketId") REFERENCES tickets ("Id") ON DELETE CASCADE
            );
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS smtp_settings (
                "Id" TEXT NOT NULL CONSTRAINT "PK_smtp_settings" PRIMARY KEY,
                "Host" TEXT NOT NULL,
                "Port" INTEGER NOT NULL,
                "Username" TEXT NOT NULL,
                "Password" TEXT NOT NULL,
                "FromEmail" TEXT NOT NULL,
                "FromName" TEXT NOT NULL,
                "EnableSsl" INTEGER NOT NULL,
                "Enabled" INTEGER NOT NULL
            );
            """);
    }
}

static async Task SeedAsync(IServiceProvider sp)
{
    var db = sp.GetRequiredService<AppDbContext>();
    if (await db.Users.AnyAsync()) return;

    var hasher = sp.GetRequiredService<TicketApi.Application.Interfaces.IPasswordHasherService>();
    var admin = new AppUser
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Email = "admin@ticket.local",
        FullName = "System Admin",
        PasswordHash = hasher.Hash("Admin123!"),
        Role = Roles.Admin,
        CreatedAtUtc = DateTime.UtcNow
    };
    var user = new AppUser
    {
        Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        Email = "user@ticket.local",
        FullName = "Demo User",
        PasswordHash = hasher.Hash("User1234!"),
        Role = Roles.User,
        CreatedAtUtc = DateTime.UtcNow
    };
    db.Users.AddRange(admin, user);
    await db.SaveChangesAsync();
}

/// <summary>
/// When Smtp:Host is set via environment, mirror settings into DB so the admin form reflects them.
/// Empty password in env leaves the stored password unchanged.
/// </summary>
static async Task SyncSmtpFromEnvironmentAsync(IServiceProvider sp)
{
    var config = sp.GetRequiredService<IConfiguration>();
    var host = config["Smtp:Host"];
    if (string.IsNullOrWhiteSpace(host)) return;

    var repo = sp.GetRequiredService<TicketApi.Application.Interfaces.ISmtpSettingsRepository>();
    var current = await repo.GetAsync();
    current.Host = host.Trim();
    if (int.TryParse(config["Smtp:Port"], out var port) && port is >= 1 and <= 65535)
        current.Port = port;
    current.Username = (config["Smtp:Username"] ?? string.Empty).Trim();
    var password = config["Smtp:Password"];
    if (!string.IsNullOrEmpty(password))
        current.Password = password;
    current.FromEmail = (config["Smtp:FromEmail"] ?? string.Empty).Trim();
    current.FromName = string.IsNullOrWhiteSpace(config["Smtp:FromName"])
        ? "Ticket Board"
        : config["Smtp:FromName"]!.Trim();
    if (bool.TryParse(config["Smtp:EnableSsl"], out var ssl))
        current.EnableSsl = ssl;
    if (bool.TryParse(config["Smtp:Enabled"], out var enabled))
        current.Enabled = enabled;
    await repo.SaveAsync(current);
}

public partial class Program { }
