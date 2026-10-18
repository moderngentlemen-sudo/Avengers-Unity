"""
Builds the graybox stage props and their pre-fractured versions, sized to match the simulation's
GrayboxStages.TestRoom so the cosmetic shatter lines up with the gameplay hit boxes.

For each prop it exports to Assets/_Project/Art/Props/:
  <slug>_intact.fbx     the whole prop, origin at its center
  <slug>_fractured.fbx  chunks "<Prop>_cell_###" under an empty "<Prop>_fractured"

Unity's ReforgedModelPostprocessor adds convex colliders and rigidbodies to *_cell_* chunks.
Fracturing is a deterministic recursive plane split (seeded), so re-running gives identical files.
Cell Fracture produces nicer shards for hero props; any tool works as long as names follow
the convention.

  python Tools/Blender/build_fractured_props.py
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import bpy  # must come first: the bpy module registers bmesh/mathutils
import bmesh
from mathutils import Vector

import reforged_pipeline as rp

# name, size (x, y=depth, z=height) in meters as in GrayboxStages, chunk count, color
PROPS = [
    ("Crate", (1.0, 1.0, 1.0), 8, (0.55, 0.38, 0.18)),
    ("Parked Car", (2.2, 1.2, 1.5), 12, (0.7, 0.1, 0.1)),
    ("Drywall", (0.3, 6.0, 4.0), 14, (0.88, 0.84, 0.72)),
    ("Weak Floor", (12.0, 6.0, 0.3), 24, (0.8, 0.52, 0.24)),
]


def prop_mesh(name, size):
    bm = bmesh.new()
    geom = bmesh.ops.create_cube(bm, size=1.0)
    for v in geom["verts"]:
        v.co = Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]))
    if name == "Parked Car":
        # cabin on top half
        cab = bmesh.ops.create_cube(bm, size=1.0)
        for v in cab["verts"]:
            v.co = Vector((v.co.x * size[0] * 0.55, v.co.y * size[1] * 0.9, v.co.z * size[2] * 0.4 + size[2] * 0.3))
        for v in geom["verts"]:
            v.co.z = v.co.z * 0.6 - size[2] * 0.2
    return bm


def split(bm, rng):
    """Splits a convex chunk with a random plane through its interior. Returns two new bmeshes."""
    coords = [v.co.copy() for v in bm.verts]
    lo = Vector((min(c.x for c in coords), min(c.y for c in coords), min(c.z for c in coords)))
    hi = Vector((max(c.x for c in coords), max(c.y for c in coords), max(c.z for c in coords)))
    ext = hi - lo
    # Cut mostly across the longest axis so chunks stay chunky.
    axis = max(range(3), key=lambda i: ext[i])
    normal = Vector((rng.uniform(-0.35, 0.35), rng.uniform(-0.35, 0.35), rng.uniform(-0.35, 0.35)))
    normal[axis] = 1.0
    normal.normalize()
    point = lo + Vector([ext[i] * rng.uniform(0.35, 0.65) for i in range(3)])

    halves = []
    for keep_front in (True, False):
        piece = bm.copy()
        geom = piece.verts[:] + piece.edges[:] + piece.faces[:]
        res = bmesh.ops.bisect_plane(piece, geom=geom, plane_co=point, plane_no=normal,
                                     clear_outer=keep_front, clear_inner=not keep_front)
        cut_edges = [e for e in res["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
        if cut_edges:
            new_faces = bmesh.ops.holes_fill(piece, edges=cut_edges, sides=0)["faces"]
            for f in new_faces:
                f.material_index = 1  # fracture interior
        bmesh.ops.recalc_face_normals(piece, faces=piece.faces[:])
        halves.append(piece)
    return halves


def volume(bm):
    return abs(bm.calc_volume())


def fracture(base, count, seed):
    rng = random.Random(seed)
    pieces = [base.copy()]
    while len(pieces) < count:
        pieces.sort(key=volume, reverse=True)
        biggest = pieces.pop(0)
        a, b = split(biggest, rng)
        if len(a.verts) < 4 or len(b.verts) < 4:
            pieces.append(biggest)
            break
        pieces += [a, b]
        biggest.free()
    return pieces


def link_mesh(name, bm, mats):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    for m in mats:
        mesh.materials.append(m)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def recenter(obj):
    """Puts the chunk's origin at its own center so its rigidbody spins naturally."""
    center = sum((v.co for v in obj.data.vertices), Vector()) / len(obj.data.vertices)
    for v in obj.data.vertices:
        v.co -= center
    obj.location = center


def build_prop(name, size, count, color, seed):
    prop_slug = rp.slug(name)
    rp.reset_scene()
    surface = rp.material(name.replace(" ", "") + "_Surface", color)
    interior = rp.material(name.replace(" ", "") + "_Interior", tuple(c * 0.55 for c in color))
    bm = prop_mesh(name, size)
    for f in bm.faces:
        f.material_index = 0
    link_mesh(name.replace(" ", ""), bm, [surface, interior])
    rp.export_fbx(os.path.join(rp.PROP_OUT, prop_slug + "_intact.fbx"), {"MESH"}, animated=False)

    rp.reset_scene()
    surface = rp.material(name.replace(" ", "") + "_Surface", color)
    interior = rp.material(name.replace(" ", "") + "_Interior", tuple(c * 0.55 for c in color))
    root = bpy.data.objects.new(name.replace(" ", "") + "_fractured", None)
    bpy.context.scene.collection.objects.link(root)
    pieces = fracture(bm, count, seed)
    total_volume = 0.0
    for i, piece in enumerate(pieces):
        total_volume += volume(piece)
        chunk = link_mesh("%s_cell_%03d" % (name.replace(" ", ""), i), piece, [surface, interior])
        recenter(chunk)
        chunk.parent = root
        piece.free()
    bm.free()
    expected = size[0] * size[1] * size[2] * (0.798 if name == "Parked Car" else 1.0)
    path = rp.export_fbx(os.path.join(rp.PROP_OUT, prop_slug + "_fractured.fbx"), {"MESH", "EMPTY"}, animated=False)
    print("OK %-11s %2d chunks, volume %.2f m^3 (solid %.2f) -> %s" % (name, len(pieces), total_volume, expected, path))
    if abs(total_volume - expected) > 0.05 * expected:
        raise SystemExit("fracture lost volume for " + name)


if __name__ == "__main__":
    for i, (name, size, count, color) in enumerate(PROPS):
        build_prop(name, size, count, color, seed=1000 + i)
