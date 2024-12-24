extends Node

signal game_over
signal wave_completed
signal score_updated

var current_score: int = 0
var current_wave: int = 0
var enemies_remaining: int = 0
var player_alive: bool = true

func _ready():
	pass

func _input(event):
	if event.is_action_pressed("ui_cancel"):  # ESC key
		if Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
			Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
		else:
			get_tree().quit()

func start_game():
	current_score = 0
	current_wave = 0
	player_alive = true
	start_next_wave()

func start_next_wave():
	current_wave += 1
	var num_enemies = 5 + (current_wave * 2)  # Increase enemies per wave
	enemies_remaining = num_enemies



func enemy_defeated():
	current_score += 100
	enemies_remaining -= 1
	score_updated.emit()
	
	if enemies_remaining <= 0:
		wave_completed.emit()
		start_next_wave()

func player_died():
	player_alive = false
	game_over.emit()
