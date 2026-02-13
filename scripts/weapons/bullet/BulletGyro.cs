using Godot;
using System;
using System.Collections.Generic;

public partial class BulletGyro : Area2D
{
	// Damage is set by the weapon that spawns the bullet
	public int Damage = 50;

	[Export]
	public float Speed = 600f;

	[Export]
	public float Lifetime = 5.0f;

	[Export]
	public int MaxPenetrations = 3; // Cuántos enemigos puede atravesar

	private Vector2 velocity = Vector2.Zero;
	private float lifeTimer = 0f;
	private Node2D shooter = null;

	// Control interno de penetración
	private int penetrationCount = 0;
	private HashSet<Node> hitBodies = new HashSet<Node>();

	public override void _Ready()
	{
		AddToGroup("projectiles");

		lifeTimer = Lifetime;
		SetPhysicsProcess(true);

		// Conectar señal de colisión
		BodyEntered += OnBodyEntered;
	}

	/// <summary>
	/// Inicializa la bala cuando se instancia.
	/// </summary>
	public void Initialize(Vector2 direction, Node2D owner = null)
	{
		if (direction == Vector2.Zero)
			direction = Vector2.Right;

		velocity = direction.Normalized() * Speed;
		shooter = owner;

		// Reset por si usas pooling
		penetrationCount = 0;
		hitBodies.Clear();
		lifeTimer = Lifetime;
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		// Movimiento manual (más fiable para proyectiles rápidos)
		GlobalPosition += velocity * dt;

		// Control de vida
		lifeTimer -= dt;
		if (lifeTimer <= 0f)
		{
			QueueFree();
		}
	}

	private void OnBodyEntered(Node body)
	{
		if (body == null)
			return;

		// No golpear al que disparó
		if (shooter != null && body == shooter)
			return;

		// Evitar daño múltiple al mismo objetivo
		if (hitBodies.Contains(body))
			return;

		hitBodies.Add(body);
		penetrationCount++;

		ApplyDamage(body);

		// Debug opcional
		GD.Print($"BulletGyro: Hit {body.Name} (penetration {penetrationCount}/{MaxPenetrations})");

		// Si alcanzó el límite de penetraciones → destruir
		if (penetrationCount >= MaxPenetrations)
		{
			QueueFree();
		}
	}

	private void ApplyDamage(Node body)
	{
		try
		{
			var method = body.GetType().GetMethod("ApplyDamage");
			if (method != null)
			{
				method.Invoke(body, new object[] { Damage });
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"BulletGyro damage error: {e.Message}");
		}
	}
}
