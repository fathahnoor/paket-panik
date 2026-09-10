"""Validate the portable design package without changing Unity files."""
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
required = ["README.md", "HANDOFF.md", "TASK_STATE.md", "NEXT_SESSION_PROMPT.md", "task_plan.md", "findings.md", "progress.md", "Design/GAME_DESIGN.md", "Design/TECH_SPEC.md", "Design/BUILD_PLAN.md", "Design/LAUNCH_PLAN.md", "Design/MARKET_RESEARCH.md", "Design/preview.html", "Design/balance.json"]
errors = []
for relative in required:
    path = root / relative
    if not path.exists():
        errors.append(f"Missing {relative}")
        continue
    text = path.read_text(encoding="utf-8-sig")
    if re.search(r"[\u2010-\u2015\u2212]", text):
        errors.append(f"Nonstandard dash in {relative}")
    if relative.endswith(".md"):
        for link in re.findall(r"\]\(([^)]+)\)", text):
            if "://" not in link and not link.startswith("#") and not (path.parent / link.split("#")[0]).exists():
                errors.append(f"Broken link {relative}: {link}")
balance = json.loads((root / "Design/balance.json").read_text())
assert balance["players"] == 2
assert balance["battery"]["capacity"] / balance["battery"]["drainPerSecond"] == 4
assert balance["roundSeconds"] == 90 and balance["tickRate"] == 20
assert balance["anger"]["idlePerSecond"] + balance["anger"]["perActiveLooterPerSecond"] - balance["anger"]["guardReductionPerSecond"] == -4
print(json.dumps({"status": "FAIL" if errors else "PASS", "filesChecked": len(required), "errors": errors}, indent=2))
raise SystemExit(bool(errors))
