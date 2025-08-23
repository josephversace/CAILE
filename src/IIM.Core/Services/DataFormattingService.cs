using Microsoft.AspNetCore.Components;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using IIM.Shared.Models;

namespace IIM.Components.Services;

public class DataFormattingService
{
    public string GetDataType(object? value)
    {
        return value switch
        {
            null => "null",
            string => "string",
            int or long => "number",
            float or double or decimal => "decimal",
            bool => "boolean",
            DateTime or DateTimeOffset => "datetime",
            IDictionary => "object",
            IEnumerable => "array",
            _ => value.GetType().Name.ToLowerInvariant()
        };
    }

    public string FormatValue(object? value)
    {
        return value switch
        {
            null => "null",
            string s => $"\"{s}\"",
            bool b => b.ToString().ToLowerInvariant(),
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss zzz"),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value?.ToString() ?? ""
        };
    }

    public string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:F1} {sizes[order]}";
    }

    public string GetRelativeTime(DateTimeOffset time)
    {
        var diff = DateTimeOffset.UtcNow - time;

        return diff switch
        {
            { TotalSeconds: < 60 } => "just now",
            { TotalMinutes: < 60 } => $"{(int)diff.TotalMinutes}m ago",
            { TotalHours: < 24 } => $"{(int)diff.TotalHours}h ago",
            { TotalDays: < 7 } => $"{(int)diff.TotalDays}d ago",
            { TotalDays: < 30 } => $"{(int)(diff.TotalDays / 7)}w ago",
            _ => time.ToString("MMM dd, yyyy")
        };
    }

    public MarkupString HighlightText(string text, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new MarkupString(text);

        var highlighted = Regex.Replace(
            text,
            Regex.Escape(searchTerm),
            $"<span class='highlight'>$&</span>",
            RegexOptions.IgnoreCase
        );

        return new MarkupString(highlighted);
    }
}
