using Godot;
using System;

public partial class button_volver2 : Button
{

[Export] public string RutaEscena = "res://scenes/ui/menu.tscn";

    public override void _Ready()
    {
        Pressed += CambiarEscena;
    }

    private void CambiarEscena()
    {
        GetTree().ChangeSceneToFile(RutaEscena);
    }
}
