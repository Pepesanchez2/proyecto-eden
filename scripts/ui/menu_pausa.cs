using Godot;

public partial class menu_pausa : CanvasLayer
{
    private Control pauseMenu;

    public override void _Ready()
    {
        pauseMenu = GetNode<Control>("PauseMenu");
        pauseMenu.Visible = false;
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("pausa"))
        {
            if (!pauseMenu.Visible)
                Pausar();
            else
                QuitarPausa();
        }
    }

    private void Pausar()
    {
        GetTree().Paused = true;
        pauseMenu.Visible = true;  
    }

    public void QuitarPausa()
    {
        GetTree().Paused = false;
        pauseMenu.Visible = false;
    }
}
