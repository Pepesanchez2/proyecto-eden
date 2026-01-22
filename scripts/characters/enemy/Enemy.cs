using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
    [Export]
    public float Speed = 80f;

    private Node2D player;

    public override void _Ready()
    {
        // Busca al jugador por grupo (recomendado)
        player = GetTree().GetFirstNodeInGroup("player") as Node2D;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (player == null)
            return;

        // Dirección hacia el jugador
        Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized();

        Velocity = direction * Speed;
        MoveAndSlide();
    }
}
