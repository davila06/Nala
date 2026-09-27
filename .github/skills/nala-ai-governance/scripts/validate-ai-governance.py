#!/usr/bin/env python3
"""Read-only structural validator for NALA Agent Skills and AI governance."""

from __future__ import annotations

import re
import sys
from pathlib import Path
from urllib.parse import unquote


REPOSITORY_ROOT = Path(__file__).resolve().parents[4]
SKILLS_ROOT = REPOSITORY_ROOT / ".github" / "skills"
GOVERNANCE_ROOT = SKILLS_ROOT / "nala-ai-governance"
VALID_NAME = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*$")
TOP_LEVEL_KEY = re.compile(r"^([A-Za-z_][A-Za-z0-9_-]*):(?:[ \t]*(.*))?$")
NESTED_KEY = re.compile(r"^[A-Za-z_][A-Za-z0-9_-]*:(?:[ \t].*|[ \t]*)$")
KNOWN_FIELDS = {"name", "description", "compatibility", "metadata"}
ACTIVATION_CUE = re.compile(
    r"\b(?:use\s+(?:when|for)|when\s+to\s+use|"
    r"usar\s+(?:al|para|cuando)|úsalo\s+(?:al|para|cuando)|"
    r"usarlo\s+(?:al|para|cuando))\b",
    re.IGNORECASE,
)
PURPOSE_CUE = re.compile(
    r"\b(?:gobierna|audita|analiza|documenta|genera|eval[uú]a|gu[ií]a|"
    r"revisa|crea|traduce|prepara|ayuda|administra|valida|detecta)\b",
    re.IGNORECASE,
)
MARKDOWN_LINK = re.compile(r"!?\[[^\]]*\]\(([^)]+)\)")
SECRET_ASSIGNMENT = re.compile(
    r"\b(?:api[_ -]?key|access[_ -]?token|refresh[_ -]?token|"
    r"client[_ -]?secret|password|passwd|connection[_ -]?string|"
    r"private[_ -]?key)\b\s*[:=]\s*[\"']?([A-Za-z0-9/+_=.-]{12,})",
    re.IGNORECASE,
)

REQUIRED_FILES = (
    "SKILL.md",
    "references/GOVERNANCE_POLICY.md",
    "references/EVIDENCE_POLICY.md",
    "references/RISK_CLASSIFICATION.md",
    "references/HUMAN_APPROVAL_POLICY.md",
    "references/AGENT_BOUNDARIES.md",
    "references/KNOWLEDGE_SOURCE_POLICY.md",
    "references/PROMPT_INJECTION_DEFENSE.md",
    "references/INCIDENT_RESPONSE.md",
    "assets/AGENT_REGISTRATION_TEMPLATE.md",
    "assets/GOVERNANCE_REVIEW_TEMPLATE.md",
    "assets/RISK_ASSESSMENT_TEMPLATE.md",
    "assets/EVIDENCE_REGISTER_TEMPLATE.md",
    "assets/EXCEPTION_REQUEST_TEMPLATE.md",
    "assets/AI_CHANGELOG_TEMPLATE.md",
    "scripts/validate-ai-governance.py",
)


def relative(path: Path) -> str:
    """Return a repository-relative path for safe, readable diagnostics."""
    try:
        return path.resolve().relative_to(REPOSITORY_ROOT).as_posix()
    except ValueError:
        return path.as_posix()


def unquote_scalar(value: str) -> str:
    """Read plain or simply quoted YAML scalars without third-party parsers."""
    value = value.strip()
    if len(value) >= 2 and value[0] == value[-1] and value[0] in {"'", '"'}:
        inner = value[1:-1]
        if value[0] == "'":
            return inner.replace("''", "'")
        return inner
    return value


