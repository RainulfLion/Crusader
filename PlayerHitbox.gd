extends Area3D
class_name PlayerHitBox

@export var armor: float = 0.0  # Player's base armor
@export var dodge_chance: float = 0.1  # Chance to dodge attacks (0.1 = 10%)
@export var parry_window: float = 0.2  # Time window for parrying in seconds
@export var parry_damage_reduction: float = 0.5  # Damage reduction when successfully parrying
@export var perfect_block_window: float = 0.15  # 150ms window for perfect block
@export var normal_block_reduction: float = 0.7  # 70% damage reduction for normal block


var recently_hit_by = {}  # Track weapons that recently hit
var is_blocking: bool = false
var block_start_time: float = 0.0
var perfect_block_active: bool = false

var is_parrying: bool = false
var parry_start_time: float = 0.0

signal perfect_block_performed(position: Vector3)

func _ready():
	print("[PlayerHitBox] Ready - Armor: ", armor, ", Dodge chance: ", dodge_chance)
	add_to_group("player_hitbox")
	
	# Connect area signals
	area_entered.connect(_on_area_entered)
	body_entered.connect(_on_body_entered)
	
	# Connect to health signals
	var health = get_parent().get_node_or_null("Health")
	if health:
		health.health_changed.connect(_on_health_changed)
		health.critical_health.connect(_on_critical_health)
		health.died.connect(_on_died)
	else:
		print("[PlayerHitBox] WARNING: No Health node found!")

func start_parry():
	is_parrying = true
	parry_start_time = Time.get_ticks_msec() / 1000.0  # Convert to seconds
	print("[PlayerHitBox] Started parrying")

func end_parry():
	is_parrying = false
	print("[PlayerHitBox] Ended parrying")

func start_block(start_time: float):
	print("[PlayerHitBox] Starting block")
	is_blocking = true
	block_start_time = start_time
	perfect_block_active = true
	
	# Start timer to end perfect block window
	get_tree().create_timer(perfect_block_window).timeout.connect(
		func(): perfect_block_active = false
	)

func end_block():
	print("[PlayerHitBox] Ending block")
	is_blocking = false
	perfect_block_active = false

func calculate_damage(incoming_damage: float) -> float:
	# Check for dodge first
	if randf() < dodge_chance:
		print("[PlayerHitBox] Attack dodged!")
		return 0.0
	
	# Handle blocking
	if is_blocking:
		var current_time = Time.get_ticks_msec() / 1000.0
		var block_timing = current_time - block_start_time
		
		if perfect_block_active:
			print("[PlayerHitBox] Perfect block! No damage taken!")
			emit_signal("perfect_block_performed", global_position)
			return 0.0
		else:
			print("[PlayerHitBox] Normal block! Reduced damage!")
			incoming_damage *= (1.0 - normal_block_reduction)
	
	# Apply armor to remaining damage
	var final_damage = max(incoming_damage - armor, 0)
	
	print("[PlayerHitBox] Incoming damage: ", incoming_damage)
	print("[PlayerHitBox] After armor/block: ", final_damage)
	
	return final_damage

func emit_perfect_block():
	print("[PlayerHitBox] Emitting perfect block signal!")
	emit_signal("perfect_block_performed", global_position)
	
	# Play perfect block effects
	if get_parent().has_method("play_perfect_block_effects"):
		get_parent().play_perfect_block_effects()

func can_be_hit_by(weapon: Node) -> bool:
	if not recently_hit_by.has(weapon):
		recently_hit_by[weapon] = Time.get_ticks_msec()
		clean_recent_hits()
		return true
	
	var last_hit_time = recently_hit_by[weapon]
	var current_time = Time.get_ticks_msec()
	if current_time - last_hit_time > 500:  # 500ms cooldown
		recently_hit_by[weapon] = current_time
		return true
	
	return false

func clean_recent_hits() -> void:
	var current_time = Time.get_ticks_msec()
	var to_remove = []
	
	for weapon in recently_hit_by:
		if current_time - recently_hit_by[weapon] > 1000:  # 1 second cleanup
			to_remove.append(weapon)
	
	for weapon in to_remove:
		recently_hit_by.erase(weapon)

func apply_damage(weapon: Node, source_position: Vector3) -> void:
	if not can_be_hit_by(weapon):
		print("[PlayerHitBox] Ignoring repeated hit from weapon")
		return
		
	if weapon.has_method("get_damage"):
		var weapon_damage = weapon.get_damage()
		var final_damage = calculate_damage(weapon_damage)
		
		var health = get_parent().get_node_or_null("Health")
		if health and health.has_method("take_damage"):
			health.take_damage(final_damage, source_position)
		else:
			print("[PlayerHitBox] ERROR: No Health component found!")

func _on_damage_taken(damage: float, source_position: Vector3) -> void:
	# Implement player-specific damage feedback
	print("[PlayerHitBox] Took ", damage, " damage from ", source_position)
	# You can emit signals here for UI updates, camera shake, etc.

func _on_health_changed(new_health: float, max_health: float) -> void:
	# Update UI elements
	var hud = get_tree().get_root().find_child("HUD", true, false)
	if hud and hud.has_method("update_health_bar"):
		hud.update_health_bar(new_health, max_health)

func _on_critical_health() -> void:
	# Show critical health effects in UI
	var hud = get_tree().get_root().find_child("HUD", true, false)
	if hud and hud.has_method("show_critical_health"):
		hud.show_critical_health()

func _on_died() -> void:
	# Trigger death UI and game over state
	var hud = get_tree().get_root().find_child("HUD", true, false)
	if hud and hud.has_method("show_game_over"):
		hud.show_game_over()

func _on_area_entered(area: Area3D) -> void:
	if area.is_in_group("Enemy_Weapon"):
		print("[PlayerHitBox] Hit by enemy weapon")
		apply_damage(area, area.global_position)

func _on_body_entered(body: Node3D) -> void:
	if body.is_in_group("Enemy_Weapon"):
		print("[PlayerHitBox] Hit by enemy weapon (body)")
		apply_damage(body, body.global_position)
