using VisionAiChrono.Application.VisionDetection.Models;

namespace VisionAiChrono.Application.VisionDetection
{
    /// <summary>
    /// Parses the plain "tasks" form field (e.g. "0,1,2" or "0 1 2") coming from
    /// the API into detection tasks. The vision detection service is addressed by
    /// the same index, so there is no model lookup involved.
    /// </summary>
    public static class TaskIndexParser
    {
        private static readonly char[] Separators = { ',', ' ', ';', '|' };

        public static bool TryParse(string? raw, out List<DetectionTask> tasks, out string? error)
        {
            tasks = new List<DetectionTask>();
            error = null;

            if (string.IsNullOrWhiteSpace(raw))
            {
                error = "At least one task is required.";
                return false;
            }

            var seen = new HashSet<int>();

            foreach (var token in raw.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(token.Trim(), out var index))
                {
                    error = $"'{token.Trim()}' is not a valid task. Use 0 (person), 1 (weapon) or 2 (fire).";
                    return false;
                }

                var task = DetectionTaskExtensions.FromIndex(index);

                if (task is null)
                {
                    error = $"Task {index} is not supported. Use 0 (person), 1 (weapon) or 2 (fire).";
                    return false;
                }

                if (seen.Add(index))
                    tasks.Add(task.Value);
            }

            if (tasks.Count == 0)
            {
                error = "At least one task is required.";
                return false;
            }

            return true;
        }
    }
}
