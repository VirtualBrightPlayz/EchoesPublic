extends Node

@export var link: String

func open_link() -> void:
	OS.shell_open(link)
