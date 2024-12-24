extends CharacterBody3D
class_name Player

# Preload required classes
const HealthScript = preload("res://Scripts/Combat/Health.gd")
const DamageHitBoxScript = preload("res://Scripts/Combat/PlayerHitbox.gd")
const PlayerWeaponScript = preload("res://Scripts/Combat/PlayerWeapon.gd")
const HUDScript = preload("res://Scripts/UI/HUD.gd")

# Components
@onready var health_component: HealthScript = $Health
@onready var hitbox: DamageHitBoxScript = $PlayerHitbox
@onready var main_camera: Camera3D = $MainCamera
@onready var animation_player: AnimationPlayer = $MainCamera/WeaponHolder/Sword/SwordAnimations
@onready var hud: CanvasLayer = $HUD
@onready var damage_overlay: ColorRect = $HUD/Control/Damaged
@export var perfect_block_window: float = 0.15  # 150ms window for perfect block
@export var block_stamina_cost: float = 10.0
@export var block_cooldown: float = 0.5


var weapon: PlayerWeaponScript

# Movement
@export var SPEED: float = 5.0
@export var JUMP_VELOCITY: float = 4.5
@export var mouse_sensitivity: float = 0.002

# Combat
@export var invincibility_time: float = 0.5
var can_take_damage: bool = true

var gravity = ProjectSettings.get_setting("physics/3d/default_gravity")
var current_animation: String = "Idle"

var attack_animations = {
	"attack1": "Flat",
	"attack2": "Offside",
	"attack3": "Slot",
	"attack4": "Thrust"
}

var current_attack_animation: String = ""
var is_attacking: bool = false

var is_blocking: bool = false
var block_start_time: float = 0.0
var can_block: bool = true
var block_timer: Timer

signal weapon_attack_started
signal weapon_attack_finished
signal block_started
signal block_finished


func _ready():
	print("Player: Starting initialization...")
	
	# Initialize camera
	if main_camera:
		print("Player: Main camera found")
		main_camera.current = true
		main_camera.keep_aspect = Camera3D.KEEP_WIDTH
	else:
		print("Player: WARNING - Main camera not found!")
	
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
	add_to_group("player")  
	
	# Initialize block timer
	block_timer = Timer.new()
	block_timer.one_shot = true
	block_timer.timeout.connect(_on_block_cooldown_finished)
	add_child(block_timer)
	print("Player: Block timer initialized")
	
	# Connect hitbox area signals
	if hitbox:
		hitbox.area_entered.connect(_on_hitbox_area_entered)
		hitbox.body_entered.connect(_on_hitbox_body_entered)
		print("Player: Hitbox signals connected")
	else:
		print("Player: WARNING - Hitbox not found!")
	
	# Wait a frame to ensure the scene is fully loaded
	await get_tree().process_frame
	
	# Find the weapon in the scene
	weapon = find_weapon()
	if weapon:
		print("Player: Found weapon at ", weapon.get_path())
		setup_weapon_signals()
	else:
		print("Player: Could not find weapon in the scene")
	
	# Initialize health component and connect signals
	if health_component:
		print("Player: Health component found")
		health_component.health_changed.connect(_on_health_changed)
		health_component.died.connect(_on_died)
		
		# Update HUD with initial health
		if hud:
			print("Player: HUD found, updating initial health")
			if hud.has_method("update_health_bar"):
				hud.update_health_bar(health_component.current_health, health_component.max_health)
				print("Player: Initial health set to ", health_component.current_health, "/", health_component.max_health)
			else:
				print("Player: WARNING - HUD missing update_health_bar method!")
		else:
			print("Player: WARNING - HUD not found!")
	else:
		print("Player: WARNING - Health component not found!")

func setup_weapon_signals():
	if not weapon:
		print("Player: Cannot setup weapon signals - no weapon found")
		return
		
	print("Player: Setting up weapon signals...")
	
	# Disconnect any existing connections to avoid duplicates
	if weapon.has_signal("attack_started") and weapon.attack_started.is_connected(_on_weapon_attack_started):
		weapon.attack_started.disconnect(_on_weapon_attack_started)
	if weapon.has_signal("attack_finished") and weapon.attack_finished.is_connected(_on_weapon_attack_finished):
		weapon.attack_finished.disconnect(_on_weapon_attack_finished)
	
	# Connect weapon signals
	weapon.attack_started.connect(_on_weapon_attack_started)
	weapon.attack_finished.connect(_on_weapon_attack_finished)
	
	# Emit signals when block starts/ends
	block_started.emit()
	block_finished.emit()
	
	print("Player: Weapon signals connected successfully")


