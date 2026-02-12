using Godot;
using System;

public partial class BulletGrimorio : Area2D
{
	// Damage set by weapon that spawns the bullet
	public int Damage = 1;

	[Export]
	public float Speed = 300f;

	[Export]
	public float Lifetime = 5.0f;

	private Vector2 velocity = Vector2.Zero;
	private float lifeTimer = 0f;
	private Node2D shooter = null;

	public override void _Ready()
	{
		AddToGroup("projectiles");
		lifeTimer = Lifetime;
		SetPhysicsProcess(true);
		// ensure Area2D monitors bodies
		try { this.Monitoring = true; this.Monitorable = true; } catch { }
		try
		{
			Connect("body_entered", new Callable(this, nameof(OnBodyEntered)));
		}
		catch { }
	}

	public void Initialize(Vector2 direction, Node2D owner = null, uint targetCollisionLayer = 0)
	{
		if (direction == Vector2.Zero)
			direction = new Vector2(1, 0);
		velocity = direction.Normalized() * Speed;
		shooter = owner;
		try
		{
			// If a target collision layer is provided, set this Area2D to only detect that layer
			if (targetCollisionLayer != 0)
			{
				// CollisionMask defines which layers this Area2D detects
				this.CollisionMask = targetCollisionLayer;
				// Put the bullet on a dedicated projectile layer so it's easier to reason about collisions
				// (layer 2). Keep the mask pointed at the target (player) layer.
				this.CollisionLayer = 2;
			}
			else
			{
				// If no specific target layer provided, listen to all layers so group filtering in OnBodyEntered works
				this.CollisionMask = uint.MaxValue;
				this.CollisionLayer = 2;
			}
		}
		catch { }

		// Debug: print collision layers/masks
		try { GD.Print($"BulletGrimorio initialized. CollisionLayer={this.CollisionLayer} CollisionMask={this.CollisionMask} target={targetCollisionLayer}"); } catch { }
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
		if (body == null)
			return;
		if (shooter != null && body == shooter)
			return;

		// Only act on the player: ignore collisions with other bodies
		bool isPlayer = false;
		try { isPlayer = body.IsInGroup("player"); } catch { isPlayer = false; }
		if (!isPlayer)
			return;

		try
		{
			try { GD.Print($"BulletGrimorio collided with {body.GetType().Name} (name={body.Name}) shooter={shooter?.Name}"); } catch { }
			// print body collision layers/masks when available for debugging
			try {
				var pb = body as PhysicsBody2D;
				if (pb != null) GD.Print($" -> body CollisionLayer={pb.CollisionLayer} CollisionMask={pb.CollisionMask}");
			} catch { }
			var meth = body.GetType().GetMethod("ApplyDamage");
			if (meth != null)
			{
				meth.Invoke(body, new object[] { Damage });
				try { GD.Print($"BulletGrimorio: golpeó al jugador por {Damage} daño."); } catch { }
			}
		}
		catch { }

		QueueFree();
	}
}
