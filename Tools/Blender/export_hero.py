"""
Artist entry point: validate a hero .blend against the pipeline contract and export its FBX.

  blender -b ArtSource/Characters/captain_america.blend -P Tools/Blender/export_hero.py -- captain_america
  python Tools/Blender/export_hero.py captain_america ArtSource/Characters/captain_america.blend

Fails (exit code 1) with a list of problems if bone names, skinning, the triangle budget, or any
animation length disagrees with the gameplay frame data in Docs/FrameData.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import bpy

import reforged_pipeline as rp


def main():
    args = rp.script_args()
    if not args:
        raise SystemExit("usage: export_hero.py <hero_slug> [file.blend]")
    hero_slug = args[0]
    if len(args) > 1:
        bpy.ops.wm.open_mainfile(filepath=os.path.abspath(args[1]))
    errors = rp.validate_hero(rp.load_frame_data(hero_slug))
    if errors:
        for e in errors:
            print("VALIDATION ERROR:", e)
        raise SystemExit(1)
    path = rp.export_fbx(os.path.join(rp.CHARACTER_OUT, hero_slug, hero_slug + ".fbx"), {"ARMATURE", "MESH"}, animated=True)
    print("OK exported", path)


if __name__ == "__main__":
    main()
