import math
import os

import bpy
import mathutils


ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
MODEL_DIR = os.path.join(ROOT, "Resources", "Models", "Cockroach")
FBX_PATH = os.path.join(MODEL_DIR, "Cockroach_LowPoly.fbx")
BLEND_PATH = os.path.join(MODEL_DIR, "Cockroach_LowPoly.blend")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def material(name, color):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    return mat


def add_uv_sphere(name, location, scale, mat, segments=18, rings=9):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    return obj


def add_cylinder_between(name, start, end, radius, mat, vertices=8):
    sx, sy, sz = start
    ex, ey, ez = end
    mid = ((sx + ex) * 0.5, (sy + ey) * 0.5, (sz + ez) * 0.5)
    dx, dy, dz = ex - sx, ey - sy, ez - sz
    length = math.sqrt(dx * dx + dy * dy + dz * dz)

    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=length, location=mid)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)

    direction = mathutils.Vector((dx, dy, dz)).normalized()
    quat = direction.to_track_quat("Z", "Y")
    obj.rotation_euler = quat.to_euler()
    return obj


def add_leg(name, side, y, mat):
    hip = (0.15 * side, y, 0.09)
    knee = (0.35 * side, y + 0.025, 0.0)
    ankle = (0.56 * side, y + 0.075, -0.07)
    foot = (0.75 * side, y + 0.13, -0.085)
    upper = add_cylinder_between(f"{name}_upper", hip, knee, 0.017, mat, 7)
    mid = add_cylinder_between(f"{name}_middle", knee, ankle, 0.014, mat, 7)
    lower = add_cylinder_between(f"{name}_lower", ankle, foot, 0.01, mat, 7)
    claw = add_cylinder_between(f"{name}_claw", foot, (foot[0] + 0.05 * side, foot[1] + 0.05, foot[2] - 0.01), 0.006, mat, 6)
    return [upper, mid, lower, claw]


def add_antenna(name, side, mat):
    base = (0.075 * side, 0.64, 0.15)
    mid = (0.25 * side, 0.86, 0.24)
    tip = (0.52 * side, 1.08, 0.22)
    return [
        add_cylinder_between(f"{name}_base", base, mid, 0.011, mat, 7),
        add_cylinder_between(f"{name}_tip", mid, tip, 0.007, mat, 7),
    ]


def add_plate(name, location, scale, mat):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    bevel = obj.modifiers.new("soft bevel", "BEVEL")
    bevel.width = 0.025
    bevel.segments = 2
    obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def build_cockroach():
    shell = material("Glossy Chestnut Shell", (0.18, 0.075, 0.025, 1.0))
    shell_dark = material("Dark Shell Edges", (0.075, 0.035, 0.017, 1.0))
    shell_highlight = material("Amber Shell Highlights", (0.46, 0.19, 0.055, 1.0))
    belly = material("Warm Brown Belly", (0.25, 0.12, 0.045, 1.0))
    leg_mat = material("Dark Jointed Legs", (0.045, 0.025, 0.014, 1.0))
    eye_mat = material("Black Eyes", (0.005, 0.004, 0.003, 1.0))

    body = add_uv_sphere("abdomen", (0, -0.13, 0.105), (0.25, 0.5, 0.105), shell, 22, 10)
    thorax = add_uv_sphere("thorax", (0, 0.28, 0.13), (0.205, 0.265, 0.105), belly, 20, 9)
    head = add_uv_sphere("head", (0, 0.56, 0.14), (0.14, 0.13, 0.08), shell_dark, 18, 8)

    left_wing = add_plate("left_wing_cover", (-0.07, -0.12, 0.205), (0.095, 0.39, 0.014), shell_highlight)
    right_wing = add_plate("right_wing_cover", (0.07, -0.12, 0.205), (0.095, 0.39, 0.014), shell_highlight)
    center_ridge = add_plate("center_shell_ridge", (0, -0.12, 0.224), (0.014, 0.41, 0.014), shell_dark)
    pronotum = add_plate("neck_shield", (0, 0.38, 0.215), (0.17, 0.1, 0.018), shell_highlight)

    segment_parts = []
    for index, y in enumerate((-0.45, -0.31, -0.17, -0.03, 0.11), start=1):
        segment_parts.append(add_plate(f"abdomen_segment_{index}", (0, y, 0.226), (0.22 - index * 0.01, 0.012, 0.012), shell_dark))

    add_uv_sphere("left_eye", (-0.062, 0.655, 0.168), (0.027, 0.023, 0.019), eye_mat, 10, 5)
    add_uv_sphere("right_eye", (0.062, 0.655, 0.168), (0.027, 0.023, 0.019), eye_mat, 10, 5)
    left_mandible = add_cylinder_between("left_mandible", (-0.04, 0.66, 0.11), (-0.12, 0.73, 0.09), 0.008, leg_mat, 6)
    right_mandible = add_cylinder_between("right_mandible", (0.04, 0.66, 0.11), (0.12, 0.73, 0.09), 0.008, leg_mat, 6)

    parts = [body, thorax, head, left_wing, right_wing, center_ridge, pronotum, left_mandible, right_mandible]
    parts.extend(segment_parts)
    for idx, y in enumerate((-0.18, 0.08, 0.34), start=1):
        parts.extend(add_leg(f"left_leg_{idx}", -1, y, leg_mat))
        parts.extend(add_leg(f"right_leg_{idx}", 1, y, leg_mat))

    parts.extend(add_antenna("left_antenna", -1, leg_mat))
    parts.extend(add_antenna("right_antenna", 1, leg_mat))

    bpy.ops.object.empty_add(type="PLAIN_AXES", location=(0, 0, 0))
    root = bpy.context.object
    root.name = "Cockroach_LowPoly"

    for obj in parts:
        obj.parent = root

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for obj in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(
        filepath=FBX_PATH,
        use_selection=True,
        apply_scale_options="FBX_SCALE_ALL",
        object_types={"EMPTY", "MESH"},
        axis_forward="-Z",
        axis_up="Y",
    )


if __name__ == "__main__":
    os.makedirs(MODEL_DIR, exist_ok=True)
    clear_scene()
    build_cockroach()
    print(f"Saved {FBX_PATH}")
