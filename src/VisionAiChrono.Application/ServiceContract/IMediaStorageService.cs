using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VisionAiChrono.Application.ServiceContract
{
    public interface IMediaStorageService
    {
        Task<string> SaveAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);

        string GetAbsolutePath(string relativePath);

        bool Exists(string relativePath);

        Task DeleteAsync(string relativePath);

        long GetMaxFileSizeBytes();
    }
}
