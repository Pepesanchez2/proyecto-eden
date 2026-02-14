
using Godot;
using System;
using System.Collections.Generic;

public partial class Navaja : Area2D
{
	[Export]
	public int Damage = 25;

	[Export]
	public float AttackInterval = 1.5f;

	[Export]
	public float DetectionRadius = 64f;

	private AnimatedSprite2D sprite;
	private Timer attackCycleTimer;
	private Timer attackDurationTimer;
    private Timer detectTimer;
	private Node2D player;
	private Node2D currentTarget = null;
	private readonly List<Node> targets = new List<Node>();
	private bool isAttacking = false;
	private int lastDamageFrame = -1;

	public override void _Ready()
	{
		player = GetTree().GetFirstNodeInGroup("player") as Node2D;
		sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (sprite != null)
		{
			sprite.Visible = false; // ocultar por defecto, mostrar solo al atacar
		}

		attackCycleTimer = GetNodeOrNull<Timer>("AttackCycleTimer");
		attackDurationTimer = GetNodeOrNull<Timer>("AttackDurationTimer");

		if (attackCycleTimer == null)
		{
			attackCycleTimer = new Timer();
			attackCycleTimer.Name = "AttackCycleTimer";
			attackCycleTimer.WaitTime = AttackInterval;
			attackCycleTimer.OneShot = false;
			AddChild(attackCycleTimer);
		}

		if (attackDurationTimer == null)
		{
			attackDurationTimer = new Timer();
			attackDurationTimer.Name = "AttackDurationTimer";
			attackDurationTimer.OneShot = true;
			AddChild(attackDurationTimer);
		}

		// detection timer: fallback scanning of enemies in case Area2D collisions are not set up
		detectTimer = GetNodeOrNull<Timer>("DetectTimer");
		if (detectTimer == null)
		{
			detectTimer = new Timer();
			detectTimer.Name = "DetectTimer";
			detectTimer.OneShot = false;
			detectTimer.WaitTime = 0.2f;
			AddChild(detectTimer);
		}

		attackCycleTimer.Timeout += OnAttackCycleTimeout;
		attackDurationTimer.Timeout += OnAttackDurationTimeout;
		detectTimer.Timeout += OnDetectTimeout;

		// prefer Area2D collision signals as observer; enable monitoring
		try { this.Monitoring = true; } catch { }
		// keep detectTimer as a disabled fallback; do not start it by default

		try { Connect("body_entered", new Callable(this, nameof(OnBodyEntered))); } catch { }
		try { Connect("body_exited", new Callable(this, nameof(OnBodyExited))); } catch { }

		if (sprite != null)
		{
			try { sprite.Connect("frame_changed", new Callable(this, nameof(OnFrameChanged))); } catch { }
		}
	}

	private void OnBodyEntered(Node body)
	{
		if (body == null) return;
		if (!body.IsInGroup("enemies") && !body.IsInGroup("enemies_hell"))
		{
			return;
		}
		if (!targets.Contains(body))
			targets.Add(body);
		if (targets.Count == 1)
		{
			attackCycleTimer.WaitTime = AttackInterval;
			attackCycleTimer.Start();
		}
	}

	private void OnBodyExited(Node body)
	{
		if (body == null) return;
		targets.Remove(body);
		if (targets.Count == 0)
		{
			attackCycleTimer.Stop();
			isAttacking = false;
			attackDurationTimer.Stop();
			if (sprite != null)
			{
				sprite.Stop();
				sprite.Visible = false;
			}
		}
	}

	private void OnAttackCycleTimeout()
	{
		if (targets.Count == 0)
		{
			attackCycleTimer.Stop();
			return;
		}

		currentTarget = PickClosestTarget();
		if (currentTarget == null)
			return;

		try
		{
			var dir = ((Node2D)currentTarget).GlobalPosition - GlobalPosition;
			if (sprite != null)
			{
				sprite.Visible = true;
				sprite.FlipH = dir.X < 0f;
				sprite.Frame = 0;
				sprite.Play();
			}
		}
		catch { }
		isAttacking = true;
		lastDamageFrame = -1;

		// Compute animation duration (frames / speed) fallback to 0.6s
		float attackDuration = 0.6f;
		try
		{
			if (sprite != null && sprite.SpriteFrames != null)
			{
				var animName = sprite.Animation ?? "default";
				int frames = sprite.SpriteFrames.GetFrameCount(animName);
				float speed = 0f;
				try
				{
					speed = (float)sprite.SpriteFrames.GetAnimationSpeed(animName);
				}
				catch { }

				if (frames > 0 && speed > 0.01f)
					attackDuration = frames / speed;
				else if (frames > 0)
					attackDuration = frames * 0.12f;
			}
		}
		catch { }

		attackDurationTimer.WaitTime = attackDuration;
		attackDurationTimer.Start();
	}

	private void OnDetectTimeout()
	{
		// scan enemies and enemies_hell and simulate enter/exit based on distance TO THE PLAYER
		if (player == null || !Godot.GodotObject.IsInstanceValid(player))
		{
			player = GetTree().GetFirstNodeInGroup("player") as Node2D;
			if (player == null)
			{
				return;
			}
		}
		Vector2 center = player.GlobalPosition;
		var all = new HashSet<Node>();
		try { foreach (var n in GetTree().GetNodesInGroup("enemies")) all.Add((Node)n); } catch { }
		try { foreach (var n in GetTree().GetNodesInGroup("enemies_hell")) all.Add((Node)n); } catch { }
		foreach (var n in all)
		{
			if (!(n is Node2D nt)) continue;
			float d = nt.GlobalPosition.DistanceTo(center);
			if (d <= DetectionRadius)
			{
				if (!targets.Contains(n))
				{
					OnBodyEntered(n);
				}
			}
			else
			{
				if (targets.Contains(n))
				{
					OnBodyExited(n);
				}
			}
		}
	}

	private void OnAttackDurationTimeout()
	{
		isAttacking = false;
		if (sprite != null)
		{
			sprite.Stop();
			sprite.Visible = false;
		}
	}

	private void OnFrameChanged()
	{
		if (!isAttacking || currentTarget == null || sprite == null) return;

		int frame = sprite.Frame;
		if ((frame == 1 || frame == 2) && lastDamageFrame != frame)
		{
			lastDamageFrame = frame;
			ApplyDamageTo(currentTarget);
		}
	}

	private Node2D PickClosestTarget()
	{
		Node2D closest = null;
		float best = float.MaxValue;
		foreach (var t in targets)
		{
			if (t == null) continue;
			if (!(t is Node2D nt)) continue;
			float d = nt.GlobalPosition.DistanceTo(GlobalPosition);
			if (d < best)
			{
				best = d;
				closest = nt;
			}
		}
		return closest;
	}

	private void ApplyDamageTo(Node body)
	{
		if (body == null) return;
		try
		{
			var method = body.GetType().GetMethod("ApplyDamage");
			if (method != null)
			{
				method.Invoke(body, new object[] { Damage });
				try
				{
					var f = body.GetType().GetField("Health");
					var fm = body.GetType().GetField("MaxHealth");
					int remaining = -1, max = -1;
					if (f != null) remaining = (int)f.GetValue(body);
					if (fm != null) max = (int)fm.GetValue(body);
					if (remaining >= 0 && max > 0)
						GD.Print($"Navaja: Target={body.Name} damage={Damage} remaining={remaining}/{max}");
					else
						GD.Print($"Navaja: Target={body.Name} damage={Damage}");
				}
				catch { GD.Print($"Navaja: Target={body.Name} damage={Damage}"); }
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"Navaja damage error: {e.Message}");
		}
	}
}
