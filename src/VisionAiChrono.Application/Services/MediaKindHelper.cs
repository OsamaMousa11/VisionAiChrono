using System;

namespace VisionAiChrono.Application.Services
{
    public static class MediaKindHelper
    {
        public const string Image = "Image";
        public const string Video = "Video";

        public static readonly string[] ImageExtensions =
        {
            ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tiff", ".tif"
        };

        public static readonly string[] VideoExtensions =
        {
            ".mp4", ".avi", ".mov", ".mkv", ".webm", ".wmv", ".flv", ".m4v", ".3gp"
        };

        public static string Detect(string fileName, string? contentType)
        {
            var ext = Path.GetExtension(fileName ?? string.Empty);

            if (Array.Exists(ImageExtensions, e => string.Equals(e, ext, StringComparison.OrdinalIgnoreCase)))
                return Image;

            if (Array.Exists(VideoExtensions, e => string.Equals(e, ext, StringComparison.OrdinalIgnoreCase)))
                return Video;

            if (!string.IsNullOrWhiteSpace(contentType))
            {
                if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    return Image;

                if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
                    return Video;
            }

            return Video;
        }

        public static bool IsImage(string fileName, string? contentType)
            => Detect(fileName, contentType) == Image;

        public static bool IsVideo(string fileName, string? contentType)
            => Detect(fileName, contentType) == Video;
    }
}
