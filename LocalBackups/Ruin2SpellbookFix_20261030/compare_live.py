import pathlib
import re

root = pathlib.Path(__file__).parent
pattern = re.compile(r'(?m)^--- !u!(\d+) &(\d+).*$')

def blocks(path):
    data = path.read_text(encoding='utf-8-sig')
    matches = list(pattern.finditer(data))
    return {m.group(2): (m.group(1), data[m.start():matches[i+1].start() if i+1 < len(matches) else len(data)])
            for i, m in enumerate(matches)}

saved = blocks(root / 'BeforeSaved.unity')
live = blocks(root / 'BeforeLive.unity')
print('saved', len(saved), 'live', len(live))
print('added', sorted(set(live) - set(saved)))
print('removed', sorted(set(saved) - set(live)))
for key in sorted(set(saved) & set(live)):
    if saved[key] != live[key]:
        name = re.search(r'(?m)^  m_Name: (.*)$', live[key][1])
        print('changed', key, live[key][0], name.group(1) if name else '')

after = blocks(root / 'AfterSaved.unity')
print('live-to-after added', sorted(set(after) - set(live)))
print('live-to-after removed', sorted(set(live) - set(after)))
for key in sorted(set(live) & set(after)):
    if live[key] != after[key]:
        name = re.search(r'(?m)^  m_Name: (.*)$', after[key][1])
        print('fixed', key, after[key][0], name.group(1) if name else '')
