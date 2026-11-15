class_name Player

extends CharacterBody3D

var grav: float = 20.0

@export var speed: int = 12
@export var jump_speed: int = 8
@export var sens: float = 0.002

const ARENA_HALF_SIZE := 19.5
const ARENA_MIN_Y := -1.0
const ARENA_MAX_Y := 40.5

@onready var _head: Node3D = $Head
@onready var _camera: Camera3D = $Head/Camera3D
var _knockback_vel := Vector3.ZERO

func _ready() -> void:
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

func apply_knockback(knockback: Vector3) -> void:
	_knockback_vel += knockback

func _physics_process(delta: float) -> void:
	var vel := velocity
	var knockback := _knockback_vel
	_knockback_vel = Vector3.ZERO

	if is_on_floor() and vel.y < 0.0:
		vel.y = 0.0

	vel.y += -grav * delta
	var move_dir := get_input_direction()
	var move_vel := move_dir * speed
	if move_dir.length_squared() > 0.0001:
		vel.x = move_vel.x + knockback.x
		vel.z = move_vel.z + knockback.z
	else:
		vel.x = knockback.x
		vel.z = knockback.z

	if is_on_floor() and Input.is_action_just_pressed("jump"):
		vel.y = jump_speed

	velocity = vel
	move_and_slide()

	var pos := global_position
	if pos.x < -ARENA_HALF_SIZE or pos.x > ARENA_HALF_SIZE:
		pos.x = clamp(pos.x, -ARENA_HALF_SIZE, ARENA_HALF_SIZE)
		global_position = pos
		velocity = Vector3(0.0, velocity.y, velocity.z)
	if pos.z < -ARENA_HALF_SIZE or pos.z > ARENA_HALF_SIZE:
		pos.z = clamp(pos.z, -ARENA_HALF_SIZE, ARENA_HALF_SIZE)
		global_position = pos
		velocity = Vector3(velocity.x, velocity.y, 0.0)
	if pos.y < ARENA_MIN_Y or pos.y > ARENA_MAX_Y:
		pos.y = clamp(pos.y, ARENA_MIN_Y, ARENA_MAX_Y)
		global_position = pos
		velocity = Vector3(velocity.x, 0.0, velocity.z)

	if $Head.has_method("update_fov"):
		$Head.update_fov(get_horizontal_speed(), delta)

func get_input_direction() -> Vector3:
	var input := Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down")
	var fwd := -_head.global_transform.basis.z
	fwd.y = 0.0
	var right := _head.global_transform.basis.x
	right.y = 0.0
	return (right.normalized() * input.x + fwd.normalized() * -input.y).normalized()

func get_camera_forward() -> Vector3:
	var fwd := -_head.global_transform.basis.z
	fwd.y = 0.0
	return fwd.normalized()

func get_horizontal_speed() -> float:
	return Vector3(velocity.x, 0.0, velocity.z).length()
