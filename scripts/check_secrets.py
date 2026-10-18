#!/usr/bin/env python3
"""Check Git-visible content without printing possible secret values."""

import argparse
import io
from pathlib import Path
import re
import subprocess
import sys
import zipfile


ROOT = Path(__file__).resolve().parents[1]
PATTERNS = {
    "Google API key": re.compile(r"AIza[0-9A-Za-z_-]{35}"),
    "GitHub token": re.compile(r"(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{40,})"),
    "AWS access key": re.compile(r"(?:AKIA|ASIA)[A-Z0-9]{16}"),
    "private key": re.compile(r"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----"),
    "personal absolute path": re.compile(r"/(?:Users|home)/[A-Za-z0-9_.-]+/"),
    "credential assignment": re.compile(
        r'''(?i)(?:password|api[_-]?key|access[_-]?token|client[_-]?secret)\s*[:=]\s*["'](?!YOUR_|PLACEHOLDER)[A-Za-z0-9_+/=-]{16,}["']'''
    ),
}


def git(*arguments: str) -> bytes:
    return subprocess.check_output(["git", *arguments], cwd=ROOT)


def scan(data: bytes, filename: str) -> list[str]:
    if filename.endswith(".docx"):
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            data = b"\n".join(archive.read(name) for name in archive.namelist() if name.endswith(".xml"))
    elif b"\0" in data:
        return []
    content = data.decode("utf-8", errors="replace")
    return [label for label, pattern in PATTERNS.items() if pattern.search(content)]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--staged", action="store_true", help="Inspect index content exactly as it will be committed.")
    args = parser.parse_args()
    arguments = ("diff", "--cached", "--name-only", "--diff-filter=ACMR", "-z") if args.staged else (
        "ls-files", "--cached", "--others", "--exclude-standard", "-z"
    )
    filenames = sorted(set(filter(None, git(*arguments).decode().split("\0"))))
    failures = []
    for filename in filenames:
        path = ROOT / filename
        if not args.staged and not path.is_file():
            continue
        data = git("show", f":{filename}") if args.staged else path.read_bytes()
        for label in scan(data, filename):
            failures.append(f"{filename}: possible {label}")
    if failures:
        print("Review required (values deliberately omitted):\n" + "\n".join(failures))
        return 1
    print(f"Checked {len(filenames)} files: no configured secret or personal-path patterns found.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
