@tool
extends EditorPlugin

const OpusCopy = preload("res://addons/opus/opus_copy.gd")

var plugin

func _enter_tree():
    plugin = OpusCopy.new()
    add_export_plugin(plugin)

func _exit_tree():
    remove_export_plugin(plugin)
