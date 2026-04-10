using Godot;
using System;

public partial class WaitPage : Control
{
    private Label messageLabel;
    private Label ellipsisLabel;
    private Timer typewriter;

    public override void _Ready()
    {
        messageLabel = GetNode<Label>("%消息内容");
        ellipsisLabel = GetNode<Label>("%省略号");
        typewriter = GetNode<Timer>("%打字机");

        typewriter.Timeout += Typewriter_Timeout;

        VisibilityChanged += WaitPage_VisibilityChanged;

        typewriter.Start();
    }

    public void SetMessage(string msg)
    {
        messageLabel.Text = msg;
        ellipsisLabel.VisibleCharacters = 0;
    }

    private void Typewriter_Timeout()
    {
        var char_count = ellipsisLabel.Text.Length;
        if (ellipsisLabel.VisibleCharacters < char_count)
        {
            ellipsisLabel.VisibleCharacters += 1;

        }
        else
        {
            ellipsisLabel.VisibleCharacters = 0;
        }
    }

    private void WaitPage_VisibilityChanged()
    {
        if (Visible)
        {
            typewriter.Start();
        }
        else
        {
            typewriter.Stop();
        }
    }
}
