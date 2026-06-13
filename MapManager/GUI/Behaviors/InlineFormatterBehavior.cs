using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Layout;
using MapManager.GUI.Models.Chat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;

namespace MapManager.GUI.Behaviors;

public static class InlineFormatterBehavior
{
    public static readonly AttachedProperty<IReadOnlyList<MessageSegment>?> SegmentsProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, IReadOnlyList<MessageSegment>?>(
            "Segments", typeof(InlineFormatterBehavior));

    public static readonly AttachedProperty<ICommand?> LinkCommandProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, ICommand?>(
            "LinkCommand", typeof(InlineFormatterBehavior));

    public static readonly AttachedProperty<ICommand?> MentionCommandProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, ICommand?>(
            "MentionCommand", typeof(InlineFormatterBehavior));

    public static readonly AttachedProperty<ICommand?> ChannelCommandProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, ICommand?>(
            "ChannelCommand", typeof(InlineFormatterBehavior));

    static InlineFormatterBehavior()
    {
        SegmentsProperty.Changed.AddClassHandler<TextBlock>(Rebuild);
        LinkCommandProperty.Changed.AddClassHandler<TextBlock>(Rebuild);
        MentionCommandProperty.Changed.AddClassHandler<TextBlock>(Rebuild);
        ChannelCommandProperty.Changed.AddClassHandler<TextBlock>(Rebuild);
    }

    public static IReadOnlyList<MessageSegment>? GetSegments(TextBlock tb) => tb.GetValue(SegmentsProperty);
    public static void SetSegments(TextBlock tb, IReadOnlyList<MessageSegment>? v) => tb.SetValue(SegmentsProperty, v);

    public static ICommand? GetLinkCommand(TextBlock tb) => tb.GetValue(LinkCommandProperty);
    public static void SetLinkCommand(TextBlock tb, ICommand? v) => tb.SetValue(LinkCommandProperty, v);

    public static ICommand? GetMentionCommand(TextBlock tb) => tb.GetValue(MentionCommandProperty);
    public static void SetMentionCommand(TextBlock tb, ICommand? v) => tb.SetValue(MentionCommandProperty, v);

    public static ICommand? GetChannelCommand(TextBlock tb) => tb.GetValue(ChannelCommandProperty);
    public static void SetChannelCommand(TextBlock tb, ICommand? v) => tb.SetValue(ChannelCommandProperty, v);

    private static void Rebuild(TextBlock tb, AvaloniaPropertyChangedEventArgs _)
    {
        tb.Inlines ??= new InlineCollection();
        tb.Inlines.Clear();

        var segments = tb.GetValue(SegmentsProperty);
        if (segments == null) return;

        var linkCmd    = tb.GetValue(LinkCommandProperty);
        var mentionCmd = tb.GetValue(MentionCommandProperty);
        var channelCmd = tb.GetValue(ChannelCommandProperty);

        foreach (var seg in segments)
        {
            if (seg.IsLink)
            {
                tb.Inlines.Add(MakeButton(seg.Text, seg.Url!, "InlineLink", linkCmd,
                    url =>
                    {
                        try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
                        catch { }
                    }));
            }
            else if (seg.IsMention)
            {
                tb.Inlines.Add(MakeButton(seg.Text, seg.Url!, "InlineMention", mentionCmd));
            }
            else if (seg.IsChannel)
            {
                tb.Inlines.Add(MakeButton(seg.Text, seg.Url!, "InlineChannel", channelCmd));
            }
            else
            {
                tb.Inlines.Add(new Run(seg.Text));
            }
        }
    }

    private static InlineUIContainer MakeButton(string label, string param, string cssClass,
        ICommand? command, Action<string>? fallback = null)
    {
        var btn = new Button
        {
            Content = label,
            Padding = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand),
            CornerRadius = new CornerRadius(0),
        };
        btn.Classes.Add(cssClass);
        btn.Click += (_, _) =>
        {
            if (command != null && command.CanExecute(param))
                command.Execute(param);
            else
                fallback?.Invoke(param);
        };
        return new InlineUIContainer { Child = btn };
    }
}
