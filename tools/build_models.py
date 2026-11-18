# build_models.py  --  builds and exports the Blender models
# -----------------------------------------------------------------------------
# Builds the two low-poly characters for the game and exports them as FBX:
#
#   Mole  --  a UV-sphere body with two eyes (white + dark pupil), a pink nose
#             and two ears.
#   Bomb  --  a dark sphere with a lighter metal band, a fuse and a spark.
#
# Rules from the PRD (section 9):
#   * low poly, simple materials,
#   * origin at the bottom centre of the model,
#   * object scale applied,
#   * exported with "Apply Transform" so Unity sees the same axes.
#
# Blender is Z-up and looks down -Y, Unity is Y-up and looks down +Z. The FBX
# exporter is told axis_forward='-Z', axis_up='Y', so a model that faces -Y in
# Blender faces +Z in Unity, which is the way the existing prefabs face.
#
# Run headless:
#   /Applications/Blender.app/Contents/MacOS/Blender -b -P tools/build_models.py
# -----------------------------------------------------------------------------

import bpy
import math
import os
import sys

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MODEL_DIR = os.path.join(PROJECT, "Assets", "Models")
SHOT_DIR = os.path.join(PROJECT, "Screenshots")
BLEND_PATH = os.path.join(MODEL_DIR, "Characters.blend")


# ----------------------------------------------------------------------------
#  Small helpers
# ----------------------------------------------------------------------------
def reset_scene():
    """Start from an empty file: no default cube, camera or light."""
    bpy.ops.wm.read_factory_settings(use_empty=True)


def material(name, rgba, roughness=0.6, metallic=0.0):
    """A simple principled material. Unity rebuilds its own URP materials later;
    these exist so the Blender preview renders look right."""
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = rgba
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    mat.diffuse_color = rgba          # used by the solid-shaded preview
    return mat


def sphere(name, location, scale, mat, segments=24, rings=12, smooth=True):
    """One UV sphere. segments/rings stay low: this is a low-poly character."""
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings,
                                        radius=1.0, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    if smooth:
        bpy.ops.object.shade_smooth()
    return obj


def cylinder(name, location, scale, mat, vertices=16, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=1.0, depth=1.0,
                                        location=location, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    return obj


def join(parts, name):
    """Merge the parts into one mesh so the prefab is a single object.
    The active object's material list is kept, and each part contributes one
    material slot, which is what Unity shows as sub-meshes."""
    bpy.ops.object.select_all(action='DESELECT')
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    merged = bpy.context.active_object
    merged.name = name
    merged.data.name = name

    # Apply the scale, then move the origin to the bottom centre of the mesh.
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bpy.context.scene.cursor.location = (0.0, 0.0, 0.0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    merged.location = (0.0, 0.0, 0.0)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return merged


# ----------------------------------------------------------------------------
#  The mole
# ----------------------------------------------------------------------------
def build_mole():
    body_mat = material("Mole_Body", (0.42, 0.26, 0.14, 1.0), 0.75)
    eye_mat = material("Mole_Eye", (0.96, 0.96, 0.94, 1.0), 0.35)
    pupil_mat = material("Mole_Pupil", (0.06, 0.05, 0.05, 1.0), 0.30)
    nose_mat = material("Mole_Nose", (0.85, 0.45, 0.50, 1.0), 0.55)

    parts = []

    # Body: a sphere squashed slightly wide, sitting on the ground plane (z = 0).
    parts.append(sphere("Body", (0.0, 0.0, 0.36),
                        (0.30, 0.30, 0.36), body_mat))

    # Ears: two small spheres on the sides of the head.
    for side, x in (("L", -0.20), ("R", 0.20)):
        parts.append(sphere("Ear_" + side, (x, 0.0, 0.67),
                            (0.075, 0.055, 0.075), nose_mat, segments=16, rings=8))

    # Eyes: white eyeballs on the front (-Y is the front in Blender), plus a pupil.
    for side, x in (("L", -0.11), ("R", 0.11)):
        parts.append(sphere("Eye_" + side, (x, -0.235, 0.47),
                            (0.072, 0.072, 0.072), eye_mat, segments=16, rings=8))
        parts.append(sphere("Pupil_" + side, (x, -0.285, 0.46),
                            (0.033, 0.033, 0.033), pupil_mat, segments=12, rings=6))

    # Nose: a small pink ball between and below the eyes.
    parts.append(sphere("Nose", (0.0, -0.295, 0.33),
                        (0.055, 0.05, 0.048), nose_mat, segments=16, rings=8))

    return join(parts, "Mole")


# ----------------------------------------------------------------------------
#  The bomb
# ----------------------------------------------------------------------------
def build_bomb():
    shell_mat = material("Bomb_Shell", (0.10, 0.10, 0.12, 1.0), 0.35, 0.15)
    band_mat = material("Bomb_Band", (0.30, 0.30, 0.34, 1.0), 0.40, 0.55)
    fuse_mat = material("Bomb_Fuse", (0.55, 0.38, 0.18, 1.0), 0.85)
    spark_mat = material("Bomb_Spark", (1.0, 0.72, 0.15, 1.0), 0.25)

    parts = []

    # Shell: a sphere of radius 0.34 resting on the ground, a little taller than wide.
    parts.append(sphere("Shell", (0.0, 0.0, 0.34),
                        (0.34, 0.34, 0.36), shell_mat))

    # Metal band around the waist of the bomb.
    band = cylinder("Band", (0.0, 0.0, 0.30), (0.35, 0.35, 0.05), band_mat,
                    vertices=24)
    parts.append(band)

    # Fuse: a thin tube leaving the top, tilted a little so it reads as a fuse.
    parts.append(cylinder("Fuse", (0.07, 0.0, 0.72), (0.035, 0.035, 0.20),
                          fuse_mat, vertices=10, rotation=(0.0, math.radians(18), 0.0)))

    # Spark on the tip.
    parts.append(sphere("Spark", (0.13, 0.0, 0.90), (0.05, 0.05, 0.05),
                        spark_mat, segments=12, rings=6))

    return join(parts, "Bomb")


# ----------------------------------------------------------------------------
#  Export and preview renders
# ----------------------------------------------------------------------------
def export_fbx(obj, path):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={'MESH'},
        apply_scale_options='FBX_SCALE_ALL',   # bake the object scale into the mesh
        bake_space_transform=True,             # the "Apply Transform" option
        mesh_smooth_type='FACE',
        use_mesh_modifiers=True,
        axis_forward='-Z',
        axis_up='Y',
        add_leaf_bones=False,
        path_mode='STRIP',
    )
    print("[build_models] wrote " + path)


def setup_preview_camera(target_height):
    """A 3/4 view camera plus two lights, used for the report screenshots."""
    bpy.ops.object.camera_add(location=(1.15, -1.45, 1.05),
                              rotation=(math.radians(68), 0.0, math.radians(38)))
    cam = bpy.context.active_object
    bpy.context.scene.camera = cam
    cam.data.lens = 60
    # Aim slightly above the ground so the character sits in the frame.
    cam.location.z += target_height * 0.25

    bpy.ops.object.light_add(type='SUN', location=(2.0, -2.5, 3.0),
                             rotation=(math.radians(50), 0.0, math.radians(35)))
    bpy.context.active_object.data.energy = 3.5
    bpy.ops.object.light_add(type='AREA', location=(-1.6, -1.2, 1.4),
                             rotation=(math.radians(65), 0.0, math.radians(-40)))
    bpy.context.active_object.data.energy = 60.0
    bpy.context.active_object.data.size = 2.0

    world = bpy.data.worlds.new("PreviewWorld")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.62, 0.68, 0.72, 1.0)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.7
    bpy.context.scene.world = world


