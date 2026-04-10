using Godot;
using System;

public partial class MessageBox : Control
{
    private Label _label;
    private Button _button;

    public override void _Ready()
    {
        _label = GetNode<Label>("%Label");
        _button = GetNode<Button>("%Button");

        _button.Pressed += Button_Pressed;
    }

    public void ShowMessage(string msg)
    {
        _label.Text = msg;
        Visible = true;
    }

    private void Button_Pressed()
    {
        Visible = false;
    }
}
