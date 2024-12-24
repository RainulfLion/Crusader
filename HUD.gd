extends CanvasLayer

@onready var health_bar: ProgressBar = $Control/HealthBar
@onready var health_label: Label = $Control/HealthLabel
@onready var damage_overlay: ColorRect = $Control/Damaged
@onready var panel: Panel = $Control/Panel
@onready var close_button: Button = $Control/Panel/CloseButton
@onready var restart_button: Button = $Control/Panel/RestartButton

var tween: Tween = null
var player = null
var health_system = null
var is_player_dead = false

func _ready():
	print("HUD: Initializing...")
	# Hide panel at start
	if panel:
		panel.visible = false
		print("HUD: Panel hidden initially")
	else:
		print("HUD: WARNING - Panel not found!")
	
	# Connect button signals
	if close_button:
		close_button.pressed.connect(_on_close_pressed)
		print("HUD: Close button connected")
	else:
		print("HUD: WARNING - Close button not found!")
		
	if restart_button:
		restart_button.pressed.connect(_on_restart_pressed)
		print("HUD: Restart button connected")
	else:
		print("HUD: WARNING - Restart button not found!")
	
	# Wait a frame to ensure player is ready
	await get_tree().process_frame
	find_player()

func _process(_delta: float) -> void:
	if not is_player_dead and health_system and health_bar:
		health_bar.value = health_system.current_health
		if health_label:
			health_label.text = str(ceil(health_system.current_health)) + "/" + str(health_system.max_health)

func find_player():
	print("HUD: Looking for player...")
	player = get_tree().get_first_node_in_group("player")
	if player:
		print("HUD: Found player, looking for health system...")
		health_system = player.get_node("Health")
		if health_system:
			print("HUD: Found health system, connecting signals...")
			# Disconnect any existing connections first
			if health_system.died.is_connected(_on_player_died):
				health_system.died.disconnect(_on_player_died)
			if health_system.health_changed.is_connected(_on_health_changed):
				health_system.health_changed.disconnect(_on_health_changed)
			if health_system.critical_health.is_connected(_on_critical_health):
				health_system.critical_health.disconnect(_on_critical_health)
			
			# Connect signals
			health_system.died.connect(_on_player_died)
			health_system.health_changed.connect(_on_health_changed)
			health_system.critical_health.connect(_on_critical_health)
			print("HUD: All signals connected successfully")
		else:
			print("HUD: ERROR - Health system not found on player!")
			# Try again next frame
			await get_tree().process_frame
			find_player()
	else:
		print("HUD: ERROR - Player not found!")
		# Try again next frame
		await get_tree().process_frame
		find_player()

func _on_player_died():
	print("HUD: Player died signal received!")
	is_player_dead = true
	
	# Update health display
	if health_bar:
		health_bar.value = 0
		health_bar.modulate = Color.RED
		print("HUD: Health bar updated to show death")
	
	if health_label:
		health_label.text = "0/100"
		print("HUD: Health label updated to show death")
	
	# Show damage effect
	show_damage_effect()
	
	# Show panel and enable mouse
	if panel:
		panel.visible = true
		Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
		print("HUD: Death panel shown and mouse enabled")
	else:
		print("HUD: ERROR - Cannot show death panel - panel not found!")

func _on_close_pressed():
	print("HUD: Close button pressed")
	get_tree().quit()

func _on_restart_pressed():
	print("HUD: Restart button pressed")
	if panel:
		panel.visible = false
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
	is_player_dead = false
	get_tree().reload_current_scene()

func _on_health_changed(new_health: float, max_health: float):
	if health_bar:
		health_bar.max_value = max_health
		health_bar.value = new_health
	if health_label:
		health_label.text = str(ceil(new_health)) + "/" + str(max_health)

func _on_critical_health():
	if health_bar and not is_player_dead:
		var critical_tween = create_tween()
		critical_tween.tween_property(health_bar, "modulate", Color(1, 0, 0), 0.2)
		critical_tween.tween_property(health_bar, "modulate", Color(1, 1, 1), 0.2)

func show_damage_effect():
	if damage_overlay:
		if tween:
			tween.kill()
		tween = create_tween()
		damage_overlay.modulate.a = 0.4
		tween.tween_property(damage_overlay, "modulate:a", 0.0, 0.5)
