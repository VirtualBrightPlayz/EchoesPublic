extends Node

const app_id = 1153378115867922483
const player_role = preload("res://scripts/player/PlayerRole.cs")

func _ready() -> void:
	DiscordRPC.clear()
	DiscordRPC.app_id = app_id

func set_activity_main_menu() -> void:
	DiscordRPC.state = "Going over Breach Procedures"
	DiscordRPC.details = "On the main menu"
	DiscordRPC.start_timestamp = int(Time.get_unix_time_from_system())
	DiscordRPC.large_image = ""
	DiscordRPC.small_image = ""
	DiscordRPC.refresh()
	
# todo: setup roles to add what images are needed
func set_activity_role(role: player_role, set_time: bool, server_name: String, server_image: String) -> void:
	DiscordRPC.state = "Playing as a %s" % tr(role.DisplayName)
	DiscordRPC.details = "Somewhere in %s" % server_name
	DiscordRPC.large_image = server_image
	DiscordRPC.small_image = role.DiscordActivityImage
	if (set_time):
		DiscordRPC.start_timestamp = int(Time.get_unix_time_from_system())
	DiscordRPC.refresh()

func set_activity_level_editor() -> void:
	DiscordRPC.state = "Editing a Level"
	DiscordRPC.details = "Level Editor"
	DiscordRPC.start_timestamp = int(Time.get_unix_time_from_system())
	DiscordRPC.large_image = ""
	DiscordRPC.small_image = ""
	DiscordRPC.refresh()
