"""
Shared Blender pipeline for Avengers: Assemble Re:Forged.

Conventions (enforced by validate_hero):
  * 1 Blender unit = 1 meter. Characters stand on the origin and face -Y. Z is up.
  * One armature named "<Hero>_Rig" using the bone names in REQUIRED_BONES (plus Prop_* bones).
  * Meshes are skinned to the armature; every vertex is weighted.
  * Triangle budget per hero: TRI_BUDGET.
  * One action per move, named exactly like the move in the simulation, and exactly
    `total` frames long at 60 fps (from Docs/FrameData/<hero>.json). Mode clips (Idle, Walk,
    Hitstun...) follow MODE_CLIPS. Unity samples clips at the sim's frame counter, so
    timing can never drift from gameplay.
  * Animate "on 2s": keys every 2 frames with CONSTANT interpolation (on 1s for Supers and
    Ultimates).
  * Pre-fractured props: chunks named "<Prop>_cell_###" under an empty "<Prop>_fractured".

Works from the Blender CLI (`blender -b -P script.py -- args`) or the `bpy` Python module.
"""
import json
import math
import os
import sys

import bpy
from mathutils import Quaternion, Vector

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FRAME_DATA_DIR = os.path.join(REPO_ROOT, "Docs", "FrameData")
CHARACTER_OUT = os.path.join(REPO_ROOT, "Assets", "_Project", "Art", "Characters")
PROP_OUT = os.path.join(REPO_ROOT, "Assets", "_Project", "Art", "Props")
FPS = 60
TRI_BUDGET = 60000

REQUIRED_BONES = [
    "Root", "Hips", "Spine", "Chest", "Neck", "Head",
    "Shoulder.L", "UpperArm.L", "LowerArm.L", "Hand.L",
    "Shoulder.R", "UpperArm.R", "LowerArm.R", "Hand.R",
    "UpperLeg.L", "LowerLeg.L", "Foot.L",
    "UpperLeg.R", "LowerLeg.R", "Foot.R",
]

# Clip name -> (length in frames, loops). Names match FighterMode in the simulation.
MODE_CLIPS = {
    "Idle": (48, True), "Walk": (32, True), "Guard": (8, True), "Airborne": (12, True),
    "Recover": (8, False), "Evade": (18, False), "Taunt": (40, False), "Hitstun": (16, False),
    "Blockstun": (8, False), "Juggle": (20, True), "Knockdown": (36, False), "Dead": (36, False),
}


def script_args():
    """Arguments after '--' (Blender CLI) or after the script name (bpy module)."""
    argv = sys.argv
    return argv[argv.index("--") + 1:] if "--" in argv else argv[1:]


def slug(name):
    return name.lower().replace(" ", "_").replace("-", "_")


def load_frame_data(hero_slug):
    path = os.path.join(FRAME_DATA_DIR, hero_slug + ".json")
    if not os.path.exists(path):
        raise SystemExit("Missing %s. Run: dotnet run --project Tools/FrameData" % path)
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.fps = FPS
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    return scene


def material(name, rgb):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (rgb[0], rgb[1], rgb[2], 1.0)
    bsdf.inputs["Roughness"].default_value = 0.6
    mat.diffuse_color = (rgb[0], rgb[1], rgb[2], 1.0)
    return mat


# ---------------------------------------------------------------------------- posing

def rest_relative(pose_bone, rotations):
    """
    Converts a list of (axis, degrees) rotations expressed in *armature* space (X = character's
    left, -Y = forward, Z = up) into the bone's local pose quaternion. Rotations apply in order and
    are relative to the rest pose, so poses read like FK: "upper arm: swing down, then forward".
    """
    q_arm = Quaternion()
    for axis, deg in rotations:
        vec = {"X": Vector((1, 0, 0)), "Y": Vector((0, 1, 0)), "Z": Vector((0, 0, 1))}[axis]
        q_arm = Quaternion(vec, math.radians(deg)) @ q_arm
    rest = pose_bone.bone.matrix_local.to_quaternion()
    return rest.inverted() @ q_arm @ rest


def blend_poses(a, b, t):
    """Per-bone slerp between two pose dicts of quaternions."""
    out = {}
    for bone in set(a) | set(b):
        qa = a.get(bone, Quaternion())
        qb = b.get(bone, Quaternion())
        out[bone] = qa.slerp(qb, t)
    return out


def key_pose(armature, pose, root_offset, frame):
    for pb in armature.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = pose.get(pb.name, Quaternion())
        pb.keyframe_insert("rotation_quaternion", frame=frame)
    root = armature.pose.bones["Root"]
    root.location = root_offset
    root.keyframe_insert("location", frame=frame)


def make_constant(action):
    for fc in action.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation = "CONSTANT"


# ---------------------------------------------------------------------------- validation & export

def triangle_count(obj):
    mesh = obj.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh()
    tris = sum(len(p.vertices) - 2 for p in mesh.polygons)
    obj.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh_clear()
    return tris


def validate_hero(frame_data):
    """Returns a list of error strings. Empty means the hero meets the pipeline contract."""
    errors = []
    armatures = [o for o in bpy.data.objects if o.type == "ARMATURE"]
    if len(armatures) != 1:
        return ["expected exactly one armature, found %d" % len(armatures)]
    rig = armatures[0]
    if tuple(rig.scale) != (1.0, 1.0, 1.0) or rig.location.length > 1e-6:
        errors.append("armature must sit at the origin with scale 1 (apply transforms)")
    names = set(rig.data.bones.keys())
    for b in REQUIRED_BONES:
        if b not in names:
            errors.append("missing bone " + b)
    for b in names:
        if b not in REQUIRED_BONES and not b.startswith("Prop_"):
            errors.append("non-standard bone %s (extra bones must start with Prop_)" % b)

    tris = 0
    for obj in bpy.data.objects:
        if obj.type != "MESH":
            continue
        tris += triangle_count(obj)
        if not any(m.type == "ARMATURE" and m.object == rig for m in obj.modifiers):
            errors.append("mesh %s is not skinned to %s" % (obj.name, rig.name))
            continue
        group_names = {g.index: g.name for g in obj.vertex_groups}
        for v in obj.data.vertices:
            if not any(group_names.get(g.group) in names and g.weight > 0 for g in v.groups):
                errors.append("mesh %s has unweighted vertex %d" % (obj.name, v.index))
                break
        for slot in obj.material_slots:
            if slot.material is None or slot.material.name.startswith("Material"):
                errors.append("mesh %s uses an unnamed material" % obj.name)
    if tris > TRI_BUDGET:
        errors.append("triangle budget exceeded: %d > %d" % (tris, TRI_BUDGET))

    for move in frame_data["moves"]:
        action = bpy.data.actions.get(move["name"])
        if action is None:
            errors.append("missing action for move " + move["name"])
            continue
        start, end = action.frame_range
        if int(round(end - start)) != move["total"]:
            errors.append("action %s is %d frames, gameplay needs %d" % (move["name"], end - start, move["total"]))
    for clip, (length, _) in MODE_CLIPS.items():
        action = bpy.data.actions.get(clip)
        if action is None:
            errors.append("missing mode clip " + clip)
        elif int(round(action.frame_range[1] - action.frame_range[0])) != length:
            errors.append("mode clip %s should be %d frames" % (clip, length))
    return errors


def export_fbx(path, object_types, animated):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=path,
        object_types=object_types,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        use_armature_deform_only=False,
        mesh_smooth_type="FACE",
        bake_anim=animated,
        bake_anim_use_all_actions=animated,
        bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,  # keep the stepped "on 2s" holds exactly
    )
    return path
