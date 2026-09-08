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


def material(name, color, roughness=0.72):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = 0.0
    return mat


def soften(obj, bevel_width=0.0, bevel_segments=1):
    try:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.shade_smooth()
        obj.select_set(False)
    except RuntimeError:
        pass

    if bevel_width > 0.0:
        bevel = obj.modifiers.new("cartoon soft bevel", "BEVEL")
        bevel.width = bevel_width
        bevel.segments = bevel_segments
        obj.modifiers.new("cartoon weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_uv_sphere(name, location, scale, mat, segments=18, rings=9):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    return soften(obj)


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
    return soften(obj, radius * 0.18, 1)


def add_leg(name, side, y, mat, foot_mat):
    hip = (0.13 * side, y, 0.1)
    knee = (0.3 * side, y + 0.015, 0.03)
    ankle = (0.47 * side, y + 0.055, -0.025)
    foot = (0.57 * side, y + 0.1, -0.038)
    return [
        add_cylinder_between(f"{name}_upper", hip, knee, 0.021, mat, 8),
        add_cylinder_between(f"{name}_middle", knee, ankle, 0.018, mat, 8),
        add_cylinder_between(f"{name}_lower", ankle, foot, 0.014, mat, 8),
        add_uv_sphere(f"{name}_toe_pad", foot, (0.048, 0.032, 0.018), foot_mat, 10, 5),
    ]


def add_antenna(name, side, mat, tip_mat):
    base = (0.1 * side, 0.62, 0.22)
    mid = (0.24 * side, 0.83, 0.33)
    tip = (0.42 * side, 1.03, 0.34)
    return [
        add_cylinder_between(f"{name}_base", base, mid, 0.012, mat, 8),
        add_cylinder_between(f"{name}_tip", mid, tip, 0.008, mat, 8),
        add_uv_sphere(f"{name}_round_tip", tip, (0.026, 0.026, 0.026), tip_mat, 10, 5),
    ]


def add_plate(name, location, scale, mat, bevel_width=0.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    return soften(obj, bevel_width, 2)


def add_eye(name, side, white_mat, pupil_mat, shine_mat):
    white = add_uv_sphere(f"{name}_white", (0.062 * side, 0.65, 0.175), (0.044, 0.032, 0.034), white_mat, 16, 8)
    pupil = add_uv_sphere(f"{name}_pupil", (0.072 * side, 0.675, 0.18), (0.017, 0.01, 0.017), pupil_mat, 12, 6)
    shine = add_uv_sphere(f"{name}_sparkle", (0.078 * side, 0.682, 0.192), (0.006, 0.004, 0.006), shine_mat, 8, 4)
    return [white, pupil, shine]


def build_cockroach():
    shell = material("Cartoon Caramel Shell", (0.72, 0.36, 0.12, 1.0))
    shell_shadow = material("Soft Cocoa Outline", (0.25, 0.13, 0.055, 1.0))
    shell_highlight = material("Honey Cream Highlights", (0.98, 0.68, 0.28, 1.0))
    belly = material("Warm Biscuit Belly", (0.82, 0.46, 0.2, 1.0))
    leg_mat = material("Rounded Cocoa Legs", (0.31, 0.17, 0.08, 1.0))
    foot_mat = material("Soft Toe Pads", (0.55, 0.3, 0.14, 1.0))
    eye_white = material("Big Eye Whites", (0.98, 0.95, 0.88, 1.0))
    pupil = material("Friendly Navy Pupils", (0.025, 0.05, 0.09, 1.0))
    shine = material("Eye Sparkles", (1.0, 1.0, 0.92, 1.0))
    blush = material("Tiny Peach Cheeks", (1.0, 0.48, 0.35, 1.0))

    body = add_uv_sphere("round_abdomen", (0, -0.13, 0.115), (0.285, 0.51, 0.14), shell, 24, 12)
    thorax = add_uv_sphere("squishy_thorax", (0, 0.29, 0.135), (0.22, 0.275, 0.12), belly, 22, 10)
    head = add_uv_sphere("friendly_head", (0, 0.56, 0.15), (0.155, 0.14, 0.095), shell, 20, 10)

    left_wing = add_plate("left_caramel_wing_cover", (-0.072, -0.12, 0.245), (0.095, 0.39, 0.016), shell_highlight, 0.035)
    right_wing = add_plate("right_caramel_wing_cover", (0.072, -0.12, 0.245), (0.095, 0.39, 0.016), shell_highlight, 0.035)
    center_ridge = add_plate("soft_center_shell_ridge", (0, -0.12, 0.266), (0.012, 0.4, 0.012), shell_shadow, 0.012)
    pronotum = add_plate("rounded_neck_shield", (0, 0.38, 0.24), (0.17, 0.1, 0.018), shell_highlight, 0.03)

    parts = [body, thorax, head, left_wing, right_wing, center_ridge, pronotum]

    for index, y in enumerate((-0.42, -0.27, -0.12, 0.03), start=1):
        width = 0.22 - index * 0.018
        parts.append(add_plate(f"soft_abdomen_stripe_{index}", (0, y, 0.266), (width, 0.01, 0.011), shell_shadow, 0.008))

    for index, (x, y) in enumerate(((-0.12, -0.32), (0.11, -0.22), (-0.08, 0.02), (0.1, 0.12)), start=1):
        parts.append(add_uv_sphere(f"honey_spot_{index}", (x, y, 0.282), (0.022, 0.03, 0.009), shell_highlight, 10, 5))

    parts.extend(add_eye("left_eye", -1, eye_white, pupil, shine))
    parts.extend(add_eye("right_eye", 1, eye_white, pupil, shine))
    parts.append(add_uv_sphere("left_blush_dot", (-0.095, 0.61, 0.14), (0.018, 0.012, 0.008), blush, 8, 4))
    parts.append(add_uv_sphere("right_blush_dot", (0.095, 0.61, 0.14), (0.018, 0.012, 0.008), blush, 8, 4))

    smile_left = add_cylinder_between("tiny_smile_left", (-0.035, 0.67, 0.12), (0, 0.69, 0.112), 0.0045, shell_shadow, 6)
    smile_right = add_cylinder_between("tiny_smile_right", (0, 0.69, 0.112), (0.035, 0.67, 0.12), 0.0045, shell_shadow, 6)
    parts.extend([smile_left, smile_right])

    for idx, y in enumerate((-0.17, 0.08, 0.33), start=1):
        parts.extend(add_leg(f"left_stubby_leg_{idx}", -1, y, leg_mat, foot_mat))
        parts.extend(add_leg(f"right_stubby_leg_{idx}", 1, y, leg_mat, foot_mat))

    parts.extend(add_antenna("left_bouncy_antenna", -1, leg_mat, shell_highlight))
    parts.extend(add_antenna("right_bouncy_antenna", 1, leg_mat, shell_highlight))

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
