extends Node3D

@export var sensitivity: float = 0.004
@export var base_fov: float = 90.0
@export var max_speed_fov: float = 120.0
@export var fov_interp_speed: float = 8.0

@onready var _camera: Camera3D = $Camera3D
@onready var _body: CharacterBody3D = get_owner()

func _ready() -> void:
	_camera.fov = base_fov

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		var motion := event as InputEventMouseMotion
		_body.rotate_y(-motion.relative.x * sensitivity)
		rotate_x(-motion.relative.y * sensitivity)

		var rot := rotation
		rot.x = clamp(rot.x, deg_to_rad(-89.0), deg_to_rad(89.0))
		rotation = rot

func update_fov(current_speed: float, dt: float) -> void:
	if current_speed > 0.0:
		var target_fov: float = lerp(base_fov, max_speed_fov, current_speed / 50.0)
		_camera.fov = lerp(_camera.fov, target_fov, fov_interp_speed * dt)
	else:
		_camera.fov = lerp(_camera.fov, base_fov, fov_interp_speed * dt)

