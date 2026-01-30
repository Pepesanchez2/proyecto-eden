using Godot;
using System;
using System.Collections.Generic;

public partial class Angel : CharacterBody2D
{
    [Export]
    public float Speed = 80f;

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

    public override void _Ready()
    {
        // Busca al jugador por grupo (recomendado)
        player = GetTree().GetFirstNodeInGroup("player") as Node2D;

        // Añadir este enemigo al grupo 'enemies' para que el Spawner pueda contarlos
        AddToGroup("enemies");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (player == null)
            return;

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

        // temporizador de ataque
        if (attackTimer > 0f)
            attackTimer -= (float)delta;

        // Si estamos lo bastante cerca del jugador, aplicar daño y separarnos un poco
        if (player != null)
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
    }
}
