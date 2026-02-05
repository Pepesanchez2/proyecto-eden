using Godot;
using System.Collections.Generic;

public partial class button_select : Control
{
    // Lista de botones de arma
    private List<Button> armas = new List<Button>();
    private Button armaSeleccionada = null;

    public override void _Ready()
    {
        // Agrega los botones individuales manualmente
        Button btn1 = GetNode<Button>("Button_Arma1");
        Button btn2 = GetNode<Button>("Button_Arma2");
        Button btn3 = GetNode<Button>("Button_Arma3");

        armas.Add(btn1);
        armas.Add(btn2);
        armas.Add(btn3);

        // Conectar las señales
        foreach (Button b in armas)
        {
            b.Pressed += () => SeleccionarArma(b);
        }

        // Botón Jugar
        Button btnJugar = GetNode<Button>("Button_Jugar");
        btnJugar.Pressed += Jugar;
    }

    private void SeleccionarArma(Button btn)
    {
        // Deseleccionar todas
        foreach (Button b in armas)
        {
            b.Flat = false; // Cambiar apariencia
        }

        // Seleccionar esta arma
        armaSeleccionada = btn;
        btn.Flat = true; // Cambia apariencia para indicar selección
        GD.Print("Arma seleccionada: " + armaSeleccionada.Name);
    }

    private void Jugar()
    {
        if (armaSeleccionada == null)
        {
            GD.Print("¡Selecciona un arma primero!");
            return;
        }

        GD.Print("JUGANDO con arma: " + armaSeleccionada.Name);

        // Cambiar de escena
        GetTree().ChangeSceneToFile("res://scenes/mapa/cielo.tscn");

        // Para pasar info del arma, puedes usar un Autoload
    }
}
