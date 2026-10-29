import pathlib, re, difflib

root=pathlib.Path(__file__).parent
before=(root/'BeforeLive.unity').read_text(encoding='utf-8-sig')
after=(root/'AfterSaved.unity').read_text(encoding='utf-8-sig')
marker=re.compile(r'(?m)^--- !u!(\d+) &(\d+).*$')

def blocks(text):
    matches=list(marker.finditer(text)); out={}
    for i,m in enumerate(matches):
        end=matches[i+1].start() if i+1<len(matches) else len(text)
        out[m.group(2)]=(m.group(1),text[m.start():end])
    return out

a,b=blocks(before),blocks(after)
added=set(b)-set(a); removed=set(a)-set(b); changed={x for x in a.keys()&b.keys() if a[x]!=b[x]}
print(f'before={len(a)} after={len(b)} added={len(added)} removed={len(removed)} changed={len(changed)}')
for k in sorted(changed,key=int):
    cl,body=b[k]; match=re.search(r'(?m)^  m_Name: (.*)$',body)
    print('CHANGED',k,cl,match.group(1) if match else '')
    if cl in ('114','224'):
        print(''.join(difflib.unified_diff(a[k][1].splitlines(True),body.splitlines(True),n=1))[:1200])
for cl in ('104','157','29'):
    x=[v for v in a.values() if v[0]==cl];y=[v for v in b.values() if v[0]==cl]
    print('CLASS',cl,'unchanged=',x==y)
