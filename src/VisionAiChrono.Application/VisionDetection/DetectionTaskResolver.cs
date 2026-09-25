using System;
using System.Text.Json;
using VisionAiChrono.Application.VisionDetection.Models;
using VisionAiChrono.Domain.Model.Entity;

namespace VisionAiChrono.Application.VisionDetection
{
    public static class DetectionTaskResolver
    {
        public static DetectionTask Resolve(AiModel model)
        {
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            if (TryParse(model.ModelType, out var task))
                return task;

            if (TryParse(ExtractTaskFromConfiguration(model.ConfigurationJson), out task))
                return task;

            return DetectionTask.Person;
        }

        public static DetectionTask Resolve(string? modelType, string? configurationJson)
        {
            if (TryParse(modelType, out var task))
                return task;

            if (TryParse(ExtractTaskFromConfiguration(configurationJson), out task))
                return task;

            return DetectionTask.Person;
        }

        private static string? ExtractTaskFromConfiguration(string? configurationJson)
        {
            if (string.IsNullOrWhiteSpace(configurationJson))
                return null;

            try
            {
                using var document = JsonDocument.Parse(configurationJson);

                if (document.RootElement.ValueKind != JsonValueKind.Object)
                    return null;

                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (property.Name.Equals("task", StringComparison.OrdinalIgnoreCase) ||
                        property.Name.Equals("modelType", StringComparison.OrdinalIgnoreCase) ||
                        property.Name.Equals("detectionTask", StringComparison.OrdinalIgnoreCase))
                    {
                        return property.Value.ValueKind == JsonValueKind.String
                            ? property.Value.GetString()
                            : null;
                    }
                }
            }
            catch (JsonException)
            {
                return null;
            }

            return null;
        }

        private static bool TryParse(string? value, out DetectionTask task)
        {
            task = DetectionTask.Person;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            return Enum.TryParse(value.Trim(), ignoreCase: true, out task);
        }
    }
}
