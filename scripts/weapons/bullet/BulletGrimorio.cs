using Godot;
using System;

public partial class BulletGrimorio : Area2D
{
    [Export]
    public int Damage = 1;

    [Export]
    public float Speed = 300f;

    [Export]
    public float Lifetime = 5.0f;

    private Vector2 velocity = Vector2.Zero;
    private float lifeTimer = 0f;
    private Node2D shooter = null;

    public override void _Ready()
    {
        AddToGroup("projectiles");
        lifeTimer = Lifetime;
        SetPhysicsProcess(true);
        try
        {
            Connect("body_entered", new Callable(this, nameof(OnBodyEntered)));
        }
        catch { }
    }

    public void Initialize(Vector2 direction, Node2D owner = null)
    {
        if (direction == Vector2.Zero)
            direction = new Vector2(1, 0);
        velocity = direction.Normalized() * Speed;
        shooter = owner;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        if (velocity != Vector2.Zero)
            GlobalPosition += velocity * dt;

        lifeTimer -= dt;
        if (lifeTimer <= 0f)
        {
            QueueFree();
            return;
        }
    }

    private void OnBodyEntered(Node body)
    {
        if (body == null)
            return;
        if (shooter != null && body == shooter)
            return;

        try
        {
            var meth = body.GetType().GetMethod("ApplyDamage");
            if (meth != null)
            {
                meth.Invoke(body, new object[] { Damage });
            }
        }
        catch { }

        QueueFree();
    }
}
