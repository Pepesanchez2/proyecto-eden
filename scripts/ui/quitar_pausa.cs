using Godot;

public partial class quitar_pausa : Button
{
    public override void _Ready()
    {
        Pressed += () =>
        {
            var parent = GetParent<menu_pausa>();
            if (parent != null)
                parent.QuitarPausa();
                QueueFree();
        };
    }
}

