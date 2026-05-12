import math
import os

import bpy


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


def add_uv_sphere(name, location, scale, mat, segments=16, rings=8):
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
    hip = (0.16 * side, y, 0.08)
    knee = (0.42 * side, y + 0.03, -0.03)
    foot = (0.68 * side, y + 0.08, -0.09)
    upper = add_cylinder_between(f"{name}_upper", hip, knee, 0.018, mat, 6)
    lower = add_cylinder_between(f"{name}_lower", knee, foot, 0.014, mat, 6)
    return [upper, lower]


def add_antenna(name, side, mat):
    base = (0.08 * side, 0.63, 0.13)
    tip = (0.45 * side, 1.04, 0.23)
    return add_cylinder_between(name, base, tip, 0.01, mat, 6)


def build_cockroach():
    shell = material("Cockroach Shell", (0.11, 0.055, 0.025, 1.0))
    belly = material("Warm Brown Belly", (0.23, 0.12, 0.055, 1.0))
    leg_mat = material("Dark Legs", (0.055, 0.032, 0.02, 1.0))
    eye_mat = material("Black Eyes", (0.005, 0.004, 0.003, 1.0))

    body = add_uv_sphere("abdomen", (0, -0.12, 0.11), (0.24, 0.48, 0.11), shell)
    thorax = add_uv_sphere("thorax", (0, 0.28, 0.12), (0.20, 0.26, 0.10), belly)
    head = add_uv_sphere("head", (0, 0.55, 0.13), (0.14, 0.13, 0.08), shell)

    add_uv_sphere("left_eye", (-0.065, 0.65, 0.17), (0.024, 0.024, 0.018), eye_mat, 8, 4)
    add_uv_sphere("right_eye", (0.065, 0.65, 0.17), (0.024, 0.024, 0.018), eye_mat, 8, 4)

    parts = [body, thorax, head]
    for idx, y in enumerate((-0.18, 0.08, 0.34), start=1):
        parts.extend(add_leg(f"left_leg_{idx}", -1, y, leg_mat))
        parts.extend(add_leg(f"right_leg_{idx}", 1, y, leg_mat))

    parts.append(add_antenna("left_antenna", -1, leg_mat))
    parts.append(add_antenna("right_antenna", 1, leg_mat))

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
    import mathutils

    os.makedirs(MODEL_DIR, exist_ok=True)
    clear_scene()
    build_cockroach()
    print(f"Saved {FBX_PATH}")
