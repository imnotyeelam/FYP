import json
import urllib.error
import urllib.request
import sys

URL = "http://127.0.0.1:8000/feedback"
CODE = """void sealMagic(int n) {
    if (n == 0) {
        return;
    }

    sealMagic(n - 1);
    absorbMagic(n);
}"""

CASES = [
    ("Call", 1, 4, "absorbMagic(4)", "sealMagic(3)"),
    ("Base Case", 2, 0, "absorbMagic(0)", "return"),
    ("Return", 3, 1, "absorbMagic(2)", "absorbMagic(1)"),
]

for phase, step, n, answer, expected in CASES:
    if len(sys.argv)>1 and phase!=sys.argv[1]:
        continue
    payload = {
        "playerAnswer": answer,
        "ruinId": "Ruin02",
        "currentStep": step,
        "currentN": n,
        "phase": phase,
        "expectedOperation": expected,
        "attemptCount": 1,
        "functionCode": CODE,
    }
    request = urllib.request.Request(
        URL,
        data=json.dumps(payload).encode("utf-8"),
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(request, timeout=35) as response:
            body = json.load(response)
            print(f"{phase}: HTTP {response.status}; source={body.get('source')}; feedback={body.get('feedback')}", flush=True)
    except urllib.error.HTTPError as error:
        try:
            detail = json.load(error).get("detail")
            if isinstance(detail, list):
                detail = [(item.get("loc"), item.get("type")) for item in detail]
        except (ValueError, TypeError):
            detail = "non-JSON error body"
        print(f"{phase}: HTTP {error.code}; detail={detail}", flush=True)
