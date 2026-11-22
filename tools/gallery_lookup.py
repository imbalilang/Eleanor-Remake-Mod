#!/usr/bin/env python3
"""Look up dsh-image-gen gallery prompt and parameters by generated image filename."""
from __future__ import annotations
import argparse, json, re, sys

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass
from pathlib import Path

DB_DIR = Path.home() / "AppData" / "Roaming" / "@deepseek-ai" / "dsh-desktop" / "IndexedDB" / "dsh-app_app_0.indexeddb.leveldb"

def extract(blob: bytes, needle: bytes) -> dict | None:
    pos = blob.lower().find(needle.lower())
    if pos < 0:
        return None
    marker = blob.rfind(b"\x06prompt", max(0, pos - 200_000), pos)
    if marker < 0:
        return None
    provider = blob.find(b"\x08provider", marker, pos + 200_000)
    if provider < 0:
        return None
    # UTF-16LE prompt begins just before the first NUL after the prompt marker.
    nul = blob.find(b"\x00", marker + 7, provider)
    if nul < 0:
        return None
    raw = blob[nul - 1:provider]
    text = raw.decode("utf-16le", "replace")
    # The record appends a quoted terminator / binary length after the prompt.
    q = text.rfind('"')
    if q >= 0:
        text = text[:q]
    text = text.replace("\ufffd", "").strip().strip('"').strip()
    params_raw = blob[provider:provider + 2500].decode("utf-8", "replace")
    out = {}
    for key in ("provider", "channel", "model", "output", "savedTo", "sessionId", "createdAt"):
        m = re.search(key + r'"\s*([^\x00"\r\n]+)', params_raw)
        if m:
            value = m.group(1).strip()
            if key == "output":
                mm = re.search(r"\d+x\d+", value)
                value = mm.group(0) if mm else value
            elif key == "savedTo":
                mm = re.search(r"[A-Za-z]:\\\\[^\x00\"\r\n]+", value)
                value = mm.group(0) if mm else value.lstrip()
            elif key == "sessionId":
                mm = re.search(r"session-[A-Za-z0-9-]+", value)
                value = mm.group(0) if mm else value.lstrip(",")
            out[key] = value
    out["prompt"] = text
    return out

def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("resource", help="e.g. image-613e77b4.png or 613e77b4")
    ap.add_argument("--db-dir", default=str(DB_DIR))
    ap.add_argument("--json", action="store_true")
    args = ap.parse_args()
    needle = Path(args.resource).name
    db = Path(args.db_dir)
    files = sorted((p for p in db.iterdir() if p.is_file()), key=lambda p: p.stat().st_mtime, reverse=True) if db.exists() else []
    for f in files:
        try:
            blob = f.read_bytes()
        except Exception:
            continue
        rec = extract(blob, needle.encode("utf-8"))
        if rec:
            rec["_source"] = str(f)
            if args.json:
                print(json.dumps(rec, ensure_ascii=False, indent=2))
            else:
                print("image:", needle)
                print("source:", f)
                for k in ("provider", "channel", "model", "output", "savedTo", "sessionId", "createdAt"):
                    if k in rec:
                        print(f"{k}: {rec[k]}")
                print("\nPROMPT:\n" + rec["prompt"])
            return 0
    print(f"NOT FOUND: {needle}", file=sys.stderr)
    return 1

if __name__ == "__main__":
    raise SystemExit(main())
