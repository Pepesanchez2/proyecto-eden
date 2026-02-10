using Godot;

public partial class ui_baston : Button
{
    [Export] public string IdArma;

    public override void _Ready()
    {
        Pressed += AlPulsar;
    }

    private void AlPulsar()
    {
        button_select.Instancia.SeleccionarArma(IdArma);
    }
}
