#!/usr/bin/env python3
"""src/index.html の /*SONGS*/ に songs/*.js を埋め込み dist/index.html を作る。"""
import glob, pathlib
root = pathlib.Path(__file__).parent
html = (root / "src/index.html").read_text(encoding="utf-8")
songs = "\n".join(p.read_text(encoding="utf-8") for p in sorted((root / "songs").glob("*.js")))
assert "/*SONGS*/" in html
(root / "dist/index.html").write_text(html.replace("/*SONGS*/", songs), encoding="utf-8")
print("built dist/index.html", len(html) + len(songs), "bytes")
