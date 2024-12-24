extends CharacterBody3D

@export var SPEED: float = 5.0
@export var DETECTION_RANGE: float = 10.0
@export var ATTACK_RANGE: float = 2.0
@export var HEIGHT_OFFSET: float = 0.0  # Height above ground level
@export var patrol_points: Array[Node3D] = []  # Array of Node3D patrol points set in inspector

# Get the gravity from the project settings
var gravity = ProjectSettings.get_setting("physics/3d/default_gravity")

enum State {PATROL, CHASE, ATTACK}
var current_state = State.PATROL
var current_patrol_index = 0
var player = null
var current_destination: Vector3
var destination_threshold = 1
var initial_model_y = 0.0  # Store the initial Y offset of the model
var is_dying: bool = false
var death_timer: Timer

@onready var animation_player = $Villager/AnimationPlayer
@onready var parasite_model = $Villager
@onready var weapon_hitbox = $Villager/GeneralSkeleton/BoneAttachmentRight/sax/EnemyWeapon
@onready var health_system = $Health if has_node("Health") else null

func _ready():
	print("Enemy initializing...")
	
	# Store initial model position relative to root
	if parasite_model:
		initial_model_y = parasite_model.position.y + HEIGHT_OFFSET
		print("Initial model Y offset: ", initial_model_y)
	
	# Find player
	player = get_tree().get_first_node_in_group("player")
	if player:
		print("Found player at: ", player.global_position)
	else:
		print("No player found!")
	
	# Wait for physics to settle
	await get_tree().physics_frame
	await get_tree().physics_frame
	
	# If no patrol points set in inspector, use current position
	if patrol_points.is_empty():
		print("Warning: No patrol points set in inspector!")
		current_destination = global_position
	else:
		print("Using inspector-set patrol points: ", patrol_points)
		# Set initial destination to first patrol point
		current_destination = patrol_points[0].global_position
		print("Initial patrol destination: ", current_destination)
	
	# Configure animation player
	if animation_player:
		# Disable root motion on all animations
		for anim_name in animation_player.get_animation_list():
			var anim = animation_player.get_animation(anim_name)
			if anim:
				anim.loop_mode = Animation.LOOP_LINEAR
				# Remove any root motion tracks if they exist
				for track_idx in anim.get_track_count():
					var path = str(anim.track_get_path(track_idx))
					if path.contains("position") or path.contains("rotation"):
						anim.remove_track(track_idx)
	
	# Initialize death timer
	death_timer = Timer.new()
	death_timer.one_shot = true
	death_timer.wait_time = 30.0
	death_timer.timeout.connect(_on_death_timer_timeout)
	add_child(death_timer)
	
	# Connect health system signals
	if health_system:
		health_system.died.connect(died)
	else:
		print("Warning: No Health node found on enemy!")

func _physics_process(delta):
	# Apply gravity
	if not is_on_floor():
		velocity.y -= gravity * delta
	else:
		velocity.y = 0
		# Update patrol points to current ground level if needed
		var current_ground_y = global_position.y + HEIGHT_OFFSET
		for i in patrol_points.size():
			if abs(patrol_points[i].global_position.y - current_ground_y) > 0.1:
				patrol_points[i].global_position.y = current_ground_y
				print("Updated patrol point ", i, " to ground level: ", patrol_points[i].global_position)
	
	# Maintain model's Y position relative to root
	if parasite_model and parasite_model.position.y != initial_model_y:
		parasite_model.position.y = initial_model_y
	
	if not is_dying:
		match current_state:
			State.PATROL:
				patrol_state()
			State.CHASE:
				chase_state()
			State.ATTACK:
				attack_state()
	
	move_and_slide()

func patrol_state():
	if patrol_points.is_empty():
		return
		
	# Check if we've reached current patrol point
	var current_point = patrol_points[current_patrol_index]
	var to_destination = current_point.global_position - global_position
	to_destination.y = 0  # Ignore height difference
	
	if to_destination.length() < destination_threshold:
		# Move to next patrol point
		current_patrol_index = (current_patrol_index + 1) % patrol_points.size()
		current_destination = patrol_points[current_patrol_index].global_position
		print("Reached patrol point, moving to next: ", current_destination)
	
	move_to_destination()
	play_animation("Run")
	
	# Check for player
	if player:
		var to_player = player.global_position - global_position
		to_player.y = 0  # Ignore height difference
		if to_player.length() < DETECTION_RANGE:
			print("Player detected! Switching to chase state")
			current_state = State.CHASE

