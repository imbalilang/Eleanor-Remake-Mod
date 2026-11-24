#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Sanity-check the visual route map against the current ending set.

Run only when the author explicitly requests route-map maintenance. The HTML's
DATA object is the single source for the preview page.
"""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path


def main() -> int:
    root = Path(__file__).resolve().parent.parent
    html_path = root / "docs" / "route_map.html"
    maintenance = root / "docs" / "ROUTE_MAP_MAINTENANCE.md"
    manifest = root / "manifest.xml"

    if not html_path.exists():
        print("ERROR: docs/route_map.html is missing")
        return 1
    if not maintenance.exists():
        print("ERROR: docs/ROUTE_MAP_MAINTENANCE.md is missing")
        return 1

    html = html_path.read_text(encoding="utf-8")
    marker = "const DATA = "
    start = html.find(marker)
    if start < 0:
        print("ERROR: could not find const DATA in route_map.html")
        return 1
    try:
        data, _ = json.JSONDecoder().raw_decode(html[start + len(marker):])
    except Exception as exc:
        print("ERROR: route_map DATA is not valid JSON: " + str(exc))
        return 1

    errors = []
    if data.get('scope') != 'current_rewrite':
        errors.append('player guide must describe the current rewrite')
    version = re.search(r"<ModVersion>([^<]+)</ModVersion>", manifest.read_text(encoding="utf-8"))
    if not version:
        errors.append("manifest ModVersion missing")
    manifest_version = version.group(1) if version else None

    endings = data.get("endings", [])
    ids = [e.get("id") for e in endings]
    expected = ['E1', 'E2', 'E3', 'E4', 'E9']
    if sorted(ids) != sorted(expected):
        errors.append("current guide must contain E1/E2/E3/E4/E9 exactly; found " + ", ".join(ids))
    if len(set(ids)) != len(ids):
        errors.append("duplicate ending ids")
    expected_completed = {"E1", "E3", "E4", "E9"}
    completed_ids = {e.get("id") for e in endings if e.get("kind") == 'route'}
    if completed_ids != expected_completed:
        errors.append("route completions must be E1/E3/E4/E9; found " + ", ".join(sorted(completed_ids)))
    aliases = {'E5': 'E1', 'E6': 'E2', 'E7': 'E3', 'E8': 'E4'}
    if data.get('endingAliases') != aliases:
        errors.append('historical duplicate ending IDs must map to their relationship endings')
    if data.get('endingPolicy') != 'relationship_and_route_completion':
        errors.append('ending policy must depend on relationship and route completion')
    for e in endings:
        if not e.get("mainRoute"):
            errors.append("ending missing mainRoute: " + str(e.get("id")))
        if 'perfect' in e or any(word in e.get('title', '') for word in ('研究归还', '研究保留')):
            errors.append('ending must not split or rank the research choice: ' + e['id'])
    if {e['id'] for e in endings if e.get('kind') == 'friend'} != {'E2'}:
        errors.append('E2 must be the single friend ending')

    chart_marker = "const CHART = "
    chart_start = html.find(chart_marker)
    chart = None
    if chart_start < 0:
        errors.append("could not find const CHART in route_map.html")
    else:
        try:
            chart, _ = json.JSONDecoder().raw_decode(html[chart_start + len(chart_marker):])
        except Exception as exc:
            errors.append("route_map CHART is not valid JSON: " + str(exc))
    if chart:
        chart_nodes = {n.get("id"): n for n in chart.get("nodes", [])}
        chart_edges = chart.get("edges", [])
        missing_chart_endings = [eid for eid in expected if eid not in chart_nodes]
        if missing_chart_endings:
            errors.append("endings missing from CHART terminal nodes: " + ", ".join(missing_chart_endings))
        for edge in chart_edges:
            if edge.get("from") not in chart_nodes or edge.get("to") not in chart_nodes:
                errors.append(f"CHART edge references unknown node: {edge}")
        outgoing = {e.get("from") for e in chart_edges}
        non_terminal = [eid for eid in expected if eid in outgoing]
        if non_terminal:
            errors.append("ending nodes must be terminal (no outgoing edges): " + ", ".join(non_terminal))
        completed_nodes = {n.get("id") for n in chart.get("nodes", []) if n.get("cls") == "completed"}
        if completed_nodes != expected_completed:
            errors.append("CHART completed nodes must be E1/E3/E4/E9; found " + ", ".join(sorted(completed_nodes)))
        if any(n.get('cls') == 'perfect' for n in chart.get('nodes', [])):
            errors.append('old perfect/non-perfect ending classification remains')
        axis = chart.get("axis")
        if not axis:
            errors.append("CHART axis is missing")
        else:
            for key in ("x", "top", "bottom", "yTop", "yBottom", "earlyOffset"):
                if key not in axis:
                    errors.append(f"CHART axis missing {key}")
            if axis.get("bottom", 0) <= axis.get("top", 0):
                errors.append("CHART axis bottom must be greater than top")
            missing_day = [n.get("id") for n in chart.get("nodes", []) if "day" not in n]
            if missing_day:
                errors.append("CHART nodes missing day: " + ", ".join(missing_day))
            if all(key in axis for key in ("top", "bottom", "yTop", "yBottom")):
                span = axis["bottom"] - axis["top"]
                if span:
                    for n in chart.get("nodes", []):
                        if "day" not in n:
                            continue
                        expected = axis["yTop"] + (n["day"] - axis["top"]) * (axis["yBottom"] - axis["yTop"]) / span
                        if abs(n.get("y", 0) - expected) > 1:
                            errors.append(
                                f"CHART node {n.get('id')} y={n.get('y')} does not match day {n['day']} (expected {expected:.0f})"
                            )

    guides = data.get('guides', [])
    guide_ids = [g.get('id') for g in guides]
    if guide_ids != ['common', 'regular', 'harem', 'maid', 'rev']:
        errors.append('guide order must be common, regular, harem, maid, rev')
    acts = [a for g in guides for a in g.get('acts', [])]
    act_ids = [a.get('id') for a in acts]
    if len(act_ids) != len(set(act_ids)):
        errors.append('duplicate act anchors')
    required_counts = {'common': 4, 'regular': 3, 'harem': 4, 'maid': 5, 'rev': 4}
    for guide in guides:
        if not guide.get('entry'):
            errors.append('missing entry requirements: ' + guide['id'])
        if len(guide.get('acts', [])) != required_counts.get(guide['id']):
            errors.append('incorrect act count: ' + guide['id'])
        for act in guide.get('acts', []):
            for key in ('title', 'day', 'early', 'scenes', 'summary', 'checks', 'sourceIds'):
                if not act.get(key):
                    errors.append(f'missing {key} in act {act.get("id")}')
    sources = [s for a in acts for s in a['sourceIds']]
    if len(sources) != len(set(sources)):
        errors.append('a scene is assigned to more than one act')
    required_sources = set()
    for filename in ('maid_v2.json', 'revolution_v2.json', 'love_romance_v01.json',
                     'love_romance_v02.json', 'love_romance_v03.json'):
        content = json.loads((root / 'Content' / filename).read_text(encoding='utf-8'))
        required_sources.update(s['id'] for s in content['scenes'])
    required_sources.update(['d01'] + [f'dv{i:02d}' for i in range(1, 14)])
    required_sources.update(f'c{i:02d}' for i in list(range(1, 15)) + list(range(17, 24)))
    missing_sources = required_sources - set(sources)
    if missing_sources:
        errors.append('current scenes missing from guide: ' + ', '.join(sorted(missing_sources)))
    anchors = set(act_ids) | {'guide-' + key for key in guide_ids} | set(ids)
    if chart:
        for node in chart['nodes']:
            if node.get('guide') not in anchors:
                errors.append('node guide target missing: ' + node['id'])
        retired = set(aliases) | {'E10', 'E11', 'E12', 'E13', 'E14', 'HG1', 'HG2', 'HG3', 'R_NON', 'O_NON'}
        if retired & set(chart_nodes):
            errors.append('retired nodes remain in current chart')
    if 'renderGuides();' not in html or 'id="route-guides"' not in html:
        errors.append('guide data is not rendered on the player page')
    guide_text = json.dumps(guides, ensure_ascii=False)
    if re.search(r'\bE[5-8]\b', guide_text) or any(word in html for word in ('非完美', '完美结局')):
        errors.append('guide still presents duplicate research endings or ending quality ranks')
    for fact in ('至少3次', '风险承担', '同一只', '实际差额', 'P2', '3000', '1500', '30／60／90'):
        if fact not in guide_text:
            errors.append('missing key entry fact: ' + fact)
    # The repeated-exit choice must not come back without a deliberate map update.
    if "dr17_withdraw" in html or "dr15_withdraw" in html or "dr14_reject" in html:
        errors.append("removed repeat-exit choices are still present in the route map")

    print(f"route map version: {data.get('version')} (manifest {manifest_version or '?'})")
    if manifest_version and data.get('version') != manifest_version:
        print("note: route map may lag the manifest; version sync is only checked at author request")
    print(f"guides: {len(guides)}, acts: {len(acts)}, current endings: {len(endings)}")
    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1
    print("OK: current routes, scene coverage, guide links and endings agree.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
