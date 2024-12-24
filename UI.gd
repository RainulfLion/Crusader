extends Control

@onready var health_bar = $HealthBar
@onready var health_system = get_node("/root/Node3D/Player/Health")

func _ready():
	health_bar.max_value = health_system.max_health
	health_bar.value = health_system.current_health
	
	var style = StyleBoxFlat.new()
	style.bg_color = Color.RED
	health_bar.add_theme_stylebox_override("fill", style)

func _process(_delta):
	health_bar.value = health_system.current_health
