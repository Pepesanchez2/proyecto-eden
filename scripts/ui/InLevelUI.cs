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
	private Panel levelUpPanel;

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

		// encontrar el panel de LevelUp para poder ocultarlo junto al HUD
		var rootUI = GetTree().Root.GetNodeOrNull("UI");
		if (rootUI != null)
		{
			levelUpPanel = rootUI.GetNodeOrNull("LevelUpPanel") as Panel;
		}
	}

	public override void _Process(double delta)
	{
		// Ocultar UI si no hay jugador en la escena (asume que escenas de menú no tienen grupo 'player')
		player = GetTree().GetFirstNodeInGroup("player") as Node2D;
		if (player == null)
		{
			// ocultar UIControl y LevelUpPanel si existen
			this.Visible = false;
			if (levelUpPanel != null)
				levelUpPanel.Visible = false;
			return;
		}
		else
		{
			this.Visible = true;
			if (levelUpPanel != null)
				levelUpPanel.Visible = false; // panel solo visible cuando se sube de nivel
		}
		var viewport = GetViewport();

		// actualizar información de oleada
		if (spawner == null)
			spawner = FindNodeRecursive(GetTree().Root as Node, "Spawner");

		if (spawner != null && waveLabel != null && waveTimerLabel != null)
		{
			// intentar leer CurrentWave, WaveTimeLeft e IsInWave de forma segura (propiedad o campo)
			try
			{
				int cw = 0;
				float wt = 0f;
				bool inWave = false;
				var st = spawner.GetType();
				var propCW = st.GetProperty("CurrentWave");
				var propWT = st.GetProperty("WaveTimeLeft");
				var propIn = st.GetProperty("IsInWave");
				if (propCW != null) cw = (int)propCW.GetValue(spawner);
				else
				{
					var f = st.GetField("currentWave");
					if (f != null) cw = (int)f.GetValue(spawner);
				}
				if (propWT != null) wt = Convert.ToSingle(propWT.GetValue(spawner));
				else
				{
					var f = st.GetField("waveTimer");
					if (f != null) wt = Convert.ToSingle(f.GetValue(spawner));
				}
				if (propIn != null) inWave = (bool)propIn.GetValue(spawner);
				else
				{
					var f = st.GetField("inWave");
					if (f != null) inWave = (bool)f.GetValue(spawner);
				}
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
					// leer valores usando propiedades o campos como fallback
					var pt = pepin.GetType();
					int xp = 0;
					int xpNext = 0;
					int level = 0;
					int health = 0;
					int maxHealth = 0;
					var p = pt.GetProperty("XP");
					if (p != null) xp = (int)p.GetValue(pepin);
					else { var f = pt.GetField("XP"); if (f != null) xp = (int)f.GetValue(pepin); }
					p = pt.GetProperty("XPToNextLevel");
					if (p != null) xpNext = (int)p.GetValue(pepin);
					else { var f = pt.GetField("XPToNextLevel"); if (f != null) xpNext = (int)f.GetValue(pepin); }
					p = pt.GetProperty("Level");
					if (p != null) level = (int)p.GetValue(pepin);
					else { var f = pt.GetField("Level"); if (f != null) level = (int)f.GetValue(pepin); }
					p = pt.GetProperty("Health");
					if (p != null) health = (int)p.GetValue(pepin);
					else { var f = pt.GetField("Health"); if (f != null) health = (int)f.GetValue(pepin); }
					p = pt.GetProperty("MaxHealth");
					if (p != null) maxHealth = (int)p.GetValue(pepin);
					else { var f = pt.GetField("MaxHealth"); if (f != null) maxHealth = (int)f.GetValue(pepin); }

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
