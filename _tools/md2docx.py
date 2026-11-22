"""Convert TZ markdown to a formatted DOCX (python-docx)."""
import re
from docx import Document
from docx.shared import Pt, Cm, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_BREAK
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml.ns import qn
from docx.oxml import OxmlElement

SRC = r"C:\Users\rafael\Desktop\stickman fight\TZ_Stickman_Fight_v1.0.md"
DST = r"C:\Users\rafael\Desktop\stickman fight\TZ_Stickman_Fight_v1.0.docx"

ACCENT = RGBColor(0x1F, 0x3A, 0x5F)
GREY = RGBColor(0x55, 0x5B, 0x66)


def shade(cell_or_par, hex_fill):
    el = cell_or_par._tc if hasattr(cell_or_par, "_tc") else cell_or_par._p
    pr = el.get_or_add_tcPr() if hasattr(el, "get_or_add_tcPr") else el.get_or_add_pPr()
    shd = OxmlElement("w:shd")
    shd.set(qn("w:val"), "clear")
    shd.set(qn("w:color"), "auto")
    shd.set(qn("w:fill"), hex_fill)
    pr.append(shd)


def add_runs(par, text, size=None, bold=False, italic=False, color=None):
    """Inline **bold**, `code`, *italic*, [label](url)."""
    pattern = re.compile(r"(\*\*.+?\*\*|`.+?`|\*[^*\n]+?\*|\[[^\]]+\]\([^)]+\))")
    for part in pattern.split(text):
        if not part:
            continue
        b, i, mono = bold, italic, False
        if part.startswith("**") and part.endswith("**") and len(part) > 4:
            part, b = part[2:-2], True
        elif part.startswith("`") and part.endswith("`") and len(part) > 2:
            part, mono = part[1:-1], True
        elif part.startswith("*") and part.endswith("*") and len(part) > 2:
            part, i = part[1:-1], True
        elif part.startswith("[") and "](" in part:
            m = re.match(r"\[([^\]]+)\]\(([^)]+)\)", part)
            if m:
                part = f"{m.group(1)} ({m.group(2)})"
        r = par.add_run(part)
        r.bold = b
        r.italic = i
        if mono:
            r.font.name = "Consolas"
            r.font.size = Pt((size or 10.5) - 0.5)
        elif size:
            r.font.size = Pt(size)
        if color is not None:
            r.font.color.rgb = color
    return par


def set_repeat_header(row):
    trPr = row._tr.get_or_add_trPr()
    el = OxmlElement("w:tblHeader")
    el.set(qn("w:val"), "true")
    trPr.append(el)
    cant = OxmlElement("w:cantSplit")
    trPr.append(cant)


USABLE_CM = 17.4


def apply_widths(tb, rows, ncol):
    """Proportional column widths from content length."""
    weights = []
    for c in range(ncol):
        lens = [len(r[c]) if c < len(r) else 0 for r in rows]
        body = lens[1:] or lens
        avg = sum(body) / max(len(body), 1)
        weights.append(max(avg, 3.0) ** 0.75)
    total = sum(weights)
    minw = min(1.8, USABLE_CM / ncol)
    widths = [max(minw, USABLE_CM * w / total) for w in weights]
    scale = USABLE_CM / sum(widths)
    widths = [w * scale for w in widths]

    tbl = tb._tbl
    tblPr = tbl.tblPr
    layout = OxmlElement("w:tblLayout")
    layout.set(qn("w:type"), "fixed")
    tblPr.append(layout)
    for row in tb.rows:
        trPr = row._tr.get_or_add_trPr()
        if not row._tr.findall(qn("w:cantSplit")):
            trPr.append(OxmlElement("w:cantSplit"))
        for c, w in enumerate(widths):
            if c < len(row.cells):
                row.cells[c].width = Cm(w)
    for c, w in enumerate(widths):
        tb.columns[c].width = Cm(w)


def add_page_number(par):
    run = par.add_run()
    fld = OxmlElement("w:fldSimple")
    fld.set(qn("w:instr"), "PAGE")
    run._r.addnext(fld)


