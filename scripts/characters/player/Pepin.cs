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
	public override void _Ready()
	{
		AddToGroup("player");
	}


	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		// Recoge el vector de entrada: izquierda/derecha/arriba/abajo
		Vector2 inputDir = Input.GetVector("izquierda", "derecha", "arriba", "abajo");

		Vector2 velocity = Velocity;

		if (inputDir != Vector2.Zero)
		{
			// Normaliza para que la velocidad diagonal no sea mayor
			Vector2 dir = inputDir.Normalized();
			velocity = dir * Speed;
		}
		else
		{
			// Cuando no hay input, acercamos la velocidad a cero suavemente
			velocity = velocity.MoveToward(Vector2.Zero, Friction * dt);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}