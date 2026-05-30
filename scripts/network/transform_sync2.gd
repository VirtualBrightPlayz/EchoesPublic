class_name TransformSync2
extends Node

class PositionData:
	var local_tick: int
	var remote_tick: int
	var pos: Vector3
	var rot: Quaternion

signal on_sync()

@export
var sync_active: bool = true

@export
var sync_transform: bool = true

@export
var display_delay_ticks: int = 150

@export
var sync_interval_ticks: int = 100

@export
var target_node: NodePath

@export
var relative_to: NodePath:
	set(v):
		relative_to = v
		relative = get_node_or_null(v)

@export
var authority_id: int = MultiplayerPeer.TARGET_PEER_SERVER

var relative: Node3D = null
var parent: Node3D = null
var sync_timer: float

var last_pos: Vector3
var last_rot: Quaternion

var last_data: PositionData = null
var data_queue: Array[PositionData] = []

func is_authority() -> bool:
	if not multiplayer or not multiplayer.has_multiplayer_peer():
		return true
	return is_multiplayer_authority() || multiplayer.get_unique_id() == authority_id

func is_sender_authority() -> bool:
	return multiplayer.get_remote_sender_id() == get_multiplayer_authority() || multiplayer.get_remote_sender_id() == authority_id

func _enter_tree() -> void:
	multiplayer.peer_connected.connect(_joined)
	if target_node == null || target_node.is_empty():
		parent = null
	else:
		parent = get_node(target_node)
	sync_timer = 0.0

func _exit_tree() -> void:
	multiplayer.peer_connected.disconnect(_joined)

func _ready() -> void:
	last_data = null
	data_queue.clear()
	authority_id = get_multiplayer_authority()

func _joined(id: int) -> void:
	var offsets = Transform3D.IDENTITY
	var other = relative
	if other and other is Node3D:
		offsets = other.global_transform
	if is_authority():
		rpc_change_owner.rpc_id(id, authority_id)
		if parent:
			rpc_position_rotation_reliable.rpc_id(id, Time.get_ticks_msec(), offsets.affine_inverse() * parent.position, parent.quaternion, relative_to)

func _process(delta: float) -> void:
	if not multiplayer.has_multiplayer_peer():
		return
	if not parent:
		parent = get_node_or_null(target_node)
		return
	if not sync_active:
		return
	var auth := is_authority()
	if parent and parent is RigidBody3D:
		parent.freeze = not auth
	if auth:
		# sending code
		sync_timer += delta
		if sync_timer > sync_interval_ticks / 1000.0:
			sync_timer = 0.0
			var offsets = Transform3D.IDENTITY
			var other = relative
			if other and other is Node3D:
				offsets = other.global_transform
			if sync_transform:
				var pos: Vector3 = offsets.affine_inverse() * parent.position
				var rot: Quaternion = parent.quaternion
				if (last_pos.distance_to(pos) > 0.05 or last_rot.angle_to(rot) > deg_to_rad(2.5)):
					last_pos = pos
					last_rot = rot
					rpc_position_rotation.rpc(Time.get_ticks_msec(), pos, rot)
			on_sync.emit()
	else:
		# processing code
		var offsets = Transform3D.IDENTITY
		var other = relative
		if other and other is Node3D:
			offsets = other.global_transform
		if sync_transform and data_queue.size() != 0:
			var target := data_queue[0]
			var last_tick := target.local_tick + (last_data.remote_tick - target.remote_tick)
			if last_tick == target.local_tick:
				# prevent NaN by avoiding divide by zero
				parent.position = offsets * last_data.pos
				parent.quaternion = last_data.rot
				last_data = data_queue.pop_front()
			else:
				var cur_tick := Time.get_ticks_msec() - display_delay_ticks
				var lerp_amount := inverse_lerp(last_tick, target.local_tick, cur_tick)
				lerp_amount = clampf(lerp_amount, 0.0, 1.0)
				parent.position = offsets * last_data.pos.lerp(target.pos, lerp_amount)
				parent.quaternion = last_data.rot.slerp(target.rot, lerp_amount)
				if cur_tick >= target.local_tick:
					last_data = data_queue.pop_front()
					#print("pop %s" % [get_path()])
		elif sync_transform and last_data != null:
			parent.position = offsets * last_data.pos
			parent.quaternion = last_data.rot

func after_teleport() -> void:
	last_data = null
	data_queue.clear()

func send_to_all() -> void:
	var offsets = Transform3D.IDENTITY
	var other = relative
	if other and other is Node3D:
		offsets = other.global_transform
	if is_authority():
		rpc_change_owner.rpc(authority_id)
		if parent:
			rpc_position_rotation_reliable.rpc(Time.get_ticks_msec(), offsets.affine_inverse() * parent.position, parent.quaternion, relative_to)

func send_relative_to(path: NodePath) -> void:
	relative_to = path
	send_to_all()

@rpc("authority", "call_local", "reliable")
func rpc_change_owner(id: int) -> void:
	sync_timer = 0.0
	authority_id = id

@rpc("any_peer", "call_remote", "unreliable")
func rpc_position_rotation(ticks: int, pos: Vector3, rot: Quaternion) -> void:
	if not is_sender_authority():
		return
	var local_tick := Time.get_ticks_msec()
	if data_queue.size() != 0 and data_queue[0].remote_tick > ticks:
		print("Transform from %s rejected on %s. Reason: remote_tick is greater than local_tick %s." % [multiplayer.get_remote_sender_id(), get_path(), ticks])
		return
	if data_queue.size() != 0 and data_queue[0].remote_tick == ticks:
		print("Transform from %s rejected on %s. Reason: remote_tick is the same as local_tick %s." % [multiplayer.get_remote_sender_id(), get_path(), ticks])
		return
	var data := PositionData.new()
	data.local_tick = local_tick
	data.remote_tick = ticks
	data.pos = pos
	data.rot = rot
	data_queue.push_back(data)
	if not last_data:
		last_data = data_queue.pop_front()

@rpc("any_peer", "call_remote", "reliable")
func rpc_position_rotation_reliable(ticks: int, pos: Vector3, rot: Quaternion, path: NodePath) -> void:
	if not is_sender_authority():
		return
	after_teleport()
	var local_tick := Time.get_ticks_msec()
	var data := PositionData.new()
	data.local_tick = local_tick
	data.remote_tick = ticks
	data.pos = pos
	data.rot = rot
	data_queue.push_back(data)
	last_data = data_queue[0]
	relative_to = path
