using Godot;
using System;
using System.Collections.Generic;

public partial class Angel : CharacterBody2D
{
    [Export]
    public float Speed = 80f;

    [Export]
    public int MaxHealth = 3;

    public int Health;

    [Export]
    public int XPOnDeath = 10;

    [Export]
    public float AttackRange = 20.0f;

    [Export]
    public int AttackDamage = 1;

    [Export]
    public float AttackCooldown = 0.6f;

    private float attackTimer = 0f;

    // Distancia a la que los enemigos comienzan a evitarse entre sí
    [Export]
    public float AvoidanceRadius = 28.0f;

    // Fuerza de repulsión para separarse
    [Export]
    public float AvoidanceStrength = 220.0f;

    // Desviación aleatoria en grados para evitar que sigan exactamente la misma línea
    [Export]
    public float RandomAngleDeg = 10.0f;

    private Node2D player;
    private AnimatedSprite2D anim;

    public override void _Ready()
    {
        // Busca al jugador por grupo (recomendado)
        player = GetTree().GetFirstNodeInGroup("player") as Node2D;

        // Añadir este enemigo al grupo 'enemies' para que el Spawner pueda contarlos
        AddToGroup("enemies");

        Health = MaxHealth;

        // intentar obtener el AnimatedSprite2D para poder hacer flip según dirección
        anim = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
    }

    public override void _PhysicsProcess(double delta)
    {
        // Verificar referencia válida al jugador (puede ser liberado)
        if (player == null || !Godot.GodotObject.IsInstanceValid(player))
        {
            player = GetTree().GetFirstNodeInGroup("player") as Node2D;
            if (player == null || !Godot.GodotObject.IsInstanceValid(player))
                return;
        }

        // Dirección base hacia el jugador
        Vector2 baseDir = (player.GlobalPosition - GlobalPosition);
        if (baseDir == Vector2.Zero)
            baseDir = new Vector2(0.0001f, 0);
        baseDir = baseDir.Normalized();

        // Añadir desviación aleatoria pequeña para que no formen fila perfecta
        float rad = RandomAngleDeg * (float)(Math.PI / 180.0);
        float angleOffset = (GD.Randf() - 0.5f) * 2f * rad;
        baseDir = baseDir.Rotated(angleOffset);

        // Calcular vector de evitación respecto a otros enemigos
        Vector2 avoidance = Vector2.Zero;
        var nodes = GetTree().GetNodesInGroup("enemies");
        foreach (var n in nodes)
        {
            if (n == this)
                continue;
            var other = n as Node2D;
            if (other == null)
                continue;
            Vector2 toOther = other.GlobalPosition - GlobalPosition;
            float dist = toOther.Length();
            if (dist > 0 && dist < AvoidanceRadius)
            {
                // cuanto más cerca, mayor la fuerza (normalizada por distancia)
                float factor = (AvoidanceRadius - dist) / AvoidanceRadius;
                avoidance -= toOther.Normalized() * factor * AvoidanceStrength * (float)delta;
            }
        }

        // Combinar la dirección hacia el jugador con la evitación
        Vector2 combined = baseDir + avoidance * (1.0f / Math.Max(1f, Speed));
        if (combined == Vector2.Zero)
            combined = baseDir;
        combined = combined.Normalized();

        Velocity = combined * Speed;

        MoveAndSlide();

        // ajustar flip horizontal si tenemos animación
        if (anim != null)
        {
            if (Mathf.Abs(Velocity.X) > 0.1f)
                anim.FlipH = Velocity.X < 0f;
        }

        // temporizador de ataque
        if (attackTimer > 0f)
            attackTimer -= (float)delta;

        // Si estamos lo bastante cerca del jugador, aplicar daño y separarnos un poco
        if (player != null && Godot.GodotObject.IsInstanceValid(player))
        {
            float dist = GlobalPosition.DistanceTo(player.GlobalPosition);
            if (dist <= AttackRange && attackTimer <= 0f)
            {
                attackTimer = AttackCooldown;
                // intentar llamar ApplyDamage(int) si existe
                try
                {
                    var meth = player.GetType().GetMethod("ApplyDamage");
                    if (meth != null)
                    {
                        meth.Invoke(player, new object[] { AttackDamage });
                    }
                }
                catch
                {
                    // ignore
                }

                // empujar ligeramente hacia fuera para evitar solapamiento
                Vector2 away = (GlobalPosition - player.GlobalPosition).Normalized();
                GlobalPosition += away * 6.0f;
            }
        }

        // health/death handled in ApplyDamage
    }

    public async void ApplyDamage(int amount)
    {
        // Si el daño proviene de un proyectil (no CaC), 20% de probabilidad de esquivar.
        // Heurística: buscar nodos de grupos típicos de proyectiles cerca de la posición.
        try
        {
            string[] projGroups = new string[] { "projectiles", "projectile", "bullets", "proyectil" };
            float detectRadius = 48.0f; // píxeles
            foreach (var g in projGroups)
            {
                var nodes = GetTree().GetNodesInGroup(g);
                foreach (var n in nodes)
                {
                    var nd = n as Node2D;
                    if (nd == null)
                        continue;
                    if (!Godot.GodotObject.IsInstanceValid(nd))
                        continue;
                    if (nd.GlobalPosition.DistanceTo(GlobalPosition) <= detectRadius)
                    {
                        // Encontrado un proyectil cercano; tirar la probabilidad de esquiva
                        if (GD.Randf() <= 0.20f)
                        {
                            // esquiva
                            GD.Print("Angel esquivó un proyectil (20%)");
                            return;
                        }
                        // Si no esquiva, proceder con el daño normalmente
                        goto apply_damage;
                    }
                }
            }
        }
        catch { }

        apply_damage:
        Health = Math.Max(0, Health - amount);
        if (Health <= 0)
        {


            if (player != null)
            {
                try
                {
                    var meth = player.GetType().GetMethod("AddXP");
                    if (meth != null)
                    {
                        meth.Invoke(player, new object[] { XPOnDeath });
                    }
                }
                catch { }
            }

            try { await ToSignal(GetTree().CreateTimer(0.35f), "timeout"); } catch { }
            QueueFree();
        }
    }
}
