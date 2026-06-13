using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MapManager.GUI.Models.Chat;

public static class MessageParser
{
    // Non-greedy: for regular messages, each [url text] is its own link
    private static readonly Regex RegularPattern = new(
        @"\[(\S+)\s+([^\]]+)\]|(https?://\S+)|(@[^\s,;:!?.]+)|(#[a-zA-Z]\w*)",
        RegexOptions.Compiled);

    // Bare URL / mention / channel (for text portions in action messages)
    private static readonly Regex TextSegmentPattern = new(
        @"(https?://\S+)|(@[^\s,;:!?.]+)|(#[a-zA-Z]\w*)",
        RegexOptions.Compiled);

    /// <summary>
    /// isAction=true uses last ] (greedy) so [url text with [brackets]] works for osu! status messages.
    /// isAction=false uses first ] so multiple [link1] [link2] work for regular chat.
    /// </summary>
    public static IReadOnlyList<MessageSegment> Parse(string text, bool isAction = false)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        return isAction ? ParseAction(text) : ParseRegular(text);
    }

    // ── action (greedy bracket) ──────────────────────────────────────────────

    private static IReadOnlyList<MessageSegment> ParseAction(string text)
    {
        var segments = new List<MessageSegment>();

        var start = text.IndexOf('[');
        var end = start >= 0 ? text.LastIndexOf(']') : -1;

        if (start >= 0 && end > start)
        {
            // Text before [
            if (start > 0)
                AddTextWithBareUrls(segments, text[..start]);

            var inside = text[(start + 1)..end];
            var spaceIdx = inside.IndexOf(' ');

            if (spaceIdx > 0 && LooksLikeUrl(inside[..spaceIdx]))
                segments.Add(new MessageSegment(MessageSegmentType.Link, inside[(spaceIdx + 1)..].Trim(), inside[..spaceIdx]));
            else
                segments.Add(new MessageSegment(MessageSegmentType.Text, inside));

            // Text after ]
            if (end < text.Length - 1)
                AddTextWithBareUrls(segments, text[(end + 1)..]);
        }
        else
        {
            AddTextWithBareUrls(segments, text);
        }

        return segments.Count > 0 ? segments : [new MessageSegment(MessageSegmentType.Text, text)];
    }

    // ── regular (non-greedy bracket) ────────────────────────────────────────

    private static IReadOnlyList<MessageSegment> ParseRegular(string text)
    {
        var segments = new List<MessageSegment>();
        int pos = 0;

        foreach (Match m in RegularPattern.Matches(text))
        {
            if (m.Index > pos)
                segments.Add(new MessageSegment(MessageSegmentType.Text, text[pos..m.Index]));

            if (m.Groups[1].Success)
                segments.Add(new MessageSegment(MessageSegmentType.Link, m.Groups[2].Value, m.Groups[1].Value));
            else if (m.Groups[3].Success)
            {
                var url = m.Groups[3].Value;
                segments.Add(new MessageSegment(MessageSegmentType.Link, url, url));
            }
            else if (m.Groups[4].Success)
            {
                var raw = m.Groups[4].Value; // @nick
                segments.Add(new MessageSegment(MessageSegmentType.Mention, raw, raw[1..]));
            }
            else if (m.Groups[5].Success)
            {
                var raw = m.Groups[5].Value; // #chan
                segments.Add(new MessageSegment(MessageSegmentType.Channel, raw, raw));
            }

            pos = m.Index + m.Length;
        }

        if (pos < text.Length)
            segments.Add(new MessageSegment(MessageSegmentType.Text, text[pos..]));

        return segments.Count > 0 ? segments : [new MessageSegment(MessageSegmentType.Text, text)];
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static void AddTextWithBareUrls(List<MessageSegment> segments, string text)
    {
        int pos = 0;
        foreach (Match m in TextSegmentPattern.Matches(text))
        {
            if (m.Index > pos)
                segments.Add(new MessageSegment(MessageSegmentType.Text, text[pos..m.Index]));

            if (m.Groups[1].Success)
                segments.Add(new MessageSegment(MessageSegmentType.Link, m.Value, m.Value));
            else if (m.Groups[2].Success)
                segments.Add(new MessageSegment(MessageSegmentType.Mention, m.Value, m.Value[1..]));
            else if (m.Groups[3].Success)
                segments.Add(new MessageSegment(MessageSegmentType.Channel, m.Value, m.Value));

            pos = m.Index + m.Length;
        }
        if (pos < text.Length)
            segments.Add(new MessageSegment(MessageSegmentType.Text, text[pos..]));
    }

    private static bool LooksLikeUrl(string s) =>
        s.StartsWith("http://") || s.StartsWith("https://");
}
