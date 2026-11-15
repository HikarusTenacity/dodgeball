extends CharacterBody3D

@export var gravity: float = 0.0
@export var flight_acceleration: float = 1.0
@export var acceleration_per_hit: float = 0.25
@export var wall_bounce_multiplier: float = 1.0
@export var max_speed: float = 250.0
@export var floor_bounciness: float = 1.0
@export var ground_friction: float = 0.0
@export var min_bounce_speed: float = 1.2
@export var trail_min_speed: float = 1.0
@export var trail_spacing: float = 0.25
@export var trail_max_points: int = 250
@export var player_bias_strength: float = 0.3
@export var player_bias_per_bounce: float = 0.12
@export var player_bias_max: float = 20.0
@export var hit_knockback_strength: float = 30.0
@export var arena_half_size: float = 20.0
@export var arena_min_y: float = -1.0
@export var arena_max_y: float = 41.0

const MAX_BOUNCES_PER_FRAME := 3

var is_live: bool = false
var _accel_ramp: float = 1.0
var _trail: MeshInstance3D
var _trail_ghosts: Array[MeshInstance3D] = []
var _trail_positions: Array[Vector3] = []
var _player: Node3D
var _player_bias: float = 0.0
var _ball_mesh: MeshInstance3D
var _ball_visual: MeshInstance3D

func _ready() -> void:
	_player = get_tree().root.get_node_or_null("Scene/Player")
	_ball_mesh = get_node_or_null("DodgeballMesh")
	if _ball_mesh != null:
		_ball_mesh.visible = false
		_ball_mesh.queue_free()

	_ball_visual = MeshInstance3D.new()
	var ball_mesh := SphereMesh.new()
	ball_mesh.radius = 0.5
	ball_mesh.height = 1.0
	_ball_visual.mesh = ball_mesh
	_ball_visual.top_level = true
	var ball_mat := StandardMaterial3D.new()
	ball_mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	ball_mat.albedo_color = Color(1.0, 0.45, 0.0, 1.0)
	ball_mat.emission_enabled = true
	ball_mat.emission = Color(1.0, 0.45, 0.0)
	_ball_visual.material_override = ball_mat
	_ball_visual.visible = true
	add_child(_ball_visual)
	_ball_visual.global_position = global_position

	_trail = get_node_or_null("Trail")
	if _trail != null:
		_trail.visible = false
		_trail.mesh = null
		_trail.queue_free()

	_trail = MeshInstance3D.new()
	_trail.name = "Trail"
	_trail.top_level = true
	_trail.visible = true
	_trail.global_position = global_position
	add_child(_trail)
	for i in range(trail_max_points):
		var ghost := MeshInstance3D.new()
		var mesh := SphereMesh.new()
		mesh.radius = 0.5
		mesh.height = 1.0
		ghost.mesh = mesh
		ghost.top_level = true
		var mat := StandardMaterial3D.new()
		mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
		mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
		mat.albedo_color = Color(1.0, 0.45, 0.0, 0.0)
		mat.emission_enabled = true
		mat.emission = Color(1.0, 0.45, 0.0)
		ghost.material_override = mat
		ghost.visible = false
		_trail.add_child(ghost)
		_trail_ghosts.append(ghost)

	throw(Vector3(30.0, 30.0, 10.0), null)

func update_trail() -> void:
	if _trail == null:
		return

	var speed := velocity.length()
	if speed < trail_min_speed:
		for ghost in _trail_ghosts:
			ghost.visible = false
		return

	var current := global_position
	if _trail_positions.is_empty():
		_trail_positions.push_front(current)
	else:
		var last := _trail_positions[0]
		var delta := current - last
		var dist := delta.length()
		if dist > trail_spacing:
			var steps := max(1, ceili(dist / trail_spacing))
			for s in range(1, steps + 1):
				var point := last + delta * (float(s) / float(steps))
				_trail_positions.push_front(point)
				if _trail_positions.size() > trail_max_points:
					_trail_positions.remove_at(trail_max_points)

	for i in range(_trail_ghosts.size()):
		if i < _trail_positions.size():
			var ghost := _trail_ghosts[i]
			ghost.global_position = _trail_positions[i]
			ghost.visible = true
			var progress := float(i) / max(1.0, float(_trail_ghosts.size() - 1))
			var scale := lerp(1.0, 0.1, progress)
			ghost.scale = Vector3(scale, scale, scale)
			var mat := ghost.material_override as StandardMaterial3D
			mat.albedo_color = Color(1.0, 0.45, 0.0, lerp(0.95, 0.08, progress))
			mat.emission = Color(1.0, 0.45, 0.0)
		else:
			_trail_ghosts[i].visible = false

