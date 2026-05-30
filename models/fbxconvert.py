import sys, os
import bpy
import math

argv = sys.argv
argv = argv[argv.index('--') + 1:]

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete()

bpy.ops.import_scene.fbx(filepath=argv[0])

bpy.ops.object.select_all(action='SELECT')
obs = []
for ob in bpy.context.scene.objects:
    if ob.type == 'MESH':
        obs.append(ob)
c = {}
c['object'] = c['active_object'] = obs[0]
c['selected_objects'] = c['selected_editable_objects'] = obs
with bpy.context.temp_override(active_object=obs[0], selected_editable_objects=obs):
    bpy.ops.object.join()
for ob in bpy.context.scene.objects:
    if ob.type == 'MESH':
        bpy.context.view_layer.objects.active = ob
        break

bpy.ops.object.shade_flat()
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.remove_doubles()
bpy.ops.mesh.normals_tools(mode='RESET')
bpy.ops.mesh.faces_shade_flat()
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.object.shade_smooth_by_angle(angle=40*(math.pi/180.0))

fullpath = os.path.realpath(argv[0])
filepath = os.path.splitext(fullpath)[0] + '.glb'
bpy.ops.export_scene.gltf(filepath=filepath)
