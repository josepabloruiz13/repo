"""Piezas semana 1 con el estilo de los videos de SwiftPay: foto de fondo + píldoras y tarjetas blancas."""
import sys, pathlib
from playwright.sync_api import sync_playwright

A = (pathlib.Path(__file__).resolve().parent.parent / "assets")

CSS = """
*{box-sizing:border-box;margin:0;padding:0}
body{width:var(--w);height:var(--h);overflow:hidden;font-family:'Poppins',sans-serif;position:relative;background:#0b2d6b}
.bg{position:absolute;inset:0;background-size:cover;background-position:var(--pos,center)}
.blur{filter:blur(22px) brightness(.55);transform:scale(1.15)}
.shade{position:absolute;inset:0;background:linear-gradient(180deg,rgba(8,25,60,.15) 0%,rgba(8,25,60,0) 30%,rgba(8,25,60,.0) 55%,rgba(8,25,60,.55) 100%)}
.col{position:absolute;left:0;right:0;display:flex;flex-direction:column;align-items:center;gap:22px;padding:0 60px}
.pill{background:#fff;border-radius:999px;padding:16px 34px;box-shadow:0 8px 24px rgba(0,0,0,.18);display:flex;align-items:center}
.pill img{height:54px}
.label{background:#1463e6;color:#fff;font-weight:700;text-transform:uppercase;border:5px solid #fff;border-radius:22px;padding:14px 30px;text-align:center;line-height:1.15;box-shadow:0 8px 24px rgba(0,0,0,.2)}
.box{background:#fff;color:#0b2d6b;font-weight:700;border-radius:24px;padding:18px 34px;text-align:center;line-height:1.15;box-shadow:0 8px 24px rgba(0,0,0,.18)}
.box b{color:#1463e6}
.card{background:#fff;border-radius:26px;box-shadow:0 10px 30px rgba(0,0,0,.22);display:flex;align-items:center;gap:26px;padding:24px 30px;width:100%}
.ico{flex:none;width:96px;height:96px;border-radius:22px;background:#e8f1fe;display:grid;place-items:center}
.ico svg{width:58px;height:58px;stroke:#1463e6;fill:none;stroke-width:2.2;stroke-linecap:round;stroke-linejoin:round}
.ct{font-weight:700;color:#0b2d6b;line-height:1.1}
.cs{font-weight:500;color:#1463e6;line-height:1.25;margin-top:6px}
.yel{background:#ffd21f;color:#0b2d6b;font-weight:700;border-radius:999px;padding:14px 38px;text-transform:uppercase;box-shadow:0 8px 24px rgba(0,0,0,.2)}
.wa{display:flex;align-items:center;gap:14px;color:#0b2d6b;font-weight:700}
.wa i{width:46px;height:46px;border-radius:50%;background:#25d366;display:grid;place-items:center}
.wa i svg{width:26px;height:26px;fill:#fff}
.mart{display:flex;align-items:center;gap:30px}
.mart img{height:150px}
.chk{display:flex;flex-direction:column;gap:8px}
.chk div{display:flex;align-items:center;gap:12px;font-weight:700;color:#0b2d6b}
.chk span{width:34px;height:34px;border-radius:50%;background:#1463e6;display:grid;place-items:center;color:#fff;font-size:22px}
.num{position:absolute;right:56px;top:66px;background:rgba(255,255,255,.9);color:#0b2d6b;font-weight:700;border-radius:999px;padding:6px 18px;font-size:26px}
"""

