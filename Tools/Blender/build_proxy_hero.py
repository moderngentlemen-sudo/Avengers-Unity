"""
Builds a stylized proxy hero (rig, skinned mesh, every gameplay animation), validates it
against the pipeline contract, and exports the FBX that Unity imports.

The proxy stands in until the sculpted model exists: artists replace the mesh, keep the rig and
the clip names/lengths, and re-run export_hero.py. Animation lengths come from
Docs/FrameData/<hero>.json, which Tools/FrameData generates from the simulation.

  python Tools/Blender/build_proxy_hero.py captain_america          (bpy module)
  Heroes with a look: captain_america, iron_man, hulk, vision, ant_man
  blender -b -P Tools/Blender/build_proxy_hero.py -- captain_america (Blender CLI)
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import bpy  # must come first: the bpy module registers bmesh/mathutils
import bmesh
from mathutils import Matrix, Quaternion, Vector

import reforged_pipeline as rp

# ----------------------------------------------------------------------------- rig

# name: (head, tail, parent). Character faces -Y; .L is +X.
BONES = {
    "Root": ((0, 0, 0), (0, 0.25, 0), None),
    "Hips": ((0, 0, 0.95), (0, 0, 1.1), "Root"),
    "Spine": ((0, 0, 1.1), (0, 0, 1.28), "Hips"),
    "Chest": ((0, 0, 1.28), (0, 0, 1.5), "Spine"),
    "Neck": ((0, 0, 1.5), (0, 0, 1.6), "Chest"),
    "Head": ((0, 0, 1.6), (0, 0, 1.86), "Neck"),
    "Shoulder.L": ((0.05, 0, 1.45), (0.19, 0, 1.45), "Chest"),
    "UpperArm.L": ((0.19, 0, 1.45), (0.46, 0, 1.45), "Shoulder.L"),
    "LowerArm.L": ((0.46, 0, 1.45), (0.71, 0, 1.45), "UpperArm.L"),
    "Hand.L": ((0.71, 0, 1.45), (0.82, 0, 1.45), "LowerArm.L"),
    "Shoulder.R": ((-0.05, 0, 1.45), (-0.19, 0, 1.45), "Chest"),
    "UpperArm.R": ((-0.19, 0, 1.45), (-0.46, 0, 1.45), "Shoulder.R"),
    "LowerArm.R": ((-0.46, 0, 1.45), (-0.71, 0, 1.45), "UpperArm.R"),
    "Hand.R": ((-0.71, 0, 1.45), (-0.82, 0, 1.45), "LowerArm.R"),
    "UpperLeg.L": ((0.11, 0, 0.95), (0.11, 0, 0.52), "Hips"),
    "LowerLeg.L": ((0.11, 0, 0.52), (0.11, 0, 0.1), "UpperLeg.L"),
    "Foot.L": ((0.11, 0, 0.1), (0.11, -0.16, 0.02), "LowerLeg.L"),
    "UpperLeg.R": ((-0.11, 0, 0.95), (-0.11, 0, 0.52), "Hips"),
    "LowerLeg.R": ((-0.11, 0, 0.52), (-0.11, 0, 0.1), "UpperLeg.R"),
    "Foot.R": ((-0.11, 0, 0.1), (-0.11, -0.16, 0.02), "LowerLeg.R"),
}

# ----------------------------------------------------------------------------- Captain America

CAP_PROP_BONES = {"Prop_Shield": ((0.6, -0.1, 1.45), (0.6, -0.3, 1.45), "LowerArm.L")}

CAP_COLORS = {
    "Suit_Blue": (0.08, 0.16, 0.5), "Suit_Red": (0.72, 0.06, 0.07), "Suit_White": (0.92, 0.92, 0.95),
    "Skin": (0.86, 0.64, 0.5), "Leather_Brown": (0.33, 0.2, 0.11),
    "Shield_Red": (0.75, 0.05, 0.06), "Shield_White": (0.95, 0.95, 0.97), "Shield_Blue": (0.07, 0.16, 0.55),
}

# (bone, center, size, material) boxes; rest pose T-pose.
CAP_PARTS = [
    ("Hips", (0, 0, 1.0), (0.36, 0.22, 0.16), "Leather_Brown"),
    ("Spine", (0, 0, 1.13), (0.34, 0.2, 0.06), "Suit_Red"),
    ("Spine", (0, 0, 1.19), (0.34, 0.2, 0.06), "Suit_White"),
    ("Spine", (0, 0, 1.25), (0.34, 0.2, 0.06), "Suit_Red"),
    ("Chest", (0, 0, 1.39), (0.44, 0.25, 0.24), "Suit_Blue"),
    ("Chest", (0, -0.13, 1.41), (0.09, 0.02, 0.09), "Suit_White"),  # chest star
    ("Neck", (0, 0, 1.55), (0.1, 0.1, 0.1), "Skin"),
    ("Head", (0, 0, 1.73), (0.22, 0.24, 0.26), "Suit_Blue"),
    ("Head", (0, -0.11, 1.67), (0.17, 0.04, 0.12), "Skin"),
    ("Head", (0, -0.125, 1.8), (0.05, 0.02, 0.06), "Suit_White"),  # helmet "A"
    ("Shoulder.L", (0.13, 0, 1.45), (0.14, 0.18, 0.16), "Suit_Blue"),
    ("UpperArm.L", (0.325, 0, 1.45), (0.27, 0.13, 0.13), "Suit_Blue"),
    ("LowerArm.L", (0.585, 0, 1.45), (0.25, 0.12, 0.12), "Suit_Red"),
    ("Hand.L", (0.765, 0, 1.45), (0.11, 0.1, 0.1), "Suit_Red"),
    ("Shoulder.R", (-0.13, 0, 1.45), (0.14, 0.18, 0.16), "Suit_Blue"),
    ("UpperArm.R", (-0.325, 0, 1.45), (0.27, 0.13, 0.13), "Suit_Blue"),
    ("LowerArm.R", (-0.585, 0, 1.45), (0.25, 0.12, 0.12), "Suit_Red"),
    ("Hand.R", (-0.765, 0, 1.45), (0.11, 0.1, 0.1), "Suit_Red"),
    ("UpperLeg.L", (0.11, 0, 0.735), (0.15, 0.16, 0.43), "Suit_Blue"),
    ("LowerLeg.L", (0.11, 0, 0.31), (0.14, 0.15, 0.42), "Suit_Red"),
    ("Foot.L", (0.11, -0.05, 0.05), (0.13, 0.26, 0.1), "Suit_Red"),
    ("UpperLeg.R", (-0.11, 0, 0.735), (0.15, 0.16, 0.43), "Suit_Blue"),
    ("LowerLeg.R", (-0.11, 0, 0.31), (0.14, 0.15, 0.42), "Suit_Red"),
    ("Foot.R", (-0.11, -0.05, 0.05), (0.13, 0.26, 0.1), "Suit_Red"),
]

# Shield rings: (radius, forward offset, material). Faces forward (-Y), strapped to the left forearm.
SHIELD_CENTER = Vector((0.6, -0.11, 1.45))
SHIELD_RINGS = [(0.38, 0.0, "Shield_Red"), (0.3, 0.006, "Shield_White"), (0.22, 0.012, "Shield_Red"), (0.14, 0.018, "Shield_Blue")]


def cap_shield(bm, deform, groups, index_of):
    for radius, fwd, mat in SHIELD_RINGS:
        add_disc(bm, deform, groups["Prop_Shield"], SHIELD_CENTER + Vector((0, -fwd, 0)), radius, 0.04, index_of[mat])
    add_star(bm, deform, groups["Prop_Shield"], SHIELD_CENTER + Vector((0, -0.04, 0)), 0.11, 0.008, index_of["Shield_White"])


# ----------------------------------------------------------------------------- Iron Man

IRON_MAN_COLORS = {
    "Armor_Red": (0.68, 0.04, 0.05), "Armor_Gold": (0.86, 0.62, 0.14), "Arc_Reactor": (0.6, 0.95, 1.0),
    "Metal_Dark": (0.12, 0.12, 0.14),
}

IRON_MAN_PARTS = [
    ("Hips", (0, 0, 1.0), (0.36, 0.22, 0.16), "Armor_Red"),
    ("Spine", (0, 0, 1.19), (0.32, 0.2, 0.18), "Armor_Gold"),
    ("Chest", (0, 0, 1.39), (0.46, 0.27, 0.26), "Armor_Red"),
    ("Chest", (0, -0.14, 1.4), (0.08, 0.02, 0.08), "Arc_Reactor"),
    ("Neck", (0, 0, 1.55), (0.1, 0.1, 0.1), "Metal_Dark"),
    ("Head", (0, 0, 1.73), (0.23, 0.25, 0.27), "Armor_Red"),
    ("Head", (0, -0.115, 1.71), (0.18, 0.04, 0.2), "Armor_Gold"),       # faceplate
    ("Head", (0.045, -0.137, 1.75), (0.05, 0.01, 0.02), "Arc_Reactor"),  # eyes
    ("Head", (-0.045, -0.137, 1.75), (0.05, 0.01, 0.02), "Arc_Reactor"),
    ("Shoulder.L", (0.13, 0, 1.46), (0.17, 0.21, 0.19), "Armor_Red"),
    ("UpperArm.L", (0.325, 0, 1.45), (0.27, 0.13, 0.13), "Armor_Gold"),
    ("LowerArm.L", (0.585, 0, 1.45), (0.26, 0.15, 0.15), "Armor_Red"),
    ("Hand.L", (0.765, 0, 1.45), (0.12, 0.11, 0.11), "Armor_Red"),
    ("Hand.L", (0.77, 0, 1.39), (0.05, 0.05, 0.01), "Arc_Reactor"),      # palm repulsor
    ("Shoulder.R", (-0.13, 0, 1.46), (0.17, 0.21, 0.19), "Armor_Red"),
    ("UpperArm.R", (-0.325, 0, 1.45), (0.27, 0.13, 0.13), "Armor_Gold"),
    ("LowerArm.R", (-0.585, 0, 1.45), (0.26, 0.15, 0.15), "Armor_Red"),
    ("Hand.R", (-0.765, 0, 1.45), (0.12, 0.11, 0.11), "Armor_Red"),
    ("Hand.R", (-0.77, 0, 1.39), (0.05, 0.05, 0.01), "Arc_Reactor"),
    ("UpperLeg.L", (0.11, 0, 0.735), (0.16, 0.17, 0.43), "Armor_Gold"),
    ("LowerLeg.L", (0.11, 0, 0.31), (0.16, 0.17, 0.42), "Armor_Red"),
    ("Foot.L", (0.11, -0.05, 0.05), (0.14, 0.27, 0.1), "Armor_Red"),
    ("Foot.L", (0.11, -0.03, -0.005), (0.07, 0.1, 0.01), "Arc_Reactor"),  # boot thruster
    ("UpperLeg.R", (-0.11, 0, 0.735), (0.16, 0.17, 0.43), "Armor_Gold"),
    ("LowerLeg.R", (-0.11, 0, 0.31), (0.16, 0.17, 0.42), "Armor_Red"),
    ("Foot.R", (-0.11, -0.05, 0.05), (0.14, 0.27, 0.1), "Armor_Red"),
    ("Foot.R", (-0.11, -0.03, -0.005), (0.07, 0.1, 0.01), "Arc_Reactor"),
]


# ----------------------------------------------------------------------------- Hulk
# Built on the shared skeleton, then scaled 1.3x (see LOOKS) so he stands ~2.4 m, matching the
# simulation's body box. Boxes overhang the bones on purpose: huge traps, forearms and fists.

HULK_COLORS = {
    "Gamma_Skin": (0.2, 0.5, 0.12), "Gamma_Dark": (0.12, 0.32, 0.08), "Pants_Purple": (0.3, 0.1, 0.42),
    "Hair_Black": (0.04, 0.04, 0.05), "Eye_Glow": (0.55, 1.0, 0.35),
}

HULK_PARTS = [
    ("Hips", (0, 0, 1.0), (0.42, 0.26, 0.18), "Pants_Purple"),
    ("Spine", (0, 0.01, 1.18), (0.44, 0.27, 0.2), "Gamma_Skin"),
    ("Chest", (0, 0.02, 1.38), (0.6, 0.34, 0.3), "Gamma_Skin"),
    ("Chest", (0, -0.15, 1.42), (0.42, 0.03, 0.14), "Gamma_Dark"),     # pecs
    ("Chest", (0.17, 0.02, 1.53), (0.16, 0.24, 0.1), "Gamma_Skin"),    # traps
    ("Chest", (-0.17, 0.02, 1.53), (0.16, 0.24, 0.1), "Gamma_Skin"),
    ("Neck", (0, -0.01, 1.55), (0.16, 0.14, 0.08), "Gamma_Skin"),
    ("Head", (0, -0.03, 1.68), (0.22, 0.22, 0.22), "Gamma_Skin"),
    ("Head", (0, 0.0, 1.8), (0.24, 0.24, 0.05), "Hair_Black"),
    ("Head", (0, -0.035, 1.73), (0.18, 0.18, 0.04), "Gamma_Dark"),    # heavy brow
    ("Head", (0.05, -0.143, 1.7), (0.04, 0.01, 0.02), "Eye_Glow"),
    ("Head", (-0.05, -0.143, 1.7), (0.04, 0.01, 0.02), "Eye_Glow"),
    ("Shoulder.L", (0.17, 0, 1.46), (0.2, 0.26, 0.24), "Gamma_Skin"),
    ("UpperArm.L", (0.33, 0, 1.45), (0.29, 0.19, 0.19), "Gamma_Skin"),
    ("LowerArm.L", (0.6, 0, 1.45), (0.27, 0.22, 0.22), "Gamma_Dark"),
    ("Hand.L", (0.79, 0, 1.45), (0.17, 0.19, 0.19), "Gamma_Skin"),
    ("Shoulder.R", (-0.17, 0, 1.46), (0.2, 0.26, 0.24), "Gamma_Skin"),
    ("UpperArm.R", (-0.33, 0, 1.45), (0.29, 0.19, 0.19), "Gamma_Skin"),
    ("LowerArm.R", (-0.6, 0, 1.45), (0.27, 0.22, 0.22), "Gamma_Dark"),
    ("Hand.R", (-0.79, 0, 1.45), (0.17, 0.19, 0.19), "Gamma_Skin"),
    ("UpperLeg.L", (0.12, 0, 0.74), (0.2, 0.22, 0.42), "Pants_Purple"),
    ("LowerLeg.L", (0.12, 0, 0.42), (0.2, 0.21, 0.2), "Pants_Purple"),   # torn cut-offs
    ("LowerLeg.L", (0.12, 0, 0.2), (0.17, 0.18, 0.24), "Gamma_Skin"),
    ("Foot.L", (0.12, -0.05, 0.05), (0.17, 0.3, 0.1), "Gamma_Dark"),
    ("UpperLeg.R", (-0.12, 0, 0.74), (0.2, 0.22, 0.42), "Pants_Purple"),
    ("LowerLeg.R", (-0.12, 0, 0.42), (0.2, 0.21, 0.2), "Pants_Purple"),
    ("LowerLeg.R", (-0.12, 0, 0.2), (0.17, 0.18, 0.24), "Gamma_Skin"),
    ("Foot.R", (-0.12, -0.05, 0.05), (0.17, 0.3, 0.1), "Gamma_Dark"),
]

# ----------------------------------------------------------------------------- Vision

VISION_COLORS = {
    "Synth_Green": (0.18, 0.45, 0.22), "Synth_Red": (0.65, 0.08, 0.12), "Cape_Gold": (0.88, 0.66, 0.12),
    "Mind_Stone": (1.0, 0.9, 0.3), "Diamond_Trim": (0.8, 0.62, 0.15),
}

VISION_PARTS = [
    ("Hips", (0, 0, 1.0), (0.34, 0.2, 0.16), "Synth_Green"),
    ("Spine", (0, 0, 1.19), (0.3, 0.19, 0.18), "Synth_Green"),
    ("Chest", (0, 0, 1.39), (0.42, 0.24, 0.24), "Synth_Green"),
    ("Chest", (0, -0.125, 1.39), (0.16, 0.02, 0.16), "Diamond_Trim"),   # chest diamond
    ("Chest", (0, -0.13, 1.39), (0.09, 0.02, 0.09), "Synth_Green"),
    ("Chest", (0, 0.06, 1.51), (0.5, 0.18, 0.05), "Cape_Gold"),         # collar
    ("Chest", (0, 0.135, 1.24), (0.44, 0.02, 0.5), "Cape_Gold"),       # cape (rigid on the chest)
    ("Spine", (0, 0.15, 0.82), (0.48, 0.02, 0.34), "Cape_Gold"),
    ("Neck", (0, 0, 1.55), (0.09, 0.09, 0.1), "Synth_Red"),
    ("Head", (0, 0, 1.72), (0.2, 0.22, 0.25), "Synth_Red"),
    ("Head", (0, -0.112, 1.77), (0.05, 0.01, 0.05), "Mind_Stone"),
    ("Head", (0.04, -0.112, 1.71), (0.04, 0.01, 0.015), "Synth_Green"),
    ("Head", (-0.04, -0.112, 1.71), (0.04, 0.01, 0.015), "Synth_Green"),
    ("Shoulder.L", (0.13, 0, 1.45), (0.13, 0.17, 0.15), "Synth_Green"),
    ("UpperArm.L", (0.325, 0, 1.45), (0.26, 0.12, 0.12), "Synth_Green"),
    ("LowerArm.L", (0.585, 0, 1.45), (0.25, 0.11, 0.11), "Synth_Green"),
    ("Hand.L", (0.765, 0, 1.45), (0.1, 0.09, 0.09), "Synth_Red"),
    ("Shoulder.R", (-0.13, 0, 1.45), (0.13, 0.17, 0.15), "Synth_Green"),
    ("UpperArm.R", (-0.325, 0, 1.45), (0.26, 0.12, 0.12), "Synth_Green"),
    ("LowerArm.R", (-0.585, 0, 1.45), (0.25, 0.11, 0.11), "Synth_Green"),
    ("Hand.R", (-0.765, 0, 1.45), (0.1, 0.09, 0.09), "Synth_Red"),
    ("UpperLeg.L", (0.1, 0, 0.735), (0.14, 0.15, 0.43), "Synth_Green"),
    ("LowerLeg.L", (0.1, 0, 0.31), (0.13, 0.14, 0.42), "Synth_Green"),
    ("Foot.L", (0.1, -0.05, 0.05), (0.12, 0.25, 0.1), "Synth_Red"),
    ("UpperLeg.R", (-0.1, 0, 0.735), (0.14, 0.15, 0.43), "Synth_Green"),
    ("LowerLeg.R", (-0.1, 0, 0.31), (0.13, 0.14, 0.42), "Synth_Green"),
    ("Foot.R", (-0.1, -0.05, 0.05), (0.12, 0.25, 0.1), "Synth_Red"),
]

# ----------------------------------------------------------------------------- Ant-Man
# Built at normal size; Shrink/Giant scale is applied at runtime by FighterView.

ANT_MAN_COLORS = {
    "Suit_Red": (0.6, 0.06, 0.06), "Suit_Black": (0.07, 0.07, 0.08), "Helmet_Silver": (0.62, 0.64, 0.68),
    "Lens_Red": (0.95, 0.15, 0.1), "Pym_Blue": (0.2, 0.55, 1.0),
}

ANT_MAN_PARTS = [
    ("Hips", (0, 0, 1.0), (0.34, 0.21, 0.16), "Suit_Black"),
    ("Hips", (0, -0.11, 1.03), (0.12, 0.02, 0.07), "Helmet_Silver"),      # belt buckle
    ("Hips", (0.15, -0.06, 1.0), (0.04, 0.04, 0.06), "Pym_Blue"),         # Pym particle canisters
    ("Hips", (-0.15, -0.06, 1.0), (0.04, 0.04, 0.06), "Lens_Red"),
    ("Spine", (0, 0, 1.19), (0.31, 0.19, 0.18), "Suit_Red"),
    ("Chest", (0, 0, 1.39), (0.42, 0.24, 0.24), "Suit_Red"),
    ("Chest", (0, -0.125, 1.39), (0.22, 0.02, 0.2), "Suit_Black"),       # chest panel
    ("Chest", (0, 0.13, 1.4), (0.24, 0.06, 0.26), "Helmet_Silver"),      # backpack regulator
    ("Neck", (0, 0, 1.55), (0.1, 0.1, 0.1), "Suit_Black"),
    ("Head", (0, 0, 1.73), (0.24, 0.26, 0.27), "Helmet_Silver"),
    ("Head", (0, -0.13, 1.66), (0.14, 0.02, 0.06), "Suit_Black"),        # mouth grille
    ("Head", (0.05, -0.133, 1.75), (0.07, 0.01, 0.06), "Lens_Red"),
    ("Head", (-0.05, -0.133, 1.75), (0.07, 0.01, 0.06), "Lens_Red"),
    ("Head", (0.1, -0.04, 1.92), (0.02, 0.02, 0.14), "Helmet_Silver"),   # antennae
    ("Head", (-0.1, -0.04, 1.92), (0.02, 0.02, 0.14), "Helmet_Silver"),
    ("Shoulder.L", (0.13, 0, 1.45), (0.14, 0.18, 0.16), "Suit_Black"),
    ("UpperArm.L", (0.325, 0, 1.45), (0.27, 0.12, 0.12), "Suit_Red"),
    ("LowerArm.L", (0.585, 0, 1.45), (0.25, 0.12, 0.12), "Suit_Black"),
    ("Hand.L", (0.765, 0, 1.45), (0.1, 0.1, 0.1), "Suit_Black"),
    ("Hand.L", (0.68, -0.06, 1.47), (0.06, 0.02, 0.04), "Pym_Blue"),     # size-change switches
    ("Shoulder.R", (-0.13, 0, 1.45), (0.14, 0.18, 0.16), "Suit_Black"),
    ("UpperArm.R", (-0.325, 0, 1.45), (0.27, 0.12, 0.12), "Suit_Red"),
    ("LowerArm.R", (-0.585, 0, 1.45), (0.25, 0.12, 0.12), "Suit_Black"),
    ("Hand.R", (-0.765, 0, 1.45), (0.1, 0.1, 0.1), "Suit_Black"),
    ("Hand.R", (-0.68, -0.06, 1.47), (0.06, 0.02, 0.04), "Lens_Red"),
    ("UpperLeg.L", (0.11, 0, 0.735), (0.15, 0.16, 0.43), "Suit_Red"),
    ("LowerLeg.L", (0.11, 0, 0.31), (0.14, 0.15, 0.42), "Suit_Black"),
    ("Foot.L", (0.11, -0.05, 0.05), (0.13, 0.26, 0.1), "Suit_Black"),
    ("UpperLeg.R", (-0.11, 0, 0.735), (0.15, 0.16, 0.43), "Suit_Red"),
    ("LowerLeg.R", (-0.11, 0, 0.31), (0.14, 0.15, 0.42), "Suit_Black"),
    ("Foot.R", (-0.11, -0.05, 0.05), (0.13, 0.26, 0.1), "Suit_Black"),
]


def build_rig(name, bones):
    data = bpy.data.armatures.new(name)
    rig = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    for bone, (head, tail, parent) in bones.items():
        eb = data.edit_bones.new(bone)
        eb.head, eb.tail = head, tail
        eb.roll = 0.0
        if parent:
            eb.parent = data.edit_bones[parent]
            eb.use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    return rig


def add_box(bm, deform, group_index, center, size, mat_index):
    geom = bmesh.ops.create_cube(bm, size=1.0)
    for v in geom["verts"]:
        v.co = Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2])) + Vector(center)
        v[deform][group_index] = 1.0
    for f in {f for v in geom["verts"] for f in v.link_faces}:
        f.material_index = mat_index


def add_disc(bm, deform, group_index, center, radius, depth, mat_index, segments=24):
    geom = bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segments,
                                 radius1=radius, radius2=radius, depth=depth)
    rot = Matrix.Rotation(1.5707963, 4, "X")  # cone axis Z -> Y, so the disc faces forward
    for v in geom["verts"]:
        v.co = (rot @ v.co) + center
        v[deform][group_index] = 1.0
    for f in {f for v in geom["verts"] for f in v.link_faces}:
        f.material_index = mat_index


def add_star(bm, deform, group_index, center, radius, depth, mat_index):
    import math
    outer = []
    for i in range(10):
        r = radius if i % 2 == 0 else radius * 0.42
        a = math.pi / 2 + i * math.pi / 5
        outer.append(Vector((math.cos(a) * r, 0, math.sin(a) * r)))
    front = [bm.verts.new(center + p + Vector((0, -depth, 0))) for p in outer]
    back = [bm.verts.new(center + p) for p in outer]
    hub_f = bm.verts.new(center + Vector((0, -depth, 0)))
    for i in range(10):
        j = (i + 1) % 10
        f = bm.faces.new((hub_f, front[j], front[i]))
        f.material_index = mat_index
        side = bm.faces.new((front[i], front[j], back[j], back[i]))
        side.material_index = mat_index
    for v in front + back + [hub_f]:
        v[deform][group_index] = 1.0


def build_mesh(rig, hero_name, look, bones):
    mesh = bpy.data.meshes.new(hero_name + "_Body")
    obj = bpy.data.objects.new(hero_name + "_Body", mesh)
    bpy.context.scene.collection.objects.link(obj)
    mats = [rp.material(n, c) for n, c in look["colors"].items()]
    for m in mats:
        mesh.materials.append(m)
    index_of = {m.name: i for i, m in enumerate(mats)}
    groups = {b: obj.vertex_groups.new(name=b).index for b in bones if b != "Root"}

    bm = bmesh.new()
    deform = bm.verts.layers.deform.verify()
    for bone, center, size, mat in look["parts"]:
        add_box(bm, deform, groups[bone], center, size, index_of[mat])
    if look.get("extras"):
        look["extras"](bm, deform, groups, index_of)
    bm.to_mesh(mesh)
    bm.free()

    obj.parent = rig
    mod = obj.modifiers.new("Armature", "ARMATURE")
    mod.object = rig
    return obj


# ----------------------------------------------------------------------------- pose library
# Rotations are (axis, degrees) in armature space relative to rest; see rp.rest_relative.
# Conventions: arm down = Y +70 (L) / Y -70 (R); arm forward = Z -90 (L) / Z +90 (R);
# leg forward = X negative; knee bend = X positive; chest lean forward = X positive.

def arm(side, down, fwd=0):
    s = 1 if side == "L" else -1
    return [("Y", s * down), ("X", -fwd)]


def reach(side, up=0):
    """Arm straight forward, optionally raised (negative up = lowered)."""
    s = 1 if side == "L" else -1
    return [("Z", -s * 90), ("X", -up)]


def bend(side, deg):
    s = 1 if side == "L" else -1
    return [("Z", -s * deg)]


def leg(side, fwd, out=4):
    s = 1 if side == "L" else -1
    return [("Y", -s * out), ("X", -fwd)]


def pose(**bones):
    """Pose keys use underscores (UpperArm_L) so poses can be derived with dict(BASE, UpperArm_L=...)."""
    bones.setdefault("root_z", 0.0)
    return bones


def bone_name(key):
    return key[:-2] + "." + key[-1] if key.endswith(("_L", "_R")) else key


READY = pose(UpperArm_L=arm("L", 70, 20), LowerArm_L=bend("L", 70), UpperArm_R=arm("R", 70, 20), LowerArm_R=bend("R", 70),
             UpperLeg_L=leg("L", 8), UpperLeg_R=leg("R", -8), LowerLeg_L=[("X", 10)], LowerLeg_R=[("X", 10)], Chest=[("Z", 12)])
POSES = {
    "ready": READY,
    "breathe": dict(READY, Chest=[("Z", 12), ("X", -3)], Head=[("X", 3)], root_z=-0.01),
    "walk_a": dict(READY, UpperLeg_L=leg("L", 28), UpperLeg_R=leg("R", -24), LowerLeg_R=[("X", 30)],
                   UpperArm_L=arm("L", 72, -15), UpperArm_R=arm("R", 72, 25)),
    "walk_b": dict(READY, UpperLeg_L=leg("L", -24), UpperLeg_R=leg("R", 28), LowerLeg_L=[("X", 30)],
                   UpperArm_L=arm("L", 72, 25), UpperArm_R=arm("R", 72, -15)),
    "windup_r": dict(READY, UpperArm_R=arm("R", 60, -30), LowerArm_R=bend("R", 110), Chest=[("Z", 30)]),
    "punch_r": dict(READY, UpperArm_R=reach("R", 5), LowerArm_R=[], Chest=[("Z", -20), ("X", 8)], UpperLeg_L=leg("L", 20)),
    "windup_l": dict(READY, UpperArm_L=arm("L", 60, -30), LowerArm_L=bend("L", 110), Chest=[("Z", -15)]),
    "punch_l": dict(READY, UpperArm_L=reach("L", 5), LowerArm_L=[], Chest=[("Z", 30), ("X", 6)]),
    "bash": dict(READY, UpperArm_L=reach("L", 10), LowerArm_L=bend("L", 20), Chest=[("Z", 35), ("X", 15)], UpperLeg_L=leg("L", 30)),
    "kick_r": dict(READY, UpperLeg_R=leg("R", 85), LowerLeg_R=[], Chest=[("X", -12)], UpperArm_L=arm("L", 40, 20)),
    "crouch": dict(READY, UpperLeg_L=leg("L", 60), LowerLeg_L=[("X", 100)], UpperLeg_R=leg("R", 60), LowerLeg_R=[("X", 100)],
                   Chest=[("X", 20)], root_z=-0.35),
    "uppercut": dict(READY, UpperArm_R=reach("R", 70), LowerArm_R=bend("R", 20), Chest=[("Z", -25), ("X", -15)],
                     UpperLeg_L=leg("L", 10), UpperLeg_R=leg("R", -10), root_z=0.05),
    "sweep": dict(READY, UpperLeg_L=leg("L", 60), LowerLeg_L=[("X", 110)], UpperLeg_R=leg("R", 85, 10), LowerLeg_R=[],
                  UpperArm_L=reach("L", -40), Chest=[("X", 30)], root_z=-0.38),
    "throw_back": dict(READY, UpperArm_L=[("Z", 60)], LowerArm_L=[], Chest=[("Z", -35)]),
    "throw": dict(READY, UpperArm_L=[("Z", -110)], LowerArm_L=[], Chest=[("Z", 40), ("X", 10)], UpperLeg_L=leg("L", 25)),
    "air_tuck": dict(READY, UpperLeg_L=leg("L", 70), LowerLeg_L=[("X", 100)], UpperLeg_R=leg("R", 50), LowerLeg_R=[("X", 90)]),
    "air_punch": dict(READY, UpperLeg_L=leg("L", 70), LowerLeg_L=[("X", 100)], UpperLeg_R=leg("R", 30), LowerLeg_R=[("X", 50)],
                      UpperArm_R=reach("R", -30), LowerArm_R=[], Chest=[("X", 20)]),
    "overhead": dict(READY, UpperArm_L=arm("L", -80), UpperArm_R=arm("R", -80), LowerArm_L=[], LowerArm_R=[], Chest=[("X", -20)],
                     UpperLeg_L=leg("L", 50), LowerLeg_L=[("X", 80)], UpperLeg_R=leg("R", 50), LowerLeg_R=[("X", 80)]),
    "slam": dict(READY, UpperArm_L=reach("L", -50), UpperArm_R=reach("R", -50), LowerArm_L=[], LowerArm_R=[], Chest=[("X", 40)],
                 UpperLeg_L=leg("L", 70), LowerLeg_L=[("X", 110)], UpperLeg_R=leg("R", 40), LowerLeg_R=[("X", 70)], root_z=-0.3),
    "power": dict(READY, UpperArm_L=arm("L", -45), UpperArm_R=arm("R", -45), LowerArm_L=[], LowerArm_R=[], Chest=[("X", -10)],
                  Head=[("X", -15)], UpperLeg_L=leg("L", 0, 14), UpperLeg_R=leg("R", 0, 14)),
    "salute": dict(READY, UpperArm_R=[("Z", 60), ("X", -55)], LowerArm_R=[("Z", 130)], Chest=[("X", -5)]),
    "block": dict(READY, UpperArm_L=reach("L", 15), LowerArm_L=bend("L", 50), UpperLeg_L=leg("L", 25), LowerLeg_L=[("X", 30)],
                  UpperLeg_R=leg("R", -10), LowerLeg_R=[("X", 30)], Chest=[("Z", 30), ("X", 10)], root_z=-0.08),
    "hurt": dict(READY, Chest=[("X", -25)], Head=[("X", -20)], UpperArm_L=arm("L", 40, -30), UpperArm_R=arm("R", 40, -30)),
    "juggle": dict(READY, Root=[("X", -35)], Chest=[("X", -20)], Head=[("X", -20)], UpperArm_L=arm("L", 20, -40),
                   UpperArm_R=arm("R", 20, -40), UpperLeg_L=leg("L", 40), LowerLeg_L=[("X", 60)], root_z=0.4),
    "fall": dict(READY, Root=[("X", -60)], Chest=[("X", -20)], UpperArm_L=arm("L", 10, -40), UpperArm_R=arm("R", 10, -40), root_z=0.3),
    "lying": dict(READY, Root=[("X", -90)], UpperArm_L=arm("L", 80), UpperArm_R=arm("R", 80), LowerArm_L=[], LowerArm_R=[],
                  UpperLeg_L=leg("L", 0, 10), UpperLeg_R=leg("R", 0, 10), LowerLeg_L=[], LowerLeg_R=[], root_z=0.14),
    "jump": dict(READY, UpperLeg_L=leg("L", 40), LowerLeg_L=[("X", 60)], UpperLeg_R=leg("R", 10), LowerLeg_R=[("X", 40)],
                 UpperArm_L=arm("L", 45), UpperArm_R=arm("R", 45)),
    "roll": dict(READY, Root=[("X", 40)], UpperLeg_L=leg("L", 80), LowerLeg_L=[("X", 110)], UpperLeg_R=leg("R", 80),
                 LowerLeg_R=[("X", 110)], Chest=[("X", 30)], root_z=-0.2),
    # Iron Man: repulsor palms (hand bent up), unibeam chest-out, hover, flight dive.
    "palm_r": dict(READY, UpperArm_R=reach("R", 5), LowerArm_R=[], Hand_R=[("Y", 70)], Chest=[("Z", -20)], UpperLeg_L=leg("L", 18)),
    "palms": dict(READY, UpperArm_L=reach("L", 5), UpperArm_R=reach("R", 5), LowerArm_L=[], LowerArm_R=[], Hand_L=[("Y", -70)],
                  Hand_R=[("Y", 70)], Chest=[("X", 5)], UpperLeg_L=leg("L", 20), LowerLeg_R=[("X", 25)]),
    "beam_charge": dict(READY, UpperArm_L=[("Z", 50)], UpperArm_R=[("Z", -50)], LowerArm_L=[], LowerArm_R=[], Chest=[("X", 10)], root_z=-0.05),
    "beam": dict(READY, UpperArm_L=[("Z", 70), ("X", 20)], UpperArm_R=[("Z", -70), ("X", 20)], LowerArm_L=[], LowerArm_R=[],
                 Chest=[("X", -22)], Head=[("X", -10)], UpperLeg_L=leg("L", 15), UpperLeg_R=leg("R", -15)),
    "hover": dict(READY, UpperArm_L=arm("L", 75, -15), UpperArm_R=arm("R", 75, -15), LowerArm_L=[], LowerArm_R=[],
                  Hand_L=[("Z", 60)], Hand_R=[("Z", -60)], UpperLeg_L=leg("L", -5, 3), UpperLeg_R=leg("R", 5, 3), LowerLeg_L=[("X", 15)]),
    "fly_dive": dict(READY, Root=[("X", 70)], UpperArm_L=arm("L", 80, -60), UpperArm_R=arm("R", 80, -60), LowerArm_L=[], LowerArm_R=[],
                     UpperLeg_L=leg("L", -10, 2), UpperLeg_R=leg("R", -10, 2), root_z=0.3),
    # Hulk / Vision / Ant-Man
    "clap": dict(READY, UpperArm_L=reach("L", 10), UpperArm_R=reach("R", 10), LowerArm_L=bend("L", 25), LowerArm_R=bend("R", 25),
                 Chest=[("X", 12)], UpperLeg_L=leg("L", 20), LowerLeg_L=[("X", 30)], root_z=-0.06),
    "clap_open": dict(READY, UpperArm_L=arm("L", 10), UpperArm_R=arm("R", 10), LowerArm_L=[], LowerArm_R=[], Chest=[("X", -12)]),
    "stomp_up": dict(READY, UpperLeg_R=leg("R", 75), LowerLeg_R=[("X", 80)], UpperArm_L=arm("L", 40, 10), UpperArm_R=arm("R", 40, 10),
                     Chest=[("X", -8)]),
    "stomp": dict(READY, UpperLeg_R=leg("R", 20), LowerLeg_R=[("X", 10)], UpperLeg_L=leg("L", -10), LowerLeg_L=[("X", 40)],
                  UpperArm_L=arm("L", 75, -10), UpperArm_R=arm("R", 75, -10), Chest=[("X", 25)], root_z=-0.12),
    "lift": dict(READY, UpperArm_L=arm("L", -70), UpperArm_R=arm("R", -70), LowerArm_L=[], LowerArm_R=[],
                 Chest=[("X", -15)], UpperLeg_L=leg("L", 10, 14), UpperLeg_R=leg("R", 10, 14), root_z=-0.05),
    "roar": dict(READY, UpperArm_L=arm("L", 20, -30), UpperArm_R=arm("R", 20, -30), LowerArm_L=bend("L", 80), LowerArm_R=bend("R", 80),
                 Chest=[("X", -18)], Head=[("X", -25)], UpperLeg_L=leg("L", 0, 16), UpperLeg_R=leg("R", 0, 16), root_z=-0.06),
    "gem_charge": dict(READY, UpperArm_L=arm("L", 75, -25), UpperArm_R=arm("R", 75, -25), LowerArm_L=[], LowerArm_R=[],
                       Chest=[("X", -10)], Head=[("X", -12)]),
    "gem_beam": dict(READY, UpperArm_L=arm("L", 60, -45), UpperArm_R=arm("R", 60, -45), LowerArm_L=[], LowerArm_R=[],
                     Chest=[("X", 14)], Head=[("X", 10)], UpperLeg_L=leg("L", 15), UpperLeg_R=leg("R", -15)),
    "float": dict(READY, UpperArm_L=arm("L", 65, 10), UpperArm_R=arm("R", 65, 10), LowerArm_L=bend("L", 25), LowerArm_R=bend("R", 25),
                  UpperLeg_L=leg("L", 10, 3), UpperLeg_R=leg("R", -4, 3), LowerLeg_L=[("X", 25)], LowerLeg_R=[("X", 10)]),
    "phase_lunge": dict(READY, UpperArm_R=reach("R", 0), LowerArm_R=[], UpperArm_L=arm("L", 75, -30), Chest=[("Z", -25), ("X", 18)],
                        UpperLeg_L=leg("L", 40), LowerLeg_L=[("X", 40)], UpperLeg_R=leg("R", -30), root_z=-0.1),
    "flex": dict(READY, UpperArm_L=arm("L", -10), UpperArm_R=arm("R", -10), LowerArm_L=[("Y", -100)], LowerArm_R=[("Y", 100)],
                 Chest=[("X", -6)], UpperLeg_L=leg("L", 0, 12), UpperLeg_R=leg("R", 0, 12)),
    "land": dict(READY, UpperLeg_L=leg("L", 45), LowerLeg_L=[("X", 70)], UpperLeg_R=leg("R", 45), LowerLeg_R=[("X", 70)],
                 Chest=[("X", 15)], root_z=-0.22),
}

# Move name -> (windup pose, strike pose). Anything not listed falls back on its command type.
CAP_MOVE_POSES = {
    "Jab": ("windup_l", "punch_l"), "Cross": ("windup_r", "punch_r"), "Shield Bash": ("windup_l", "bash"),
    "Liberty Kick": ("crouch", "kick_r"), "Step-In Elbow": ("windup_r", "punch_r"), "Low Shield Sweep": ("crouch", "sweep"),
    "Haymaker": ("windup_r", "punch_r"), "Star-Spangled Uppercut": ("crouch", "uppercut"), "Shield Charge": ("windup_l", "bash"),
    "Shield Sweep": ("crouch", "sweep"), "Air Jab": ("air_tuck", "air_punch"), "Air Shield Swipe": ("air_tuck", "throw"),
    "Air Hammer Kick": ("air_tuck", "kick_r"), "Aerial Shield Spike": ("overhead", "slam"), "Shield Drop": ("overhead", "slam"),
    "Shield Toss": ("throw_back", "throw"), "Air Shield Toss": ("throw_back", "throw"), "Rising Shield Slash": ("crouch", "uppercut"),
    "Shield Plant": ("block", "block"), "Charging Star": ("windup_l", "bash"), "Shield Slingshot": ("throw_back", "throw"),
    "Brooklyn Brawl": ("punch_l", "punch_r"), "Leap of Liberty": ("crouch", "slam"), "Rally Cry": ("ready", "power"),
    "Shield of Freedom": ("throw_back", "throw"), "Avengers Assemble": ("salute", "power"), "Star-Spangled Stand": ("block", "power"),
    "Shoulder Toss": ("block", "throw"),
}


IRON_MAN_MOVE_POSES = {
    "Gauntlet Jab": ("windup_l", "punch_l"), "Repulsor Palm": ("windup_r", "palm_r"), "Thruster Kick": ("crouch", "kick_r"),
    "Unibeam Burst": ("beam_charge", "beam"), "Boot-Jet Launcher": ("crouch", "uppercut"), "Air Palm": ("air_tuck", "palm_r"),
    "Air Thruster Spin": ("overhead", "slam"), "Repulsor Blast": ("windup_r", "palm_r"), "Smart Missiles": ("ready", "beam"),
    "Pulse Bolts": ("windup_r", "palms"), "Repulsor Barrage": ("palm_r", "palms"), "Afterburner Dive": ("hover", "fly_dive"),
    "Overclock Protocol": ("beam_charge", "power"), "Unibeam": ("beam_charge", "beam"), "Proton Cannon": ("crouch", "palms"),
    "Iron Legion": ("salute", "power"), "Jet Toss": ("block", "throw"),
}

HULK_MOVE_POSES = {
    "Hammer Fist": ("windup_r", "punch_r"), "Backhand": ("windup_l", "punch_l"), "Double Axe Handle": ("overhead", "slam"),
    "Gamma Haymaker": ("windup_r", "punch_r"), "Hulk Uppercut": ("crouch", "uppercut"), "Gamma Charge": ("windup_l", "bash"),
    "Stomp": ("stomp_up", "stomp"), "Air Smash": ("air_tuck", "air_punch"), "Meteor Drop": ("overhead", "slam"),
    "Boulder Toss": ("lift", "throw"), "Gamma Leap": ("crouch", "jump"), "Thunderclap": ("clap_open", "clap"),
    "Shoulder Barge": ("windup_l", "bash"), "Ground Clap": ("overhead", "slam"), "Gamma Wave": ("overhead", "slam"),
    "Smash Flurry": ("punch_l", "punch_r"), "Earthshaker Leap": ("crouch", "slam"), "Car Fists": ("lift", "clap"),
    "Gamma Crush": ("lift", "slam"), "World Breaker Smash": ("roar", "slam"), "Gamma Rage": ("flex", "roar"),
    "World Breaker Throw": ("lift", "throw"), "Savage Slam": ("lift", "slam"), "Gamma Toss": ("block", "throw"),
}

VISION_MOVE_POSES = {
    "Synthezoid Jab": ("windup_l", "punch_l"), "Synthezoid Cross": ("windup_r", "punch_r"), "Phase Backfist": ("windup_l", "phase_lunge"),
    "Solid Palm": ("windup_r", "palm_r"), "Density Uppercut": ("crouch", "uppercut"), "Solid Lance": ("windup_r", "phase_lunge"),
    "Phase Sweep": ("crouch", "sweep"), "Air Phase Strike": ("float", "air_punch"), "Air Solar Kick": ("air_tuck", "kick_r"),
    "Diamond Dive": ("overhead", "fly_dive"), "Mind Stone Beam": ("gem_charge", "gem_beam"), "Ascension": ("crouch", "float"),
    "Phase Drop": ("float", "slam"), "Intangible Rush": ("float", "phase_lunge"), "Solar Lances": ("gem_charge", "palms"),
    "Diamond Hammer": ("overhead", "slam"), "Phantom Flight": ("float", "fly_dive"), "Mind Stone Pulse": ("gem_charge", "power"),
    "Synthezoid Overdrive": ("float", "power"), "Mind Stone Solar Beam": ("gem_charge", "gem_beam"), "Phase Field": ("float", "power"),
    "Density Throw": ("block", "throw"),
}

ANT_MAN_MOVE_POSES = {
    "Quick Jab": ("windup_l", "punch_l"), "Quick Hook": ("windup_r", "punch_r"), "Shrink Dash Punch": ("crouch", "phase_lunge"),
    "Pym Haymaker": ("windup_r", "punch_r"), "Growth Spurt": ("crouch", "uppercut"), "Giant Shoulder": ("windup_l", "bash"),
    "Ant Sweep": ("crouch", "sweep"), "Ant Kick": ("air_tuck", "kick_r"), "Pinball Kick": ("air_tuck", "kick_r"),
    "Size Drop": ("overhead", "slam"), "Red Disc": ("throw_back", "throw"), "Blue Disc": ("throw_back", "throw"),
    "Flying Ant Mount": ("crouch", "float"), "Ant Swarm": ("ready", "power"), "Pinball": ("roll", "phase_lunge"),
    "Giant Stomp": ("stomp_up", "stomp"), "Quantum Dive": ("overhead", "fly_dive"), "Colony Call": ("salute", "power"),
    "Giant-Man Rampage": ("stomp_up", "stomp"), "Giant-Man": ("crouch", "flex"), "Quantum Tunnel": ("block", "power"),
    "Pym Toss": ("block", "throw"),
}

# Each proxy: palette, armor boxes, extra Prop_ bones, extra geometry, signature poses, mode overrides,
# and an optional uniform scale so the model matches the simulation's body height.
LOOKS = {
    "captain_america": dict(colors=CAP_COLORS, parts=CAP_PARTS, prop_bones=CAP_PROP_BONES, extras=cap_shield,
                            move_poses=CAP_MOVE_POSES, mode_poses={}),
    "iron_man": dict(colors=IRON_MAN_COLORS, parts=IRON_MAN_PARTS, prop_bones={}, extras=None,
                     move_poses=IRON_MAN_MOVE_POSES, mode_poses={"Airborne": "hover", "Taunt": "power"}),
    "hulk": dict(colors=HULK_COLORS, parts=HULK_PARTS, prop_bones={}, extras=None, scale=1.3,
                 move_poses=HULK_MOVE_POSES, mode_poses={"Taunt": "roar"}),
    "vision": dict(colors=VISION_COLORS, parts=VISION_PARTS, prop_bones={}, extras=None,
                   move_poses=VISION_MOVE_POSES, mode_poses={"Airborne": "float", "Taunt": "gem_charge"}),
    "ant_man": dict(colors=ANT_MAN_COLORS, parts=ANT_MAN_PARTS, prop_bones={}, extras=None, scale=0.95,
                    move_poses=ANT_MAN_MOVE_POSES, mode_poses={"Taunt": "flex"}),
}


def poses_for(move, look):
    if move["name"] in look["move_poses"]:
        return look["move_poses"][move["name"]]
    if move["tier"] in ("Super", "Ultimate"):
        return "salute", "power"
    if move["aerial"]:
        return ("overhead", "slam") if "FloorBreak" in move["flags"] else ("air_tuck", "air_punch")
    if move["input"] == "Heavy" and move["direction"] == "Up":
        return "crouch", "uppercut"
    if move["direction"] == "Down":
        return "crouch", "sweep"
    if move["input"] in ("Special", "HeroLight", "Grab") or move["projectile"]:
        return "throw_back", "throw"
    return "windup_r", "punch_r"


# ----------------------------------------------------------------------------- animation

SCALE = 1.0  # the current look's uniform scale; pose heights (root_z) are authored at 1.0


def resolve(rig, p):
    """Pose dict of (axis, deg) lists -> {bone: quaternion} plus root height."""
    out = {}
    for bone, rots in p.items():
        if bone == "root_z":
            continue
        name = bone_name(bone)
        out[name] = rp.rest_relative(rig.pose.bones[name], rots)
    return out, p.get("root_z", 0.0) * SCALE


def sample_keys(total, step, extra=()):
    frames = set(range(0, total + 1, step))
    frames.add(total)
    frames.update(f for f in extra if 0 <= f <= total)
    return sorted(frames)


def ease(t):
    return t * t * (3 - 2 * t)


def bake(rig, name, total, pose_at, step):
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    rig.animation_data.action = action
    for f in sample_keys(total, step, getattr(pose_at, "extra", ())):
        quats, z = pose_at(f)
        rp.key_pose(rig, quats, Vector((0, 0, z)), f)
    rp.make_constant(action)
    return action


def blend(rig, a, b, t):
    qa, za = resolve(rig, POSES[a])
    qb, zb = resolve(rig, POSES[b])
    return rp.blend_poses(qa, qb, t), za + (zb - za) * t


def move_clip(rig, move, look):
    windup, strike = poses_for(move, look)
    s, a, r, total = move["startup"], move["active"], move["recovery"], move["total"]
    step = 1 if move["tier"] in ("Super", "Ultimate") else 2

    def pose_at(f):
        if f <= s:
            return blend(rig, "ready", windup, ease(min(1.0, f / max(1.0, s * 0.7))))
        if f <= s + a:
            return blend(rig, strike, strike, 0.0)
        return blend(rig, strike, "ready", ease((f - s - a) / max(1, r)))

    pose_at.extra = (s, s + 1, s + a)  # the impact always lands exactly on the first active frame
    return bake(rig, move["name"], total, pose_at, step)


def mode_clip(rig, name, length, look):
    import math
    if name == "Idle":
        fn = lambda f: blend(rig, "ready", "breathe", 0.5 - 0.5 * math.cos(2 * math.pi * f / length))
    elif name == "Walk":
        fn = lambda f: blend(rig, "walk_a", "walk_b", 0.5 - 0.5 * math.cos(2 * math.pi * f / length))
    elif name == "Knockdown" or name == "Dead":
        fn = lambda f: blend(rig, "fall", "lying", ease(min(1.0, f / 10.0)))
    else:
        target = {"Guard": "block", "Blockstun": "block", "Airborne": "jump", "Recover": "land", "Evade": "roll",
                  "Taunt": "salute", "Hitstun": "hurt", "Juggle": "juggle"}[name]
        target = look["mode_poses"].get(name, target)
        start = {"Recover": "land", "Hitstun": "hurt", "Taunt": "ready", "Evade": "roll"}.get(name, target)
        fn = lambda f: blend(rig, start, target, ease(min(1.0, f / max(1.0, length * 0.5))))
    return bake(rig, name, length, fn, 2)


# ----------------------------------------------------------------------------- main

def scaled(look, bones):
    """Applies the look's uniform scale to the skeleton and the box parts."""
    k = look.get("scale", 1.0)
    mul = lambda v: tuple(c * k for c in v)
    bones = {b: (mul(h), mul(t), parent) for b, (h, t, parent) in bones.items()}
    look = dict(look, parts=[(b, mul(c), mul(size), m) for b, c, size, m in look["parts"]])
    return look, bones


