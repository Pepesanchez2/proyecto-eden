using Godot;

public partial class quitar_pausa : Button
{
    public override void _Ready()
    {
        Pressed += () =>
        {
            var parent = GetParent<menu_pausa>();
<<<<<<< HEAD
            if (parent == null)
            parent.QuitarPausa();
=======
            if (parent != null)
                parent.QuitarPausa();
>>>>>>> fcc40aee539dd591add4fd26a57357e3ee6385ad
        };
    }
}

