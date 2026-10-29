import hashlib
import pathlib
import re

root = pathlib.Path(__file__).parent
before = (root / "BeforeLive.unity").read_text(encoding="utf-8-sig")
after = (root / "FinalSaved.unity").read_text(encoding="utf-8-sig")
marker = re.compile(r"(?m)^--- !u!(\d+) &(\d+).*$")


def sections(text):
    matches = list(marker.finditer(text))
    result = {}
    for i, match in enumerate(matches):
        end = matches[i + 1].start() if i + 1 < len(matches) else len(text)
        result[match.group(2)] = (match.group(1), text[match.start():end])
    return result


a, b = sections(before), sections(after)
added = sorted(set(b) - set(a), key=int)
removed = sorted(set(a) - set(b), key=int)
changed = sorted((k for k in a.keys() & b.keys() if a[k] != b[k]), key=int)
out = [f"before_sections={len(a)} after_sections={len(b)} added={len(added)} removed={len(removed)} changed={len(changed)}"]
for kind, keys, data in (("ADDED", added, b), ("REMOVED", removed, a), ("CHANGED", changed, b)):
    for key in keys:
        klass, content = data[key]
        name = re.search(r"(?m)^  m_Name: (.*)$", content)
        name = name.group(1) if name else ""
        out.append(f"{kind} fileID={key} class={klass} name={name}")

header_a = before[: marker.search(before).start()]
header_b = after[: marker.search(after).start()]
out.append(f"HEADER_EQUAL={header_a == header_b}")
(root / "SceneBlockDiff.txt").write_text("\n".join(out) + "\n", encoding="utf-8")
print("\n".join(out))
