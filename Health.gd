extends Node
class_name Health

# Signals for health changes and critical state
signal health_changed(new_health: float, max_health: float)
signal critical_health
signal died  # Signal for when the character dies

# Health value can be set from the editor
@export var max_health: float = 100.0
var current_health: float = max_health

func _ready():
	print("[Health] Starting health: ", current_health)
	# Emit initial health state
	emit_signal("health_changed", current_health, max_health)

# Function to apply damage to the health
func take_damage(amount: float, source_position: Vector3) -> void:
	print("[Health] Received damage: ", amount, " from position: ", source_position)
	print("[Health] Current health before damage: ", current_health)
	
	current_health -= amount
	current_health = clamp(current_health, 0, max_health)  # Ensure health stays within bounds
	
	print("[Health] Health after damage: ", current_health)
	
	# Emit health changed signal
	emit_signal("health_changed", current_health, max_health)
	
	# Check for critical health (20% or less)
	if current_health > 0 and current_health <= (max_health * 0.2):
		emit_signal("critical_health")
	
	if current_health <= 0:
		print("[Health] Health depleted - Character dying!")
		die()

# Function to handle character death
func die() -> void:
	print("[Health] Character died!")
	current_health = 0
	emit_signal("died")  # Emit the died signal
