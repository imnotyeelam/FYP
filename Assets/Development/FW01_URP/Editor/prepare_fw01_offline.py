"""Non-destructive YAML preparation for FW01 when the Unity Editor cannot run.

Run with Python from the project root. Unity must still import and validate output.
"""
from collections import Counter, deque
from pathlib import Path
import hashlib
import re
import shutil

ASSETS = Path("Assets")
ROOT = ASSETS / "Development/FW01_URP"
SOURCE = ASSETS / "TriForge Assets/Fantasy Worlds - Forest/Scenes/S_FW01_Demo_Summer.unity"
LIT = "933532a4fcc9baf4fa0491de14d08ed7"
REF = re.compile(r"guid: ([0-9a-f]{32})")
GUID = re.compile(r"^guid: ([0-9a-f]{32})", re.M)
SRC_SHADER = re.compile(r"(  m_Shader: \{fileID: \d+, guid: )([0-9a-f]{32})(, type: \d+\})")


def uid(key):
    return hashlib.md5(("FW01_URP/" + key).encode()).hexdigest()


def meta(path, guid):
    path.with_name(path.name + ".meta").write_text("fileFormatVersion: 2\nguid: " + guid + "\n", encoding="utf-8")


def safe(name):
    return re.sub(r"[^a-zA-Z0-9_-]", "_", name)


def refs(text):
    return set(REF.findall(text))


def saved_tex(text, name):
    match = re.search(r"^    - " + re.escape(name) + r":\s*\n        m_Texture: (\{[^\n]+\})\n        m_Scale: (\{[^\n]+\})\n        m_Offset: (\{[^\n]+\})", text, re.M)
    return match.groups() if match else None


def saved_color(text, name):
    match = re.search(r"^    - " + re.escape(name) + r": (\{[^\n]+\})", text, re.M)
    return match.group(1) if match else None


def saved_float(text, name):
    match = re.search(r"^    - " + re.escape(name) + r": ([^\n]+)", text, re.M)
    return match.group(1) if match else None


def shader_name(path):
    if path is None:
        return "<builtin or missing>"
    if path.suffix == ".shader":
        match = re.search(r'Shader\s+"([^"]+)"', path.read_text(encoding="utf-8-sig", errors="replace"))
        return match.group(1) if match else path.stem
    return path.stem


def category(path):
    s = (path.stem + " " + str(path.parent)).lower()
    return "vegetation" if any(x in s for x in ("leaf", "leaves", "flower", "grass", "bush", "plant", "fern", "weed", "foliage", "shrub", "sapling", "ivy", "moss")) else "hard surface"


def material_copy(text, vegetation):
    text = SRC_SHADER.sub(lambda m: m.group(1) + LIT + m.group(3), text, count=1)
    if "m_Shader: {fileID:" in text and LIT not in text.split("m_Shader:", 1)[1].split("\n", 1)[0]:
        text = re.sub(r"(  m_Shader: )\{[^\n]+\}", r"\g<1>{fileID: 4800000, guid: " + LIT + ", type: 3}", text, count=1)
    base = next((saved_tex(text, k) for k in ("_MainTex", "_BaseMap", "_BaseColor", "_ColorMap", "_Albedo", "_AlbedoMap", "_DiffuseMap", "_BaseLayerBaseColor", "_BaseColor1") if saved_tex(text, k) and "fileID: 0}" not in saved_tex(text, k)[0]), None)
    normal = next((saved_tex(text, k) for k in ("_BumpMap", "_NormalMap", "_NormalTex", "_BaseLayerNormal", "_Normal1") if saved_tex(text, k) and "fileID: 0}" not in saved_tex(text, k)[0]), None)
    color = next((saved_color(text, k) for k in ("_Color", "_BaseColor", "_TintColor") if saved_color(text, k)), "{r: 1, g: 1, b: 1, a: 1}")
    if base:
        entry = "    - _BaseMap:\n        m_Texture: " + base[0] + "\n        m_Scale: " + base[1] + "\n        m_Offset: " + base[2] + "\n"
        if saved_tex(text, "_BaseMap"):
            text = re.sub(r"    - _BaseMap:\n        m_Texture: \{[^\n]+\}\n        m_Scale: \{[^\n]+\}\n        m_Offset: \{[^\n]+\}\n", entry, text, count=1)
        else:
            text = re.sub(r"(    m_TexEnvs:\n)", r"\1" + entry, text, count=1)
    if normal:
        entry = "    - _BumpMap:\n        m_Texture: " + normal[0] + "\n        m_Scale: " + normal[1] + "\n        m_Offset: " + normal[2] + "\n"
        if saved_tex(text, "_BumpMap"):
            text = re.sub(r"    - _BumpMap:\n        m_Texture: \{[^\n]+\}\n        m_Scale: \{[^\n]+\}\n        m_Offset: \{[^\n]+\}\n", entry, text, count=1)
        else:
            text = re.sub(r"(    m_TexEnvs:\n)", r"\1" + entry, text, count=1)
    if saved_color(text, "_BaseColor"):
        text = re.sub(r"(    - _BaseColor: )\{[^\n]+\}", r"\1" + color, text, count=1)
    else:
        text = re.sub(r"(    m_Colors:\n)", r"\1    - _BaseColor: " + color + "\n", text, count=1)
    if vegetation:
        cutoff = next((saved_float(text, k) for k in ("_Cutoff", "_AlphaCutoff", "_Threshold") if saved_float(text, k)), "0.4")
        for key, value in (("_AlphaClip", "1"), ("_Cull", "0"), ("_Cutoff", cutoff)):
            if saved_float(text, key):
                text = re.sub(r"(    - " + key + r": )[^\n]+", lambda m: m.group(1) + value, text, count=1)
            else:
                text = re.sub(r"(    m_Floats:\n)", lambda m: m.group(1) + "    - " + key + ": " + value + "\n", text, count=1)
        text = re.sub(r"  m_CustomRenderQueue: -?\d+", "  m_CustomRenderQueue: 2450", text, count=1)
        text = re.sub(r"  m_ValidKeywords:(?: \[\])?\n(?:  - [^\n]+\n)*", "  m_ValidKeywords:\n  - _ALPHATEST_ON\n" + ("  - _NORMALMAP\n" if normal else ""), text, count=1)
    else:
        text = re.sub(r"  m_ValidKeywords:(?: \[\])?\n(?:  - [^\n]+\n)*", "  m_ValidKeywords:" + ("\n  - _NORMALMAP" if normal else " []") + "\n", text, count=1)
    text = re.sub(r"  m_InvalidKeywords:\n(?:  - [^\n]+\n)*", "  m_InvalidKeywords: []\n", text, count=1)
    return text, bool(base)


