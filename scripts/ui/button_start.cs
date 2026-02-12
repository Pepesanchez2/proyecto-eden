using Godot;

public partial class button_start : Button
{
    [Export] public string RutaEscena = "res://scenes/ui/menu_armas.tscn";

    public override void _Ready()
    {
        Pressed += CambiarEscena;
		Pressed += () => GD.Print("CLICK DETECTADO");
    }

    private void CambiarEscena()
    {
        GetTree().ChangeSceneToFile(RutaEscena);
    }
}