def render(path, wireframe=False):
    scene = bpy.context.scene
    scene.render.resolution_x = 800
    scene.render.resolution_y = 800
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = path
    scene.render.engine = 'BLENDER_EEVEE'

    made = None
    if wireframe:
        # A wireframe render: copy the object, turn its faces into a wire cage and
        # hide the solid original. Simple and reliable, unlike the viewport overlay.
        obj = bpy.context.scene.objects.get("Mole") or bpy.context.scene.objects.get("Bomb")
        made = obj.copy()
        made.data = obj.data.copy()
        made.name = obj.name + "_Wire"
        bpy.context.collection.objects.link(made)
        mod = made.modifiers.new("Wire", 'WIREFRAME')
        mod.thickness = 0.012
        solid = made.data.materials[:]
        wire_mat = material("Wire", (0.05, 0.05, 0.06, 1.0), 0.4)
        made.data.materials.clear()
        made.data.materials.append(wire_mat)
        obj.hide_render = True

    bpy.ops.render.render(write_still=True)
    print("[build_models] wrote " + path)

    if made is not None:
        obj = bpy.context.scene.objects.get(made.name.replace("_Wire", ""))
        obj.hide_render = False
        bpy.data.objects.remove(made, do_unlink=True)


def main():
    os.makedirs(MODEL_DIR, exist_ok=True)
    os.makedirs(SHOT_DIR, exist_ok=True)

    # ---- Mole -------------------------------------------------------------
    reset_scene()
    mole = build_mole()
    setup_preview_camera(target_height=0.72)
    export_fbx(mole, os.path.join(MODEL_DIR, "Mole.fbx"))
    render(os.path.join(SHOT_DIR, "model_mole.png"))
    render(os.path.join(SHOT_DIR, "model_mole_wireframe.png"), wireframe=True)

    # ---- Bomb -------------------------------------------------------------
    reset_scene()
    bomb = build_bomb()
    setup_preview_camera(target_height=0.72)
    export_fbx(bomb, os.path.join(MODEL_DIR, "Bomb.fbx"))
    render(os.path.join(SHOT_DIR, "model_bomb.png"))
    render(os.path.join(SHOT_DIR, "model_bomb_wireframe.png"), wireframe=True)

    # Keep the source file next to the FBX so the student can open and tweak it.
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    print("[build_models] done")


if __name__ == "__main__":
    main()
