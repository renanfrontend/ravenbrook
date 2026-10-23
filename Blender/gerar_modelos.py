# Gera os modelos 3D do Ravenbrook no Blender e exporta em FBX para Assets/Resources/Modelos.
#
# Uso (o Unity chama isto pelo menu Ravenbrook > Gerar modelos no Blender):
#   blender.exe -b --factory-startup -P Blender/gerar_modelos.py -- <pasta de saída>
#
# Os personagens saem em peças com o pivô na articulação (Quadril, Tronco, Cabeca,
# BracoE, BracoD, PernaE, PernaD). O Unity anima essas peças por código (ProceduralBody).
# As coordenadas abaixo estão no sistema do Unity (X direita, Y cima, Z frente) e são
# convertidas para o Blender (Z cima, personagem olhando para -Y) pela função B().

import bpy
import math
import os
import random
import sys
from mathutils import Vector
from bpy_extras import node_shader_utils

argv = sys.argv
OUT = argv[argv.index("--") + 1] if "--" in argv else os.path.abspath("Modelos")
os.makedirs(OUT, exist_ok=True)


def log(*a):
    print("[Ravenbrook]", *a, flush=True)


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def mat(name, rgb, rough=0.6, metal=0.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = (rgb[0], rgb[1], rgb[2], 1.0)
    try:
        w = node_shader_utils.PrincipledBSDFWrapper(m, is_readonly=False)
        w.base_color = rgb
        w.roughness = rough
        w.metallic = metal
    except Exception as e:  # versões sem nós: fica só a cor difusa
        log("material sem nós:", name, e)
    return m


def B(u):
    """Unity (x, y, z) -> Blender (x, z, y): o FBX gira 180° na conversão de eixos."""
    return Vector((u[0], u[2], u[1]))


def S(s):
    return (s[0], s[2], s[1])


def active():
    return bpy.context.view_layer.objects.active


def finish(o, m, smooth=False):
    o.data.materials.append(m)
    if smooth:
        for p in o.data.polygons:
            p.use_smooth = True
    return o


def box(c, s, m, bevel=0.0, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=B(c), rotation=rot)
    o = active()
    o.scale = S(s)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    if bevel > 0:
        bv = o.modifiers.new("chanfro", "BEVEL")
        bv.width = bevel
        bv.segments = 3
        bpy.ops.object.modifier_apply(modifier=bv.name)
    return finish(o, m, smooth=bevel > 0)


def cyl(c, r, h, m, axis="Y", verts=14, r2=None):
    rot = {"Y": (0, 0, 0), "Z": (math.pi / 2, 0, 0), "X": (0, math.pi / 2, 0)}[axis]
    if r2 is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=h, location=B(c), rotation=rot)
    else:
        bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=r2, depth=h, location=B(c), rotation=rot)
    o = active()
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return finish(o, m, smooth=True)


def sph(c, s, m, seg=18):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=seg // 2, radius=1, location=B(c))
    o = active()
    o.scale = S(s)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(o, m, smooth=True)


def join(objs, name, pivot):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    if len(objs) > 1:
        bpy.ops.object.join()
    o = active()
    o.name = name
    o.data.name = name
    bpy.context.scene.cursor.location = B(pivot)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    return o


def roughen(o, amount, seed):
    rnd = random.Random(seed)
    for v in o.data.vertices:
        v.co += Vector((rnd.uniform(-amount, amount), rnd.uniform(-amount, amount), rnd.uniform(-amount, amount)))


def tear(o, slot_mat, chance, seed):
    """Troca o material de algumas faces (roupa rasgada / manchas de sangue)."""
    rnd = random.Random(seed)
    o.data.materials.append(slot_mat)
    idx = len(o.data.materials) - 1
    for p in o.data.polygons:
        if rnd.random() < chance:
            p.material_index = idx


