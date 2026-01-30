using Godot;
using System;

public partial class InLevelUI : Control
{
	private Label waveLabel;
	private Label waveTimerLabel;
	private ProgressBar xpBar;
	private Label levelLabel;
	private ProgressBar healthBar;

	private Node2D player;
	private Node spawner;

	public override void _Ready()
	{
		waveLabel = GetNodeOrNull<Label>("WaveLabel");
		waveTimerLabel = GetNodeOrNull<Label>("WaveTimerLabel");
		xpBar = GetNodeOrNull<ProgressBar>("XPBar");
		levelLabel = GetNodeOrNull<Label>("LevelLabel");
		healthBar = GetNodeOrNull<ProgressBar>("HealthBar");

		player = GetTree().GetFirstNodeInGroup("player") as Node2D;

		// intentar encontrar Spawner en la escena (por nombre)
		spawner = FindNodeRecursive(GetTree().Root as Node, "Spawner");
	}

	public override void _Process(double delta)
	{
		var viewport = GetViewport();

		// actualizar información de oleada
		if (spawner != null && waveLabel != null && waveTimerLabel != null)
		{
			try
			{
				var st = spawner.GetType();
				int cw = (int)st.GetProperty("CurrentWave").GetValue(spawner);
				float wt = (float)st.GetProperty("WaveTimeLeft").GetValue(spawner);
				bool inWave = (bool)st.GetProperty("IsInWave").GetValue(spawner);
				waveLabel.Text = inWave ? $"Oleada {cw}" : $"Preparando...";
				waveTimerLabel.Text = $"{Math.Ceiling(wt)}s";
			}
			catch
			{
				// ignore
			}
		}

		// actualizar XP/level y health
		if (player != null)
		{
			var pepin = player as Node;
			if (pepin != null)
			{
				// obtener campos por reflexión (Pepin.cs)
				try
				{
					int xp = (int)pepin.GetType().GetProperty("XP").GetValue(pepin);
					int xpNext = (int)pepin.GetType().GetProperty("XPToNextLevel").GetValue(pepin);
					int level = (int)pepin.GetType().GetProperty("Level").GetValue(pepin);
					int health = (int)pepin.GetType().GetProperty("Health").GetValue(pepin);
					int maxHealth = (int)pepin.GetType().GetProperty("MaxHealth").GetValue(pepin);

					if (xpBar != null)
					{
						xpBar.MaxValue = xpNext;
						xpBar.Value = xp;
					}
					if (levelLabel != null)
						levelLabel.Text = $"Lv {level}";
					if (healthBar != null)
					{
						healthBar.MaxValue = maxHealth;
						healthBar.Value = health;
						// posicionar la barra justo debajo del jugador en pantalla
						var cam = FindCamera2D(GetTree().Root as Node);
						if (cam != null)
						{
							Vector2 screenPos = (player.GlobalPosition - cam.GlobalPosition) + (viewport.GetVisibleRect().Size / 2);
							Vector2 pos = screenPos + new Vector2(-healthBar.Size.X/2, 40);
							healthBar.Position = pos;
						}
					}
				}
				catch (Exception)
				{
					// ignore reflection errors
				}
			}
		}
	}

	// Helper: buscar recursivamente un nodo por nombre
	private Node FindNodeRecursive(Node parent, string name)
	{
		if (parent == null)
			return null;

		if (parent.Name == name)
			return parent;

		foreach (var item in parent.GetChildren())
		{
			var child = item as Node;
			if (child == null)
				continue;
			var found = FindNodeRecursive(child, name);
			if (found != null)
				return found;
		}

		return null;
	}

	private Camera2D FindCamera2D(Node parent)
	{
		if (parent == null)
			return null;

		if (parent is Camera2D cam)
			return cam;

		foreach (var item in parent.GetChildren())
		{
			var child = item as Node;
			if (child == null)
				continue;
			var found = FindCamera2D(child);
			if (found != null)
				return found;
		}

		return null;
	}
}
