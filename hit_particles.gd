extends GPUParticles3D

@export var debug_mode: bool = false  # Debug mode for printing additional information

func _ready():
	# Configure particle system for one-shot behavior
	emitting = false
	one_shot = true
	
	# Set up cleanup timer
	var cleanup_timer = get_tree().create_timer(lifetime + 0.1)  # Add small buffer
	cleanup_timer.timeout.connect(queue_free)
	
	if debug_mode:
		print("[HitParticles] Configured:")
		print("  - Lifetime: ", lifetime)
		print("  - One shot: ", one_shot)
		print("  - Position: ", global_position)
