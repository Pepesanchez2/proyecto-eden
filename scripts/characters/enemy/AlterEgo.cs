using Godot;
using System;

public partial class AlterEgo : CharacterBody2D
{
    [Export]
    public float Speed = 80f;
    [Export] public int xp = 10;
    public InLevelUI _ui;

    [Export]
    public int MaxHealth = 4;

    public int Health;

    [Export]
    public float AttackRange = 20.0f;

    [Export]
    public int AttackDamage = 1;

    [Export]
    public float AttackCooldown = 0.6f;

    private float attackTimer = 0f;

    private Node2D player;
    private AnimatedSprite2D anim;

    public override void _Ready()
    {
        player = GetTree().GetFirstNodeInGroup("player") as Node2D;

        AddToGroup("enemies_hell");

        Health = MaxHealth;

        anim = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

        Node currentScene = GetTree().CurrentScene;

        CanvasLayer uiLayer = GetTree().CurrentScene.GetNode<CanvasLayer>("UI");

        InLevelUI _ui = uiLayer.GetNode<InLevelUI>("InLevelUI");

        GD.PrintErr(_ui);

        if (_ui == null)
            GD.PrintErr("No se encontró InLevelUI en el enemigo " + Name);
    }

    public override void _PhysicsProcess(double delta)
    {

        if (_ui == null)
            GD.PrintErr("No se encontró InLevelUI en el enemigo " + Name);

        if (player == null || !Godot.GodotObject.IsInstanceValid(player))
        {
            player = GetTree().GetFirstNodeInGroup("player") as Node2D;
            if (player == null || !Godot.GodotObject.IsInstanceValid(player))
                return;
        }

        Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized();

        Velocity = direction * Speed;
        MoveAndSlide();

        if (anim != null)
        {
            if (Mathf.Abs(Velocity.X) > 0.1f)
                anim.FlipH = Velocity.X < 0f;
        }

        if (attackTimer > 0f)
            attackTimer -= (float)delta;

        float dist = GlobalPosition.DistanceTo(player.GlobalPosition);
        if (dist <= AttackRange && attackTimer <= 0f)
        {
            attackTimer = AttackCooldown;
            try
            {
                var meth = player.GetType().GetMethod("ApplyDamage");
                if (meth != null)
                    meth.Invoke(player, new object[] { AttackDamage });
            }
            catch { }

            Vector2 away = (GlobalPosition - player.GlobalPosition).Normalized();
            GlobalPosition += away * 4.0f;
        }
    }

    public void ApplyDamage(int amount)
    {
        Health = Math.Max(0, Health - amount);
        if (Health <= 0)
        {
            if (_ui != null)
            {
                _ui.AgregarExperiencia(xp);
                GD.Print("Enviada la experiencia");
            }

            QueueFree();
        }
    }
}