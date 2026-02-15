using Godot;
using System;

public partial class InLevelUI : Control
{
    // =========================
    // Vida del jugador
    // =========================
    private AnimatedSprite2D healthSprite;
    private Player player;

    // =========================
    // Contador de oleadas
    // =========================
    private Timer waveTimer;
    private Label waveTimerLabel;
    private Label waveOleadaLabel;
    private int tiempoInicial = 60;
    private int timeLeft = 60;
    private int oleada = 1;

    // =========================
    // Experiencia
    // =========================
    private ProgressBar expBar;
    private Label expLabel;
    private int experienciaActual = 0;
    private int xpPorNivel = 40; // XP necesaria por nivel

    public override void _Ready()
    {
        // --- Vida ---
        healthSprite = GetNodeOrNull<AnimatedSprite2D>("health_bar");
        if (healthSprite == null)
            GD.PushWarning("No se encontró AnimatedSprite2D llamado 'health_bar'");
        CallDeferred(nameof(ConnectPlayer));

        // --- Oleadas / Timer ---
        waveTimer = GetNode<Timer>("WaveTimer");
        waveTimerLabel = GetNode<Label>("WaveTimerLabel");
        waveOleadaLabel = GetNode<Label>("WaveOleadaLabel");

        waveTimer.Timeout += OnWaveTimerTimeout;

        waveTimerLabel.Text = timeLeft.ToString();
        waveOleadaLabel.Text = "Oleada " + oleada;

        waveTimer.Start();

        // --- Experiencia ---
        expBar = GetNode<ProgressBar>("ExpBar");
        expLabel = GetNode<Label>("ExpLabel");

        expBar.MaxValue = xpPorNivel;
        expBar.Value = experienciaActual;
        ActualizarExpLabel();
    }

    // =========================
    // Conexión al jugador
    // =========================
    private void ConnectPlayer()
    {
        player = GetTree().GetFirstNodeInGroup("player") as Player;

        if (player == null)
        {
            CallDeferred(nameof(ConnectPlayer));
            return;
        }

        player.HealthChanged += OnPlayerHealthChanged;
        OnPlayerHealthChanged(player.Health, player.MaxHealth);
    }

    private void OnPlayerHealthChanged(int current, int max)
    {
        if (healthSprite == null) return;

        var frames = healthSprite.SpriteFrames;
        var anim = healthSprite.Animation;

        if (frames == null || !frames.HasAnimation(anim)) return;

        int frameCount = frames.GetFrameCount(anim);
        int frame = max - current;

        healthSprite.Frame = Mathf.Clamp(frame, 0, frameCount - 1);
    }

    // =========================
    // Timer de oleadas
    // =========================
    private void OnWaveTimerTimeout()
    {
        timeLeft--;

        if (timeLeft <= 0)
        {
            if (oleada >= 5)
            {
                GetTree().ChangeSceneToFile("res://scenes/ui/menu_cambio.tscn");
            }
            oleada++;
            waveOleadaLabel.Text = "Oleada " + oleada;
            timeLeft = tiempoInicial;
        }

        waveTimerLabel.Text = timeLeft.ToString();
    }

    // =========================
    // Experiencia
    // =========================
    public void AgregarExperiencia(int xp)
    {
        experienciaActual += xp;
		GD.Print("Experiencia sumada");

        // Subir nivel si llega al máximo
        if (experienciaActual >= xpPorNivel)
        {
            experienciaActual -= xpPorNivel;
            GD.Print("¡LEVEL UP!");
            // Aquí podrías disparar animación o señal
        }

        // Actualizar barra y label
        expBar.Value = experienciaActual;
        ActualizarExpLabel();
    }

    private void ActualizarExpLabel()
    {
        if (expLabel != null)
            expLabel.Text = "XP: " + experienciaActual + "/" + xpPorNivel;
    }
}
