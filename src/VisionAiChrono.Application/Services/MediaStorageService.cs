using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitectureTemplate_Application.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using VisionAiChrono.Application.ServiceContract;

namespace VisionAiChrono.Application.Services
{
    public class MediaStorageService : IMediaStorageService
    {
        private const string DefaultFolder = "App_Data/media";
        private const long DefaultMaxBytes = 200L * 1024 * 1024;

        private readonly string _rootPath;
        private readonly long _maxFileSizeBytes;

        public MediaStorageService(IWebHostEnvironment environment, IConfiguration configuration)
        {
            var configuredPath = configuration["MediaStorage:RootPath"];

            _rootPath = string.IsNullOrWhiteSpace(configuredPath)
                ? Path.Combine(environment.ContentRootPath, DefaultFolder)
                : Path.IsPathRooted(configuredPath)
                    ? configuredPath
                    : Path.Combine(environment.ContentRootPath, configuredPath);

            var configuredMax = configuration.GetValue<long?>("MediaStorage:MaxFileSizeBytes");

            _maxFileSizeBytes = configuredMax is > 0 ? configuredMax.Value : DefaultMaxBytes;

            Directory.CreateDirectory(_rootPath);
        }

        public long GetMaxFileSizeBytes() => _maxFileSizeBytes;

        public async Task<string> SaveAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
        {
            if (stream is null || !stream.CanRead)
                throw new BadRequestException("No readable content was provided.");

            var safeName = MakeSafeFileName(fileName);
            var relativeDirectory = DateTime.UtcNow.ToString("yyyy/MM");
            var directory = Path.Combine(_rootPath, relativeDirectory);

            Directory.CreateDirectory(directory);

            var relativePath = Path.Combine(relativeDirectory, safeName).Replace('\\', '/');
            var absolutePath = Path.Combine(_rootPath, relativePath);

            await using var fileStream = new FileStream(
                absolutePath, FileMode.Create, FileAccess.Write, FileShare.None,
                bufferSize: 81920, useAsync: true);

            var buffer = new byte[81920];
            long total = 0;

            while (true)
            {
                var read = await stream.ReadAsync(buffer, cancellationToken);

                if (read == 0)
                    break;

                total += read;

                if (total > _maxFileSizeBytes)
                    throw new BadRequestException($"File exceeds the maximum allowed size of {_maxFileSizeBytes} bytes.");

                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            return relativePath;
        }

        public string GetAbsolutePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new BadRequestException("File path is empty.");

            var combined = Path.GetFullPath(Path.Combine(_rootPath, relativePath));

            if (!combined.StartsWith(Path.GetFullPath(_rootPath), StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException("Invalid file path.");

            return combined;
        }

        public bool Exists(string relativePath)
        {
            try
            {
                return File.Exists(GetAbsolutePath(relativePath));
            }
            catch (BadRequestException)
            {
                return false;
            }
        }

        public Task DeleteAsync(string relativePath)
        {
            try
            {
                var absolutePath = GetAbsolutePath(relativePath);

                if (File.Exists(absolutePath))
                    File.Delete(absolutePath);
            }
            catch (BadRequestException)
            {
            }

            return Task.CompletedTask;
        }

        private static string MakeSafeFileName(string? fileName)
        {
            var name = Path.GetFileName(fileName ?? string.Empty);

            if (string.IsNullOrWhiteSpace(name))
                name = "file";

            var extension = Path.GetExtension(name);
            var baseName = Path.GetFileNameWithoutExtension(name);

            foreach (var invalid in Path.GetInvalidFileNameChars())
                baseName = baseName.Replace(invalid, '_');

            if (baseName.Length > 80)
                baseName = baseName[..80];

            var unique = $"{Guid.NewGuid():N}{extension}";

            return $"{baseName}_{unique}";
        }
    }
}
