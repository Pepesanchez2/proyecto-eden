using Godot;

public partial class button_options : Button
{
    [Export] public string RutaEscena = "res://scenes/ui/menu_options.tscn";

    public override void _Ready()
    {
        Pressed += CambiarEscena;
    }

    private void CambiarEscena()
    {
        GetTree().ChangeSceneToFile(RutaEscena);
    }
}
