using Godot;
using System;
using System.Threading.Tasks;

public partial class Transition : CanvasLayer
{
    private AnimationPlayer animationPlayer;

    public override void _Ready()
    {
        animationPlayer = GetNode<AnimationPlayer>("过渡动画");
    }

    public async Task FadeInAsync()
    {
        animationPlayer.Play("淡入淡出");
        await ToSignal(animationPlayer, "animation_finished");
    }

    public async Task FadeOutAsync()
    {
        animationPlayer.PlayBackwards("淡入淡出");
        await ToSignal(animationPlayer, "animation_finished");
    }

    public async Task WipeInAsync()
    {
        animationPlayer.Play("关门");
        await ToSignal(animationPlayer, "animation_finished");
    }

    public async Task WipeOutAsync()
    {
        animationPlayer.Play("开门");
        await ToSignal(animationPlayer, "animation_finished");
    }
}
