"""Semana 4 (30 nov - 4 dic 2026): sucursales y equipo (carrusel), cómo empezar (reel), diciembre / caja ágil (imagen)."""
import sys
sys.path.insert(0, __file__.rsplit("/", 1)[0])
from lib import *

I["building"] = '<svg viewBox="0 0 24 24"><path d="M3 21h18M5 21V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v16"/><path d="M9 7h2M13 7h2M9 11h2M13 11h2M9 15h2M13 15h2M10 21v-3h4v3"/></svg>'
I["store"] = '<svg viewBox="0 0 24 24"><path d="M3 9l1.5-5h15L21 9M3 9h18M3 9a3 3 0 0 0 6 0 3 3 0 0 0 6 0 3 3 0 0 0 6 0M5 12v9h14v-9M10 21v-5h4v5"/></svg>'
I["users"] = '<svg viewBox="0 0 24 24"><circle cx="9" cy="8" r="3.5"/><path d="M2 21c0-4 3-6.5 7-6.5s7 2.5 7 6.5"/><circle cx="17.5" cy="9" r="2.5"/><path d="M17 14.5c3 .2 5 2.3 5 5.5"/></svg>'
I["lock"] = '<svg viewBox="0 0 24 24"><rect x="4" y="11" width="16" height="10" rx="2"/><path d="M8 11V7a4 4 0 0 1 8 0v4M12 15v2"/></svg>'
I["form"] = '<svg viewBox="0 0 24 24"><rect x="4" y="3" width="16" height="18" rx="2"/><path d="M8 8h8M8 12h8M8 16h4"/><path d="m14 16 1.5 1.5L18 15"/></svg>'
I["sliders"] = '<svg viewBox="0 0 24 24"><path d="M4 6h10M18 6h2M4 12h4M12 12h8M4 18h12M20 18h0"/><circle cx="16" cy="6" r="2"/><circle cx="10" cy="12" r="2"/><circle cx="18" cy="18" r="2"/></svg>'
I["bolt"] = '<svg viewBox="0 0 24 24"><path d="M13 2 4 14h7l-1 8 9-12h-7z"/></svg>'
I["gift"] = '<svg viewBox="0 0 24 24"><rect x="3" y="8" width="18" height="4" rx="1"/><path d="M5 12v9h14v-9M12 8v13M12 8S10.5 3 8 4s0 4 4 4zM12 8s1.5-5 4-4 0 4-4 4z"/></svg>'
I["cash"] = '<svg viewBox="0 0 24 24"><rect x="2" y="6" width="20" height="12" rx="2"/><circle cx="12" cy="12" r="3"/><path d="M6 12h.01M18 12h.01"/></svg>'
I["chart"] = '<svg viewBox="0 0 24 24"><path d="M3 3v18h18"/><path d="M7 15v3M11 11v7M15 8v10M19 5v13"/></svg>'

P = {}

# ---------- Lunes 30: carrusel (3) sucursales y equipo ----------
def mini(icon, txt, big=False):
    s = 110 if big else 84
    i = 66 if big else 50
    return (f'<div style="display:flex;flex-direction:column;align-items:center;gap:10px">'
            f'<div class="ico" style="width:{s}px;height:{s}px;border-radius:24px{";background:#1463e6" if big else ""}">'
            f'{I[icon].replace("<svg", f"<svg style=\"width:{i}px;height:{i}px{";stroke:#fff" if big else ""}\"")}</div>'
            f'<div class="ct" style="font-size:{30 if big else 26}px;text-align:center">{txt}</div></div>')

diagrama = f"""
<div class="card" style="flex-direction:column;gap:0;padding:34px 30px 36px">
 {mini("building", "Casa matriz: ve todo", True)}
 <svg viewBox="0 0 900 90" style="width:100%;height:90px;stroke:#1463e6;stroke-width:4;fill:none;stroke-dasharray:10 8">
  <path d="M450 6v30M150 36h600M150 36v48M450 36v48M750 36v48"/></svg>
 <div style="display:grid;grid-template-columns:repeat(3,1fr);width:100%">
  {mini("store", "Sucursal 1")}{mini("store", "Sucursal 2")}{mini("store", "Sucursal 3")}
 </div>
 <div class="cs" style="font-size:28px;margin-top:22px;text-align:center">Cada sucursal ve solo lo suyo</div>
</div>"""

