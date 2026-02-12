using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class LevelUpUI : Panel
{
    private Label titleLabel;
    private VBoxContainer optionsContainer;
    private List<string> choices = new List<string>();

    // List of all available weapons by ID
    private readonly List<string> AVAILABLE_WEAPONS = new List<string>()
    {
        "gyro",
        "colt",
        "falocha",
        "navaja",
        "ricochet",
        "baston",
        "agua",
        "puñetazo",
        "sombrero"
    };

    // Max number of weapons the player can have
    private const int MAX_WEAPONS = 5;

    // Mapeo de weaponId a ruta de sprite
    private readonly Dictionary<string, string> WEAPON_TEXTURES = new Dictionary<string, string>()
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

    public override void _Ready()
    {
        titleLabel = GetNodeOrNull<Label>("UI_LevelUpPanel_VBox#Title");
        optionsContainer = GetNodeOrNull<VBoxContainer>("UI_LevelUpPanel#VBox");

        // asegurar que el panel esté oculto al inicio
        Visible = false;
    }

    public void ShowLevelUp()
    {
        // no mostrar el panel si estamos en una escena de menú o pausa
        var cs = GetTree().CurrentScene;
        string sceneName = cs != null ? cs.Name.ToString().ToLowerInvariant() : "";
        if (sceneName.Contains("menu") || sceneName.Contains("pause") || sceneName.Contains("main"))
        {
            GD.Print("LevelUpUI: escena de menú detectada, no se mostrará la pantalla de mejora.");
            GetTree().Paused = false;
            return;
        }

        // encontrar jugador y sus armas
        var player = GetTree().GetFirstNodeInGroup("player") as Node;
        List<string> ownedWeapons = new List<string>();
        
        try
        {
            if (player != null)
            {
                var field = player.GetType().GetField("OwnedWeapons");
                if (field != null)
                {
                    var val = field.GetValue(player);
                    if (val is System.Collections.IEnumerable)
                    {
                        foreach (var it in (System.Collections.IEnumerable)val)
                        {
                            if (it != null)
                                ownedWeapons.Add(it.ToString().ToLower());
                        }
                    }
                }
            }
        }
        catch { }

        // Check if player already has max weapons
        if (ownedWeapons.Count >= MAX_WEAPONS)
        {
            GD.Print("LevelUpUI: jugador ya tiene máximo de armas, no se mostrará selección.");
            GetTree().Paused = false;
            return;
        }

        // Get available weapons (those not owned)
        List<string> availableWeapons = new List<string>();
        foreach (var weapon in AVAILABLE_WEAPONS)
        {
            if (!ownedWeapons.Contains(weapon.ToLower()))
            {
                availableWeapons.Add(weapon);
            }
        }

        // If not enough weapons available, show what we can
        if (availableWeapons.Count == 0)
        {
            GD.Print("LevelUpUI: no hay armas disponibles para elegir.");
            GetTree().Paused = false;
            return;
        }

        // Select 2 random weapons (or less if not enough available)
        choices.Clear();
        Random rnd = new Random();
        int selectCount = Math.Min(2, availableWeapons.Count);
        
        // Shuffle available weapons and take the first 'selectCount'
        var shuffled = availableWeapons.OrderBy(x => rnd.Next()).ToList();
        for (int i = 0; i < selectCount; i++)
        {
            choices.Add($"New:{shuffled[i]}");
        }

        // Crear botones visuales con sprites
        CreateVisualOptions();

        if (titleLabel != null)
        {
            titleLabel.Text = "Elige un arma";
        }

        Visible = true;
        GetTree().Paused = true;
    }

    private void CreateVisualOptions()
    {
        // Limpiar opciones anteriores
        if (optionsContainer != null)
        {
            foreach (var child in optionsContainer.GetChildren())
            {
                if (child is Node n && n.Name != "Title")
                    n.QueueFree();
            }
        }

        // Crear botones visuales para cada opción
        for (int i = 0; i < choices.Count; i++)
        {
            var button = CreateWeaponButton(choices[i], i);
            if (optionsContainer != null)
                optionsContainer.AddChild(button);
        }
    }

    private Button CreateWeaponButton(string weaponChoice, int index)
    {
        var button = new Button();
        button.CustomMinimumSize = new Vector2(300, 100);
        button.Alignment = HorizontalAlignment.Left;
        
        // Crear contenedor horizontal para imagen y texto
        var hbox = new HBoxContainer();
        hbox.MouseFilter = Control.MouseFilterEnum.Ignore;
        button.AddChild(hbox);

        // Obtener nombre del arma
        string weaponId = weaponChoice.StartsWith("New:") 
            ? weaponChoice.Substring(4) 
            : weaponChoice;

        // Sprite del arma
        var texture = GetWeaponTexture(weaponId);
        if (texture != null)
        {
            var sprite = new TextureRect();
            sprite.Texture = texture;
            sprite.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            sprite.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
            sprite.CustomMinimumSize = new Vector2(80, 80);
            sprite.MouseFilter = Control.MouseFilterEnum.Ignore;
            hbox.AddChild(sprite);
        }

        // Separador
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(20, 0);
        spacer.MouseFilter = Control.MouseFilterEnum.Ignore;
        hbox.AddChild(spacer);

        // Nombre del arma
        var label = new Label();
        label.Text = $"Nueva arma:\n{char.ToUpper(weaponId[0])}{weaponId.Substring(1)}";
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.SizeFlagsVertical = Control.SizeFlags.ExpandFill | Control.SizeFlags.ShrinkCenter;
        hbox.AddChild(label);

        // Evento de presión
        int optionIndex = index;
        button.Pressed += () => OnOptionPressed(optionIndex);

        return button;
    }

    private Texture2D GetWeaponTexture(string weaponId)
    {
        string texturePath = "";
        if (WEAPON_TEXTURES.TryGetValue(weaponId.ToLower(), out var path))
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

    private void OnOptionPressed(int idx)
    {
        if (idx < 0 || idx >= choices.Count)
            return;
        
        var choice = choices[idx];
        
        // notificar al jugador
        var player = GetTree().GetFirstNodeInGroup("player") as Node;
        if (player != null)
        {
            try
            {
                var meth = player.GetType().GetMethod("OnLevelUpChoice");
                if (meth != null)
                    meth.Invoke(player, new object[] { choice });
            }
            catch { }
        }

        // Hide panel and resume game
        Visible = false;
        GetTree().Paused = false;
    }
}
