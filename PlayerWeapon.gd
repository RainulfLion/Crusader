extends Area3D
class_name PlayerWeapon

# Signals
signal attack_started
signal attack_finished
signal critical_hit(position: Vector3)

# Export variables for player weapon stats
@export var base_damage: float = 20.0
@export var hit_cooldown: float = 0.3  # Cooldown between attacks
@export var stamina_cost: float = 10.0  # Stamina cost per attack
@export var critical_hit_chance: float = 0.15  # 15% chance for critical hit
@export var critical_damage_multiplier: float = 2.0
@export var debug_mode: bool = false  # Toggle debug output

# Sound effects
@export var swing_sound: AudioStreamPlayer3D
@export var swing_sounds: Array[AudioStream]  # Array of different swing sounds

# State variables
var is_attacking: bool = false
var has_hit_this_animation: bool = false
var can_hit: bool = true
var current_damage: float = base_damage

# Node references
@onready var cooldown_timer: Timer = Timer.new()
@onready var collision_shape = get_node("CollisionShape3D") if has_node("CollisionShape3D") else null

func _ready():
	print("[PlayerWeapon] Ready - Base damage: ", base_damage)
	
	# Set collision layers to match working system
	collision_layer = 1  # Same as other working collisions
	collision_mask = 1   # Same as other working collisions
	
	# Setup cooldown timer
	add_child(cooldown_timer)
	cooldown_timer.one_shot = true
	cooldown_timer.timeout.connect(_on_cooldown_timer_timeout)
	
	# Add to player weapon group (lowercase for consistency)
	if not is_in_group("player_weapon"):
		add_to_group("player_weapon")
		
	print("[PlayerWeapon] Setup:")
	print("  - Collision Layer: ", collision_layer)
	print("  - Collision Mask: ", collision_mask)
	print("  - Groups: ", get_groups())
	
	# Connect area signals
	area_entered.connect(_on_area_entered)
	
	# Disable collision initially
	if collision_shape:
		collision_shape.disabled = true
	else:
		print("[PlayerWeapon] WARNING: No collision shape found!")

func start_attack() -> bool:
	if is_attacking or not can_hit:
		return false
		
	# Check if player has enough stamina
	var player = get_parent()
	if player.has_method("get_stamina") and player.get_stamina() < stamina_cost:
		print("[PlayerWeapon] Not enough stamina for attack!")
		return false
	
	is_attacking = true
	has_hit_this_animation = false
	current_damage = base_damage
	can_hit = false  # Set to false when attack starts
	
	# Enable collision
	if collision_shape:
		collision_shape.disabled = false
	
	# Play swing sound if available
	if swing_sound and swing_sounds.size() > 0:
		# Pick a random swing sound
		var random_sound = swing_sounds[randi() % swing_sounds.size()]
		swing_sound.stream = random_sound
		swing_sound.play()
		if debug_mode:
			print("[PlayerWeapon] Playing swing sound")
	
	# Use stamina
	if player.has_method("use_stamina"):
		player.use_stamina(stamina_cost)
	
	emit_signal("attack_started")
	cooldown_timer.start(hit_cooldown)
	
	print("[PlayerWeapon] Attack started. Damage: ", current_damage)
	return true

func end_attack():
	print("[PlayerWeapon] Ending attack")
	is_attacking = false
	if collision_shape:
		collision_shape.disabled = true
	
	emit_signal("attack_finished")

func _on_cooldown_timer_timeout():
	print("[PlayerWeapon] Attack cooldown finished")
	can_hit = true

func _on_area_entered(area: Area3D) -> void:
	print("[PlayerWeapon] Area entered: ", area.name)
	print("  - Area groups: ", area.get_groups())
	print("  - Area collision layer: ", area.collision_layer)
	print("  - Area collision mask: ", area.collision_mask)
	print("  - Is attacking: ", is_attacking)
	print("  - Has hit this animation: ", has_hit_this_animation)
	
	# Check for both uppercase and lowercase variants
	if area.is_in_group("enemy_hitbox") or area.is_in_group("Enemy_HitBox"):
		print("[PlayerWeapon] Hit enemy hitbox!")
		if not has_hit_this_animation and is_attacking:
			has_hit_this_animation = true
			can_hit = false
			cooldown_timer.start(hit_cooldown)
			
			# Apply damage through the hitbox
			if area.has_method("apply_damage"):
				print("[PlayerWeapon] Calling apply_damage on enemy hitbox")
				print("  - Current damage: ", current_damage)
				area.apply_damage(self, global_position)
			else:
				print("[PlayerWeapon] ERROR: Enemy hitbox missing apply_damage method!")

func get_damage() -> float:
	# Check for critical hit
	var damage = current_damage
	if randf() < critical_hit_chance:
		print("[PlayerWeapon] Critical hit!")
		damage *= critical_damage_multiplier
		emit_signal("critical_hit", global_position)
	
	print("[PlayerWeapon] Returning damage: ", damage)
	return damage
