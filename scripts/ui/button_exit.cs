using Godot;
using System;

public partial class button_exit : Button
{
 public override void _Ready()
    {
        Pressed += SalirDelJuego;
		Pressed += () => GD.Print("CLICK DETECTADO");
    }

    private void SalirDelJuego()
    {
        GetTree().Quit();
    }
}