using Godot;

public partial class Camera : Node3D
{
	[Export] public float Sensitivity { get; set; } = 0.004f;
	[Export] public float BaseFov { get; set; } = 90.0f;
	[Export] public float MaxSpeedFov { get; set; } = 120.0f;
	[Export] public float FovInterpSpeed { get; set; } = 8.0f;
	
	private Camera3D _camera;
	private CharacterBody3D _body;

	public override void _Ready() {
		_camera = GetNode<Camera3D>("Camera3D");
		_body = GetOwner<CharacterBody3D>();
		_camera.Fov = BaseFov;
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (
			@event is InputEventMouseMotion motion && 
			Input.MouseMode == Input.MouseModeEnum.Captured
		) {
			_body.RotateY(-motion.Relative.X * Sensitivity);
			RotateX(-motion.Relative.Y * Sensitivity);

			var rot = Rotation;
			rot.X = Mathf.Clamp(rot.X, Mathf.DegToRad(-89), Mathf.DegToRad(89));
			Rotation = rot;
		}
	}

	public void UpdateFov(float currentSpeed, float dt) {
		if (currentSpeed > 0) {
			float targetFov = Mathf.Lerp(BaseFov, MaxSpeedFov, currentSpeed / 50.0f);
			_camera.Fov = Mathf.Lerp(_camera.Fov, targetFov, FovInterpSpeed * dt);
		} else {
			_camera.Fov = Mathf.Lerp(_camera.Fov, BaseFov, FovInterpSpeed * dt);
		}
	}
}
