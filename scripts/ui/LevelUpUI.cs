using Godot;
using System;
using System.Collections.Generic;

public partial class LevelUpUI : Panel
{
    private Label titleLabel;
    private Button opt1;
    private Button opt2;
    private Button opt3;

    private List<string> choices = new List<string>();

    public override void _Ready()
    {
        titleLabel = GetNodeOrNull<Label>("VBox/Title");
        opt1 = GetNodeOrNull<Button>("VBox/Option1");
        opt2 = GetNodeOrNull<Button>("VBox/Option2");
        opt3 = GetNodeOrNull<Button>("VBox/Option3");

        if (opt1 != null) opt1.Pressed += () => OnOptionPressed(0);
        if (opt2 != null) opt2.Pressed += () => OnOptionPressed(1);
        if (opt3 != null) opt3.Pressed += () => OnOptionPressed(2);

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
            return;
        }
        // encontrar jugador y sus armas
        var player = GetTree().GetFirstNodeInGroup("player") as Node;
        List<string> owned = new List<string>();
        try
        {
            if (player != null)
            {
                var prop = player.GetType().GetProperty("OwnedWeapons");
                if (prop != null)
                {
                    var val = prop.GetValue(player);
                    if (val is System.Collections.IEnumerable)
                    {
                        foreach (var it in (System.Collections.IEnumerable)val)
                        {
                            if (it != null)
                                owned.Add(it.ToString());
                        }
                    }
                }
            }
        }
        catch { }

        // construir 3 opciones: hasta 2 upgrades de armas existentes y al menos 1 arma nueva
        choices.Clear();

        var pool = new List<string>() { "Colt", "Shotgun", "Laser" };

        // añadir upgrades (hasta 2)
        int added = 0;
        foreach (var w in owned)
        {
            if (added >= 2) break;
            choices.Add($"Upgrade:{w}");
            added++;
        }

        // rellenar con nuevas armas evitando duplicados
        var rnd = new Random();
        while (choices.Count < 3)
        {
            var candidate = pool[rnd.Next(pool.Count)];
            if (owned.Contains(candidate))
                continue;
            var entry = $"New:{candidate}";
            if (!choices.Contains(entry))
                choices.Add(entry);
        }

        // asignar texto a botones
        if (opt1 != null) opt1.Text = OptionToText(choices[0]);
        if (opt2 != null) opt2.Text = OptionToText(choices[1]);
        if (opt3 != null) opt3.Text = OptionToText(choices[2]);

        Visible = true;
        GetTree().Paused = true;
    }

    private string OptionToText(string opt)
    {
        if (opt.StartsWith("Upgrade:"))
            return $"Mejorar {opt.Substring(8)}";
        if (opt.StartsWith("New:"))
            return $"Nueva arma: {opt.Substring(4)}";
        return opt;
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

        Visible = false;
        GetTree().Paused = false;
    }
}
