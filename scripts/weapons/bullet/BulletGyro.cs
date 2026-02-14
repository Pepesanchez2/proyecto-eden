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

		// Prefer AnimatedSprite2D if present (gyro has animation)
		var anim = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (anim == null)
		{
			// Fallback to Sprite2D
			var sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
			if (sprite == null)
			{
				try
				{
					var tex = ResourceLoader.Load<Texture2D>("res://assets/weapons/GyroSaber.png");
					if (tex != null)
					{
						sprite = new Sprite2D();
						sprite.Name = "Sprite2D";
						sprite.Texture = tex;
						AddChild(sprite);
					}
				}
				catch { }
			}
		}

		var col = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (col == null)
		{
			try
			{
				col = new CollisionShape2D();
				col.Name = "CollisionShape2D";
				col.Shape = new RectangleShape2D() { Size = new Vector2(8, 8) };
				AddChild(col);
			}
			catch { }
		}

		try { Connect("body_entered", new Callable(this, nameof(OnBodyEntered))); } catch { }
	}

	/// <summary>
	/// Inicializa la bala cuando se instancia.
	/// </summary>
	public void Initialize(Vector2 direction, Node2D owner = null)
	{
		if (direction == Vector2.Zero)
			direction = new Vector2(1, 0);

		velocity = direction.Normalized() * Speed;
		shooter = owner;

		// Reset por si usas pooling
		penetrationCount = 0;
		hitBodies.Clear();
		lifeTimer = Lifetime;

		// orient visual if possible
		try
		{
			var anim = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
			if (anim != null)
			{
				anim.FlipH = direction.X < 0f;
				// prefer animation named "gyro" if present
				try
				{
					if (anim.SpriteFrames != null && anim.SpriteFrames.HasAnimation("gyro"))
					{
						anim.Animation = "gyro";
					}
				}
				catch { }
				try { anim.Frame = 0; anim.Play(); } catch { }
			}
			else
			{
				var spr = GetNodeOrNull<Sprite2D>("Sprite2D");
				if (spr != null) spr.FlipH = direction.X < 0f;
			}
		}
		catch { }
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		if (velocity != Vector2.Zero)
			GlobalPosition += velocity * dt;

		lifeTimer -= dt;
		if (lifeTimer <= 0f)
		{
			QueueFree();
			return;
		}
	}

	private void OnBodyEntered(Node body)
	{
		if (body == null) return;
		if (shooter != null && body == shooter) return;

		if (hitBodies.Contains(body)) return;

		hitBodies.Add(body);
		penetrationCount++;

		try
		{
			var meth = body.GetType().GetMethod("ApplyDamage");
			if (meth != null)
			{
				meth.Invoke(body, new object[] { Damage });
				// try to read remaining health for optional logging
				try
				{
					var f = body.GetType().GetField("Health");
					var fm = body.GetType().GetField("MaxHealth");
					int remaining = -1, max = -1;
					if (f != null) remaining = (int)f.GetValue(body);
					if (fm != null) max = (int)fm.GetValue(body);
					if (remaining >= 0 && max > 0)
						GD.Print($"BulletGyro: Hit {body.Name} damage={Damage} remaining={remaining}/{max}");
					else
						GD.Print($"BulletGyro: Hit {body.Name} damage={Damage}");
				}
				catch { GD.Print($"BulletGyro: Hit {body.Name} damage={Damage}"); }
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"BulletGyro damage error: {e.Message}");
		}

		if (penetrationCount >= MaxPenetrations)
			QueueFree();
	}
}