def main():
    if not SOURCE.is_file():
        raise SystemExit("Source scene missing")
    # Regeneration only writes deterministic files inside this development root.
    guid_paths = {}
    for m in ASSETS.rglob("*.meta"):
        if ROOT in m.parents:
            continue
        match = GUID.search(m.read_text(encoding="utf-8-sig", errors="replace"))
        if match:
            guid_paths[match.group(1)] = m.with_name(m.name[:-5])
    # The Lit shader belongs to PackageCache, outside Assets.
    queue = deque(refs(SOURCE.read_text(encoding="utf-8-sig", errors="replace")))
    seen = set()
    prefabs, materials, terrains = {}, {}, {}
    while queue:
        g = queue.popleft()
        if g in seen:
            continue
        seen.add(g)
        p = guid_paths.get(g)
        if not p or not p.is_file():
            continue
        if p.suffix == ".prefab":
            prefabs[g] = p
            queue.extend(refs(p.read_text(encoding="utf-8-sig", errors="replace")) - seen)
        elif p.suffix == ".mat":
            materials[g] = p
        elif p.suffix == ".asset" and "Terrain Data" in str(p):
            terrains[g] = p
            queue.extend(refs(p.read_text(encoding="utf-8-sig", errors="replace")) - seen)
    risky = {}
    already_urp = []
    families = Counter()
    for g, p in materials.items():
        text = p.read_text(encoding="utf-8-sig", errors="replace")
        match = SRC_SHADER.search(text)
        shader_guid = match.group(2) if match else ""
        name = shader_name(guid_paths.get(shader_guid))
        families[name.split("/")[0]] += 1
        if shader_guid == LIT or name.startswith("Universal Render Pipeline/"):
            already_urp.append(p)
        elif name.startswith("Skybox/") or "skybox" in p.stem.lower():
            continue
        else:
            risky[g] = (p, name)
    for folder in ("Scenes", "Materials", "Prefabs", "Reports", "Editor", "TerrainData"):
        (ROOT / folder).mkdir(parents=True, exist_ok=True)
    # Keep the authored Editor scripts. This helper creates only new assets.
    mapping = {}
    for g in risky:
        mapping[g] = uid("material/" + g)
    for g in prefabs:
        mapping[g] = uid("prefab/" + g)
    for g in terrains:
        mapping[g] = uid("terrain/" + g)
    no_base = []
    for g, (p, _) in risky.items():
        output = ROOT / "Materials" / (safe(p.stem) + "_" + g[:8] + "_URP.mat")
        text, has_base = material_copy(p.read_text(encoding="utf-8-sig", errors="replace"), category(p) == "vegetation")
        if not has_base:
            no_base.append(str(p))
        output.write_text(text, encoding="utf-8")
        meta(output, mapping[g])
    for g, p in prefabs.items():
        output = ROOT / "Prefabs" / (safe(p.stem) + "_" + g[:8] + "_URP.prefab")
        text = p.read_text(encoding="utf-8-sig", errors="replace")
        output.write_text(REF.sub(lambda m: "guid: " + mapping.get(m.group(1), m.group(1)), text), encoding="utf-8")
        meta(output, mapping[g])
    for g, p in terrains.items():
        output = ROOT / "TerrainData" / (safe(p.stem) + "_URP.asset")
        text = p.read_text(encoding="utf-8-sig", errors="replace")
        output.write_text(REF.sub(lambda m: "guid: " + mapping.get(m.group(1), m.group(1)), text), encoding="utf-8")
        meta(output, mapping[g])
    target = ROOT / "Scenes/FW01_MainMap_Summer_URP.unity"
    source_text = SOURCE.read_text(encoding="utf-8-sig", errors="replace")
    target.write_text(REF.sub(lambda m: "guid: " + mapping.get(m.group(1), m.group(1)), source_text), encoding="utf-8")
    meta(target, uid("scene/summer"))
    report = ROOT / "Reports/FW01_URP_Audit.txt"
    report.write_text("FW01 Summer source audit (YAML dependency graph; Unity import pending)\n"
        + "Scene: " + str(SOURCE).replace("\\", "/") + "\n"
        + f"Direct/indirect material assets: {len(materials)}\nAlready URP: {len(already_urp)}\nPink high risk: {len(risky)}\nPrefab dependencies: {len(prefabs)}\nTerrainData dependencies: {len(terrains)}\n"
        + "Shader families:\n" + "".join(f"  {k}: {v}\n" for k, v in families.most_common())
        + "Priority: tree bark, leaves, flowers, grass, bushes, rocks, ruins, ground, Terrain trees/details.\n"
        + "High-risk materials:\n" + "".join(f"  {p} | {shader} | {category(p)}\n" for p, shader in risky.values()), encoding="utf-8")
    (ROOT / "Reports/FW01_URP_Summary.txt").write_text(
        "FW01 Summer URP development recovery (offline prepared; Unity import and visual validation pending)\n"
        + "Development scene: " + str(target).replace("\\", "/") + "\n"
        + f"Migrated materials: {len(risky)}\nMigrated prefabs (development copies): {len(prefabs)}\nMigrated TerrainData: {len(terrains)}\n"
        + "Recovered shader families: " + ", ".join(families.keys()) + "\n"
        + "Likely visible after Unity import: rocks, ruins, bark, ground, leaves, flowers, grass and bushes with source base maps.\n"
        + "Manual checks: vegetation silhouette/cutout, terrain tree/detail prototypes, lighting, skybox, transparent VFX.\n"
        + "Known limits: wind/custom shader behavior not retained; baked lighting not rebuilt; no Unity render or compilation validation was possible.\n"
        + f"Materials without identified base texture: {len(no_base)}\n" + "".join("  " + p + "\n" for p in no_base), encoding="utf-8")
    copied = [target] + list((ROOT / "Prefabs").glob("*.prefab")) + list((ROOT / "TerrainData").glob("*.asset"))
    remaining = []
    for p in copied:
        present = refs(p.read_text(encoding="utf-8", errors="replace"))
        for g in (set(risky) | set(prefabs) | set(terrains)) & present:
            remaining.append(f"{p}: original reference {g} ({guid_paths[g]})")
    material_errors = []
    for p in (ROOT / "Materials").glob("*.mat"):
        t = p.read_text(encoding="utf-8", errors="replace")
        if LIT not in t.split("m_Shader:", 1)[1].split("\n", 1)[0]:
            material_errors.append(f"{p}: shader is not URP/Lit")
        if category(p) == "vegetation" and ("_ALPHATEST_ON" not in t or "- _Cull: 0" not in t):
            material_errors.append(f"{p}: vegetation cutout/double-sided settings missing")
    (ROOT / "Reports/FW01_URP_Validation.txt").write_text(
        "FW01 Summer static reference validation (Unity import pending)\n"
        + f"Development scene: {target}\nMigrated materials: {len(risky)}\nMigrated prefabs: {len(prefabs)}\n"
        + f"Remaining original high-risk/prefab/TerrainData GUID references: {len(remaining)}\n"
        + f"Material errors: {len(material_errors)}\nWarnings: {len(no_base)} materials without identified base map\n"
        + "".join(x + "\n" for x in remaining + material_errors), encoding="utf-8")
    if remaining or material_errors:
        raise SystemExit(f"Static validation failed: {len(remaining)} references, {len(material_errors)} material errors")
    print(f"Scene copied; materials={len(risky)}, prefabs={len(prefabs)}, TerrainData={len(terrains)}, no base map={len(no_base)}")


if __name__ == "__main__":
    main()
