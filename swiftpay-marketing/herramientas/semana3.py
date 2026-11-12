"""Semana 3 (23-27 nov 2026): lealtad (carrusel), control en caja (reel), tiendas/ferreterías/farmacias (imagen)."""
import sys
sys.path.insert(0, __file__.rsplit("/", 1)[0])
from lib import *

I["gift"] = '<svg viewBox="0 0 24 24"><rect x="3" y="8" width="18" height="4" rx="1"/><path d="M5 12v9h14v-9M12 8v13M12 8S10.5 3 8 4s0 4 4 4zM12 8s1.5-5 4-4 0 4-4 4z"/></svg>'
I["wallet"] = '<svg viewBox="0 0 24 24"><path d="M3 7a2 2 0 0 1 2-2h13v4"/><rect x="3" y="7" width="18" height="13" rx="2"/><circle cx="16.5" cy="13.5" r="1.5"/></svg>'
I["cash"] = '<svg viewBox="0 0 24 24"><rect x="2" y="6" width="20" height="12" rx="2"/><circle cx="12" cy="12" r="3"/><path d="M6 12h.01M18 12h.01"/></svg>'
I["percent"] = '<svg viewBox="0 0 24 24"><path d="M19 5 5 19"/><circle cx="7" cy="7" r="2.5"/><circle cx="17" cy="17" r="2.5"/></svg>'
I["lock"] = '<svg viewBox="0 0 24 24"><rect x="4" y="11" width="16" height="10" rx="2"/><path d="M8 11V7a4 4 0 0 1 8 0v4M12 15v2"/></svg>'
I["shield"] = '<svg viewBox="0 0 24 24"><path d="M12 3 4 6v6c0 5 3.5 8 8 9 4.5-1 8-4 8-9V6z"/><path d="m9 12 2 2 4-4"/></svg>'
I["calendar"] = '<svg viewBox="0 0 24 24"><rect x="3" y="5" width="18" height="16" rx="2"/><path d="M3 10h18M8 3v4M16 3v4M8 14h3"/></svg>'
I["clock"] = '<svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></svg>'
I["combo"] = '<svg viewBox="0 0 24 24"><rect x="3" y="3" width="8" height="8" rx="1.5"/><rect x="13" y="3" width="8" height="8" rx="1.5"/><rect x="3" y="13" width="8" height="8" rx="1.5"/><path d="M17 14v6M14 17h6"/></svg>'

P = {}

# ---------- Lunes 23: carrusel (3) programa de lealtad ----------
def sellos(n_llenos=9, total=10):
    s = ""
    for i in range(total):
        if i < n_llenos:
            s += '<div style="width:74px;height:74px;border-radius:50%;background:#1463e6;color:#fff;display:grid;place-items:center;font-size:38px;font-weight:700">✓</div>'
        else:
            s += '<div style="width:74px;height:74px;border-radius:50%;border:4px dashed #ffb800;background:#fff7d6;display:grid;place-items:center">' + I["gift"].replace('<svg', '<svg style="width:42px;height:42px;stroke:#0b2d6b;fill:none;stroke-width:2.2"') + '</div>'
    return f'<div style="display:grid;grid-template-columns:repeat(5,74px);gap:18px;justify-content:center">{s}</div>'

P["2026-11-23_carrusel_1.png"] = (1080, 1350, page(1080, 1350, "foto_cafe.jpg", f"""
<div class="col" style="top:56px">{logo_pill()}
 <div class="label" style="font-size:54px;margin-top:6px">Clientes que<br>siempre vuelven</div>
 <div class="box" style="font-size:36px">Tu propio <b>programa de lealtad</b></div></div>
<div class="col" style="bottom:70px">
 <div class="card" style="flex-direction:column;gap:22px;padding:30px 30px 34px">
  <div class="ct" style="font-size:36px;text-align:center">Por ejemplo: <span style="color:#1463e6">el décimo café, gratis</span></div>
  {sellos()}
 </div>
 <div class="yel" style="font-size:34px">Desliza →</div></div>
<div class="num">1/3</div>"""))

