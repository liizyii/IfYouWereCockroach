import os

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


def soften(obj, amount=0.015, segments=2):
    bevel = obj.modifiers.new("soft_edge", "BEVEL")
    bevel.width = amount
    bevel.segments = segments
    bevel.affect = "EDGES"
    obj.modifiers.new("weighted_normals", "WEIGHTED_NORMAL")
    return obj


def cube(name, location, scale, mat, parent):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    obj.parent = parent
    soften(obj, min(scale) * 0.25 if min(scale) > 0 else 0.01, 2)
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


def cylinder(name, location, radius, depth, mat, parent, vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    obj.parent = parent
    soften(obj, 0.008, 1)
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
    cube("chair_seat", (x, y, 0.38), (0.18, 0.16, 0.035), mats["wood"], parent)
    cube("chair_back", (x, y + 0.13, 0.62), (0.18, 0.035, 0.26), mats["wood_dark"], parent)
    for lx in (-0.13, 0.13):
        for ly in (-0.1, 0.1):
            cube("chair_leg", (x + lx, y + ly, 0.18), (0.025, 0.025, 0.18), mats["wood_dark"], parent)


def build_table(mats):
    r = root("DiningTable_LowPoly")
    cube("top", (0, 0, 0.72), (0.52, 0.36, 0.04), mats["wood"], r)
    cube("table_runner", (0, 0, 0.765), (0.12, 0.34, 0.01), mats["cloth_tan"], r)
    cylinder("plate", (0.14, -0.05, 0.795), 0.1, 0.015, mats["porcelain"], r, 24)
    cylinder("bowl", (-0.08, -0.08, 0.81), 0.075, 0.055, mats["porcelain"], r, 20)
    cylinder("cup", (-0.18, 0.06, 0.84), 0.045, 0.09, mats["cool_white"], r, 18)
    cube("fork", (0.28, -0.08, 0.82), (0.012, 0.12, 0.006), mats["metal"], r)
    cube("knife", (0.31, 0.05, 0.82), (0.015, 0.13, 0.006), mats["metal"], r)
    for x in (-0.42, 0.42):
        for y in (-0.27, 0.27):
            cube("leg", (x, y, 0.34), (0.035, 0.035, 0.34), mats["wood_dark"], r)
    cube("front_crossbar", (0, -0.27, 0.42), (0.42, 0.018, 0.025), mats["wood_dark"], r)
    cube("back_crossbar", (0, 0.27, 0.42), (0.42, 0.018, 0.025), mats["wood_dark"], r)
    add_chair(r, mats, -0.72, 0, 1.5708)
    add_chair(r, mats, 0.72, 0, -1.5708)
    add_chair(r, mats, 0, -0.58, 0)
    return r


def build_sofa(mats):
    r = root("Sofa_LowPoly")
    cube("seat", (0, 0, 0.28), (0.5, 0.42, 0.18), mats["fabric_blue"], r)
    cube("back", (0, 0.32, 0.54), (0.52, 0.09, 0.34), mats["fabric_blue"], r)
    cube("left_arm", (-0.47, 0, 0.44), (0.06, 0.42, 0.32), mats["fabric_blue_dark"], r)
    cube("right_arm", (0.47, 0, 0.44), (0.06, 0.42, 0.32), mats["fabric_blue_dark"], r)
    cube("cushion_left", (-0.24, -0.06, 0.48), (0.22, 0.3, 0.055), mats["fabric_blue_light"], r)
    cube("cushion_right", (0.24, -0.06, 0.48), (0.22, 0.3, 0.055), mats["fabric_blue_light"], r)
    cube("cushion_gap", (0, -0.06, 0.515), (0.012, 0.31, 0.015), mats["fabric_blue_dark"], r)
    cube("front_seam", (0, -0.29, 0.46), (0.42, 0.012, 0.015), mats["fabric_blue_dark"], r)
    cube("throw_pillow", (-0.24, 0.25, 0.62), (0.12, 0.04, 0.13), mats["red"], r)
    cube("folded_blanket", (0.22, 0.2, 0.68), (0.16, 0.05, 0.11), mats["blanket"], r)
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
            cube("leg", (x, y, 0.19), (0.03, 0.03, 0.18), mats["wood_dark"], r)
    return r


def build_bed(mats):
    r = root("Bed_LowPoly")
    cube("frame", (0, 0, 0.22), (0.5, 0.48, 0.16), mats["wood_dark"], r)
    cube("mattress", (0, 0, 0.38), (0.47, 0.45, 0.11), mats["sheet"], r)
    cube("blanket", (0, -0.05, 0.5), (0.47, 0.28, 0.045), mats["blanket"], r)
    cube("blanket_fold", (0, 0.12, 0.555), (0.47, 0.035, 0.025), mats["blanket_dark"], r)
    cube("headboard", (0, 0.52, 0.5), (0.52, 0.055, 0.36), mats["wood_dark"], r)
    cube("headboard_top", (0, 0.54, 0.72), (0.56, 0.07, 0.05), mats["wood"], r)
    cube("pillow_left", (-0.17, 0.3, 0.55), (0.14, 0.1, 0.045), mats["pillow"], r)
    cube("pillow_right", (0.17, 0.3, 0.55), (0.14, 0.1, 0.045), mats["pillow"], r)
    for x in (-0.38, 0.38):
        for y in (-0.35, 0.35):
            cube("bed_leg", (x, y, 0.08), (0.035, 0.035, 0.08), mats["wood_dark"], r)
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
        "metal": material("Metal", (0.55, 0.58, 0.57, 1)),
        "metal_dark": material("Dark Metal", (0.24, 0.25, 0.25, 1)),
        "dark": material("Dark Appliance", (0.08, 0.085, 0.09, 1)),
        "black": material("Black", (0.005, 0.005, 0.005, 1)),
        "seal": material("Rubber Seal", (0.02, 0.025, 0.025, 1)),
        "glass": material("Dark Glass", (0.06, 0.08, 0.09, 1)),
        "glass_blue": material("Blue Glass", (0.35, 0.58, 0.7, 1)),
        "warm_glow": material("Warm Oven Glow", (0.9, 0.38, 0.12, 1)),
        "wood": material("Wood", (0.43, 0.27, 0.15, 1)),
        "wood_dark": material("Dark Wood", (0.23, 0.13, 0.07, 1)),
        "cloth_tan": material("Table Cloth", (0.72, 0.56, 0.36, 1)),
        "fabric_blue": material("Muted Blue Fabric", (0.23, 0.36, 0.43, 1)),
        "fabric_blue_dark": material("Dark Blue Fabric", (0.15, 0.27, 0.34, 1)),
        "fabric_blue_light": material("Light Blue Fabric", (0.33, 0.48, 0.55, 1)),
        "sheet": material("Sheet", (0.76, 0.78, 0.73, 1)),
        "blanket": material("Blanket", (0.28, 0.36, 0.52, 1)),
        "blanket_dark": material("Blanket Fold", (0.18, 0.24, 0.42, 1)),
        "pillow": material("Pillow", (0.88, 0.86, 0.78, 1)),
        "cardboard": material("Cardboard", (0.54, 0.39, 0.23, 1)),
        "cloth": material("Cloth", (0.52, 0.23, 0.27, 1)),
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
