using Godot;
using System.Collections.Generic;

public partial class Dodgeball : CharacterBody3D
{
	[Export] public float Gravity = 0.0f;
	[Export] public float FlightAcceleration = 1.0f;
	[Export] public float AccelerationPerHit = 0.25f;
	[Export] public float WallBounceMultiplier = 1f;
	[Export] public float MaxSpeed = 250.0f;
	[Export] public float FloorBounciness = 1.0f;
	[Export] public float GroundFriction = 0.0f;
	[Export] public float MinBounceSpeed = 1.2f;
	[Export] public float TrailMinSpeed = 1f;
	[Export] public float TrailSpacing = 0.25f;
	[Export] public int TrailMaxPoints = 250;
	[Export] public float PlayerBiasStrength = 0.3f;
	[Export] public float PlayerBiasPerBounce = 0.12f;
	[Export] public float PlayerBiasMax = 20f;
	[Export] public float HitKnockbackStrength = 30f;
	[Export] public float ArenaHalfSize = 20f;
	[Export] public float ArenaMinY = -1f;
	[Export] public float ArenaMaxY = 41f;

	public bool IsLive { get; private set; }
	private const int MaxBouncesPerFrame = 3;
	private float _accelRamp = 1.0f;
	private MeshInstance3D _trail;
	private readonly List<MeshInstance3D> _trailGhosts = new();
	private readonly List<Vector3> _trailPositions = new();
	private Node3D _player;
	private float _playerBias;
	private MeshInstance3D _ballMesh;
	private MeshInstance3D _ballVisual;

	public override void _Ready()
	{
		_player = GetTree().Root.GetNodeOrNull<Node3D>("Scene/Player");
		_ballMesh = GetNodeOrNull<MeshInstance3D>("DodgeballMesh");
		if (_ballMesh != null)
		{
			_ballMesh.Visible = false;
			_ballMesh.QueueFree();
		}

		_ballVisual = new MeshInstance3D();
		var ballMesh = new SphereMesh();
		ballMesh.Radius = 0.5f;
		ballMesh.Height = 1.0f;
		_ballVisual.Mesh = ballMesh;
		_ballVisual.TopLevel = true;
		var ballMat = new StandardMaterial3D();
		ballMat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		ballMat.AlbedoColor = new Color(1f, 0.45f, 0f, 1f);
		ballMat.EmissionEnabled = true;
		ballMat.Emission = new Color(1f, 0.45f, 0f);
		_ballVisual.MaterialOverride = ballMat;
		_ballVisual.Visible = true;
		AddChild(_ballVisual);
		_ballVisual.GlobalPosition = GlobalPosition;

		_trail = GetNodeOrNull<MeshInstance3D>("Trail");
		if (_trail != null)
		{
			_trail.Visible = false;
			_trail.Mesh = null;
			_trail.QueueFree();
		}

		_trail = new MeshInstance3D();
		_trail.Name = "Trail";
		_trail.TopLevel = true;
		_trail.Visible = true;
		_trail.GlobalPosition = GlobalPosition;
		AddChild(_trail);
		for (int i = 0; i < TrailMaxPoints; i++)
		{
			var ghost = new MeshInstance3D();
			var mesh = new SphereMesh();
			mesh.Radius = 0.5f;
			mesh.Height = 1.0f;
			ghost.Mesh = mesh;
			ghost.TopLevel = true;
			var mat = new StandardMaterial3D();
			mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
			mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
			mat.AlbedoColor = new Color(1f, 0.45f, 0f, 0f);
			mat.EmissionEnabled = true;
			mat.Emission = new Color(1f, 0.45f, 0f);
			ghost.MaterialOverride = mat;
			ghost.Visible = false;
			_trail.AddChild(ghost);
			_trailGhosts.Add(ghost);
		}

		Throw(new Vector3(30, 30, 10), null);
	}

	private void UpdateTrail()
	{
		if (_trail == null)
			return;

		float speed = Velocity.Length();
		if (speed < TrailMinSpeed)
		{
			foreach (var ghost in _trailGhosts)
				ghost.Visible = false;
			return;
		}

		Vector3 current = GlobalPosition;
		if (_trailPositions.Count == 0)
		{
			_trailPositions.Insert(0, current);
		}
		else
		{
			Vector3 last = _trailPositions[0];
			Vector3 delta = current - last;
			float dist = delta.Length();
			if (dist > TrailSpacing)
			{
				int steps = Mathf.Max(1, Mathf.CeilToInt(dist / TrailSpacing));
				for (int s = 1; s <= steps; s++)
				{
					Vector3 point = last + delta * ((float)s / steps);
					_trailPositions.Insert(0, point);
					if (_trailPositions.Count > TrailMaxPoints)
						_trailPositions.RemoveAt(TrailMaxPoints);
				}
			}
		}

		for (int i = 0; i < _trailGhosts.Count; i++)
		{
			if (i < _trailPositions.Count)
			{
				var ghost = _trailGhosts[i];
				ghost.GlobalPosition = _trailPositions[i];
				ghost.Visible = true;

				float progress = (float)i / Mathf.Max(1f, _trailGhosts.Count - 1);
				float scale = Mathf.Lerp(1f, 0.1f, progress);
				ghost.Scale = new Vector3(scale, scale, scale);

				var mat = (StandardMaterial3D)ghost.MaterialOverride;
				mat.AlbedoColor = new Color(1f, 0.45f, 0f, Mathf.Lerp(0.95f, 0.08f, progress));
				mat.Emission = new Color(1f, 0.45f, 0f);
			}
			else
			{
				_trailGhosts[i].Visible = false;
			}
		}
	}

