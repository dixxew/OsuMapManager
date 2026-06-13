namespace MapManager.GUI.Models.Chat;

public enum MessageSegmentType { Text, Link, Mention, Channel }

public record MessageSegment(MessageSegmentType Type, string Text, string? Url = null)
{
    public bool IsLink    => Type == MessageSegmentType.Link;
    public bool IsMention => Type == MessageSegmentType.Mention;
    public bool IsChannel => Type == MessageSegmentType.Channel;
}
