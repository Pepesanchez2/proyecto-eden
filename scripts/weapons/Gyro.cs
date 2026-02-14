using Godot;
using System;
using System.Reflection;

public partial class Gyro : Node2D
{
    [Export]
    public PackedScene BulletScene;

    [Export]
    public float FireInterval = 1.0f;

    [Export]
    public int BulletDamage = 20;

    [Export]
    public NodePath OwnerPath;

    private float fireTimer = 0f;
    private Node2D ownerNode;

    public override void _Ready()
    {
        fireTimer = FireInterval;
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
        if (ownerNode == null)
        {
            ownerNode = GetTree().GetFirstNodeInGroup("player") as Node2D;
            if (ownerNode == null)
                return;
        }

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

        Node2D node = null;
        try
        {
            if (BulletScene != null)
            {
                var inst = BulletScene.Instantiate();
                node = inst as Node2D;
            }
        }
        catch { }

        if (node == null)
        {
            try
            {
                var b = new BulletGyro();
                node = b as Node2D;
            }
            catch { }
        }

        if (node == null) return;

        node.GlobalPosition = ownerNode.GlobalPosition;

        var parent = ownerNode.GetParent();
        if (parent != null)
            parent.AddChild(node);
        else
            GetTree().Root.AddChild(node);

        try { if (node.HasMethod("Initialize")) node.Call("Initialize", dir, ownerNode); } catch { }

        try
        {
            var prop = node.GetType().GetProperty("Damage");
            if (prop != null && prop.CanWrite) prop.SetValue(node, BulletDamage);
            else
            {
                var field = node.GetType().GetField("Damage");
                if (field != null) field.SetValue(node, BulletDamage);
            }
        }
        catch { }
    }

    public void ApplyUpgrade()
    {
        FireInterval = Math.Max(0.1f, FireInterval * 0.9f);
        BulletDamage += 5;
    }
}