	public void Throw(Vector3 initialVelocity, Node thrower)
	{
		_accelRamp = 1.0f;
		_playerBias = 0f;
		Velocity = initialVelocity;
		IsLive = true;
		_trailPositions.Clear();

		if (thrower != null)
		{
			AddCollisionExceptionWith(thrower);
			GetTree().CreateTimer(0.15).Timeout += () =>
			{
				if (IsInstanceValid(this) && IsInstanceValid(thrower))
					RemoveCollisionExceptionWith(thrower);
			};
		}
	}

	private Vector3 ApplyPlayerBiasOnBounce(Vector3 velocity)
	{
		if (_player == null)
			return velocity;

		Vector3 toPlayer = _player.GlobalPosition - GlobalPosition;
		if (toPlayer.LengthSquared() < 0.0001f)
			return velocity;

		Vector3 desired = toPlayer.Normalized();
		float biasStrength = Mathf.Min(PlayerBiasStrength + _playerBias, PlayerBiasMax);
		Vector3 current = velocity.Normalized();
		Vector3 blended = current.Lerp(desired, biasStrength * 0.1f).Normalized();
		return blended * Mathf.Max(velocity.Length(), 1f);
	}

	private void ApplyPlayerHitKnockback(KinematicCollision3D collision)
	{
		if (collision.GetCollider() is not Player player)
			return;

		Vector3 hitDir = Velocity;
		hitDir.Y = 0f;
		if (hitDir.LengthSquared() < 0.0001f)
		{
			hitDir = player.GlobalPosition - GlobalPosition;
			hitDir.Y = 0f;
		}
		if (hitDir.LengthSquared() < 0.0001f)
		{
			hitDir = -collision.GetNormal();
			hitDir.Y = 0f;
		}
		if (hitDir.LengthSquared() < 0.0001f)
			hitDir = new Vector3(1f, 0f, 0f);

		Vector3 push = hitDir.Normalized();
		push = new Vector3(push.X, 0.3f, push.Z).Normalized();
		float knockbackMagnitude = Mathf.Max(Velocity.Length(), HitKnockbackStrength);
		player.ApplyKnockback(push * knockbackMagnitude);
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		UpdateTrail();
		_ballVisual.GlobalPosition = GlobalPosition;
		_ballVisual.Visible = true;

		if (IsLive && Velocity.LengthSquared() > 0.001f)
		{
			Vector3 dir = Velocity.Normalized();
			float accel = FlightAcceleration * _accelRamp;
			float newSpeed = Mathf.Min(Velocity.Length() + accel * dt, MaxSpeed);
			Velocity = dir * newSpeed;
		}

		Velocity += Vector3.Down * Gravity * dt;

		Vector3 motion = Velocity * dt;
		for (int i = 0; i < MaxBouncesPerFrame; i++)
		{
			KinematicCollision3D collision = MoveAndCollide(motion);
			if (collision == null)
				break;

			Vector3 normal = collision.GetNormal();
			bool hitFloor = normal.Y > 0.7f;

			if (collision.GetCollider() is Player)
				ApplyPlayerHitKnockback(collision);

			if (IsLive)
				_accelRamp *= 1f + AccelerationPerHit;

			if (hitFloor)
			{
				_playerBias = Mathf.Min(_playerBias + PlayerBiasPerBounce, PlayerBiasMax);
				Velocity = Velocity.Bounce(normal) * FloorBounciness;
				Velocity = ApplyPlayerBiasOnBounce(Velocity);
				motion = collision.GetRemainder().Bounce(normal) * FloorBounciness;

				if (Mathf.Abs(Velocity.Y) < MinBounceSpeed)
				{
					Velocity = new Vector3(
						Velocity.X * GroundFriction,
						0f,
						Velocity.Z * GroundFriction
					);
					IsLive = false;
					break;
				}
			}
			else
			{
				_playerBias = Mathf.Min(_playerBias + PlayerBiasPerBounce, PlayerBiasMax);
				float mult = IsLive ? WallBounceMultiplier : 0.85f;
				Vector3 bounced = Velocity.Bounce(normal) * mult;
				bounced = ApplyPlayerBiasOnBounce(bounced);
				if (bounced.Length() > MaxSpeed)
					bounced = bounced.Normalized() * MaxSpeed;
				Velocity = bounced;
				motion = collision.GetRemainder().Bounce(normal) * mult;
			}
		}

		if (IsLive)
		{
			var pos = GlobalPosition;
			if (pos.X <= -ArenaHalfSize || pos.X >= ArenaHalfSize)
			{
				Velocity = new Vector3(-Velocity.X, Velocity.Y, Velocity.Z);
				pos.X = Mathf.Clamp(pos.X, -ArenaHalfSize, ArenaHalfSize);
				GlobalPosition = pos;
			}

			if (pos.Z <= -ArenaHalfSize || pos.Z >= ArenaHalfSize)
			{
				Velocity = new Vector3(Velocity.X, Velocity.Y, -Velocity.Z);
				pos.Z = Mathf.Clamp(pos.Z, -ArenaHalfSize, ArenaHalfSize);
				GlobalPosition = pos;
			}

			if (pos.Y <= ArenaMinY || pos.Y >= ArenaMaxY)
			{
				Velocity = new Vector3(Velocity.X, -Velocity.Y, Velocity.Z);
				pos.Y = Mathf.Clamp(pos.Y, ArenaMinY, ArenaMaxY);
				GlobalPosition = pos;
			}
		}

		_ballVisual.GlobalPosition = GlobalPosition;
	}
}
