
using System.Text;
using System.Text.Json;

namespace Knowtable.Api.Services;

public static class TiptapTextExtractor
{
    public static string ExtractText(JsonElement? document)
    {
        if (document is null ||
            document.Value.ValueKind != JsonValueKind.Object)
            return string.Empty;

        return ExtractNode(document.Value).Trim();
    }

    private static string ExtractNode(JsonElement node)
    {
        if (node.ValueKind != JsonValueKind.Object)
            return string.Empty;

        var type = node.TryGetProperty("type", out var typeElement)
            ? typeElement.GetString()
            : null;

        if (type == "text")
        {
            return node.TryGetProperty("text", out var text)
                ? text.GetString() ?? string.Empty
                : string.Empty;
        }

        return type switch
        {
            "hardBreak" => "\n",
            "paragraph" => ExtractInlineChildren(node),
            "heading" => ExtractHeading(node),
            "bulletList" => ExtractList(node, false),
            "orderedList" => ExtractList(node, true),
            "listItem" => ExtractBlockChildren(node, "\n"),
            "blockquote" => ExtractBlockquote(node),
            "codeBlock" => ExtractCodeBlock(node),
            "horizontalRule" => string.Empty,

            // Traverse unknown node types rather than discarding content.
            _ => ExtractBlockChildren(node, "\n\n")
        };
    }

    private static string ExtractInlineChildren(JsonElement node)
    {
        if (!node.TryGetProperty("content", out var content) ||
            content.ValueKind != JsonValueKind.Array)
            return string.Empty;

        var builder = new StringBuilder();

        foreach (var child in content.EnumerateArray())
            builder.Append(ExtractNode(child));

        return builder.ToString();
    }

    private static string ExtractBlockChildren(
        JsonElement node,
        string separator)
    {
        if (!node.TryGetProperty("content", out var content) ||
            content.ValueKind != JsonValueKind.Array)
            return string.Empty;

        var parts = new List<string>();

        foreach (var child in content.EnumerateArray())
        {
            var text = ExtractNode(child);

            if (!string.IsNullOrWhiteSpace(text))
                parts.Add(text.Trim());
        }

        return string.Join(separator, parts);
    }

    private static string ExtractHeading(JsonElement node)
    {
        var level = 1;

        if (node.TryGetProperty("attrs", out var attrs) &&
            attrs.TryGetProperty("level", out var levelElement) &&
            levelElement.TryGetInt32(out var parsedLevel))
        {
            level = Math.Clamp(parsedLevel, 1, 6);
        }

        var text = ExtractInlineChildren(node);

        return string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : $"{new string('#', level)} {text}";
    }

    private static string ExtractList(
        JsonElement node,
        bool ordered)
    {
        if (!node.TryGetProperty("content", out var content) ||
            content.ValueKind != JsonValueKind.Array)
            return string.Empty;

        var index = 1;

        if (ordered &&
            node.TryGetProperty("attrs", out var attrs) &&
            attrs.TryGetProperty("start", out var startElement) &&
            startElement.TryGetInt32(out var start))
        {
            index = start;
        }

        var items = new List<string>();

        foreach (var child in content.EnumerateArray())
        {
            var text = ExtractNode(child).Trim();

            if (string.IsNullOrWhiteSpace(text))
                continue;

            var marker = ordered ? $"{index++}. " : "- ";
            var lines = text.Split('\n');

            var item = marker + lines[0];

            for (var i = 1; i < lines.Length; i++)
                item += "\n  " + lines[i];

            items.Add(item);
        }

        return string.Join("\n", items);
    }

    private static string ExtractBlockquote(JsonElement node)
    {
        var text = ExtractBlockChildren(node, "\n\n");

        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return string.Join(
            "\n",
            text.Split('\n').Select(line => $"> {line}"));
    }

    private static string ExtractCodeBlock(JsonElement node)
    {
        var code = ExtractInlineChildren(node).TrimEnd();

        return string.IsNullOrWhiteSpace(code)
            ? string.Empty
            : $"```\n{code}\n```";
    }
}
