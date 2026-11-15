using Godot;
using System;

public partial class Player : CharacterBody3D {
	double grav = 20.0;
	[Export]
	public int speed { get; set; } = 12;
	[Export]
	public int jumpSpeed { get; set; } = 8;
	[Export]
	public double sens { get; set; }= .002;
	
	private Node3D _head;
	private Camera _Camera;
	private CollisionShape3D _colShape;
	private CapsuleShape3D _capsule;
	private Vector3 _knockbackVel;
	
	private const float ArenaHalfSize = 19.5f;
	private const float ArenaMinY = -1f;
	private const float ArenaMaxY = 40.5f;
	
	public void ApplyKnockback(Vector3 knockback)
	{
		_knockbackVel += knockback;
	}
	
	public override void _PhysicsProcess(double delta) {
		var vel = Velocity;
		Vector3 knockback = _knockbackVel;
		_knockbackVel = Vector3.Zero;

		if (IsOnFloor() && vel.Y < 0f)
			vel.Y = 0f;

		vel.Y += (float) (-grav * delta);
		var moveDir = GetInputDirection();
		Vector3 moveVel = moveDir * speed;
		if (moveDir.LengthSquared() > 0.0001f)
		{
			vel.X = moveVel.X + knockback.X;
			vel.Z = moveVel.Z + knockback.Z;
		}
		else
		{
			vel.X = knockback.X;
			vel.Z = knockback.Z;
		}
		
		if (IsOnFloor() && Input.IsActionJustPressed("jump")) {
			vel.Y = jumpSpeed;
		}
		
		Velocity = vel;
		MoveAndSlide();

		var pos = GlobalPosition;
		if (pos.X < -ArenaHalfSize || pos.X > ArenaHalfSize)
		{
			pos.X = Mathf.Clamp(pos.X, -ArenaHalfSize, ArenaHalfSize);
			GlobalPosition = pos;
			Velocity = new Vector3(0f, Velocity.Y, Velocity.Z);
		}
		if (pos.Z < -ArenaHalfSize || pos.Z > ArenaHalfSize)
		{
			pos.Z = Mathf.Clamp(pos.Z, -ArenaHalfSize, ArenaHalfSize);
			GlobalPosition = pos;
			Velocity = new Vector3(Velocity.X, Velocity.Y, 0f);
		}
		if (pos.Y < ArenaMinY || pos.Y > ArenaMaxY)
		{
			pos.Y = Mathf.Clamp(pos.Y, ArenaMinY, ArenaMaxY);
			GlobalPosition = pos;
			Velocity = new Vector3(Velocity.X, 0f, Velocity.Z);
		}
		
		_Camera.UpdateFov(
			GetHorizontalSpeed(), 
			(float) delta
		);
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		_head = GetNode<Node3D>("Head");
		_Camera = _head.GetNode<Camera>(".");
		
		_colShape = GetNode<CollisionShape3D>("CollisionShape3D");
		_capsule = (CapsuleShape3D)_colShape.Shape.Duplicate();
		_colShape.Shape = _capsule;
		
		Input.MouseMode = Input.MouseModeEnum.Captured;	
	}
	
	public Vector3 GetInputDirection() {
		Vector2 input = Input.GetVector(
		"ui_left", 
		"ui_right", 
		"ui_up", 
		"ui_down"
		);
		Vector3 fwd = -_head.GlobalTransform.Basis.Z;
		fwd.Y = 0;
		Vector3 right = _head.GlobalTransform.Basis.X;
		right.Y = 0;
		return (right.Normalized() * input.X + fwd.Normalized() * -input.Y).Normalized();
	}

	public Vector3 GetCameraForward() {
		Vector3 fwd = -_head.GlobalTransform.Basis.Z;
		fwd.Y = 0;
		return fwd.Normalized();
	}

	public float GetHorizontalSpeed() => new Vector3(
		Velocity.X, 
		0, 
		Velocity.Z
	).Length();

	public void SetColliderHeight(float height, float posY) {
		_capsule.Height = height;
		_colShape.Position = new Vector3(0, posY, 0);
	}

	public void SetHeadHeight(float posY) {
		var pos = _head.Position;
		pos.Y = posY;
		_head.Position = pos;
	}
}
