namespace TicketApi.Application.Interfaces;

public interface IReplyFileStore
{
    Task SaveAsync(string storedName, byte[] content, CancellationToken ct = default);
    Task<byte[]?> ReadAsync(string storedName, CancellationToken ct = default);
}