P["2026-11-30_carrusel_1.png"] = (1080, 1350, page(1080, 1350, None, f"""
<div class="col" style="top:56px">{logo_pill()}
 <div class="label" style="font-size:54px;margin-top:6px">¿Tienes varias<br>sucursales?</div>
 <div class="box" style="font-size:36px">Todo tu negocio <b>en una sola app</b></div></div>
<div class="col" style="top:560px">{diagrama}</div>
<div class="col" style="bottom:70px"><div class="yel" style="font-size:34px">Desliza →</div></div>
<div class="num">1/3</div>""", shade=False))

def lam3(n, icon, title, sub, extra="", top=270):
    return (1080, 1350, page(1080, 1350, None, f"""
<div class="col" style="top:56px">{logo_pill()}</div>
<div class="col" style="top:{top}px;gap:30px">
 <div class="card" style="flex-direction:column;align-items:flex-start;padding:50px;gap:30px">
  <div class="ico" style="width:150px;height:150px;border-radius:34px">{I[icon].replace('<svg','<svg style="width:90px;height:90px"')}</div>
  <div class="ct" style="font-size:62px">{title}</div>
  <div class="cs" style="font-size:36px;color:#2b3752;font-weight:500">{sub}</div>
 </div>{extra}</div>
<div class="num">{n}/3</div>""", shade=False))

P["2026-11-30_carrusel_2.png"] = lam3(2, "building", "La casa matriz ve todo",
    "Cada sucursal ve solo lo suyo. Inventario por bodega o sucursal, con traslados entre sucursales.",
    mart_card(32, 130), top=290)
P["2026-11-30_carrusel_3.png"] = lam3(3, "users", "Permisos por rol",
    "Tu equipo trabaja con permisos por grupos de usuarios. Por ejemplo: cajero, supervisor y administrador.",
    f'<div class="yel" style="font-size:36px">30 días gratis · sin tarjeta</div>{wa_box()}', top=330)

# ---------- Miércoles 2: reel 20 s cómo empezar (6 escenas acumulativas 1080x1920) ----------
reel_cards = [
    card("form", "Regístrate en línea", "En minutos, sin visitas ni papeleo", 42, 29),
    card("sliders", "Elige tus funciones", "Pagas solo por las funciones y usuarios que eliges", 42, 29),
    card("bolt", "Se activa de inmediato", "No requiere aprobación", 42, 29),
    card("gift", "30 días gratis", "La prueba no requiere tarjeta", 42, 29),
]
def reel_scene(k):
    cards = "".join(reel_cards[:min(k, 4)])
    cta = f'<div class="box" style="font-size:34px">En celular o tablet · <b>App Store y Google Play</b></div>{wa_box(38)}' if k >= 5 else ""
    return (1080, 1920, page(1080, 1920, None, f"""
<div class="col" style="top:140px">{logo_pill(62)}
 <div class="label" style="font-size:62px;margin-top:10px">Empieza hoy<br>con SwiftMart</div>
 <div class="box" style="font-size:42px">Así de <b>fácil</b></div></div>
<div class="col" style="top:700px;gap:28px">{cards}</div>
<div class="col" style="bottom:150px;gap:26px">{cta}</div>""", shade=False))
for k in range(6):
    P[f"_reel_{k}.png"] = reel_scene(k)

# ---------- Viernes 4: diciembre, caja ágil ----------
P["2026-12-04_diciembre.png"] = (1080, 1350, page(1080, 1350, None, f"""
<div class="col" style="top:56px">{logo_pill()}
 <div class="label" style="font-size:56px;margin-top:6px">Listo para<br>diciembre</div>
 <div class="box" style="font-size:38px">Tu caja, <b>más ágil</b></div></div>
<div class="col" style="top:500px;gap:22px">
 {card("scan","Caja rápida","Escanea el código de barras o busca por nombre",40,27)}
 {card("cash","Cierre de caja por turno","Cada pago con su forma de pago y quién cobró",40,27)}
 {card("chart","Reportes de ventas","Ventas por categoría y transacciones",40,27)}
</div>
<div class="col" style="bottom:60px"><div class="yel" style="font-size:36px">30 días gratis · sin tarjeta</div>{wa_box(32)}</div>""", shade=False))

render(P, sys.argv[1])
print("\n".join(P))