def lam3(n, icon, title, sub, extra=""):
    return (1080, 1350, page(1080, 1350, "foto_cafe.jpg", f"""
<div class="col" style="top:56px">{logo_pill()}</div>
<div class="col" style="top:300px;gap:30px">
 <div class="card" style="flex-direction:column;align-items:flex-start;padding:50px;gap:30px">
  <div class="ico" style="width:150px;height:150px;border-radius:34px">{I[icon].replace('<svg','<svg style="width:90px;height:90px"')}</div>
  <div class="ct" style="font-size:62px">{title}</div>
  <div class="cs" style="font-size:36px;color:#2b3752;font-weight:500">{sub}</div>
 </div>{extra}</div>
<div class="num">{n}/3</div>""", blur=True, shade=False))

P["2026-11-23_carrusel_2.png"] = lam3(2, "gift", "Puntos o sellos", "Tus clientes suman puntos o sellos por compra o visita. Las recompensas las defines tú.")
P["2026-11-23_carrusel_3.png"] = lam3(3, "wallet", "Tarjeta en su celular", "Tarjeta digital en la billetera (Wallet) del cliente. Canje al momento de cobrar, con historial de compras y canjes.",
    f'<div class="box" style="font-size:32px">Planes de lealtad: <b>+US$4 al mes</b></div><div class="yel" style="font-size:36px">30 días gratis · sin tarjeta</div>{wa_box()}')

# ---------- Miércoles 25: reel control en caja (escenas acumulativas 1080x1920) ----------
reel_cards = [
    card("percent", "Tú defines el descuento máximo", "Cambios de precio en caja, con control", 42, 29),
    card("lock", "Si se pasa, pide PIN", "El supervisor autoriza con su PIN", 42, 29),
    card("shield", "Nunca bajo costo", "No se vende bajo costo sin autorización", 42, 29),
]
def reel_scene(k):
    cards = "".join(reel_cards[:min(k, 3)])
    cta = f'<div class="yel" style="font-size:42px">30 días gratis · sin tarjeta</div>{wa_box(38)}' if k >= 4 else ""
    return (1080, 1920, page(1080, 1920, None, f"""
<div class="col" style="top:150px">{logo_pill(62)}
 <div class="label" style="font-size:62px;margin-top:10px">Control<br>en tu caja</div>
 <div class="box" style="font-size:42px">Descuentos <b>sin sorpresas</b></div></div>
<div class="col" style="top:760px;gap:30px">{cards}</div>
<div class="col" style="bottom:200px;gap:28px">{cta}</div>""", shade=False))
for k in range(5):
    P[f"_reel_{k}.png"] = reel_scene(k)

# ---------- Viernes 27: tiendas, ferreterías y farmacias ----------
P["2026-11-27_promociones.png"] = (1080, 1350, page(1080, 1350, None, f"""
<div class="col" style="top:56px">{logo_pill()}
 <div class="label" style="font-size:48px;margin-top:6px">¿Tienes tienda, ferretería<br>o farmacia?</div>
 <div class="box" style="font-size:38px">Promociones que <b>se cobran solas</b></div></div>
<div class="col" style="top:480px;gap:22px">
 {card("combo","Combos y extras","Se suman solos a la factura",40,27)}
 {card("calendar","Ofertas con fecha","Con fecha de inicio y de fin",40,27)}
 {card("clock","Precios por horario","Descuentos en ciertos días y horas",40,27)}
 <div class="box" style="font-size:30px">Siempre se cobra <b>el precio más bajo que aplique</b></div>
</div>
<div class="col" style="bottom:60px"><div class="yel" style="font-size:36px">30 días gratis · sin tarjeta</div>{wa_box(32)}</div>""", shade=False))

render(P, sys.argv[1])
print("\n".join(P))
