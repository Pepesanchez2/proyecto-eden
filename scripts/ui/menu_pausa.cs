using Godot;
using System.Collections.Generic;

public partial class menu_pausa : CanvasLayer
{
	private ColorRect overlay;
	private HBoxContainer inventoryContainer;
	private Button botonVolver;
	private Button botonQuitarPausa;
	private TextureRect textureRect;
	private bool isPaused = false;

	public override void _Ready()
	{
		// Hacer que este nodo no se vea afectado por la pausa del juego
		ProcessMode = ProcessModeEnum.Always;

		// Crear overlay oscuro si no existe
		overlay = GetNodeOrNull<ColorRect>("Overlay");
		if (overlay == null)
		{
			overlay = new ColorRect();
			overlay.Name = "Overlay";
			overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			overlay.Color = new Color(0, 0, 0, 0.6f);
			overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
			overlay.Visible = false;
			overlay.ZIndex = -1;
			AddChild(overlay);
		}
		else
		{
			overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
		}

		// Obtener referencias a los botones
		botonVolver = GetNodeOrNull<Button>("volver");
		botonQuitarPausa = GetNodeOrNull<Button>("quitar_pausa");
		textureRect = GetNodeOrNull<TextureRect>("TextureRect");

		// Configurar ProcessMode para botones
		if (botonVolver != null)
		{
			botonVolver.ProcessMode = ProcessModeEnum.Always;
			botonVolver.MouseFilter = Control.MouseFilterEnum.Stop;
		}
		if (botonQuitarPausa != null)
		{
			botonQuitarPausa.ProcessMode = ProcessModeEnum.Always;
			botonQuitarPausa.MouseFilter = Control.MouseFilterEnum.Stop;
		}

		// Obtener contenedor de inventario de la escena
		inventoryContainer = GetNodeOrNull<HBoxContainer>("InventoryContainer");
		
		// Ocultar todos los elementos de pausa al inicio
		if (botonVolver != null) botonVolver.Visible = false;
		if (botonQuitarPausa != null) botonQuitarPausa.Visible = false;
		if (textureRect != null) textureRect.Visible = false;
		if (inventoryContainer != null) inventoryContainer.Visible = false;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			if (Input.IsActionJustPressed("pausa"))
			{
				if (!isPaused)
					Pausar();
				else
					QuitarPausa();
				
				GetTree().Root.SetInputAsHandled();
			}
		}
	}

	private void Pausar()
	{
		isPaused = true;
		GetTree().Paused = true;

		// Mostrar overlay
		if (overlay != null)
			overlay.Visible = true;

		// Mostrar botones
		if (botonVolver != null) botonVolver.Visible = true;
		if (botonQuitarPausa != null) botonQuitarPausa.Visible = true;
		if (textureRect != null) textureRect.Visible = true;

		// Mostrar inventario
		UpdateInventory();
		if (inventoryContainer != null)
			inventoryContainer.Visible = true;
	}

	public void QuitarPausa()
	{
		isPaused = false;
		GetTree().Paused = false;

		// Ocultar overlay
		if (overlay != null)
			overlay.Visible = false;

		// Ocultar botones
		if (botonVolver != null) botonVolver.Visible = false;
		if (botonQuitarPausa != null) botonQuitarPausa.Visible = false;
		if (textureRect != null) textureRect.Visible = false;

		// Ocultar inventario
		if (inventoryContainer != null)
			inventoryContainer.Visible = false;
	}

	private void UpdateInventory()
	{
		if (inventoryContainer == null)
			return;

		// Limpiar contenedor anterior
		foreach (var child in inventoryContainer.GetChildren())
		{
			if (child is Node n)
				n.QueueFree();
		}

		// Obtener armas del jugador
		var player = GetTree().GetFirstNodeInGroup("player") as Node;
		if (player == null)
			return;

		List<string> weapons = new List<string>();
		try
		{
			var field = player.GetType().GetField("OwnedWeapons");
			if (field != null)
			{
				var val = field.GetValue(player);
				if (val is System.Collections.IEnumerable)
				{
					var list = (System.Collections.IEnumerable)val;
					foreach (var w in list)
					{
						if (w != null)
							weapons.Add(w.ToString());
					}
				}
			}
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"Error obteniendo OwnedWeapons: {ex.Message}");
		}

		// Crear casillas para cada arma
		foreach (var weapon in weapons)
		{
			var slot = CreateWeaponSlot(weapon);
			inventoryContainer.AddChild(slot);
		}
	}

	private PanelContainer CreateWeaponSlot(string weaponId)
	{
		var slot = new PanelContainer();
		slot.CustomMinimumSize = new Vector2(80, 80);
		
		var bg = new StyleBoxFlat();
		bg.BgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
		slot.AddThemeStyleboxOverride("panel", bg);

		var vbox = new VBoxContainer();
		vbox.Alignment = BoxContainer.AlignmentMode.Center;
		slot.AddChild(vbox);

		// Sprite de arma
		var texture = GetWeaponTexture(weaponId);
		if (texture != null)
		{
			var sprite = new TextureRect();
			sprite.Texture = texture;
			sprite.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
			sprite.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
			sprite.CustomMinimumSize = new Vector2(60, 60);
			sprite.MouseFilter = Control.MouseFilterEnum.Ignore;
			vbox.AddChild(sprite);
		}

		// Nombre del arma
		var label = new Label();
		label.Text = weaponId.Substring(0, 1).ToUpper() + weaponId.Substring(1);
		label.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.MouseFilter = Control.MouseFilterEnum.Ignore;
		vbox.AddChild(label);

		return slot;
	}

	private Texture2D GetWeaponTexture(string weaponId)
	{
		// Mapeo de weaponId a ruta de sprite
		var textureMap = new Dictionary<string, string>()
		{
			{ "gyro", "res://assets/weapons/GyroSaber1.png" },
			{ "colt", "res://assets/weapons/Colt.png" },
			{ "falocha", "res://assets/weapons/Falocha.png" },
			{ "navaja", "res://assets/weapons/navaja1.png" },
			{ "ricochet", "res://assets/weapons/Ricochet.png" },
			{ "baston", "res://assets/weapons/Baston.png" },
			{ "agua", "res://assets/weapons/Agua.png" },
			{ "puñetazo", "res://assets/weapons/PuñetazoSimple.png" },
			{ "sombrero", "res://assets/weapons/sombrero_magico.png" }
		};

		string texturePath = "";
		if (textureMap.TryGetValue(weaponId.ToLower(), out var path))
		{
			texturePath = path;
		}

		if (string.IsNullOrEmpty(texturePath))
			return null;

		try
		{
			return ResourceLoader.Load<Texture2D>(texturePath);
		}
		catch { return null; }
	}
}
