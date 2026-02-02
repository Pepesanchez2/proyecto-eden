using Godot;
using System;
using System.Collections.Generic;

public partial class Spawner : Node2D
{
	// Lista de escenas de enemigos (defínelas en el inspector)
	[Export]
	public PackedScene[] EnemyScenes = new PackedScene[0];

	// Intervalo entre spawns en segundos
	[Export]
	public float SpawnInterval = 3.0f;

	// Máximo de enemigos activos simultáneamente
	[Export]
	public int MaxEnemies = 10;

	// Opcional: usar oleadas
	[Export]
	public bool UseWaves = true;

	[Export]
	public float WaveDuration = 20.0f;

	[Export]
	public float TimeBetweenWaves = 8.0f;

	private int currentWave = 0;

	private float waveTimer = 0f;

	private bool inWave = false;

	// Distancias mínima y máxima desde el jugador donde aparecerán los enemigos
	[Export]
	public float MinDistance = 800.0f;

	[Export]
	public float MaxDistance = 1200.0f;

	// Opcional: path al nodo jugador
	[Export]
	public NodePath PlayerPath;

	// Opcional: nodo padre donde añadir los enemigos (si no, se añadirá como hijo del Spawner)
	[Export]
	public NodePath EnemiesParentPath;

	private float timer = 0f;
	private Node2D player;
	private Node enemiesParent;

	public override void _Ready()
	{
		GD.Randomize();

		// Intentar por PlayerPath
		if (PlayerPath != null && PlayerPath != "")
			player = GetNodeOrNull<Node2D>(PlayerPath);

		// Si no está, buscar por grupo 'player'
		if (player == null)
			player = GetTree().GetFirstNodeInGroup("player") as Node2D;

		// Si sigue sin encontrarse, buscar por nombre 'Pepin' en la escena
		if (player == null)
		{
			var root = GetTree().Root as Node;
			var found = FindNodeRecursive(root, "Pepin");
			if (found != null)
				player = found as Node2D;
		}

		// Nodo donde añadiremos los enemigos
		if (EnemiesParentPath != null && EnemiesParentPath != "")
			enemiesParent = GetNodeOrNull(EnemiesParentPath);

		if (enemiesParent == null)
			enemiesParent = GetParent();

		// Inicializar sistema de oleadas
		if (UseWaves)
		{
			currentWave = 1;
			inWave = true;
			waveTimer = WaveDuration;
		}

		// Si no hay enemigos configurados en el inspector, intentar cargar todas las escenas
		// dentro de res://scenes/characters/enemy/ y usarlas como opciones por defecto.
		if (EnemyScenes == null || EnemyScenes.Length == 0)
		{
			var loaded = new List<PackedScene>();
			var dir = DirAccess.Open("res://scenes/characters/enemy");
			if (dir != null)
			{
				dir.ListDirBegin();
				string file = dir.GetNext();
				while (!string.IsNullOrEmpty(file))
				{
					// ignorar '.' y '..'
					if (file == "." || file == "..")
					{
						file = dir.GetNext();
						continue;
					}

					if (!dir.CurrentIsDir() && file.EndsWith(".tscn"))
					{
						var path = $"res://scenes/characters/enemy/{file}";
						var ps = ResourceLoader.Load<PackedScene>(path);
						if (ps != null)
							loaded.Add(ps);
					}
					file = dir.GetNext();
				}
				dir.ListDirEnd();
			}
			
			if (loaded.Count > 0)
			{
				EnemyScenes = loaded.ToArray();
				GD.Print($"Spawner: cargadas {EnemyScenes.Length} escenas desde res://scenes/characters/enemy/");
			}
			else
			{
				// Fallback: intentar cargar Angel en caso de que no haya archivos en la carpeta
				var angel = ResourceLoader.Load<PackedScene>("res://scenes/enemies/Angel.tscn");
				if (angel != null)
				{
					EnemyScenes = new PackedScene[] { angel };
					GD.Print("Spawner: Angel.tscn añadido automáticamente a EnemyScenes (fallback)." );
				}
				else
				{
					GD.PrintErr("Spawner: no se pudieron encontrar escenas en res://scenes/characters/enemy/ ni el fallback Angel.tscn");
				}
			}
		}
	}

	public override void _Process(double delta)
	{
		if (player == null)
			return;

		// manejar temporizador de oleadas
		if (UseWaves)
		{
			waveTimer -= (float)delta;
			if (waveTimer <= 0f)
			{
				if (inWave)
				{
					inWave = false;
					waveTimer = TimeBetweenWaves;
				}
				else
				{
					currentWave += 1;
					inWave = true;
					waveTimer = WaveDuration;
				}
			}
		}

		timer += (float)delta;
		if (timer < SpawnInterval)
			return;

		timer = 0f;

		// Contar enemigos activos por grupo 'enemies'
		var current = GetTree().GetNodesInGroup("enemies").Count;
		if (current >= MaxEnemies)
			return;

		// si usamos oleadas, solo spawnear durante la oleada
		if (UseWaves && !inWave)
			return;

		SpawnEnemy();
	}

	// Propiedades públicas para la UI
	public int CurrentWave => currentWave;
	public float WaveTimeLeft => waveTimer;
	public bool IsInWave => inWave;

	private void SpawnEnemy()
	{
		if (EnemyScenes == null || EnemyScenes.Length == 0)
			return;

		int idx = (int)(GD.Randf() * EnemyScenes.Length);
		if (idx < 0) idx = 0;
		if (idx >= EnemyScenes.Length) idx = EnemyScenes.Length - 1;

		var scene = EnemyScenes[idx];
		if (scene == null)
			return;

		var inst = scene.Instantiate<Node2D>();

		// Generar posición aleatoria fuera de la zona cercana al jugador
	float angle = GD.Randf() * (float)(Math.PI * 2);
		float dist = Mathf.Lerp(MinDistance, MaxDistance, GD.Randf());
		Vector2 spawnPos = player.GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

		if (enemiesParent != null)
			enemiesParent.AddChild(inst);
		else
			AddChild(inst);

		inst.GlobalPosition = spawnPos;
	}

	// Helper: buscar recursivamente un nodo por nombre
	private Node FindNodeRecursive(Node parent, string name)
	{
		if (parent == null)
			return null;

		if (parent.Name == name)
			return parent;

		foreach (var item in parent.GetChildren())
		{
			var child = item as Node;
			if (child == null)
				continue;
			var found = FindNodeRecursive(child, name);
			if (found != null)
				return found;
		}

		return null;
	}
}
