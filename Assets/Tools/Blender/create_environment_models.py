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


def cube(name, location, scale, mat, parent):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    obj.parent = parent
    return obj


def sphere(name, location, scale, mat, parent, segments=12, rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    obj.parent = parent
    return obj


def cylinder(name, location, radius, depth, mat, parent, vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    obj.parent = parent
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
    cube("top_door", (0, -0.012, 0.78), (0.47, 0.02, 0.23), mats["cool_white"], r)
    cube("bottom_door", (0, -0.012, 0.33), (0.47, 0.02, 0.3), mats["cool_white"], r)
    cube("handle_top", (0.33, -0.04, 0.78), (0.025, 0.035, 0.18), mats["metal"], r)
    cube("handle_bottom", (0.33, -0.04, 0.32), (0.025, 0.035, 0.22), mats["metal"], r)
    return r


def build_stove(mats):
    r = root("Stove_LowPoly")
    cube("base", (0, 0, 0.38), (0.5, 0.42, 0.38), mats["dark"], r)
    cube("top", (0, 0, 0.78), (0.53, 0.45, 0.04), mats["metal"], r)
    for x in (-0.2, 0.2):
        for y in (-0.14, 0.14):
            cylinder("burner", (x, y, 0.84), 0.08, 0.025, mats["black"], r, 16)
    cube("oven_window", (0, -0.43, 0.38), (0.32, 0.015, 0.18), mats["glass"], r)
    return r


def build_table(mats):
    r = root("DiningTable_LowPoly")
    cube("top", (0, 0, 0.72), (0.52, 0.36, 0.04), mats["wood"], r)
    for x in (-0.42, 0.42):
        for y in (-0.27, 0.27):
            cube("leg", (x, y, 0.34), (0.035, 0.035, 0.34), mats["wood_dark"], r)
    return r


def build_sofa(mats):
    r = root("Sofa_LowPoly")
    cube("seat", (0, 0, 0.28), (0.5, 0.42, 0.18), mats["fabric_blue"], r)
    cube("back", (0, 0.32, 0.54), (0.52, 0.09, 0.34), mats["fabric_blue"], r)
    cube("left_arm", (-0.47, 0, 0.44), (0.06, 0.42, 0.32), mats["fabric_blue_dark"], r)
    cube("right_arm", (0.47, 0, 0.44), (0.06, 0.42, 0.32), mats["fabric_blue_dark"], r)
    cube("cushion_left", (-0.24, -0.06, 0.48), (0.22, 0.3, 0.055), mats["fabric_blue_light"], r)
    cube("cushion_right", (0.24, -0.06, 0.48), (0.22, 0.3, 0.055), mats["fabric_blue_light"], r)
    return r


def build_coffee_table(mats):
    r = root("CoffeeTable_LowPoly")
    cube("top", (0, 0, 0.38), (0.5, 0.36, 0.035), mats["wood"], r)
    cube("shelf", (0, 0, 0.19), (0.43, 0.3, 0.025), mats["wood_dark"], r)
    for x in (-0.4, 0.4):
        for y in (-0.26, 0.26):
            cube("leg", (x, y, 0.19), (0.03, 0.03, 0.18), mats["wood_dark"], r)
    return r


def build_bed(mats):
    r = root("Bed_LowPoly")
    cube("frame", (0, 0, 0.22), (0.5, 0.48, 0.16), mats["wood_dark"], r)
    cube("mattress", (0, 0, 0.38), (0.47, 0.45, 0.11), mats["sheet"], r)
    cube("blanket", (0, -0.05, 0.5), (0.47, 0.28, 0.045), mats["blanket"], r)
    cube("pillow_left", (-0.17, 0.3, 0.55), (0.14, 0.1, 0.045), mats["pillow"], r)
    cube("pillow_right", (0.17, 0.3, 0.55), (0.14, 0.1, 0.045), mats["pillow"], r)
    return r


def build_sink(mats):
    r = root("Sink_LowPoly")
    cube("cabinet", (0, 0, 0.35), (0.45, 0.36, 0.35), mats["white"], r)
    cube("basin", (0, 0, 0.72), (0.42, 0.32, 0.06), mats["porcelain"], r)
    cylinder("faucet", (0, -0.05, 0.86), 0.035, 0.18, mats["metal"], r, 12)
    cube("drain", (0, -0.02, 0.79), (0.055, 0.055, 0.01), mats["dark"], r)
    return r


def build_clutter(mats):
    r = root("Clutter_LowPoly")
    cube("box", (-0.12, 0.02, 0.22), (0.24, 0.2, 0.22), mats["cardboard"], r)
    cylinder("can", (0.22, -0.1, 0.18), 0.09, 0.36, mats["metal"], r, 10)
    sphere("cloth", (0.08, 0.16, 0.12), (0.18, 0.1, 0.08), mats["cloth"], r, 10, 5)
    return r


def build_human(mats):
    r = root("Human_LowPoly")
    cube("torso", (0, 0, 1.02), (0.24, 0.16, 0.42), mats["shirt"], r)
    sphere("head", (0, 0, 1.55), (0.16, 0.14, 0.16), mats["skin"], r, 14, 7)
    cube("neck", (0, 0, 1.32), (0.08, 0.06, 0.08), mats["skin"], r)
    cube("left_arm", (-0.32, 0, 1.0), (0.07, 0.06, 0.38), mats["skin"], r)
    cube("right_arm", (0.32, 0, 1.0), (0.07, 0.06, 0.38), mats["skin"], r)
    cube("left_leg", (-0.1, 0, 0.42), (0.075, 0.075, 0.42), mats["pants"], r)
    cube("right_leg", (0.1, 0, 0.42), (0.075, 0.075, 0.42), mats["pants"], r)
    cube("left_foot", (-0.1, -0.06, 0.06), (0.1, 0.16, 0.045), mats["shoe"], r)
    cube("right_foot", (0.1, -0.06, 0.06), (0.1, 0.16, 0.045), mats["shoe"], r)
    sphere("left_eye", (-0.055, -0.12, 1.58), (0.018, 0.012, 0.018), mats["black"], r, 8, 4)
    sphere("right_eye", (0.055, -0.12, 1.58), (0.018, 0.012, 0.018), mats["black"], r, 8, 4)
    return r


def main():
    os.makedirs(MODEL_DIR, exist_ok=True)
    os.makedirs(HUMAN_DIR, exist_ok=True)
    clear_scene()

    mats = {
        "white": material("Warm White", (0.78, 0.82, 0.8, 1)),
        "cool_white": material("Cool White", (0.9, 0.95, 0.94, 1)),
        "porcelain": material("Porcelain", (0.92, 0.94, 0.9, 1)),
        "metal": material("Metal", (0.55, 0.58, 0.57, 1)),
        "dark": material("Dark Appliance", (0.08, 0.085, 0.09, 1)),
        "black": material("Black", (0.005, 0.005, 0.005, 1)),
        "glass": material("Dark Glass", (0.06, 0.08, 0.09, 1)),
        "wood": material("Wood", (0.43, 0.27, 0.15, 1)),
        "wood_dark": material("Dark Wood", (0.23, 0.13, 0.07, 1)),
        "fabric_blue": material("Muted Blue Fabric", (0.23, 0.36, 0.43, 1)),
        "fabric_blue_dark": material("Dark Blue Fabric", (0.15, 0.27, 0.34, 1)),
        "fabric_blue_light": material("Light Blue Fabric", (0.33, 0.48, 0.55, 1)),
        "sheet": material("Sheet", (0.76, 0.78, 0.73, 1)),
        "blanket": material("Blanket", (0.28, 0.36, 0.52, 1)),
        "pillow": material("Pillow", (0.88, 0.86, 0.78, 1)),
        "cardboard": material("Cardboard", (0.54, 0.39, 0.23, 1)),
        "cloth": material("Cloth", (0.52, 0.23, 0.27, 1)),
        "skin": material("Skin", (0.72, 0.52, 0.39, 1)),
        "shirt": material("Shirt", (0.36, 0.42, 0.5, 1)),
        "pants": material("Pants", (0.12, 0.15, 0.19, 1)),
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
    }

    human = build_human(mats)

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    for filename, model in models.items():
        export(model, os.path.join(MODEL_DIR, filename))
    export(human, os.path.join(HUMAN_DIR, "Human_LowPoly.fbx"))


if __name__ == "__main__":
    main()
