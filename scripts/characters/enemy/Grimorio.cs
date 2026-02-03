using Godot;
using System;

public partial class Grimorio : CharacterBody2D
{
    [Export]
    public float Speed = 50f;

    [Export]
    public int MaxHealth = 120;

    public int Health;

    [Export]
    public int XPOnDeath = 12;

    // Rango y disparo a distancia
    [Export]
    public float ShootRange = 300.0f;

    [Export]
    public float ShootInterval = 3.0f;

    [Export]
    public PackedScene BulletScene;

    private float shootTimer = 0f;

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

        // intentar cargar escena de proyectil por defecto
        if (BulletScene == null)
        {
            var ps = ResourceLoader.Load<PackedScene>("res://scenes/weapons/bullet_grimorio.tscn");
            if (ps != null)
                BulletScene = ps;
        }

        shootTimer = ShootInterval;
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

        // distancia al jugador
        float dist = GlobalPosition.DistanceTo(player.GlobalPosition);

        // ataque por contacto
        if (attackTimer > 0f)
            attackTimer -= (float)delta;

        // disparo a distancia si el jugador está dentro del rango
        if (dist <= ShootRange)
        {
            shootTimer -= (float)delta;
            if (shootTimer <= 0f)
            {
                shootTimer = ShootInterval;
                // disparar un proyectil hacia el jugador
                try
                {
                    if (BulletScene != null)
                    {
                        var b = BulletScene.Instantiate();
                        var bn = b as Node2D;
                        if (bn != null)
                        {
                            bn.GlobalPosition = GlobalPosition;
                            if (GetParent() != null)
                                GetParent().AddChild(bn);
                            else
                                GetTree().Root.AddChild(bn);

                            // inicializar dirección y evitar herir al que dispara
                            if (bn.HasMethod("Initialize"))
                            {
                                var dir = (player.GlobalPosition - GlobalPosition).Normalized();
                                bn.Call("Initialize", dir, this as Node2D);
                            }

                            // intentar asignar daño 1 si existe la propiedad
                            try
                            {
                                var prop = bn.GetType().GetProperty("Damage");
                                if (prop != null && prop.CanWrite)
                                    prop.SetValue(bn, 1);
                                else
                                {
                                    var field = bn.GetType().GetField("Damage");
                                    if (field != null)
                                        field.SetValue(bn, 1);
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            }
        }

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
            // dar XP al jugador si existe AddXP
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
            QueueFree();
        }
    }
}