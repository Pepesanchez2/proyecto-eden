using Godot;
using System;

public partial class InLevelUI : Control
{
	private AnimatedSprite2D healthSprite;
	private Player player;

	public override void _Ready()
	{
		// Buscar el AnimatedSprite2D de la barra de vida
		healthSprite = GetNodeOrNull<AnimatedSprite2D>("health_bar");

		if (healthSprite == null)
			GD.PushWarning("No se encontró AnimatedSprite2D llamado 'health_bar'");

		// Conectarse al jugador cuando ya exista
		CallDeferred(nameof(ConnectPlayer));
	}

	private void ConnectPlayer()
	{
		player = GetTree().GetFirstNodeInGroup("player") as Player;

		if (player == null)
		{
			// El jugador aún no existe, reintentar
			CallDeferred(nameof(ConnectPlayer));
			return;
		}

		// Conectar señal (solo una vez)
		player.HealthChanged += OnPlayerHealthChanged;

		// 🔥 Actualización inicial obligatoria
		OnPlayerHealthChanged(player.Health, player.MaxHealth);
	}

	private void OnPlayerHealthChanged(int current, int max)
	{
		if (healthSprite == null)
			return;

		var frames = healthSprite.SpriteFrames;
		var anim = healthSprite.Animation;

		if (frames == null || !frames.HasAnimation(anim))
			return;

		int frameCount = frames.GetFrameCount(anim);

		// 1 punto de vida = 1 frame
		int frame = max - current;

		healthSprite.Frame = Mathf.Clamp(frame, 0, frameCount - 1);
	}
}