def build():
    md = open(SRC, encoding="utf-8").read().split("\n")
    doc = Document()

    sec = doc.sections[0]
    sec.page_width, sec.page_height = Cm(21.0), Cm(29.7)
    sec.left_margin = sec.right_margin = Cm(1.8)
    sec.top_margin, sec.bottom_margin = Cm(1.8), Cm(1.6)

    st = doc.styles["Normal"]
    st.font.name = "Calibri"
    st.font.size = Pt(10.5)
    st.paragraph_format.space_after = Pt(4)
    st.paragraph_format.line_spacing = 1.08

    for name, size in (("Heading 1", 17), ("Heading 2", 13.5), ("Heading 3", 11.5)):
        s = doc.styles[name]
        s.font.name = "Calibri"
        s.font.size = Pt(size)
        s.font.bold = True
        s.font.color.rgb = ACCENT
        s.paragraph_format.space_before = Pt(14 if name == "Heading 1" else 10)
        s.paragraph_format.space_after = Pt(6)
        s.paragraph_format.keep_with_next = True

    footer = sec.footer.paragraphs[0]
    footer.alignment = WD_ALIGN_PARAGRAPH.CENTER
    fr = footer.add_run("Physics Stickman Playground · ТЗ v1.0 · стр. ")
    fr.font.size = Pt(8)
    fr.font.color.rgb = GREY
    add_page_number(footer)

    # ---------- title page ----------
    body_start = 0
    title = md[0].lstrip("# ").strip()
    subtitle = md[1].lstrip("# ").strip()
    t = doc.add_paragraph()
    t.alignment = WD_ALIGN_PARAGRAPH.CENTER
    t.paragraph_format.space_before = Pt(90)
    r = t.add_run(title)
    r.bold = True
    r.font.size = Pt(26)
    r.font.color.rgb = ACCENT

    s = doc.add_paragraph()
    s.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = s.add_run(subtitle)
    r.font.size = Pt(15)
    r.font.color.rgb = GREY
    s.paragraph_format.space_after = Pt(30)

    # metadata table (lines 3.. until blank before ---)
    i = 2
    while i < len(md) and not md[i].startswith("|"):
        i += 1
    meta_rows = []
    while i < len(md) and md[i].startswith("|"):
        meta_rows.append(md[i])
        i += 1
    if meta_rows:
        cells = [[c.strip() for c in r.strip("|").split("|")] for r in meta_rows]
        cells = [c for c in cells if not set("".join(c)) <= set("-: ")]
        ncol = len(cells[0])
        tb = doc.add_table(rows=0, cols=ncol)
        tb.style = "Table Grid"
        tb.alignment = WD_TABLE_ALIGNMENT.CENTER
        for ridx, row in enumerate(cells):
            tr = tb.add_row().cells
            for cidx, val in enumerate(row[:ncol]):
                p = tr[cidx].paragraphs[0]
                p.paragraph_format.space_after = Pt(2)
                add_runs(p, val, size=9.5, bold=(ridx == 0),
                         color=RGBColor(0xFF, 0xFF, 0xFF) if ridx == 0 else None)
                if ridx == 0:
                    shade(tr[cidx], "1F3A5F")
        if ncol == 2:
            for row in tb.rows:
                row.cells[0].width = Cm(4.6)
                row.cells[1].width = Cm(13.0)

    brk = doc.add_paragraph()
    brk.add_run().add_break(WD_BREAK.PAGE)

    # ---------- TOC field ----------
    toc_head = doc.add_paragraph()
    toc_head.paragraph_format.space_before = Pt(0)
    toc_head.paragraph_format.space_after = Pt(8)
    r = toc_head.add_run("Оглавление")
    r.bold = True
    r.font.size = Pt(17)
    r.font.color.rgb = ACCENT
    tp = doc.add_paragraph()
    tr = tp.add_run()
    for kind, payload in (
        ("begin", None),
        ("instr", 'TOC \\o "1-2" \\h \\z \\u'),
        ("separate", None),
        ("text", "Оглавление обновится при открытии файла в Word (или нажмите F9)."),
        ("end", None),
    ):
        if kind == "instr":
            el = OxmlElement("w:instrText")
            el.set(qn("xml:space"), "preserve")
            el.text = payload
        elif kind == "text":
            el = OxmlElement("w:t")
            el.text = payload
        else:
            el = OxmlElement("w:fldChar")
            el.set(qn("w:fldCharType"), kind)
        tr._r.append(el)
    settings_el = doc.settings.element
    uf = OxmlElement("w:updateFields")
    uf.set(qn("w:val"), "true")
    settings_el.append(uf)
    brk2 = doc.add_paragraph()
    brk2.add_run().add_break(WD_BREAK.PAGE)

    body_start = i
    # ---------- body ----------
    while body_start < len(md) and (md[body_start].strip() in ("", "---")):
        body_start += 1

    lines = md[body_start:]
    idx = 0
    while idx < len(lines):
        line = lines[idx]
        stripped = line.strip()

        if stripped.startswith("```"):
            lang = stripped[3:].strip()
            idx += 1
            block = []
            while idx < len(lines) and not lines[idx].strip().startswith("```"):
                block.append(lines[idx])
                idx += 1
            idx += 1
            if lang == "mermaid":
                lab = doc.add_paragraph()
                add_runs(lab, "_Схема (Mermaid — отрисовывается в Markdown-версии; ниже исходник):_", size=8.5, italic=True, color=GREY)
            p = doc.add_paragraph()
            p.paragraph_format.left_indent = Cm(0.4)
            p.paragraph_format.space_after = Pt(8)
            shade(p, "F2F4F7")
            for ln in block:
                r = p.add_run(ln + "\n")
                r.font.name = "Consolas"
                r.font.size = Pt(8.5)
            continue

        if not stripped:
            idx += 1
            continue

        if re.fullmatch(r"-{3,}|\*{3,}|_{3,}", stripped):
            p = doc.add_paragraph()
            p.paragraph_format.space_before = Pt(2)
            p.paragraph_format.space_after = Pt(2)
            pPr = p._p.get_or_add_pPr()
            bdr = OxmlElement("w:pBdr")
            bot = OxmlElement("w:bottom")
            bot.set(qn("w:val"), "single")
            bot.set(qn("w:sz"), "6")
            bot.set(qn("w:space"), "1")
            bot.set(qn("w:color"), "C9D0DA")
            bdr.append(bot)
            pPr.append(bdr)
            idx += 1
            continue

        # table
        if stripped.startswith("|") and idx + 1 < len(lines) and re.match(r"^\|[\s:|-]+\|$", lines[idx + 1].strip()):
            raw = []
            while idx < len(lines) and lines[idx].strip().startswith("|"):
                raw.append(lines[idx])
                idx += 1
            rows = [[c.strip() for c in r.strip().strip("|").split("|")] for r in raw]
            rows = [r for r in rows if not re.fullmatch(r"[-\s:|]+", "".join(r))]
            if not rows:
                continue
            ncol = max(len(r) for r in rows)
            tb = doc.add_table(rows=0, cols=ncol)
            tb.style = "Table Grid"
            tb.autofit = True
            fs = 8.5 if ncol <= 4 else (8.0 if ncol <= 5 else 7.0)
            for ridx, row in enumerate(rows):
                cells = tb.add_row().cells
                for cidx in range(ncol):
                    val = row[cidx] if cidx < len(row) else ""
                    p = cells[cidx].paragraphs[0]
                    p.paragraph_format.space_after = Pt(1)
                    p.paragraph_format.space_before = Pt(1)
                    add_runs(p, val, size=fs, bold=(ridx == 0),
                             color=RGBColor(0xFF, 0xFF, 0xFF) if ridx == 0 else None)
                    if ridx == 0:
                        shade(cells[cidx], "2F5578")
                    elif ridx % 2 == 0:
                        shade(cells[cidx], "F4F6F9")
            set_repeat_header(tb.rows[0])
            apply_widths(tb, rows, ncol)
            doc.add_paragraph().paragraph_format.space_after = Pt(2)
            continue

        m = re.match(r"^(#{1,6})\s+(.*)$", stripped)
        if m:
            lvl = len(m.group(1))
            style = f"Heading {min(lvl, 3)}"
            p = doc.add_paragraph(style=style)
            add_runs(p, m.group(2))
            if lvl == 1:
                p.paragraph_format.page_break_before = True if doc.paragraphs.index(p) > 2 else False
            idx += 1
            continue

        if stripped.startswith(">"):
            p = doc.add_paragraph()
            p.paragraph_format.left_indent = Cm(0.8)
            p.paragraph_format.space_after = Pt(6)
            shade(p, "FFF6E0")
            add_runs(p, stripped.lstrip("> ").strip(), size=9.5, italic=True)
            idx += 1
            continue

        mcheck = re.match(r"^(\s*)[-*]\s+\[( |x|X)\]\s+(.*)$", line)
        if mcheck:
            indent = len(mcheck.group(1)) // 2
            p = doc.add_paragraph(style="List Bullet" + (" 2" if indent >= 1 else ""))
            p.paragraph_format.space_after = Pt(2)
            add_runs(p, "\u2610 " + mcheck.group(3))
            idx += 1
            continue

        mbul = re.match(r"^(\s*)[-*]\s+(.*)$", line)
        if mbul:
            indent = len(mbul.group(1)) // 2
            p = doc.add_paragraph(style="List Bullet" + (" 2" if indent >= 1 else ""))
            p.paragraph_format.space_after = Pt(2)
            add_runs(p, mbul.group(2))
            idx += 1
            continue

        mnum = re.match(r"^(\s*)(\d+)\.\s+(.*)$", line)
        if mnum:
            p = doc.add_paragraph()
            p.paragraph_format.left_indent = Cm(0.9)
            p.paragraph_format.first_line_indent = Cm(-0.55)
            p.paragraph_format.space_after = Pt(3)
            r = p.add_run(f"{mnum.group(2)}. ")
            r.bold = True
            add_runs(p, mnum.group(3))
            idx += 1
            continue

        p = doc.add_paragraph()
        p.alignment = WD_ALIGN_PARAGRAPH.LEFT
        add_runs(p, stripped)
        idx += 1

    doc.save(DST)
    print("OK ->", DST)


build()
