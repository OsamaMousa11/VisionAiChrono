using CleanArchitectureTemplate_Application.Exceptions;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using VisionAiChrono.Application.VisionDetection;
using VisionAiChrono.Application.VisionDetection.Models;

namespace VisionAiChrono.Infrastructure.VisionDetection;

public sealed class VisionDetectionService : IVisionDetectionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VisionDetectionService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public VisionDetectionService(HttpClient httpClient, ILogger<VisionDetectionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<DetectionResponse> DetectAsync(
        DetectionTask task, Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var endpoint = $"/detect/{task.ToRoute()}";

        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));
        content.Add(streamContent, "file", fileName);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(endpoint, content, ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach the vision detection service at {Endpoint}", endpoint);
            throw new ExternalServiceException(
                "Vision detection service is unavailable. Make sure the Python service is running.");
        }

        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Vision detection service returned {StatusCode}: {Body}", response.StatusCode, body);
            throw new ExternalServiceException(
                $"Vision detection service returned an error ({(int)response.StatusCode}): {body}");
        }

        var result = JsonSerializer.Deserialize<DetectionResponse>(body, JsonOptions);
        return result ?? throw new ExternalServiceException("Vision detection service returned an empty response.");
    }

    public async Task<Stream> DownloadVideoAsync(string videoId, CancellationToken ct = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync($"/download/{videoId}", ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to download video {VideoId}", videoId);
            throw new ExternalServiceException("Vision detection service is unavailable.");
        }

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new NotFoundException($"Video '{videoId}' not found or has expired.");

            throw new ExternalServiceException(
                $"Vision detection service returned an error ({(int)response.StatusCode}) while downloading the video.");
        }

        return await response.Content.ReadAsStreamAsync(ct);
    }

    private static string GetContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",
            ".webm" => "video/webm",
            _ => "application/octet-stream"
        };
}