def parse_front_matter(path: Path, errors: list[str]) -> dict[str, str]:
    """Validate fenced, indentation-based front matter and return top-level fields.

    This checks the YAML subset used by repository skills: scalar top-level keys
    and indented mappings. It deliberately uses only the Python standard library.
    """
    try:
        text = path.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as exc:
        errors.append(f"{relative(path)}: cannot read UTF-8 front matter ({type(exc).__name__})")
        return {}

    lines = text.splitlines()
    if not lines or lines[0].strip() != "---":
        errors.append(f"{relative(path)}: YAML front matter must start with --- on line 1")
        return {}

    closing = next((index for index in range(1, len(lines)) if lines[index].strip() == "---"), None)
    if closing is None:
        errors.append(f"{relative(path)}: YAML front matter has no closing --- delimiter")
        return {}

    fields: dict[str, str] = {}
    seen: dict[str, int] = {}
    parent_key: str | None = None
    parent_has_mapping = False

    for line_number, line in enumerate(lines[1:closing], start=2):
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        if "\t" in line:
            errors.append(f"{relative(path)}:{line_number}: tabs are not allowed in YAML indentation")
            continue

        if line[0].isspace():
            nested = line.lstrip(" ")
            indent = len(line) - len(nested)
            if indent == 0 or indent % 2 != 0 or not parent_has_mapping:
                errors.append(f"{relative(path)}:{line_number}: invalid nested YAML mapping indentation")
                continue
            if not NESTED_KEY.match(nested):
                errors.append(f"{relative(path)}:{line_number}: expected a nested YAML key/value")
            continue

        match = TOP_LEVEL_KEY.match(line)
        if not match:
            errors.append(f"{relative(path)}:{line_number}: malformed top-level YAML field")
            parent_key = None
            parent_has_mapping = False
            continue

        key = match.group(1)
        raw_value = (match.group(2) or "").strip()
        parent_key = key
        parent_has_mapping = raw_value == ""

        if key in KNOWN_FIELDS:
            seen[key] = seen.get(key, 0) + 1
            if seen[key] > 1:
                errors.append(f"{relative(path)}:{line_number}: duplicate recognized field '{key}'")
            fields.setdefault(key, unquote_scalar(raw_value))

        if raw_value.startswith(("'", '"')) and not (
            len(raw_value) >= 2 and raw_value[-1] == raw_value[0]
        ):
            errors.append(f"{relative(path)}:{line_number}: unterminated quoted YAML scalar")

    return fields


def validate_skill_front_matter(skill_dir: Path, skill_file: Path, errors: list[str]) -> None:
    fields = parse_front_matter(skill_file, errors)
    name = fields.get("name", "").strip()
    description = fields.get("description", "").strip()

    if not name:
        errors.append(f"{relative(skill_file)}: required field 'name' is missing or empty")
    else:
        if not VALID_NAME.fullmatch(name):
            errors.append(f"{relative(skill_file)}: name must use lowercase letters, digits, and hyphens")
        if len(name) > 64:
            errors.append(f"{relative(skill_file)}: name exceeds the 64-character limit")
        if name != skill_dir.name:
            errors.append(f"{relative(skill_file)}: name '{name}' must match directory '{skill_dir.name}'")

    if not description:
        errors.append(f"{relative(skill_file)}: required field 'description' is missing or empty")
    else:
        if len(description) > 1024:
            errors.append(f"{relative(skill_file)}: description exceeds the 1024-character limit")
        if len(description.split()) < 8 or not PURPOSE_CUE.search(description):
            errors.append(f"{relative(skill_file)}: description must state a clear purpose")
        if not ACTIVATION_CUE.search(description):
            errors.append(f"{relative(skill_file)}: description must state when to activate the skill")


