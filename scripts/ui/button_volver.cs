using Godot;
using System;

public partial class button_volver : Button
{
	// Called when the node enters the scene tree for the first time.
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