func find_weapon() -> PlayerWeaponScript:
	print("Searching for weapon...")
	# First try to find it at the expected path
	var direct_weapon = $MainCamera/WeaponHolder/Sword/WeaponHitbox
	if direct_weapon:
		print("Found weapon at direct path: ", direct_weapon.get_path())
		direct_weapon.add_to_group("Player_Weapon")
		return direct_weapon
	
	# If not found at direct path, search through children
	print("Direct path failed, searching children...")
	var potential_weapons = find_children("*", "PlayerWeapon")
	print("Found potential weapons: ", potential_weapons)
	
	if potential_weapons.size() > 0:
		var found_weapon = potential_weapons[0]
		found_weapon.add_to_group("Player_Weapon")
		print("Found weapon through search: ", found_weapon.get_path())
		return found_weapon
		
	# If still not found, try searching by class
	print("Searching by class name...")
	potential_weapons = find_children("*", "PlayerWeaponScript", true)
	print("Found potential weapons by class: ", potential_weapons)
	
	if potential_weapons.size() > 0:
		var found_weapon = potential_weapons[0]
		found_weapon.add_to_group("Player_Weapon")
		print("Found weapon through class search: ", found_weapon.get_path())
		return found_weapon
	
	print("ERROR: No weapon found in scene! Scene tree structure:")
	print_scene_tree()
	return null

func print_scene_tree(node: Node = self, indent: String = ""):
	print(indent + node.name + " (" + node.get_class() + ")")
	for child in node.get_children():
		print_scene_tree(child, indent + "  ")

func _input(event):
	if event is InputEventMouseMotion:
		rotate_y(-event.relative.x * mouse_sensitivity)
		main_camera.rotate_x(-event.relative.y * mouse_sensitivity)
		main_camera.rotation.x = clamp(main_camera.rotation.x, -PI/2, PI/2)
	
	# Handle attack inputs
	if not is_attacking and weapon:
		if event.is_action_pressed("AttackOne"):
			print("AttackOne button pressed!")
			current_attack_animation = "Flat"
			weapon.start_attack()
		
		elif event.is_action_pressed("AttackTwo"):
			print("AttackTwo button pressed!")
			current_attack_animation = "Offside"
			weapon.start_attack()
		
		elif event.is_action_pressed("AttackThree"):
			print("AttackThree button pressed!")
			current_attack_animation = "Slot"
			weapon.start_attack()
		
		elif event.is_action_pressed("AttackFour"):
			print("AttackFour button pressed!")
			current_attack_animation = "Thrust"
			weapon.start_attack()
	
	# Handle block inputs
	if event.is_action_pressed("block") and not is_attacking:
		start_block()
	elif event.is_action_released("block"):
		end_block()
			
			
			
func start_block():
	if not can_block:
		return
		
	print("Player: Starting block")
	is_blocking = true
	block_start_time = Time.get_ticks_msec() / 1000.0
	
	# Play block animation if you have one
	if animation_player and animation_player.has_animation("Block"):
		animation_player.play("Block")
	
	# Start block cooldown
	can_block = false
	block_timer.start(block_cooldown)
	
	# Notify hitbox about block start
	if hitbox:
		hitbox.start_block(block_start_time)

func end_block():
	print("Player: Ending block")
	is_blocking = false
	if hitbox:
		hitbox.end_block()
	
	# Return to idle animation if not attacking
	if not is_attacking and animation_player:
		animation_player.play("Idle")

func _on_block_cooldown_finished():
	can_block = true
		
