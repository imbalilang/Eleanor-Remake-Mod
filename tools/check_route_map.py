#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Sanity-check the visual route map against the current manifest and ending set.

Run after any story/route/ending change. The HTML's DATA object is the single
source for the preview page.
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
    # The planned design is deliberately separate from the shipped DATA/CHART.
    plan_path = root / "docs" / "后宫线_v0.5_声望检查点与问题修复_执行参数.json"
    plan = {}
    plan_marker = 'id="dual-v05-plan"'
    if plan_path.exists():
        try:
            plan = json.loads(plan_path.read_text(encoding="utf-8"))
            if plan.get("status") == "planned_not_implemented":
                if plan_marker not in html:
                    errors.append("dual v0.5 plan exists but its pending-design notice is missing")
                if [g.get("reputation") for g in plan.get("gates", [])] != [50, 100, 150]:
                    errors.append("dual v0.5 reputation checkpoints must be 50/100/150")
                policy = plan.get("policy", {})
                if not policy.get("mainlineWaits") or not policy.get("nativeCustomersContinue"):
                    errors.append("dual v0.5 must wait at gates while native business continues")
                if policy.get("failToOrdinaryWhenBelowGate") or policy.get("reputationConsumed"):
                    errors.append("dual v0.5 gates must not consume reputation or reject the route")
                if "v0.5" not in maintenance.read_text(encoding="utf-8"):
                    errors.append("route maintenance is missing the planned dual v0.5 rules")
        except (ValueError, TypeError, AttributeError) as exc:
            errors.append("invalid dual v0.5 plan: " + str(exc))
    if plan_path.exists() and plan.get('status') == 'implemented_pending_playtest':
        if data.get('dualDesignVersion') != '0.5':errors.append('runtime map missing dual v0.5 implementation')
        for i in range(1,4):
            if f'HG{i}' not in html:errors.append('runtime reputation checkpoint missing')
        if [g.get('reputation') for g in plan['gates']] != [50,100,150]:errors.append('checkpoint threshold drift')
        if '待实施的声望检查点' in html:errors.append('stale planned-only banner')
        for day in data.get('days',[]):
            if day.get('id') == 'dr05' and any('暂缓' in str(c) for c in day.get('choices',[])):errors.append('deferred confession returned')
        for edge in (json.JSONDecoder().raw_decode(html.split('const CHART = ',1)[1])[0]['edges']):
            if edge.get('from','').startswith('HG') and edge.get('to','').startswith('E'):errors.append('reputation gate exits to an ending')
    version = re.search(r"<ModVersion>([^<]+)</ModVersion>", manifest.read_text(encoding="utf-8"))
    if not version:
        errors.append("manifest ModVersion missing")
    elif data.get("version") != version.group(1):
        errors.append(f"route map version {data.get('version')} != manifest {version.group(1)}")

    endings = data.get("endings", [])
    ids = [e.get("id") for e in endings]
    expected = [f"E{i}" for i in range(1, 15)]
    if sorted(ids) != sorted(expected):
        errors.append("endings must contain E1-E14 exactly; found " + ", ".join(ids))
    if len(set(ids)) != len(ids):
        errors.append("duplicate ending ids")
    expected_perfect = {"E1", "E3", "E4", "E9"}
    perfect_ids = {e.get("id") for e in endings if e.get("perfect")}
    if perfect_ids != expected_perfect:
        errors.append("perfect endings must be E1/E4/E3/E9; found " + ", ".join(sorted(perfect_ids)))
    for e in endings:
        if not e.get("mainRoute"):
            errors.append("ending missing mainRoute: " + str(e.get("id")))

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
        perfect_nodes = {n.get("id") for n in chart.get("nodes", []) if n.get("cls") == "perfect"}
        if perfect_nodes != expected_perfect:
            errors.append("CHART perfect nodes must be E1/E4/E3/E9; found " + ", ".join(sorted(perfect_nodes)))
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

    days = data.get("days", [])
    required_days = {"start", "maid_entry", "drive", "threat", "direction", "resolution",
                     "dual_entry", "dr05", "dr07", "dr11", "dr14", "dr15", "dr17", "dr20", "gala_closing"}
    day_ids = {d.get("id") for d in days}
    missing_days = required_days - day_ids
    if missing_days:
        errors.append("missing timeline nodes: " + ", ".join(sorted(missing_days)))
    for day in days:
        for eid in day.get("endings", []):
            if eid not in ids:
                errors.append(f"timeline node {day.get('id')} references unknown ending {eid}")

    # The repeated-exit choice must not come back without a deliberate map update.
    if "dr17_withdraw" in html or "dr15_withdraw" in html or "dr14_reject" in html:
        errors.append("removed repeat-exit choices are still present in the route map")

    print(f"route map version: {data.get('version')} (manifest {version.group(1) if version else '?'})")
    print(f"timeline nodes: {len(days)}, endings: {len(endings)}")
    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1
    print("OK: route map matches the current ending set and manifest version.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