def validate_skill_folders(errors: list[str]) -> int:
    if not SKILLS_ROOT.is_dir():
        errors.append(".github/skills/: skills directory does not exist")
        return 0

    skill_dirs = sorted(path for path in SKILLS_ROOT.iterdir() if path.is_dir())
    for skill_dir in skill_dirs:
        skill_file = skill_dir / "SKILL.md"
        if not skill_file.is_file():
            errors.append(f"{relative(skill_dir)}: skill directory is missing SKILL.md")
            continue
        validate_skill_front_matter(skill_dir, skill_file, errors)
    return len(skill_dirs)


def validate_required_files(errors: list[str]) -> None:
    for required in REQUIRED_FILES:
        path = GOVERNANCE_ROOT / required
        if not path.is_file():
            errors.append(f"{relative(path)}: required governance file is missing")


def validate_nonempty_files(errors: list[str]) -> int:
    checked = 0
    if not GOVERNANCE_ROOT.is_dir():
        errors.append(f"{relative(GOVERNANCE_ROOT)}: governance skill directory is missing")
        return checked

    for path in sorted(item for item in GOVERNANCE_ROOT.rglob("*") if item.is_file()):
        checked += 1
        try:
            content = path.read_bytes()
        except OSError as exc:
            errors.append(f"{relative(path)}: cannot read file ({type(exc).__name__})")
            continue
        if not content.strip():
            errors.append(f"{relative(path)}: empty file")
    return checked


def validate_markdown_links(errors: list[str]) -> int:
    checked = 0
    root = GOVERNANCE_ROOT.resolve()
    for path in sorted(GOVERNANCE_ROOT.rglob("*.md")):
        checked += 1
        try:
            text = path.read_text(encoding="utf-8")
        except (OSError, UnicodeError) as exc:
            errors.append(f"{relative(path)}: cannot read Markdown ({type(exc).__name__})")
            continue

        for match in MARKDOWN_LINK.finditer(text):
            destination = match.group(1).strip()
            if destination.startswith("<") and ">" in destination:
                destination = destination[1 : destination.index(">")]
            if not destination or destination.startswith("#"):
                continue
            if re.match(r"^[A-Za-z][A-Za-z0-9+.-]*:", destination):
                continue

            target = destination.split("#", 1)[0].split("?", 1)[0]
            if not target:
                continue
            resolved = (path.parent / unquote(target)).resolve()
            try:
                resolved.relative_to(root)
            except ValueError:
                errors.append(f"{relative(path)}: relative link escapes this skill directory")
                continue
            if not resolved.exists():
                errors.append(f"{relative(path)}: broken relative link '{target}'")
    return checked


def scan_apparent_secrets(errors: list[str]) -> int:
    scanned = 0
    validator = Path(__file__).resolve()
    for path in sorted(item for item in GOVERNANCE_ROOT.rglob("*") if item.is_file()):
        if path.resolve() == validator:
            continue
        scanned += 1
        try:
            lines = path.read_text(encoding="utf-8").splitlines()
        except (OSError, UnicodeError) as exc:
            errors.append(f"{relative(path)}: cannot scan for apparent secrets ({type(exc).__name__})")
            continue

        for line_number, line in enumerate(lines, start=1):
            if SECRET_ASSIGNMENT.search(line):
                errors.append(
                    f"{relative(path)}:{line_number}: apparent secret assignment detected; value redacted"
                )
    return scanned


def main() -> int:
    errors: list[str] = []
    skill_count = validate_skill_folders(errors)
    validate_required_files(errors)
    file_count = validate_nonempty_files(errors)
    markdown_count = validate_markdown_links(errors)
    secret_scan_count = scan_apparent_secrets(errors)

    print("NALA AI Governance validator (read-only)")
    print(
        f"Checked {skill_count} skill folders, {file_count} governance files, "
        f"{markdown_count} Markdown files, and {secret_scan_count} files for apparent secrets."
    )
    if errors:
        print(f"Result: FAIL ({len(errors)} issue(s))")
        for error in errors:
            print(f"ERROR: {error}")
        return 1

    print("Result: PASS (no empty files, broken relative links, front matter errors, or apparent secret assignments)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
