using Godot;

public partial class button_cambio : Button
{

    public override void _Ready()
    {
        Pressed += Jugar;
    }

    public void Jugar()
    {
        GetTree().ChangeSceneToFile("res://scenes/levels/cielo.tscn");
    }
}
