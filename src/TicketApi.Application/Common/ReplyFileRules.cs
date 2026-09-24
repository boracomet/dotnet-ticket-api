using TicketApi.Application.DTOs;

namespace TicketApi.Application.Common;

public static class ReplyFileRules
{
    public const int MaxFiles = 3;
    public const int MaxBytes = 5 * 1024 * 1024;

    public static (string Extension, string ContentType) Inspect(ReplyFileUpload file)
    {
        var name = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(name).ToLowerInvariant();
        if (file.Content.Length == 0 || file.Content.Length > MaxBytes)
            throw new AppException(400, "VALIDATION_ERROR", "Each file must be between 1 byte and 5 MB");

        var contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => throw new AppException(400, "VALIDATION_ERROR", "Only PDF, JPG and PNG files are allowed")
        };

        if (!SignatureMatches(file.Content, ext))
            throw new AppException(400, "VALIDATION_ERROR", "File content does not match PDF, JPG or PNG");

        return (ext, contentType);
    }

    private static bool SignatureMatches(byte[] data, string ext)
    {
        if (ext is ".jpg" or ".jpeg")
            return data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF;
        if (ext == ".png")
            return data.Length >= 8
                && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47
                && data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A;
        if (ext == ".pdf")
            return data.Length >= 4 && data[0] == 0x25 && data[1] == 0x50 && data[2] == 0x44 && data[3] == 0x46;
        return false;
    }
}
