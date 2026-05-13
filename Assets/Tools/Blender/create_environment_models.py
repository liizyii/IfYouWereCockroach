import os
import math

import bpy


ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
MODEL_DIR = os.path.join(ROOT, "Resources", "Models", "Environment")
HUMAN_DIR = os.path.join(ROOT, "Resources", "Models", "Human")
BLEND_PATH = os.path.join(MODEL_DIR, "Apartment_Prototype_Models.blend")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def material(name, color):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    return mat


def soften(obj, amount=0.015, segments=3):
    bevel = obj.modifiers.new("soft_edge", "BEVEL")
    bevel.width = max(0.006, min(amount, 0.055))
    bevel.segments = segments
    bevel.affect = "EDGES"
    obj.modifiers.new("weighted_normals", "WEIGHTED_NORMAL")
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.shade_smooth()
    obj.select_set(False)
    return obj


def cube(name, location, scale, mat, parent):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    obj.parent = parent
    soften(obj, min(scale) * 0.38 if min(scale) > 0 else 0.01, 4)
    return obj


def sphere(name, location, scale, mat, parent, segments=12, rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    obj.parent = parent
    bpy.ops.object.shade_smooth()
    return obj


def cylinder(name, location, radius, depth, mat, parent, vertices=12, rotation=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    if rotation is not None:
        obj.rotation_euler = rotation
    obj.data.materials.append(mat)
    obj.parent = parent
    soften(obj, 0.008, 1)
    return obj


def wavy_surface(name, center, size, mat, parent, cols=12, rows=8, ripple=0.018):
    verts = []
    faces = []
    for row in range(rows + 1):
        v = row / rows
        for col in range(cols + 1):
            u = col / cols
            x = center[0] + (u - 0.5) * size[0]
            y = center[1] + (v - 0.5) * size[1]
            z = center[2] + math.sin(u * math.pi * 3.0) * ripple + math.cos(v * math.pi * 2.0) * ripple * 0.45
            verts.append((x, y, z))

    for row in range(rows):
        for col in range(cols):
            a = row * (cols + 1) + col
            faces.append((a, a + 1, a + cols + 2, a + cols + 1))

    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    obj.data.materials.append(mat)
    obj.parent = parent
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.shade_smooth()
    obj.select_set(False)
    return obj


def root(name):
    bpy.ops.object.empty_add(type="PLAIN_AXES", location=(0, 0, 0))
    obj = bpy.context.object
    obj.name = name
    return obj


def export(root_obj, path):
    bpy.ops.object.select_all(action="DESELECT")
    root_obj.select_set(True)
    for child in root_obj.children_recursive:
        child.select_set(True)
    bpy.context.view_layer.objects.active = root_obj
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        apply_scale_options="FBX_SCALE_ALL",
        object_types={"EMPTY", "MESH"},
        axis_forward="-Z",
        axis_up="Y",
    )


def build_fridge(mats):
    r = root("Fridge_LowPoly")
    cube("body", (0, 0, 0.55), (0.5, 0.42, 0.55), mats["white"], r)
    cube("left_side_shadow", (-0.515, 0, 0.55), (0.012, 0.42, 0.54), mats["cool_shadow"], r)
    cube("right_side_shadow", (0.515, 0, 0.55), (0.012, 0.42, 0.54), mats["cool_shadow"], r)
    cube("top_door", (0, -0.012, 0.78), (0.47, 0.02, 0.23), mats["cool_white"], r)
    cube("bottom_door", (0, -0.012, 0.33), (0.47, 0.02, 0.3), mats["cool_white"], r)
    cube("door_split", (0, -0.052, 0.57), (0.47, 0.012, 0.012), mats["seal"], r)
    cube("handle_top", (0.33, -0.04, 0.78), (0.025, 0.035, 0.18), mats["metal"], r)
    cube("handle_bottom", (0.33, -0.04, 0.32), (0.025, 0.035, 0.22), mats["metal"], r)
    cube("rubber_seal_top", (0, -0.037, 0.78), (0.49, 0.01, 0.25), mats["seal"], r)
    cube("rubber_seal_bottom", (0, -0.037, 0.33), (0.49, 0.01, 0.32), mats["seal"], r)
    for i, z in enumerate((0.1, 0.14, 0.18)):
        cube(f"lower_vent_{i}", (-0.18, -0.046, z), (0.18, 0.008, 0.012), mats["metal_dark"], r)
    cube("red_magnet", (-0.22, -0.05, 0.55), (0.045, 0.009, 0.04), mats["red"], r)
    cube("yellow_note", (-0.09, -0.051, 0.44), (0.065, 0.009, 0.055), mats["paper"], r)
    cube("shopping_note", (0.08, -0.052, 0.28), (0.07, 0.008, 0.045), mats["paper_white"], r)
    for x in (-0.34, 0.34):
        for y in (-0.28, 0.28):
            cylinder("fridge_foot", (x, y, 0.02), 0.035, 0.04, mats["seal"], r, 12)
    return r


def build_stove(mats):
    r = root("Stove_LowPoly")
    cube("base", (0, 0, 0.38), (0.5, 0.42, 0.38), mats["dark"], r)
    cube("top", (0, 0, 0.78), (0.53, 0.45, 0.04), mats["metal"], r)
    cube("back_panel", (0, 0.39, 0.95), (0.53, 0.055, 0.16), mats["dark"], r)
    for x in (-0.2, 0.2):
        for y in (-0.14, 0.14):
            cylinder("burner", (x, y, 0.84), 0.08, 0.025, mats["black"], r, 16)
            cylinder("burner_ring", (x, y, 0.865), 0.105, 0.012, mats["metal_dark"], r, 24)
    cube("oven_window", (0, -0.43, 0.38), (0.32, 0.015, 0.18), mats["glass"], r)
    cube("oven_inner_glow", (0, -0.446, 0.38), (0.27, 0.008, 0.13), mats["warm_glow"], r)
    cube("oven_handle", (0, -0.46, 0.62), (0.34, 0.025, 0.025), mats["metal"], r)
    for i in range(4):
        cylinder("control_knob", (-0.27 + i * 0.18, -0.46, 0.72), 0.028, 0.025, mats["metal"], r, 16)
    cylinder("pan_base", (-0.2, -0.14, 0.9), 0.13, 0.035, mats["black"], r, 24)
    cube("pan_handle", (-0.36, -0.14, 0.9), (0.11, 0.025, 0.018), mats["black"], r)
    return r


def add_chair(parent, mats, x, y, rot=0):
    sphere("chair_seat_pad", (x, y, 0.4), (0.2, 0.17, 0.04), mats["chair_pad"], parent, 24, 10)
    cube("chair_seat", (x, y, 0.36), (0.18, 0.16, 0.025), mats["wood"], parent)
    cube("chair_back_frame", (x, y + 0.13, 0.62), (0.2, 0.025, 0.26), mats["wood_dark"], parent)
    for lx in (-0.07, 0.0, 0.07):
        cylinder("chair_back_slat", (x + lx, y + 0.105, 0.62), 0.01, 0.44, mats["wood"], parent, 10)
    for lx in (-0.13, 0.13):
        for ly in (-0.1, 0.1):
            cylinder("chair_leg", (x + lx, y + ly, 0.18), 0.018, 0.36, mats["wood_dark"], parent, 10)


def build_table(mats):
    r = root("DiningTable_LowPoly")
    cube("thick_rounded_top", (0, 0, 0.72), (0.54, 0.37, 0.055), mats["wood"], r)
    cube("beveled_top_lip", (0, 0, 0.775), (0.58, 0.41, 0.018), mats["wood_dark"], r)
    wavy_surface("cloth_runner_soft", (0, 0, 0.797), (0.18, 0.62), mats["cloth_tan"], r, 10, 18, 0.006)
    cylinder("dinner_plate_outer", (0.14, -0.05, 0.822), 0.115, 0.018, mats["porcelain"], r, 36)
    cylinder("dinner_plate_inner", (0.14, -0.05, 0.836), 0.078, 0.006, mats["plate_shadow"], r, 36)
    cylinder("bowl", (-0.08, -0.08, 0.835), 0.078, 0.07, mats["porcelain"], r, 28)
    sphere("bowl_food", (-0.08, -0.08, 0.884), (0.06, 0.045, 0.018), mats["stain_red"], r, 16, 6)
    cylinder("cup", (-0.18, 0.06, 0.865), 0.048, 0.11, mats["cool_white"], r, 24)
    cylinder("cup_coffee", (-0.18, 0.06, 0.923), 0.041, 0.008, mats["coffee"], r, 24)
    cube("fork_handle", (0.28, -0.08, 0.84), (0.012, 0.12, 0.006), mats["metal"], r)
    for x in (0.266, 0.276, 0.286):
        cube("fork_tine", (x, -0.145, 0.846), (0.004, 0.035, 0.004), mats["metal"], r)
    cube("knife_blade_table", (0.31, 0.05, 0.84), (0.016, 0.13, 0.006), mats["metal"], r)
    cube("knife_handle_table", (0.31, 0.13, 0.842), (0.022, 0.055, 0.008), mats["black"], r)
    for x in (-0.42, 0.42):
        for y in (-0.27, 0.27):
            cylinder("rounded_leg", (x, y, 0.34), 0.026, 0.68, mats["wood_dark"], r, 12)
    cube("front_crossbar", (0, -0.27, 0.42), (0.42, 0.018, 0.025), mats["wood_dark"], r)
    cube("back_crossbar", (0, 0.27, 0.42), (0.42, 0.018, 0.025), mats["wood_dark"], r)
    for y in (-0.16, -0.04, 0.08, 0.2):
        cube("wood_grain_line", (0, y, 0.768), (0.45, 0.006, 0.004), mats["wood_grain"], r)
    add_chair(r, mats, -0.72, 0, 1.5708)
    add_chair(r, mats, 0.72, 0, -1.5708)
    add_chair(r, mats, 0, -0.58, 0)
    return r


def build_sofa(mats):
    r = root("Sofa_LowPoly")
    cube("sofa_lower_shadow_base", (0, -0.02, 0.2), (0.52, 0.42, 0.1), mats["fabric_blue_dark"], r)
    sphere("soft_seat_base", (0, -0.02, 0.33), (0.56, 0.45, 0.18), mats["fabric_blue"], r, 32, 14)
    sphere("rounded_back_cushion", (0, 0.31, 0.58), (0.54, 0.085, 0.36), mats["fabric_blue"], r, 28, 12)
    sphere("left_arm_round", (-0.48, 0, 0.44), (0.07, 0.43, 0.33), mats["fabric_blue_dark"], r, 18, 10)
    sphere("right_arm_round", (0.48, 0, 0.44), (0.07, 0.43, 0.33), mats["fabric_blue_dark"], r, 18, 10)
    sphere("cushion_left", (-0.24, -0.06, 0.49), (0.23, 0.31, 0.065), mats["fabric_blue_light"], r, 28, 10)
    sphere("cushion_right", (0.24, -0.06, 0.49), (0.23, 0.31, 0.065), mats["fabric_blue_light"], r, 28, 10)
    wavy_surface("left_cushion_fabric_surface", (-0.24, -0.06, 0.56), (0.42, 0.52), mats["fabric_blue_light"], r, 12, 12, 0.008)
    wavy_surface("right_cushion_fabric_surface", (0.24, -0.06, 0.56), (0.42, 0.52), mats["fabric_blue_light"], r, 12, 12, 0.008)
    cube("cushion_gap", (0, -0.06, 0.515), (0.012, 0.31, 0.015), mats["fabric_blue_dark"], r)
    cube("front_seam", (0, -0.29, 0.46), (0.42, 0.012, 0.015), mats["fabric_blue_dark"], r)
    sphere("throw_pillow", (-0.24, 0.25, 0.62), (0.135, 0.05, 0.125), mats["red"], r, 20, 10)
    sphere("folded_blanket", (0.22, 0.2, 0.68), (0.18, 0.06, 0.11), mats["blanket"], r, 20, 10)
    for x in (-0.22, 0.22):
        cube("fabric_stitch", (x, -0.3, 0.5), (0.16, 0.008, 0.01), mats["fabric_thread"], r)
    for x in (-0.33, -0.16, 0.16, 0.33):
        cube("vertical_back_fabric_tuck", (x, 0.235, 0.61), (0.01, 0.01, 0.22), mats["fabric_thread"], r)
    for x in (-0.38, 0.38):
        for y in (-0.28, 0.25):
            cube("sofa_leg", (x, y, 0.08), (0.03, 0.03, 0.08), mats["wood_dark"], r)
    return r


def build_coffee_table(mats):
    r = root("CoffeeTable_LowPoly")
    cube("top", (0, 0, 0.38), (0.5, 0.36, 0.035), mats["wood"], r)
    cube("shelf", (0, 0, 0.19), (0.43, 0.3, 0.025), mats["wood_dark"], r)
    cube("book_blue", (-0.14, 0.04, 0.44), (0.16, 0.1, 0.018), mats["book_blue"], r)
    cube("book_red", (-0.12, 0.04, 0.465), (0.15, 0.095, 0.014), mats["red"], r)
    cube("remote", (0.19, -0.1, 0.445), (0.045, 0.16, 0.012), mats["black"], r)
    cylinder("mug", (0.18, 0.15, 0.49), 0.045, 0.075, mats["porcelain"], r, 18)
    cylinder("vase", (0.02, 0.12, 0.5), 0.035, 0.12, mats["glass_blue"], r, 18)
    cube("magazine", (-0.22, -0.15, 0.44), (0.13, 0.18, 0.01), mats["paper_white"], r)
    for x in (-0.4, 0.4):
        for y in (-0.26, 0.26):
            cylinder("rounded_leg", (x, y, 0.19), 0.022, 0.36, mats["wood_dark"], r, 10)
    return r


def build_bed(mats):
    r = root("Bed_LowPoly")
    cube("wood_frame_rounded", (0, 0, 0.22), (0.52, 0.5, 0.16), mats["wood_dark"], r)
    sphere("rounded_mattress", (0, 0, 0.4), (0.5, 0.47, 0.115), mats["sheet"], r, 30, 12)
    wavy_surface("slightly_wrinkled_sheet_top", (0, 0.02, 0.52), (0.88, 0.78), mats["sheet"], r, 16, 12, 0.009)
    sphere("soft_blanket", (0, -0.07, 0.54), (0.49, 0.3, 0.052), mats["blanket"], r, 28, 10)
    wavy_surface("blanket_wrinkled_top", (0, -0.08, 0.59), (0.86, 0.46), mats["blanket"], r, 14, 8, 0.014)
    cube("blanket_fold", (0, 0.12, 0.555), (0.47, 0.035, 0.025), mats["blanket_dark"], r)
    cube("headboard", (0, 0.52, 0.5), (0.52, 0.055, 0.36), mats["wood_dark"], r)
    cube("headboard_top", (0, 0.54, 0.72), (0.56, 0.07, 0.05), mats["wood"], r)
    sphere("pillow_left", (-0.17, 0.3, 0.57), (0.16, 0.12, 0.055), mats["pillow"], r, 24, 10)
    sphere("pillow_right", (0.17, 0.3, 0.57), (0.16, 0.12, 0.055), mats["pillow"], r, 24, 10)
    cube("pillow_left_indent", (-0.17, 0.2, 0.592), (0.12, 0.01, 0.006), mats["pillow_shadow"], r)
    cube("pillow_right_indent", (0.17, 0.2, 0.592), (0.12, 0.01, 0.006), mats["pillow_shadow"], r)
    for x in (-0.18, 0.0, 0.18):
        cube("blanket_wrinkle", (x, -0.1, 0.56), (0.012, 0.24, 0.01), mats["blanket_dark"], r)
    for x in (-0.38, 0.38):
        for y in (-0.35, 0.35):
            cylinder("bed_leg", (x, y, 0.08), 0.025, 0.16, mats["wood_dark"], r, 10)
    return r


def build_sink(mats):
    r = root("Sink_LowPoly")
    cube("cabinet", (0, 0, 0.35), (0.45, 0.36, 0.35), mats["white"], r)
    cube("basin", (0, 0, 0.72), (0.42, 0.32, 0.06), mats["porcelain"], r)
    cylinder("faucet", (0, -0.05, 0.86), 0.035, 0.18, mats["metal"], r, 12)
    cube("drain", (0, -0.02, 0.79), (0.055, 0.055, 0.01), mats["dark"], r)
    cylinder("left_tap", (-0.12, -0.09, 0.84), 0.025, 0.035, mats["metal"], r, 12)
    cylinder("right_tap", (0.12, -0.09, 0.84), 0.025, 0.035, mats["metal"], r, 12)
    cube("soap_bottle", (0.26, 0.12, 0.85), (0.045, 0.035, 0.09), mats["soap"], r)
    cube("cabinet_left_door", (-0.12, -0.02, 0.36), (0.17, 0.015, 0.24), mats["cool_white"], r)
    cube("cabinet_right_door", (0.12, -0.02, 0.36), (0.17, 0.015, 0.24), mats["cool_white"], r)
    cube("cabinet_handle_left", (-0.05, -0.04, 0.38), (0.012, 0.018, 0.12), mats["metal"], r)
    cube("cabinet_handle_right", (0.05, -0.04, 0.38), (0.012, 0.018, 0.12), mats["metal"], r)
    cylinder("toothbrush_cup", (-0.28, 0.1, 0.84), 0.035, 0.08, mats["glass_blue"], r, 16)
    return r


def build_kitchen_counter(mats):
    r = root("KitchenCounter_LowPoly")
    cube("cabinet_body", (0, 0, 0.38), (0.6, 0.28, 0.38), mats["wood"], r)
    cube("stone_countertop", (0, 0, 0.79), (0.66, 0.34, 0.055), mats["counter_stone"], r)
    cube("backsplash", (0, 0.29, 0.98), (0.66, 0.035, 0.19), mats["tile_warm"], r)
    for x in (-0.32, 0, 0.32):
        cube("tile_seam_vertical", (x, 0.267, 0.98), (0.006, 0.008, 0.17), mats["tile_grout"], r)
    for z in (0.92, 1.02):
        cube("tile_seam_horizontal", (0, 0.266, z), (0.62, 0.008, 0.006), mats["tile_grout"], r)
    for x in (-0.36, 0, 0.36):
        cube("lower_door", (x, -0.292, 0.36), (0.18, 0.016, 0.28), mats["wood_dark"], r)
        cube("door_handle", (x + 0.055, -0.314, 0.39), (0.018, 0.018, 0.12), mats["metal"], r)
    for x in (-0.18, 0.18):
        cube("drawer_front", (x, -0.294, 0.66), (0.28, 0.018, 0.1), mats["wood_dark"], r)
        cube("drawer_handle", (x, -0.318, 0.66), (0.12, 0.018, 0.014), mats["metal"], r)
    cylinder("cutting_board", (-0.24, -0.1, 0.86), 0.13, 0.018, mats["cutting_board"], r, 28)
    cube("knife_blade", (-0.16, -0.1, 0.885), (0.12, 0.018, 0.008), mats["metal"], r)
    cube("knife_handle", (-0.29, -0.1, 0.885), (0.055, 0.022, 0.012), mats["black"], r)
    cylinder("sauce_stain", (0.2, -0.02, 0.86), 0.075, 0.008, mats["stain_red"], r, 24)
    for i, x in enumerate((-0.02, 0.04, 0.11, 0.28)):
        sphere("crumb", (x, -0.14 + i * 0.035, 0.875), (0.025, 0.018, 0.012), mats["crumb"], r, 8, 4)
    cylinder("small_pan", (0.36, 0.08, 0.87), 0.12, 0.035, mats["black"], r, 24)
    cube("pan_handle", (0.52, 0.08, 0.87), (0.14, 0.025, 0.018), mats["black"], r)
    return r


def build_clutter(mats):
    r = root("Clutter_LowPoly")
    cube("box", (-0.12, 0.02, 0.22), (0.24, 0.2, 0.22), mats["cardboard"], r)
    cylinder("can", (0.22, -0.1, 0.18), 0.09, 0.36, mats["metal"], r, 10)
    sphere("cloth", (0.08, 0.16, 0.12), (0.18, 0.1, 0.08), mats["cloth"], r, 10, 5)
    cube("paper_label", (-0.12, -0.18, 0.28), (0.13, 0.01, 0.08), mats["paper"], r)
    cube("small_bottle", (0.08, -0.18, 0.26), (0.055, 0.055, 0.2), mats["soap"], r)
    cube("open_flap_left", (-0.24, -0.02, 0.42), (0.13, 0.018, 0.07), mats["cardboard"], r)
    cube("open_flap_right", (0.0, -0.02, 0.42), (0.13, 0.018, 0.07), mats["cardboard"], r)
    cylinder("crushed_can", (0.32, 0.12, 0.08), 0.07, 0.12, mats["metal_dark"], r, 12)
    return r


def build_bookshelf(mats):
    r = root("Bookshelf_LowPoly")
    cube("outer_frame", (0, 0, 0.82), (0.5, 0.18, 0.82), mats["wood_dark"], r)
    cube("back_panel", (0, 0.08, 0.82), (0.46, 0.025, 0.78), mats["wood"], r)
    for z in (0.32, 0.6, 0.88, 1.16):
        cube("shelf_board", (0, -0.02, z), (0.46, 0.16, 0.025), mats["wood"], r)
    for i, x in enumerate((-0.32, -0.24, -0.15, -0.04, 0.06, 0.18, 0.29)):
        height = 0.18 + (i % 3) * 0.035
        color = ("book_blue", "red", "book_green")[i % 3]
        cube("book", (x, -0.12, 0.43 + (i % 2) * 0.29), (0.035, 0.045, height), mats[color], r)
    cube("lower_cabinet", (0, -0.04, 0.14), (0.44, 0.15, 0.13), mats["wood"], r)
    cube("cabinet_handle", (0, -0.14, 0.15), (0.18, 0.018, 0.015), mats["metal"], r)
    return r


def build_wardrobe(mats):
    r = root("Wardrobe_LowPoly")
    cube("body", (0, 0, 0.9), (0.55, 0.24, 0.9), mats["wood"], r)
    cube("left_door", (-0.145, -0.135, 0.92), (0.24, 0.018, 0.76), mats["wood_dark"], r)
    cube("right_door", (0.145, -0.135, 0.92), (0.24, 0.018, 0.76), mats["wood_dark"], r)
    cube("top_trim", (0, -0.145, 1.72), (0.58, 0.035, 0.045), mats["wood_dark"], r)
    cube("bottom_trim", (0, -0.145, 0.1), (0.58, 0.035, 0.045), mats["wood_dark"], r)
    cube("left_handle", (-0.04, -0.165, 0.94), (0.018, 0.02, 0.22), mats["metal"], r)
    cube("right_handle", (0.04, -0.165, 0.94), (0.018, 0.02, 0.22), mats["metal"], r)
    for x in (-0.38, 0.38):
        cube("wardrobe_foot", (x, 0.12, 0.04), (0.045, 0.045, 0.04), mats["wood_dark"], r)
    return r


def build_desk(mats):
    r = root("Desk_LowPoly")
    cube("desktop", (0, 0, 0.56), (0.55, 0.32, 0.035), mats["wood"], r)
    cube("left_drawers", (-0.35, 0.05, 0.3), (0.14, 0.25, 0.28), mats["wood_dark"], r)
    for z in (0.22, 0.34, 0.46):
        cube("drawer_line", (-0.35, -0.09, z), (0.12, 0.015, 0.01), mats["metal"], r)
    for x in (-0.48, 0.48):
        cube("desk_leg", (x, 0.22, 0.28), (0.025, 0.025, 0.28), mats["wood_dark"], r)
    cube("monitor_stand", (0.1, -0.02, 0.64), (0.045, 0.035, 0.08), mats["black"], r)
    cube("monitor", (0.1, -0.05, 0.78), (0.22, 0.035, 0.14), mats["screen"], r)
    cube("keyboard", (0.08, -0.2, 0.6), (0.23, 0.065, 0.012), mats["black"], r)
    cylinder("lamp_base", (-0.22, -0.08, 0.61), 0.045, 0.018, mats["metal"], r, 16)
    cube("lamp_arm", (-0.22, -0.08, 0.74), (0.018, 0.018, 0.13), mats["metal"], r)
    sphere("lamp_shade", (-0.22, -0.08, 0.86), (0.08, 0.08, 0.045), mats["lamp_warm"], r, 16, 8)
    return r


def build_toilet(mats):
    r = root("Toilet_LowPoly")
    cube("base", (0, 0, 0.2), (0.18, 0.22, 0.2), mats["porcelain"], r)
    sphere("bowl", (0, -0.02, 0.42), (0.22, 0.28, 0.11), mats["porcelain"], r, 18, 8)
    sphere("inner_shadow", (0, -0.03, 0.44), (0.14, 0.19, 0.045), mats["cool_shadow"], r, 18, 6)
    cube("tank", (0, 0.24, 0.67), (0.28, 0.12, 0.22), mats["porcelain"], r)
    cube("tank_lid", (0, 0.24, 0.91), (0.31, 0.13, 0.025), mats["cool_white"], r)
    cube("flush_button", (0.12, 0.14, 0.94), (0.04, 0.025, 0.01), mats["metal"], r)
    cube("floor_pipe", (0, 0.33, 0.12), (0.08, 0.06, 0.08), mats["metal"], r)
    return r


def build_human(mats):
    r = root("Human_LowPoly")
    cube("torso", (0, 0, 1.02), (0.24, 0.16, 0.42), mats["shirt"], r)
    cube("shirt_collar", (0, -0.09, 1.3), (0.18, 0.035, 0.045), mats["shirt_light"], r)
    cube("belt", (0, -0.01, 0.69), (0.25, 0.17, 0.035), mats["belt"], r)
    sphere("head", (0, 0, 1.55), (0.16, 0.14, 0.16), mats["skin"], r, 14, 7)
    sphere("left_ear", (-0.17, -0.01, 1.55), (0.035, 0.022, 0.05), mats["skin"], r, 8, 4)
    sphere("right_ear", (0.17, -0.01, 1.55), (0.035, 0.022, 0.05), mats["skin"], r, 8, 4)
    sphere("hair", (0, 0.025, 1.67), (0.17, 0.13, 0.055), mats["hair"], r, 14, 6)
    sphere("hair_front", (0, -0.085, 1.64), (0.13, 0.045, 0.04), mats["hair"], r, 12, 5)
    cube("neck", (0, 0, 1.32), (0.08, 0.06, 0.08), mats["skin"], r)
    cube("left_arm", (-0.32, 0, 1.0), (0.07, 0.06, 0.38), mats["skin"], r)
    cube("right_arm", (0.32, 0, 1.0), (0.07, 0.06, 0.38), mats["skin"], r)
    sphere("left_hand", (-0.32, -0.005, 0.58), (0.075, 0.055, 0.055), mats["skin"], r, 10, 5)
    sphere("right_hand", (0.32, -0.005, 0.58), (0.075, 0.055, 0.055), mats["skin"], r, 10, 5)
    for i, x in enumerate((-0.36, -0.32, -0.28)):
        cube("left_finger", (x, -0.045, 0.52 - i * 0.01), (0.012, 0.035, 0.035), mats["skin_shadow"], r)
    for i, x in enumerate((0.28, 0.32, 0.36)):
        cube("right_finger", (x, -0.045, 0.52 - i * 0.01), (0.012, 0.035, 0.035), mats["skin_shadow"], r)
    cube("left_leg", (-0.1, 0, 0.42), (0.075, 0.075, 0.42), mats["pants"], r)
    cube("right_leg", (0.1, 0, 0.42), (0.075, 0.075, 0.42), mats["pants"], r)
    sphere("left_knee", (-0.1, -0.06, 0.42), (0.07, 0.025, 0.055), mats["pants_light"], r, 8, 4)
    sphere("right_knee", (0.1, -0.06, 0.42), (0.07, 0.025, 0.055), mats["pants_light"], r, 8, 4)
    cube("left_foot", (-0.1, -0.06, 0.06), (0.1, 0.16, 0.045), mats["shoe"], r)
    cube("right_foot", (0.1, -0.06, 0.06), (0.1, 0.16, 0.045), mats["shoe"], r)
    sphere("left_eye", (-0.055, -0.12, 1.58), (0.018, 0.012, 0.018), mats["black"], r, 8, 4)
    sphere("right_eye", (0.055, -0.12, 1.58), (0.018, 0.012, 0.018), mats["black"], r, 8, 4)
    cube("left_brow", (-0.055, -0.135, 1.63), (0.055, 0.008, 0.012), mats["hair"], r)
    cube("right_brow", (0.055, -0.135, 1.63), (0.055, 0.008, 0.012), mats["hair"], r)
    sphere("nose", (0, -0.15, 1.52), (0.028, 0.02, 0.035), mats["skin"], r, 8, 4)
    sphere("left_cheek", (-0.07, -0.145, 1.5), (0.028, 0.012, 0.02), mats["skin_warm"], r, 8, 4)
    sphere("right_cheek", (0.07, -0.145, 1.5), (0.028, 0.012, 0.02), mats["skin_warm"], r, 8, 4)
    cube("mouth", (0, -0.145, 1.45), (0.055, 0.008, 0.01), mats["mouth"], r)
    return r


def main():
    os.makedirs(MODEL_DIR, exist_ok=True)
    os.makedirs(HUMAN_DIR, exist_ok=True)
    clear_scene()

    mats = {
        "white": material("Warm White", (0.78, 0.82, 0.8, 1)),
        "cool_white": material("Cool White", (0.9, 0.95, 0.94, 1)),
        "cool_shadow": material("Cool Shadow", (0.55, 0.62, 0.62, 1)),
        "porcelain": material("Porcelain", (0.92, 0.94, 0.9, 1)),
        "plate_shadow": material("Plate Inner Shadow", (0.72, 0.74, 0.7, 1)),
        "metal": material("Metal", (0.55, 0.58, 0.57, 1)),
        "metal_dark": material("Dark Metal", (0.24, 0.25, 0.25, 1)),
        "counter_stone": material("Speckled Counter Stone", (0.58, 0.57, 0.53, 1)),
        "tile_warm": material("Warm Kitchen Tile", (0.73, 0.69, 0.6, 1)),
        "tile_grout": material("Tile Grout", (0.36, 0.35, 0.32, 1)),
        "dark": material("Dark Appliance", (0.08, 0.085, 0.09, 1)),
        "black": material("Black", (0.005, 0.005, 0.005, 1)),
        "coffee": material("Coffee", (0.16, 0.075, 0.025, 1)),
        "seal": material("Rubber Seal", (0.02, 0.025, 0.025, 1)),
        "glass": material("Dark Glass", (0.06, 0.08, 0.09, 1)),
        "glass_blue": material("Blue Glass", (0.35, 0.58, 0.7, 1)),
        "warm_glow": material("Warm Oven Glow", (0.9, 0.38, 0.12, 1)),
        "wood": material("Wood", (0.43, 0.27, 0.15, 1)),
        "wood_dark": material("Dark Wood", (0.23, 0.13, 0.07, 1)),
        "wood_grain": material("Wood Grain", (0.18, 0.09, 0.035, 1)),
        "cloth_tan": material("Table Cloth", (0.72, 0.56, 0.36, 1)),
        "fabric_blue": material("Muted Blue Fabric", (0.23, 0.36, 0.43, 1)),
        "fabric_blue_dark": material("Dark Blue Fabric", (0.15, 0.27, 0.34, 1)),
        "fabric_blue_light": material("Light Blue Fabric", (0.33, 0.48, 0.55, 1)),
        "fabric_thread": material("Fabric Thread", (0.08, 0.14, 0.17, 1)),
        "chair_pad": material("Chair Pad", (0.42, 0.24, 0.16, 1)),
        "sheet": material("Sheet", (0.76, 0.78, 0.73, 1)),
        "blanket": material("Blanket", (0.28, 0.36, 0.52, 1)),
        "blanket_dark": material("Blanket Fold", (0.18, 0.24, 0.42, 1)),
        "pillow": material("Pillow", (0.88, 0.86, 0.78, 1)),
        "pillow_shadow": material("Pillow Seam Shadow", (0.66, 0.64, 0.58, 1)),
        "cardboard": material("Cardboard", (0.54, 0.39, 0.23, 1)),
        "cloth": material("Cloth", (0.52, 0.23, 0.27, 1)),
        "cutting_board": material("Cutting Board", (0.62, 0.38, 0.18, 1)),
        "stain_red": material("Food Stain", (0.5, 0.07, 0.035, 1)),
        "crumb": material("Food Crumb", (0.78, 0.52, 0.25, 1)),
        "paper": material("Paper", (0.9, 0.82, 0.58, 1)),
        "paper_white": material("White Paper", (0.9, 0.88, 0.78, 1)),
        "red": material("Red Accent", (0.62, 0.11, 0.09, 1)),
        "book_blue": material("Book Blue", (0.1, 0.18, 0.42, 1)),
        "book_green": material("Book Green", (0.1, 0.34, 0.2, 1)),
        "screen": material("Dim Screen", (0.03, 0.08, 0.1, 1)),
        "lamp_warm": material("Warm Lamp Shade", (0.95, 0.68, 0.32, 1)),
        "soap": material("Soap Bottle", (0.34, 0.72, 0.66, 1)),
        "skin": material("Skin", (0.72, 0.52, 0.39, 1)),
        "skin_shadow": material("Skin Shadow", (0.55, 0.37, 0.27, 1)),
        "skin_warm": material("Skin Warm", (0.82, 0.48, 0.42, 1)),
        "hair": material("Hair", (0.06, 0.04, 0.03, 1)),
        "mouth": material("Mouth", (0.32, 0.07, 0.07, 1)),
        "shirt": material("Shirt", (0.36, 0.42, 0.5, 1)),
        "shirt_light": material("Shirt Highlight", (0.48, 0.56, 0.66, 1)),
        "belt": material("Belt", (0.04, 0.03, 0.025, 1)),
        "pants": material("Pants", (0.12, 0.15, 0.19, 1)),
        "pants_light": material("Pants Highlight", (0.18, 0.22, 0.28, 1)),
        "shoe": material("Shoe", (0.04, 0.035, 0.03, 1)),
    }

    models = {
        "Fridge_LowPoly.fbx": build_fridge(mats),
        "Stove_LowPoly.fbx": build_stove(mats),
        "DiningTable_LowPoly.fbx": build_table(mats),
        "Sofa_LowPoly.fbx": build_sofa(mats),
        "CoffeeTable_LowPoly.fbx": build_coffee_table(mats),
        "Bed_LowPoly.fbx": build_bed(mats),
        "Sink_LowPoly.fbx": build_sink(mats),
        "KitchenCounter_LowPoly.fbx": build_kitchen_counter(mats),
        "Clutter_LowPoly.fbx": build_clutter(mats),
        "Bookshelf_LowPoly.fbx": build_bookshelf(mats),
        "Wardrobe_LowPoly.fbx": build_wardrobe(mats),
        "Desk_LowPoly.fbx": build_desk(mats),
        "Toilet_LowPoly.fbx": build_toilet(mats),
    }

    human = build_human(mats)

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    for filename, model in models.items():
        export(model, os.path.join(MODEL_DIR, filename))
    export(human, os.path.join(HUMAN_DIR, "Human_LowPoly.fbx"))


if __name__ == "__main__":
    main()
