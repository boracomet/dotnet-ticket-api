using Microsoft.Extensions.Configuration;
using TicketApi.Application.Interfaces;

namespace TicketApi.Infrastructure.Storage;

public class ReplyFileStore : IReplyFileStore
{
    private readonly string _root;

    public ReplyFileStore(IConfiguration config)
    {
        _root = config["FileStorage:Root"] ?? "uploads";
    }

    public async Task SaveAsync(string storedName, byte[] content, CancellationToken ct = default)
    {
        var path = Resolve(storedName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, content, ct);
    }

    public async Task<byte[]?> ReadAsync(string storedName, CancellationToken ct = default)
    {
        var path = Resolve(storedName);
        if (!File.Exists(path)) return null;
        return await File.ReadAllBytesAsync(path, ct);
    }

    private string Resolve(string storedName)
    {
        var normalized = storedName.Replace('\\', '/');
        if (!System.Text.RegularExpressions.Regex.IsMatch(
                normalized,
                "^[0-9a-fA-F]{32}/[0-9a-fA-F]{32}\\.(pdf|jpg|jpeg|png)$"))
            throw new InvalidOperationException("Invalid stored file name");

        var relative = normalized.Replace('/', Path.DirectorySeparatorChar);
        var rootFull = Path.GetFullPath(_root + Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(_root, relative));
        if (!full.StartsWith(rootFull, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid stored file name");
        return full;
    }
}
