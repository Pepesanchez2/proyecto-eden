using Godot;
using System;

public partial class Jesucristo : CharacterBody2D
{
    [Export]
    public float Speed = 80f;

    [Export]
    public int MaxHealth = 4;

    public int Health;

    [Export]
    public int XPOnDeath = 12;

    // Ataque por contacto
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
        // Busca al jugador por grupo (recomendado)
        player = GetTree().GetFirstNodeInGroup("player") as Node2D;

        // Añadir este enemigo al grupo 'enemies' para que el Spawner pueda contarlos
        AddToGroup("enemies");

        Health = MaxHealth;

        anim = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
    }

    public override void _PhysicsProcess(double delta)
    {
        // Asegurarse de que la referencia al jugador sigue siendo válida
        if (player == null || !Godot.GodotObject.IsInstanceValid(player))
        {
            player = GetTree().GetFirstNodeInGroup("player") as Node2D;
            if (player == null || !Godot.GodotObject.IsInstanceValid(player))
                return;
        }

        // Dirección hacia el jugador
        Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized();

        Velocity = direction * Speed;
        MoveAndSlide();

        if (anim != null)
        {
            if (Mathf.Abs(Velocity.X) > 0.1f)
                anim.FlipH = Velocity.X > 0f;
        }

        // ataque por contacto
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

    public async void ApplyDamage(int amount)
    {
        Health = Math.Max(0, Health - amount);
        if (Health <= 0)
        {


            if (player != null)
            {
                try
                {
                    var meth = player.GetType().GetMethod("AddXP");
                    if (meth != null)
                        meth.Invoke(player, new object[] { XPOnDeath });
                }
                catch { }
            }

            try { await ToSignal(GetTree().CreateTimer(0.35f), "timeout"); } catch { }
            QueueFree();
        }
    }
}