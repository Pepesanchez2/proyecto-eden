using Godot;
using System;

public partial class BulletColt : Area2D
{
	// Damage is set by the weapon that spawns the bullet (do not export here)
	public int Damage = 50;

	[Export]
	public float Speed = 600f;

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
		// Ensure there is a Sprite2D and CollisionShape2D so the bullet works
		var sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		if (sprite == null)
		{
			try
			{
				var tex = ResourceLoader.Load<Texture2D>("res://assets/weapons/bullet/bullet_colt.png");
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

		var col = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (col == null)
		{
			try
			{
				col = new CollisionShape2D();
				col.Name = "CollisionShape2D";
				col.Shape = new CircleShape2D() { Radius = 6.0f };
				AddChild(col);
			}
			catch { }
		}

		// connect body_entered safely
		try { Connect("body_entered", new Callable(this, nameof(OnBodyEntered))); } catch { }
	}

	public void Initialize(Vector2 direction, Node2D owner = null)
	{
		if (direction == Vector2.Zero)
			direction = new Vector2(1, 0);
		velocity = direction.Normalized() * Speed;
		shooter = owner;
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

		// La detección de colisiones principal se hace por la señal body_entered (OnBodyEntered).
	}

	private void OnBodyEntered(Node body)
	{
		if (body == null)
			return;
		if (shooter != null && body == shooter)
			return;

		try
		{
			var meth = body.GetType().GetMethod("ApplyDamage");
			if (meth != null)
			{
				meth.Invoke(body, new object[] { Damage });
			}
		}
		catch { }

		// Debug: print which enemy was hit and remaining health if available
		try
		{
			string typeName = "?";
			string objName = "?";
			try { typeName = body.GetType().Name; } catch { }
			try { objName = body.Name; } catch { }

			int remaining = -1;
			int max = -1;
			try
			{
				var f = body.GetType().GetField("Health");
				if (f != null) remaining = (int)f.GetValue(body);
				var fm = body.GetType().GetField("MaxHealth");
				if (fm != null) max = (int)fm.GetValue(body);
			}
			catch { }

			if (remaining >= 0 && max > 0)
				GD.Print($"BulletColt: Hit {typeName} '{objName}' - damage={Damage} remaining={remaining}/{max}");
			else
				GD.Print($"BulletColt: Hit {typeName} '{objName}' - damage={Damage}");
		}
		catch { }

		QueueFree();
	}
}
