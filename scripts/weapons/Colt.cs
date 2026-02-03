using Godot;
using System;
using System.Reflection;

public partial class Colt : Node2D
{
    [Export]
    public PackedScene BulletScene;

    [Export]
    public float FireInterval = 2.0f; // cada cuanto dispara

    [Export]
    public int BulletDamage = 15;

    [Export]
    public int UpgradeLevel = 0;

    [Export]
    public NodePath OwnerPath;

    private float fireTimer = 0f;
    private Node2D ownerNode;

    public override void _Ready()
    {
        fireTimer = FireInterval;
        // intentar cargar escena por defecto si no se configuró
        if (BulletScene == null)
        {
            var ps = ResourceLoader.Load<PackedScene>("res://scenes/weapons/bullet_colt.tscn");
            if (ps != null)
                BulletScene = ps;
        }

        if (OwnerPath != null && OwnerPath != "")
            ownerNode = GetNodeOrNull<Node2D>(OwnerPath);

        if (ownerNode == null)
            ownerNode = GetTree().GetFirstNodeInGroup("player") as Node2D;
    }

    public override void _Process(double delta)
    {
        fireTimer -= (float)delta;
        if (fireTimer <= 0f)
        {
            fireTimer = FireInterval;
            FireOnce();
        }
    }

    private void FireOnce()
    {
        if (BulletScene == null)
            return;

        if (ownerNode == null)
        {
            ownerNode = GetTree().GetFirstNodeInGroup("player") as Node2D;
            if (ownerNode == null)
                return;
        }

        // intentar obtener la dirección 'facing' del jugador por reflexión
        Vector2 dir = new Vector2(1, 0);
        try
        {
            var field = ownerNode.GetType().GetField("facing", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
            {
                var val = field.GetValue(ownerNode);
                if (val is Vector2)
                    dir = (Vector2)val;
            }
        }
        catch { }

        var inst = BulletScene.Instantiate();
        if (inst == null)
            return;

        var node = inst as Node2D;
        if (node == null)
            return;

        node.GlobalPosition = ownerNode.GlobalPosition;

        // Añadir al mismo padre que el jugador para mantener orden
        var parent = ownerNode.GetParent();
        if (parent != null)
            parent.AddChild(node);
        else
            GetTree().Root.AddChild(node);

        // Inicializar dirección y shooter si el bullet implementa Initialize
        try
        {
            if (node.HasMethod("Initialize"))
            {
                node.Call("Initialize", dir, ownerNode);
            }
        }
        catch { }

        // intentar establecer daño del proyectil si tiene la propiedad
        try
        {
            var prop = node.GetType().GetProperty("Damage");
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(node, BulletDamage);
            }
            else
            {
                // intentar vía campo
                var field = node.GetType().GetField("Damage");
                if (field != null)
                    field.SetValue(node, BulletDamage);
            }
        }
        catch { }
    }

    // Aplicar mejora (subir nivel): ajusta la cadencia y el daño
    public void ApplyUpgrade()
    {
        UpgradeLevel += 1;
        // ejemplo: cada nivel reduce FireInterval un 12% y aumenta daño en +5
        FireInterval = Math.Max(0.2f, FireInterval * 0.88f);
        BulletDamage += 5;
    }
}