def build(hero_slug):
    global SCALE
    data = rp.load_frame_data(hero_slug)
    rp.reset_scene()
    rig_name = hero_slug.title().replace("_", "") + "_Rig"
    if hero_slug not in LOOKS:
        raise SystemExit("No proxy look for %s; add one to LOOKS" % hero_slug)
    look, bones = scaled(LOOKS[hero_slug], dict(BONES, **LOOKS[hero_slug]["prop_bones"]))
    if look.get("extras") and look.get("scale", 1.0) != 1.0:
        raise SystemExit("extras geometry is authored at scale 1; scale it before using it with a scaled look")
    SCALE = look.get("scale", 1.0)
    rig = build_rig(rig_name, bones)
    build_mesh(rig, hero_slug.title().replace("_", ""), look, bones)
    rig.animation_data_create()

    for move in data["moves"]:
        move_clip(rig, move, look)
    for clip, (length, _) in rp.MODE_CLIPS.items():
        mode_clip(rig, clip, length, look)
    rig.animation_data.action = bpy.data.actions["Idle"]

    errors = rp.validate_hero(data)
    if errors:
        for e in errors:
            print("VALIDATION ERROR:", e)
        raise SystemExit(1)

    out_dir = os.path.join(rp.CHARACTER_OUT, hero_slug)
    fbx = rp.export_fbx(os.path.join(out_dir, hero_slug + ".fbx"), {"ARMATURE", "MESH"}, animated=True)
    source_dir = os.path.join(rp.REPO_ROOT, "ArtSource", "Characters")
    os.makedirs(source_dir, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(source_dir, hero_slug + "_proxy.blend"))
    print("OK %s: %d actions, %d bones -> %s" % (data["hero"], len(bpy.data.actions), len(rig.data.bones), fbx))


if __name__ == "__main__":
    args = rp.script_args()
    build(args[0] if args else "captain_america")