func _physics_process(delta):
	# Get the input direction and handle the movement/deceleration.
	var input_dir = Input.get_vector("left", "right", "forward", "back")
	var direction = (transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	
	# Allow movement even during attacks
	if direction:
		velocity.x = direction.x * SPEED
		velocity.z = direction.z * SPEED
		# Only play running animation if not attacking
		if not is_attacking:
			play_animation("Running")
	else:
		velocity.x = move_toward(velocity.x, 0, SPEED)
		velocity.z = move_toward(velocity.z, 0, SPEED)
		# Only play idle animation if not attacking
		if not is_attacking:
			play_animation("Idle")
	
	# Add the gravity.
	if not is_on_floor():
		velocity.y -= gravity * delta
	
	move_and_slide()

func play_animation(anim_name: String):
	if animation_player and animation_player.has_animation(anim_name):
		# Don't interrupt attack animations
		if is_attacking:
			return
			
		if anim_name != current_animation:
			current_animation = anim_name
			animation_player.play(anim_name)

func _on_health_changed(new_health: float, max_health: float):
	print("Player: Health changed to ", new_health, "/", max_health)
	
	# Update HUD
	if hud:
		if hud.has_method("update_health_bar"):
			print("Player: Updating HUD health bar")
			hud.update_health_bar(new_health, max_health)
		else:
			print("Player: WARNING - HUD missing update_health_bar method!")
	else:
		print("Player: WARNING - Cannot update HUD - HUD not found!")
	
	# Only show damage overlay if health actually decreased
	if health_component and new_health < health_component.current_health and damage_overlay:
		print("Player: Showing damage overlay")
		damage_overlay.visible = true  
		damage_overlay.modulate.a = 0.5
		var tween = create_tween()
		tween.tween_property(damage_overlay, "modulate:a", 0.0, 0.5)
		tween.tween_callback(func(): damage_overlay.visible = false)
	else:
		print("Player: No damage taken, skipping damage overlay")
	
	# Show damage overlay
	if damage_overlay:
		print("Player: Showing damage overlay")
		damage_overlay.visible = true  
		damage_overlay.modulate.a = 0.5
		var tween = create_tween()
		tween.tween_property(damage_overlay, "modulate:a", 0.0, 0.5)
		tween.tween_callback(func(): damage_overlay.visible = false)
	else:
		print("Player: WARNING - Damage overlay not found!")

func _on_died():
	print("Player: Died!")
	set_physics_process(false)
	set_process_input(false)
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE

func _on_attack_started():
	pass

func _on_attack_finished():
	if current_animation != "Running":
		play_animation("Idle")

func _on_weapon_attack_started():
	# Play the selected attack animation
	if animation_player and current_attack_animation:
		print("Starting attack animation: ", current_attack_animation)
		is_attacking = true
		animation_player.play(current_attack_animation)
		# Wait for animation to finish
		await animation_player.animation_finished
		is_attacking = false
		current_attack_animation = ""
		
		# Tell weapon to end attack
		if weapon:
			weapon.end_attack()
		
		# Only return to idle if we're not already in another animation
		if not is_attacking:
			animation_player.play("Idle")

func _on_weapon_attack_finished():
	print("Player weapon attack finished")
	# Don't immediately return to idle, let the animation finish first
	if not is_attacking and animation_player:
		animation_player.play("Idle")

func play_attack_animation(anim_name: String):
	if animation_player:
		if animation_player.has_animation(anim_name):
			print("Starting attack animation: ", anim_name)
			animation_player.stop()  
			animation_player.play(anim_name)
			current_animation = anim_name
			# Connect to the animation finished signal if not already connected
			if not animation_player.animation_finished.is_connected(_on_animation_finished):
				animation_player.animation_finished.connect(_on_animation_finished)
		else:
			print("Player: WARNING - Attack animation not found: ", anim_name)
			print("Available animations: ", animation_player.get_animation_list())
	else:
		print("Player: ERROR - Animation player is null! Path: ", $MainCamera/WeaponHolder/Sword/SwordAnimations)

func _on_animation_finished(anim_name: String):
	print("Animation finished: ", anim_name)
	# Only return to idle if we finished an attack animation
	if anim_name in ["Flat", "Offside", "Slot", "Thrust"]:
		if not weapon.is_attacking:  
			play_animation("Idle")
			print("Returning to Idle after attack")

func _on_hitbox_area_entered(area: Area3D) -> void:
	if not can_take_damage:
		return
		
	if area.is_in_group("Enemy_Weapon"):
		print("Player: Hit by enemy weapon")
		can_take_damage = false
		
		# Start invincibility timer
		await get_tree().create_timer(invincibility_time).timeout
		can_take_damage = true

func _on_hitbox_body_entered(body: Node3D) -> void:
	if not can_take_damage:
		return
		
	if body.is_in_group("Enemy_Weapon"):
		print("Player: Hit by enemy weapon (body)")
		can_take_damage = false
		
		# Start invincibility timer
		await get_tree().create_timer(invincibility_time).timeout
		can_take_damage = true