def export(name):
    bpy.ops.object.select_all(action="SELECT")
    path = os.path.join(OUT, name + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH", "EMPTY"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False)
    log("exportado", path)


# ---------------------------------------------------------------- personagens

def humanoid(kind, seed):
    rnd = random.Random(seed)
    brute = kind == "brute"
    zombie = kind == "zombie"
    w = 1.25 if brute else 1.0
    shoe = mat("Bota", (0.05, 0.05, 0.05), 0.45)
    if kind == "human":
        skin = mat("Pele", (0.86, 0.68, 0.55), 0.5)
        shirt = mat("Camisa", (0.12, 0.17, 0.3), 0.85)
        pants = mat("Calca", (0.1, 0.11, 0.17), 0.85)
        sleeve, forearm, hand = shirt, shirt, skin
    elif zombie:
        skin = mat("Pele", (0.5, 0.56, 0.45), 0.45)
        shirt = mat("Camisa", rnd.choice([(0.55, 0.52, 0.45), (0.45, 0.12, 0.1), (0.18, 0.25, 0.4), (0.3, 0.34, 0.28)]), 0.9)
        pants = mat("Calca", rnd.choice([(0.15, 0.17, 0.25), (0.25, 0.22, 0.18), (0.1, 0.1, 0.1)]), 0.9)
        sleeve, forearm, hand = shirt, skin, skin
    else:
        skin = mat("Borracha", (0.07, 0.07, 0.07), 0.35)
        shirt = mat("Sobretudo", (0.11, 0.12, 0.11), 0.8)
        pants = mat("Calca", (0.09, 0.09, 0.09), 0.8)
        sleeve, forearm, hand = shirt, shirt, mat("Luva", (0.05, 0.05, 0.05), 0.4)
    blood = mat("Sangue", (0.28, 0.02, 0.02), 0.25)

    parts = []
    for side, name in ((-1, "PernaE"), (1, "PernaD")):
        x = 0.11 * w * side
        objs = [box((x, 0.72, 0), (0.17 * w, 0.48, 0.19 * w), pants, 0.04),
                box((x, 0.3, 0), (0.15 * w, 0.44, 0.16 * w), pants, 0.035),
                box((x, 0.06, 0.04), (0.17 * w, 0.12, 0.28 * w), shoe, 0.03)]
        if brute:
            objs.append(box((x, 0.2, 0.01), (0.19 * w, 0.32, 0.22 * w), shoe, 0.03))
        parts.append(join(objs, name, (x, 0.95, 0)))

    hips = [box((0, 0.98, 0), (0.36 * w, 0.2, 0.22 * w), pants, 0.05)]
    if brute:
        hips.append(box((0, 0.72, 0), (0.6, 0.62, 0.36), shirt, 0.05))  # saia do sobretudo
    elif kind == "human":
        hips.append(box((0.21, 0.97, 0), (0.06, 0.18, 0.11), mat("Coldre", (0.06, 0.05, 0.05), 0.5), 0.015))
    parts.append(join(hips, "Quadril", (0, 0.98, 0)))

    torso = [box((0, 1.25, 0), (0.42 * w, 0.56, 0.24 * w), shirt, 0.07)]
    if kind == "human":
        vest = mat("Colete", (0.07, 0.07, 0.08), 0.7)
        torso += [box((0, 1.3, 0), (0.45, 0.4, 0.27), vest, 0.05),
                  box((0.1, 1.42, 0.14), (0.07, 0.08, 0.012), mat("Distintivo", (0.85, 0.7, 0.3), 0.25, 1.0), 0.005),
                  box((0, 1.06, 0), (0.4, 0.05, 0.25), vest, 0.01),
                  box((-0.12, 1.4, 0.14), (0.08, 0.05, 0.02), mat("Radio", (0.02, 0.02, 0.02), 0.4), 0.005)]
    elif brute:
        torso += [box((0, 1.28, 0), (0.62, 0.66, 0.36), shirt, 0.06),
                  box((0, 1.56, 0), (0.42, 0.1, 0.3), shirt, 0.04)]
    t = join(torso, "Tronco", (0, 0.98, 0))
    if zombie:
        tear(t, skin, 0.12, seed)
        tear(t, blood, 0.1, seed + 1)
    parts.append(t)

    head = [cyl((0, 1.57, 0), 0.05, 0.1, skin, verts=10)]
    if brute:
        rubber = mat("Mascara", (0.06, 0.06, 0.06), 0.3)
        lens = mat("Lente", (0.6, 0.04, 0.03), 0.1)
        head += [sph((0, 1.71, 0), (0.135, 0.15, 0.14), rubber),
                 cyl((-0.065, 1.74, 0.125), 0.035, 0.02, lens, axis="Z"),
                 cyl((0.065, 1.74, 0.125), 0.035, 0.02, lens, axis="Z"),
                 cyl((0, 1.61, 0.15), 0.045, 0.1, mat("Filtro", (0.25, 0.27, 0.22), 0.4, 0.6), axis="Z")]
    else:
        eye = mat("Olho", (0.95, 0.85, 0.3) if zombie else (0.05, 0.04, 0.03), 0.2)
        head += [sph((0, 1.7, 0), (0.11, 0.13, 0.12), skin),
                 sph((-0.04, 1.715, 0.1), (0.016, 0.016, 0.01), eye, 8),
                 sph((0.04, 1.715, 0.1), (0.016, 0.016, 0.01), eye, 8),
                 box((0, 1.69, 0.115), (0.025, 0.04, 0.03), skin, 0.008)]
        if kind == "human":
            hair = mat("Cabelo", (0.16, 0.1, 0.07), 0.6)
            head += [sph((0, 1.78, -0.015), (0.118, 0.08, 0.125), hair),
                     box((0, 1.69, -0.12), (0.08, 0.16, 0.04), hair, 0.015)]
        else:
            head.append(box((0, 1.63, 0.105), (0.06, 0.025, 0.02), blood, 0.005))
            if rnd.random() < 0.6:
                head.append(sph((0.01, 1.78, -0.02), (0.112, 0.07, 0.12), mat("Cabelo", (0.12, 0.1, 0.08), 0.7)))
    h = join(head, "Cabeca", (0, 1.53, 0))
    if zombie:
        roughen(h, 0.004, seed + 2)
    parts.append(h)

    for side, name in ((-1, "BracoE"), (1, "BracoD")):
        x = 0.27 * w * side
        objs = [box((x, 1.28, 0), (0.12 * w, 0.42, 0.12 * w), sleeve, 0.04),
                box((x, 1.01, 0), (0.1 * w, 0.26, 0.1 * w), forearm, 0.035),
                sph((x, 0.86, 0), (0.055 * w, 0.06, 0.055 * w), hand, 10)]
        a = join(objs, name, (x, 1.48, 0))
        if zombie:
            roughen(a, 0.004, seed + side)
        parts.append(a)
    return parts


# ---------------------------------------------------------------- objetos

def crate():
    wood = mat("Madeira", (0.6, 0.47, 0.3), 0.8)
    dark = mat("MadeiraEscura", (0.32, 0.22, 0.13), 0.8)
    objs = [box((0, 0.5, 0), (0.94, 0.94, 0.94), wood, 0.01)]
    for x in (-0.46, 0.46):
        for z in (-0.46, 0.46):
            objs.append(box((x, 0.5, z), (0.09, 1.0, 0.09), dark, 0.01))
    for y in (0.04, 0.96):
        for z in (-0.47, 0.47):
            objs.append(box((0, y, z), (1.0, 0.08, 0.06), dark, 0.01))
        for x in (-0.47, 0.47):
            objs.append(box((x, y, 0), (0.06, 0.08, 1.0), dark, 0.01))
    for z in (-0.475, 0.475):
        objs.append(box((0, 0.5, z), (1.25, 0.08, 0.04), dark, 0.005, rot=(0, math.radians(45), 0)))
    join(objs, "Caixa", (0, 0, 0))


def desk():
    wood = mat("Madeira", (0.5, 0.34, 0.2), 0.6)
    metal = mat("Metal", (0.2, 0.21, 0.22), 0.4, 0.8)
    objs = [box((0, 0.76, 0), (1.6, 0.05, 0.8), wood, 0.01),
            box((0.45, 0.43, 0), (0.6, 0.6, 0.74), wood, 0.01),
            box((-0.2, 0.5, 0.36), (0.75, 0.45, 0.02), wood, 0.005)]
    for i in range(3):
        objs.append(box((0.45, 0.22 + i * 0.19, -0.375), (0.5, 0.15, 0.02), mat("Gaveta", (0.42, 0.28, 0.16), 0.6), 0.005))
        objs.append(box((0.45, 0.25 + i * 0.19, -0.39), (0.12, 0.02, 0.02), metal, 0.003))
    for x in (-0.76, ):
        for z in (-0.36, 0.36):
            objs.append(box((x, 0.37, z), (0.05, 0.74, 0.05), metal, 0.005))
    join(objs, "Mesa", (0, 0, 0))


def barricade():
    wood = mat("Madeira", (0.5, 0.37, 0.24), 0.85)
    dark = mat("MadeiraEscura", (0.28, 0.19, 0.12), 0.85)
    nail = mat("Prego", (0.3, 0.3, 0.32), 0.3, 1.0)
    objs = [box((-0.85, 0.65, 0), (0.12, 1.3, 0.12), dark, 0.01), box((0.85, 0.65, 0), (0.12, 1.3, 0.12), dark, 0.01)]
    for y, ang in ((0.35, 7), (0.72, -11), (1.05, 4)):
        objs.append(box((0, y, 0.09), (2.0, 0.18, 0.05), wood, 0.008, rot=(0, math.radians(ang), 0)))
        for x in (-0.85, 0.85):
            objs.append(cyl((x, y + x * math.tan(math.radians(-ang)) * 0.0, 0.12), 0.012, 0.02, nail, axis="Z", verts=6))
    join(objs, "Barricada", (0, 0, 0))


def statue():
    stone = mat("Pedra", (0.55, 0.55, 0.52), 0.9)
    bronze = mat("Bronze", (0.45, 0.33, 0.18), 0.4, 0.9)
    objs = [cyl((0, 0.5, 0), 0.75, 1.0, stone, verts=24),
            cyl((0, 1.04, 0), 0.8, 0.08, stone, verts=24),
            cyl((0, 0.04, 0), 0.8, 0.08, stone, verts=24),
            cyl((0, 1.65, 0), 0.42, 1.1, stone, verts=18, r2=0.22),       # túnica
            box((0, 2.3, 0), (0.42, 0.42, 0.24), stone, 0.08),            # tronco
            sph((0, 2.62, 0), (0.13, 0.15, 0.14), stone),                 # cabeça
            sph((0, 2.68, -0.03), (0.15, 0.12, 0.15), stone),             # cabelo
            box((0.3, 2.62, 0.02), (0.1, 0.5, 0.1), stone, 0.04, rot=(0, math.radians(25), 0)),
            box((-0.26, 2.15, 0.08), (0.1, 0.45, 0.1), stone, 0.04, rot=(0, math.radians(-10), 0)),
            cyl((0.42, 2.92, 0.02), 0.04, 0.18, bronze, verts=10),
            cyl((0.42, 2.97, 0.02), 0.09, 0.06, bronze, verts=12, r2=0.05)]
    join(objs, "Estatua", (0, 0, 0))


def typewriter():
    body = mat("Corpo", (0.07, 0.07, 0.08), 0.35, 0.4)
    key = mat("Tecla", (0.85, 0.82, 0.72), 0.4)
    metal = mat("Metal", (0.55, 0.56, 0.58), 0.3, 0.9)
    paper = mat("Papel", (0.9, 0.88, 0.8), 0.9)
    objs = [box((0, 0.06, 0.02), (0.45, 0.12, 0.34), body, 0.02),
            box((0, 0.15, 0.1), (0.42, 0.1, 0.14), body, 0.02),
            cyl((0, 0.2, 0.1), 0.03, 0.46, metal, axis="X", verts=12),
            box((0, 0.3, 0.12), (0.24, 0.22, 0.004), paper, 0.0, rot=(math.radians(15), 0, 0))]
    for row in range(3):
        for i in range(9):
            objs.append(cyl((-0.16 + i * 0.04 + row * 0.01, 0.13 + row * 0.015, -0.1 + row * 0.04), 0.013, 0.012, key, verts=8))
    join(objs, "MaquinaEscrever", (0, 0, 0))


def chest():
    wood = mat("Madeira", (0.33, 0.21, 0.12), 0.7)
    metal = mat("Metal", (0.3, 0.3, 0.32), 0.35, 0.9)
    gold = mat("Fecho", (0.8, 0.65, 0.3), 0.3, 1.0)
    objs = [box((0, 0.3, 0), (1.1, 0.6, 0.6), wood, 0.015),
            cyl((0, 0.6, 0), 0.3, 1.1, wood, axis="X", verts=16)]
    for x in (-0.4, 0.4):
        objs.append(box((x, 0.3, 0), (0.06, 0.62, 0.62), metal, 0.005))
        objs.append(cyl((x, 0.6, 0), 0.31, 0.06, metal, axis="X", verts=16))
    objs.append(box((0, 0.52, 0.31), (0.12, 0.14, 0.03), gold, 0.01))
    o = join(objs, "Bau", (0, 0, 0))
    # corta a metade de baixo do cilindro da tampa (fica dentro da caixa, não aparece)


def herb():
    pot = mat("Vaso", (0.45, 0.25, 0.15), 0.8)
    soil = mat("Terra", (0.12, 0.08, 0.05), 1.0)
    leaf = mat("Folha", (0.3, 0.75, 0.3), 0.5)
    objs = [cyl((0, 0.09, 0), 0.1, 0.18, pot, verts=14, r2=0.13), cyl((0, 0.175, 0), 0.115, 0.01, soil, verts=14)]
    rnd = random.Random(5)
    for i in range(7):
        a = i / 7 * math.tau
        objs.append(sph((math.sin(a) * 0.07, 0.25 + rnd.random() * 0.08, math.cos(a) * 0.07), (0.045, 0.015, 0.09), leaf, 10))
        o = objs[-1]
        o.rotation_euler = (math.radians(rnd.uniform(-30, 30)), 0, a)
        bpy.context.view_layer.objects.active = o
        o.select_set(True)
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
        o.select_set(False)
    join(objs, "Erva", (0, 0, 0))


MODELS = [
    ("Jogador", lambda: humanoid("human", 1)),
    ("Zumbi", lambda: humanoid("zombie", 7)),
    ("Brutamonte", lambda: humanoid("brute", 3)),
    ("Caixa", crate),
    ("Mesa", desk),
    ("Barricada", barricade),
    ("Estatua", statue),
    ("MaquinaEscrever", typewriter),
    ("Bau", chest),
    ("Erva", herb),
]

if __name__ == "__main__":
    only = [a for a in argv[argv.index("--") + 2:]] if "--" in argv and len(argv) > argv.index("--") + 2 else []
    for name, build in MODELS:
        if only and name not in only:
            continue
        reset()
        build()
        export(name)
    log("pronto:", len(MODELS), "modelos em", OUT)