func throw(initial_velocity: Vector3, thrower: Node) -> void:
	_accel_ramp = 1.0
	_player_bias = 0.0
	velocity = initial_velocity
	is_live = true
	_trail_positions.clear()

	if thrower != null:
		add_collision_exception_with(thrower)
		var timer := get_tree().create_timer(0.15)
		timer.timeout.connect(func() -> void:
			if is_instance_valid(self) and is_instance_valid(thrower):
				remove_collision_exception_with(thrower)
		)

func apply_player_bias_on_bounce(velocity: Vector3) -> Vector3:
	if _player == null:
		return velocity

	var to_player := _player.global_position - global_position
	if to_player.length_squared() < 0.0001:
		return velocity

	var desired := to_player.normalized()
	var bias_strength := min(player_bias_strength + _player_bias, player_bias_max)
	var current := velocity.normalized()
	var blended := current.lerp(desired, bias_strength * 0.1).normalized()
	return blended * max(velocity.length(), 1.0)

func apply_player_hit_knockback(collision: KinematicCollision3D) -> void:
	if not (collision.get_collider() is Player):
		return

	var player := collision.get_collider() as Player
	var hit_dir := velocity
	hit_dir.y = 0.0
	if hit_dir.length_squared() < 0.0001:
		hit_dir = player.global_position - global_position
		hit_dir.y = 0.0
	if hit_dir.length_squared() < 0.0001:
		hit_dir = -collision.get_normal()
		hit_dir.y = 0.0
	if hit_dir.length_squared() < 0.0001:
		hit_dir = Vector3(1.0, 0.0, 0.0)

	var push := hit_dir.normalized()
	push = Vector3(push.x, 0.3, push.z).normalized()
	var knockback_magnitude := max(velocity.length(), hit_knockback_strength)
	player.apply_knockback(push * knockback_magnitude)

func _physics_process(delta: float) -> void:
	var dt := float(delta)
	update_trail()
	_ball_visual.global_position = global_position
	_ball_visual.visible = true

	if is_live and velocity.length_squared() > 0.001:
		var dir := velocity.normalized()
		var accel := flight_acceleration * _accel_ramp
		var new_speed := min(velocity.length() + accel * dt, max_speed)
		velocity = dir * new_speed

	velocity += Vector3.DOWN * gravity * dt

	var motion := velocity * dt
	for i in range(MAX_BOUNCES_PER_FRAME):
		var collision := move_and_collide(motion)
		if collision == null:
			break

		var normal := collision.get_normal()
		var hit_floor := normal.y > 0.7

		if collision.get_collider() is Player:
			apply_player_hit_knockback(collision)

		if is_live:
			_accel_ramp *= 1.0 + acceleration_per_hit

		if hit_floor:
			_player_bias = min(_player_bias + player_bias_per_bounce, player_bias_max)
			velocity = velocity.bounce(normal) * floor_bounciness
			velocity = apply_player_bias_on_bounce(velocity)
			motion = collision.get_remainder().bounce(normal) * floor_bounciness

			if abs(velocity.y) < min_bounce_speed:
				velocity = Vector3(velocity.x * ground_friction, 0.0, velocity.z * ground_friction)
				is_live = false
				break
		else:
			_player_bias = min(_player_bias + player_bias_per_bounce, player_bias_max)
			var mult := wall_bounce_multiplier if is_live else 0.85
			var bounced := velocity.bounce(normal) * mult
			bounced = apply_player_bias_on_bounce(bounced)
			if bounced.length() > max_speed:
				bounced = bounced.normalized() * max_speed
			velocity = bounced
			motion = collision.get_remainder().bounce(normal) * mult

	if is_live:
		var pos := global_position
		if pos.x <= -arena_half_size or pos.x >= arena_half_size:
			velocity = Vector3(-velocity.x, velocity.y, velocity.z)
			pos.x = clamp(pos.x, -arena_half_size, arena_half_size)
			global_position = pos
		if pos.z <= -arena_half_size or pos.z >= arena_half_size:
			velocity = Vector3(velocity.x, velocity.y, -velocity.z)
			pos.z = clamp(pos.z, -arena_half_size, arena_half_size)
			global_position = pos
		if pos.y <= arena_min_y or pos.y >= arena_max_y:
			velocity = Vector3(velocity.x, -velocity.y, velocity.z)
			pos.y = clamp(pos.y, arena_min_y, arena_max_y)
			global_position = pos

	_ball_visual.global_position = global_position

