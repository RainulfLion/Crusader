extends Area3D
class_name EnemyWeapon

# Signals
signal attack_started
signal attack_finished

# Export variables for enemy weapon stats
@export var base_damage: float = 15.0
@export var attack_cooldown: float = 1.5  # Longer cooldown for enemies
@export var damage_variance: float = 0.2  # +/- 20% damage variance
@export var knockback_force: float = 5.0
@export var stun_chance: float = 0.1  # 10% chance to stun player
@export var stun_duration: float = 1.0  # How long the stun lasts

# State variables
var is_attacking: bool = false
var has_hit_this_animation: bool = false
var can_hit: bool = true
var current_damage: float = base_damage

# Node references
@onready var cooldown_timer: Timer = Timer.new()
@onready var collision_shape = get_node("CollisionShape3D") if has_node("CollisionShape3D") else null
@onready var animation_player = get_parent().get_node("AnimationPlayer") if get_parent().has_node("AnimationPlayer") else null

func _ready():
	print("[EnemyWeapon] Ready - Base damage: ", base_damage)
	
	# Setup cooldown timer
	add_child(cooldown_timer)
	cooldown_timer.one_shot = true
	cooldown_timer.timeout.connect(_on_cooldown_timer_timeout)
	
	# Add to enemy weapon group
	if not is_in_group("Enemy_Weapon"):
		add_to_group("Enemy_Weapon")
	
	# Connect area signals
	area_entered.connect(_on_area_entered)
	
	# Disable collision initially
	if collision_shape:
		collision_shape.disabled = true
	else:
		print("[EnemyWeapon] WARNING: No collision shape found!")

func start_attack() -> void:
	is_attacking = true
	has_hit_this_animation = false
	
	# Calculate damage with random variance
	var variance_multiplier = 1.0 + (randf() * 2.0 - 1.0) * damage_variance
	current_damage = base_damage * variance_multiplier
	
	# Enable collision
	if collision_shape:
		collision_shape.disabled = false
	
	emit_signal("attack_started")

func end_attack() -> void:
	is_attacking = false
	if collision_shape:
		collision_shape.disabled = true
	
	emit_signal("attack_finished")
	cooldown_timer.start(attack_cooldown)

func get_damage() -> float:
	return current_damage

func apply_knockback(target: Node3D) -> void:
	if target.has_method("apply_knockback"):
		var knockback_direction = (target.global_position - global_position).normalized()
		knockback_direction.y = 0  # Keep knockback horizontal
		target.apply_knockback(knockback_direction * knockback_force)

func apply_stun(target: Node3D) -> void:
	if randf() < stun_chance and target.has_method("apply_stun"):
		print("[EnemyWeapon] Stun applied!")
		target.apply_stun(stun_duration)

func _on_area_entered(area: Area3D) -> void:
	if not is_attacking or not can_hit or has_hit_this_animation:
		return
		
	if area.is_in_group("player_hitbox"):
		print("[EnemyWeapon] Hit player!")
		has_hit_this_animation = true
		can_hit = false
		
		# Apply additional effects
		var player = area.get_parent()
		apply_knockback(player)
		apply_stun(player)

func _on_cooldown_timer_timeout() -> void:
	can_hit = true

func _process(_delta: float) -> void:
	# Optional: Add visual effects for attack state
	pass
