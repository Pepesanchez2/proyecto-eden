using Godot;
using System.Collections.Generic;

public partial class button_select2 : Button {

	public static button_select2 Instancia;

    public string armaSeleccionada;

    public override void _Ready()
    {
		Instancia = this;
        Pressed += Jugar;
    }

     public void SeleccionarArma(string idArma)
    {
        armaSeleccionada = idArma;
        GD.Print("Arma seleccionada: " + idArma);
    }

    public void Jugar()
    {
        if (armaSeleccionada == null)
        {
            GD.Print("Selecciona un arma primero");
            return;
        }

        GD.Print("Entrando al juego con: " + armaSeleccionada);

        GetTree().ChangeSceneToFile("res://scenes/levels/infierno.tscn");
    }
}
