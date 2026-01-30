using Godot;
using System;

public partial class Pepin : CharacterBody2D
{
	// Velocidad de movimiento en píxeles/segundo
	[Export]
	public float Speed = 300.0f;

	// Fuerza de frenado cuando no hay input
	[Export]
	public float Friction = 1000.0f;

	private AnimatedSprite2D anim;

	// Dash
	[Export]
	public float DashSpeed = 800.0f;

	[Export]
	public float DashTime = 1.0f; // duración del dash en segundos (pedido)

	[Export]
	public float DashCooldown = 0.5f;

	[Export]
	public float DashLateralOffset = 20.0f; // desplazamiento lateral al iniciar dash

	private bool isDashing = false;
	private float dashTimer = 0f;
	private float dashCooldownTimer = 0f;
	private Vector2 facing = new Vector2(1, 0);

	// Invencibilidad durante el dash (desactiva colisiones)
	private bool isInvincible = false;
	private float invincibilityTimer = 0f;
	private uint originalCollisionLayer;
	private uint originalCollisionMask;

	// Salud y experiencia
	[Export]
	public int MaxHealth = 10;

	public int Health;

	[Export]
	public int Level = 1;

	public int XP = 0;

	[Export]
	public int XPToNextLevel = 100;
	public override void _Ready()
	{
		AddToGroup("player");
		anim = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

		// Guardar layers/masks originales
		originalCollisionLayer = CollisionLayer;
		originalCollisionMask = CollisionMask;

		// Inicializar salud
		Health = MaxHealth;
	}


	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		// Recoge el vector de entrada: izquierda/derecha/arriba/abajo
		Vector2 inputDir = Input.GetVector("izquierda", "derecha", "arriba", "abajo");

		Vector2 velocity = Velocity;

		if (isDashing)
		{
			// Si estamos en dash, decrementamos el timer y mantenemos la velocidad del dash
			dashTimer -= dt;
			if (dashTimer <= 0f)
			{
				isDashing = false;
				dashCooldownTimer = DashCooldown;
			}
			// No sobrescribimos la velocidad aquí: se estableció al iniciar el dash
		}
		else
		{
			if (inputDir != Vector2.Zero)
			{
				// Normaliza para que la velocidad diagonal no sea mayor
				Vector2 dir = inputDir.Normalized();
				velocity = dir * Speed;
				facing = dir; // actualizar hacia donde mira el jugador
			}
			else
			{
				// Cuando no hay input, acercamos la velocidad a cero suavemente
				velocity = velocity.MoveToward(Vector2.Zero, Friction * dt);
			}
			
			// cooldown del dash
			dashCooldownTimer = Math.Max(0f, dashCooldownTimer - dt);
		}

		// Actualizar animación/flip según input
		if (anim != null)
		{
			if (inputDir == Vector2.Zero)
			{
				// Quieto: frame 0 y parar animación
				anim.Stop();
				anim.Frame = 0;
			}
			else
			{
				// Moviendo: reproducir animación
				anim.Play();
				// Si se mueve hacia la izquierda, voltear horizontalmente
				if (inputDir.X < 0)
					anim.FlipH = true;
				else if (inputDir.X > 0)
					anim.FlipH = false;
			}
		}

		// Dash input: usar la acción 'dash' (mapea Space en InputMap)
		if (!isDashing && dashCooldownTimer <= 0f && Input.IsActionJustPressed("dash"))
		{
			isDashing = true;
			dashTimer = DashTime;
			// establecer invencibilidad y desactivar colisiones temporales
			isInvincible = true;
			invincibilityTimer = DashTime; // mismo tiempo que el dash (puedes variar)
			CollisionLayer = 0;
			CollisionMask = 0;

			// Aplicar desplazamiento lateral ligero para 'deslazarse' un poco
			Vector2 perp = new Vector2(-facing.Y, facing.X).Normalized();
			float side = (GD.Randf() < 0.5f) ? -1f : 1f;
			GlobalPosition += perp * (DashLateralOffset * side);

			Velocity = facing.Normalized() * DashSpeed;
			// opcional: reproducir efecto/animación de dash aquí
		}

		// Actualizar invencibilidad
		if (isInvincible)
		{
			invincibilityTimer -= dt;
			if (invincibilityTimer <= 0f)
			{
				isInvincible = false;
				// restaurar layers/masks originales
				CollisionLayer = originalCollisionLayer;
				CollisionMask = originalCollisionMask;
			}
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	public void ApplyDamage(int amount)
	{
		if (isInvincible)
			return;

		Health = Math.Max(0, Health - amount);
		// Aquí puedes añadir efectos cuando recibe daño (parpadeo, sonido, knockback...)
		if (Health <= 0)
		{
			// muerte simple: desactivar o reiniciar
			QueueFree();
		}
	}

	public void AddXP(int amount)
	{
		XP += amount;
		while (XP >= XPToNextLevel)
		{
			XP -= XPToNextLevel;
			Level += 1;
			// aumentar progresivamente requisito (ejemplo simple)
			XPToNextLevel = (int)(XPToNextLevel * 1.2f);
			// puedes añadir efectos de subir de nivel aquí
		}
	}
}