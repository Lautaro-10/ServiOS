namespace ServiOS.Helpers;

public static class ImageUploadHelper
{
    public const long MaxFileSize = 5 * 1024 * 1024;

    public static async Task<string> SaveAsync(
        IFormFile file,
        string webRootPath,
        string folder,
        CancellationToken cancellationToken = default)
    {
        var (content, extension) = await ReadImageAsync(file, cancellationToken);
        var directory = Path.Combine(webRootPath, "uploads", folder);
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        await File.WriteAllBytesAsync(Path.Combine(directory, fileName), content, cancellationToken);
        return $"/uploads/{folder}/{fileName}";
    }

    public static async Task<string> SavePrivateAsync(
        IFormFile file,
        string contentRootPath,
        string folder,
        CancellationToken cancellationToken = default)
    {
        var (content, extension) = await ReadImageAsync(file, cancellationToken);
        var directory = Path.Combine(contentRootPath, "App_Data", "uploads", folder);
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        await File.WriteAllBytesAsync(Path.Combine(directory, fileName), content, cancellationToken);
        return fileName;
    }

    public static void DeleteManagedFile(string webRootPath, string? filePath, string folder)
    {
        var prefix = $"/uploads/{folder}/";
        if (string.IsNullOrWhiteSpace(filePath) || !filePath.StartsWith(prefix, StringComparison.Ordinal))
        {
            return;
        }

        var fileName = Path.GetFileName(filePath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        var fullPath = Path.Combine(webRootPath, "uploads", folder, fileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    public static void DeletePrivateFile(string contentRootPath, string? fileName, string folder)
    {
        if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
        {
            return;
        }

        var fullPath = Path.Combine(contentRootPath, "App_Data", "uploads", folder, fileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private static async Task<(byte[] Content, string Extension)> ReadImageAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length <= 0 || file.Length > MaxFileSize)
        {
            throw new InvalidDataException("La imagen debe pesar entre 1 byte y 5 MB.");
        }

        await using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        var content = memoryStream.ToArray();
        var extension = GetImageExtension(content);

        if (extension == null)
        {
            throw new InvalidDataException("El archivo debe ser una imagen JPG, PNG o WEBP válida.");
        }

        return (content, extension);
    }

    private static string? GetImageExtension(byte[] content)
    {
        if (content.Length >= 3 &&
            content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
        {
            return ".jpg";
        }

        if (content.Length >= 8 &&
            content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47 &&
            content[4] == 0x0D && content[5] == 0x0A && content[6] == 0x1A && content[7] == 0x0A)
        {
            return ".png";
        }

        if (content.Length >= 12 &&
            content[0] == 0x52 && content[1] == 0x49 && content[2] == 0x46 && content[3] == 0x46 &&
            content[8] == 0x57 && content[9] == 0x45 && content[10] == 0x42 && content[11] == 0x50)
        {
            return ".webp";
        }

        return null;
    }
}
