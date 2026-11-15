extends CanvasLayer

@onready var stats_label: Label = $UIContainer/StatsLabel
var player: Node

func _ready() -> void:
	player = get_parent()

func _process(_delta: float) -> void:
	if not player or not stats_label:
		return

	var vel := player.velocity
	var total_speed := vel.length()
	var horizontal_speed := Vector3(vel.x, 0.0, vel.z).length()
	var x := int(player.global_position.x)
	var y := int(player.global_position.y)
	var z := int(player.global_position.z)

	var text := ""
	text += "pos:      %d, %d, %d\n" % [x, y, z]
	text += "speed:    %.2f m/s\n" % total_speed
	text += "speed (h): %.2f m/s\n" % horizontal_speed
	text += "speed (v): %.2f m/s\n" % vel.y
	stats_label.text = text

