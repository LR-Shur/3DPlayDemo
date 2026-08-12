import bpy
import os
import sys

source = os.path.abspath(sys.argv[sys.argv.index('--') + 1])
target = os.path.abspath(sys.argv[sys.argv.index('--') + 2])
os.makedirs(target, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=source)
objects = [obj for obj in bpy.data.objects if obj.type in {'MESH', 'ARMATURE'}]
for obj in objects:
    obj.select_set(True)
if objects:
    bpy.context.view_layer.objects.active = objects[0]
name = os.path.splitext(os.path.basename(source))[0]
output = os.path.join(target, name + '.fbx')
bpy.ops.export_scene.fbx(filepath=output, use_selection=True, apply_scale_options='FBX_SCALE_ALL', object_types={'MESH', 'ARMATURE'}, add_leaf_bones=False, bake_anim=False)
print(output)