WA_SVG = '<svg viewBox="0 0 24 24"><path d="M12 2a10 10 0 0 0-8.6 15.1L2 22l5-1.3A10 10 0 1 0 12 2zm5.3 14.1c-.2.6-1.3 1.2-1.8 1.2-.5.1-1 .2-3.3-.7-2.8-1.1-4.5-3.9-4.7-4.1-.1-.2-1.1-1.5-1.1-2.9s.7-2 1-2.3c.2-.3.5-.3.7-.3h.5c.2 0 .4 0 .6.5l.8 1.9c.1.2.1.4 0 .5l-.4.6-.4.4c-.1.2-.3.3-.1.6.2.3.8 1.3 1.7 2.1 1.1 1 2.1 1.3 2.4 1.5.3.1.5.1.6-.1l.9-1c.2-.3.4-.2.6-.1l1.8.9c.3.1.4.2.5.3.1.2.1.6-.1 1.1z"/></svg>'
I = {
 "scan": '<svg viewBox="0 0 24 24"><path d="M3 7V5a2 2 0 0 1 2-2h2M17 3h2a2 2 0 0 1 2 2v2M21 17v2a2 2 0 0 1-2 2h-2M7 21H5a2 2 0 0 1-2-2v-2"/><path d="M7 8v8M10 8v8M13 8v8M16 8v8"/></svg>',
 "alert": '<svg viewBox="0 0 24 24"><circle cx="12" cy="13" r="8"/><path d="M12 9v4l2 2M5 3 2 6M22 6l-3-3"/></svg>',
 "tag": '<svg viewBox="0 0 24 24"><path d="M20.6 13.4 13.4 20.6a2 2 0 0 1-2.8 0L3 13V3h10l7.6 7.6a2 2 0 0 1 0 2.8z"/><circle cx="7.5" cy="7.5" r="1.5"/></svg>',
 "phone": '<svg viewBox="0 0 24 24"><rect x="6" y="2" width="12" height="20" rx="2"/><path d="M10 18h4M9 7h6M9 11h4"/></svg>',
 "chef": '<svg viewBox="0 0 24 24"><rect x="3" y="4" width="18" height="12" rx="2"/><path d="M8 20h8M12 16v4M7 8h4M7 12h7"/></svg>',
 "split": '<svg viewBox="0 0 24 24"><path d="M6 2h12v20l-3-2-3 2-3-2-3 2z"/><path d="M9 7h6M9 11h6M9 15h3"/></svg>',
}

def wa_box(fs=34):
    return f'<div class="pill wa" style="font-size:{fs}px">{"<i>"+WA_SVG+"</i>"}+506 6180 1322</div>'

def logo_pill(h=54):
    return f'<div class="pill"><img src="{A/"swiftpay_logo.png"}" style="height:{h}px"></div>'

def card(icon, title, sub, tf=40, sf=27):
    return f'<div class="card"><div class="ico">{I[icon]}</div><div><div class="ct" style="font-size:{tf}px">{title}</div><div class="cs" style="font-size:{sf}px">{sub}</div></div></div>'

def mart_card(fs=34, logo=150):
    return (f'<div class="card mart" style="justify-content:center"><img src="{A/"swiftmart_logo.png"}" style="height:{logo}px">'
            f'<div class="chk" style="font-size:{fs}px"><div><span>✓</span>Vende</div><div><span>✓</span>Factura</div><div><span>✓</span>Inventario</div></div></div>')

def page(w, h, bg, body, pos="center", blur=False, shade=True):
    if bg is None:
        b = '<div class="bg" style="background:radial-gradient(110% 70% at 90% 0%,#14b4f5 0%,rgba(20,180,245,0) 55%),linear-gradient(165deg,#1463e6 0%,#0b3a9c 60%,#0b2d6b 100%)"></div>'
    else:
        b = f'<div class="bg{" blur" if blur else ""}" style="background-image:url(\'{A/bg}\');--pos:{pos}"></div>'
    s = '<div class="shade"></div>' if shade else ""
    return f"<html><head><meta charset='utf-8'><style>:root{{--w:{w}px;--h:{h}px}}{CSS}</style></head><body>{b}{s}{body}</body></html>"


def render(P, out):
    out = pathlib.Path(out); out.mkdir(parents=True, exist_ok=True); tmp = out / "_html"; tmp.mkdir(exist_ok=True)
    with sync_playwright() as p:
        b = p.chromium.launch()
        for nombre, (w, h, html) in P.items():
            f = tmp / (nombre + ".html"); f.write_text(html, encoding="utf-8")
            pg = b.new_page(viewport={"width": w, "height": h}, device_scale_factor=1)
            pg.goto(f.as_uri(), wait_until="networkidle"); pg.screenshot(path=str(out / nombre)); pg.close()
        b.close()