func chase_state():
	if not player:
		print("No player found, returning to patrol")
		current_state = State.PATROL
		return
	
	var to_player = player.global_position - global_position
	to_player.y = 0  # Ignore height difference
	var distance = to_player.length()
	
	if distance > DETECTION_RANGE:
		print("Player out of range, returning to patrol")
		current_state = State.PATROL
		if not patrol_points.is_empty():
			current_destination = patrol_points[current_patrol_index].global_position
	elif distance <= ATTACK_RANGE:
		print("Player in attack range! Distance: ", distance)
		current_state = State.ATTACK
	else:
		# Move directly to player position with minimal offset
		var direction = to_player.normalized()
		var attack_position = player.global_position - (direction * destination_threshold)
		current_destination = Vector3(attack_position.x, global_position.y + HEIGHT_OFFSET, attack_position.z)
		move_to_destination()
		play_animation("Run")

func attack_state():
	if not player:
		current_state = State.PATROL
		return
		
	var to_player = player.global_position - global_position
	to_player.y = 0
	var distance = to_player.length()
	
	if distance > ATTACK_RANGE * 1.2:
		print("Player outside attack range, returning to chase. Distance: ", distance)
		current_state = State.CHASE
	else:
		# Always try to get as close as possible during attack
		var direction = to_player.normalized()
		if distance > destination_threshold:
			velocity.x = direction.x * SPEED * 0.7
			velocity.z = direction.z * SPEED * 0.7
		else:
			velocity.x = 0
			velocity.z = 0
		
		# Face the player (at same height)
		look_at(Vector3(player.global_position.x, global_position.y + HEIGHT_OFFSET, player.global_position.z))
		rotation.x = 0
		rotation.z = 0
		
		play_animation("SaxSlash1")
		if weapon_hitbox:
			weapon_hitbox.monitoring = true

func move_to_destination():
	# Get direction to destination (ignoring Y)
	var to_destination = current_destination - global_position
	to_destination.y = 0
	
	if to_destination.length() > destination_threshold:
		# Move towards destination
		var direction = to_destination.normalized()
		velocity.x = direction.x * SPEED
		velocity.z = direction.z * SPEED
		
		# Look in movement direction
		look_at(Vector3(current_destination.x, global_position.y + HEIGHT_OFFSET, current_destination.z))
		rotation.x = 0
		rotation.z = 0
	else:
		# Close enough to destination, stop moving
		velocity.x = 0
		velocity.z = 0

func play_animation(anim_name: String) -> void:
	if not animation_player:
		return
		
	if animation_player.has_animation(anim_name):
		if not animation_player.is_playing() or animation_player.current_animation != anim_name:
			# Play animation without root motion
			animation_player.play(anim_name)
			print("Playing animation: ", anim_name, " at ground level: ", global_position.y + HEIGHT_OFFSET)

func died():
	if is_dying:
		return
		
	is_dying = true
	print("Enemy died, playing death animation...")
	
	# Disable physics processing but keep collision shape for physical interaction
	set_physics_process(false)
	
	# Change collision layer/mask to only collide with environment
	# Use set_deferred for physics properties during physics callback
	if has_node("CollisionShape3D"):
		# Assuming layer 1 is environment/world collision
		set_deferred("collision_layer", 1)  # Only collide with environment
		set_deferred("collision_mask", 1)   # Only be affected by environment
	
	if weapon_hitbox:
		weapon_hitbox.set_deferred("monitoring", false)
		weapon_hitbox.set_deferred("monitorable", false)
	
	# Stop current state behavior
	current_state = -1
	velocity = Vector3.ZERO
	
	# Play death animation if available
	if animation_player and animation_player.has_animation("VillagerDeath"):
		print("Playing death animation...")
		var death_anim = animation_player.get_animation("VillagerDeath")
		death_anim.loop_mode = Animation.LOOP_NONE  # Ensure animation doesn't loop
		animation_player.play("VillagerDeath")
		# Wait for animation to finish
		await animation_player.animation_finished
		print("Death animation finished")
	else:
		print("Warning: No Death animation found!")
	
	# Start death timer
	print("Starting 30 second death timer...")
	death_timer.start()

func _on_death_timer_timeout():
	print("Death timer finished, removing enemy...")
	queue_free()
