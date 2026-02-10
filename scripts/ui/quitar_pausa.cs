using Godot;

public partial class quitar_pausa : Button
{
    public override void _Ready()
    {
        Pressed += () =>
        {
            GetParent<Control>().GetParent<menu_pausa>().QuitarPausa();
        };
    }
}
