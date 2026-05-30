class_name PlayerAnimsScript
extends Node

var anims: PlayerAnims

var walk_paths: Array = []
var walk_speed_path

func _ready() -> void:
	anims = get_parent() as PlayerAnims
	if anims is PlayerAnims:
		walk_paths = anims.walkPaths
		walk_speed_path = anims.walkSpeedPath
	else:
		print("anims not found")

func _process(delta: float) -> void:
	return
	if walk_speed_path is StringName:
		lerp_setf(walk_speed_path, anims.walking, delta)
	for path in walk_paths:
		if path is StringName:
			lerp_setv(path, anims.walkingDirection, delta)

func lerp_setf(path: StringName, value: float, delta: float) -> void:
	var f = get(path)
	if f is float:
		set(path, lerpf(f, value as float, delta * anims.animLerpSpeed))

func lerp_setv(path: StringName, value: Vector2, delta: float) -> void:
	var f = get(path)
	if f is Vector2:
		set(path, f.lerp(value as Vector2, delta * anims.animLerpSpeed))
	
