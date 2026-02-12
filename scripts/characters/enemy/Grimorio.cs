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

        // intentar cargar escena de proyectil por defecto (protegido contra errores de parseo)
        if (BulletScene == null)
        {
            try
            {
                var ps = ResourceLoader.Load<PackedScene>("res://scenes/weapons/bullet_grimorio.tscn");
                if (ps != null)
                    BulletScene = ps;
            }
            catch (Exception e)
            {
                GD.PrintErr($"Grimorio: no se pudo cargar bullet_grimorio.tscn: {e.Message}");
                BulletScene = null;
            }
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
                            // Spawn the bullet slightly in front of the Grimorio so it doesn't start overlapping
                            // the shooter (which can prevent immediate body_entered events).
                            float spawnOffset = 12.0f;
                            bn.GlobalPosition = GlobalPosition + direction * spawnOffset;
                            if (GetParent() != null)
                                GetParent().AddChild(bn);
                            else
                                GetTree().Root.AddChild(bn);

                            // inicializar dirección y evitar herir al que dispara (llamada tipada si es posible)
                            // 'direction' (calculated above) holds the normalized direction toward the player
                            var dir = direction;
                            // Intentar hacer cast al tipo C# conocido para invocar Initialize directamente
                            var typed = bn as BulletGrimorio;
                            if (typed != null)
                            {
                                uint targetLayer = 0;
                                try {
                                    var pbody = player as CharacterBody2D;
                                    if (pbody != null) targetLayer = pbody.CollisionLayer;
                                } catch { }
                                typed.Initialize(dir, this as Node2D, targetLayer);
                                try
                                {
                                    typed.Damage = 1;
                                }
                                catch { }
                                // flip visual depending on direction (left -> flip horizontally)
                                try
                                {
                                    var sprite = typed.GetNodeOrNull<Sprite2D>("Sprite2D");
                                    if (sprite != null)
                                        sprite.FlipH = dir.X < 0f;
                                    else
                                    {
                                        var anim = typed.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
                                        if (anim != null)
                                            anim.FlipH = dir.X < 0f;
                                    }
                                }
                                catch { }
                            }
                            else
                            {
                                // Fallback: usar Call si la instancia expone Initialize a Godot
                                try
                                {
                                    if (bn.HasMethod("Initialize"))
                                    {
                                        bn.Call("Initialize", dir, this as Node2D);
                                    }
                                }
                                catch { }

                        // also flip fallback sprite if present
                        try
                        {
                            var sprite = bn.GetNodeOrNull<Sprite2D>("Sprite2D");
                            if (sprite != null)
                                sprite.FlipH = dir.X < 0f;
                            else
                            {
                                var anim = bn.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
                                if (anim != null)
                                    anim.FlipH = dir.X < 0f;
                            }
                        }
                        catch { }

                                // intentar asignar daño mediante reflexión como último recurso
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

    public async void ApplyDamage(int amount)
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

            try { await ToSignal(GetTree().CreateTimer(0.35f), "timeout"); } catch { }
            QueueFree();
        }
    }
}