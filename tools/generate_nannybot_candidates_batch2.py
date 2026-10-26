"""Generate the second set of ten nanny-robot candidates without touching batch 1."""

import os
import sys

sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from generate_nannybot_candidates import ROOT, STYLES_BATCH2, generate

OUT_DIR = os.path.join(ROOT, "docs", "nannybot-textures-2")
HTML_PATH = os.path.join(ROOT, "docs", "nannybot-textures-2.html")

if __name__ == "__main__":
    generate(
        STYLES_BATCH2,
        OUT_DIR,
        HTML_PATH,
        "保姆机器人贴图候选 · 第二批",
        "再 10 个新风格方案，每个方案含南 / 北 / 东 / 西四向。第一批保持原样，这里单独挑选。",
    )
