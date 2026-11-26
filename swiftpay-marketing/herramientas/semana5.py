"""Semana 5 (7-11 dic 2026): SwiftMart precios nuevos (imagen), SwiftGo presentación (carrusel 4), SwiftRent (imagen)."""
import sys
sys.path.insert(0, __file__.rsplit("/", 1)[0])
from lib import *

I["store"] = '<svg viewBox="0 0 24 24"><path d="M3 9l1.5-5h15L21 9M3 9h18M3 9a3 3 0 0 0 6 0 3 3 0 0 0 6 0 3 3 0 0 0 6 0M5 12v9h14v-9M10 21v-5h4v5"/></svg>'
I["book"] = '<svg viewBox="0 0 24 24"><rect x="4" y="3" width="16" height="18" rx="2"/><path d="M8 8h8M8 12h8M8 16h5"/></svg>'
I["star"] = '<svg viewBox="0 0 24 24"><path d="m12 3 2.8 5.7 6.2.9-4.5 4.4 1.1 6.2L12 17.3 6.4 20.2l1.1-6.2L3 9.6l6.2-.9z"/></svg>'
I["bell"] = '<svg viewBox="0 0 24 24"><path d="M6 8a6 6 0 0 1 12 0c0 7 3 8 3 8H3s3-1 3-8M10 20a2 2 0 0 0 4 0"/></svg>'
I["link"] = '<svg viewBox="0 0 24 24"><path d="M10 14a4 4 0 0 0 5.7 0l3-3a4 4 0 0 0-5.7-5.7l-1 1"/><path d="M14 10a4 4 0 0 0-5.7 0l-3 3a4 4 0 0 0 5.7 5.7l1-1"/></svg>'
I["repeat"] = '<svg viewBox="0 0 24 24"><path d="M17 2l3 3-3 3"/><path d="M4 11V9a4 4 0 0 1 4-4h12M7 22l-3-3 3-3"/><path d="M20 13v2a4 4 0 0 1-4 4H4"/></svg>'
I["users"] = '<svg viewBox="0 0 24 24"><circle cx="9" cy="8" r="3.5"/><path d="M2 21c0-4 3-6.5 7-6.5s7 2.5 7 6.5"/><circle cx="17.5" cy="9" r="2.5"/><path d="M17 14.5c3 .2 5 2.3 5 5.5"/></svg>'
I["card"] = '<svg viewBox="0 0 24 24"><rect x="2" y="5" width="20" height="14" rx="2"/><path d="M2 10h20M6 15h4"/></svg>'
I["key"] = '<svg viewBox="0 0 24 24"><circle cx="8" cy="15" r="4"/><path d="m11 12 9-9M16 7l3 3M14 9l2 2"/></svg>'

def precio(icon, nombre, detalle, precio, unidad, incl=False):
    tag = '<span style="background:#ffd21f;color:#0b2d6b;border-radius:999px;padding:2px 14px;font-size:20px;margin-left:12px;vertical-align:middle">Incluido</span>' if incl else ''
    return (f'<div class="card" style="padding:20px 28px"><div class="ico" style="width:80px;height:80px">{I[icon]}</div>'
            f'<div style="flex:1"><div class="ct" style="font-size:34px">{nombre}{tag}</div><div class="cs" style="font-size:22px;color:#5d6984">{detalle}</div></div>'
            f'<div style="text-align:right"><div class="ct" style="font-size:42px;color:#1463e6">{precio}</div><div class="cs" style="font-size:20px;color:#5d6984;margin-top:0">{unidad}</div></div></div>')

P = {}

# ---------- Lunes 7: SwiftMart precios actualizados ----------
P["2026-12-07_precios_swiftmart.png"] = (1080, 1350, page(1080, 1350, None, f"""
<div class="col" style="top:60px">{logo_pill()}
 <div class="label" style="font-size:52px;margin-top:6px">Nuevos precios<br>de SwiftMart</div>
 <div class="box" style="font-size:36px">Pagas <b>solo por lo que usas</b></div></div>
<div class="col" style="top:540px;gap:20px">
 {precio("store","Retail e inventario","Punto de venta, caja e inventario","US$8","por usuario al mes",True)}
 {precio("split","Facturación electrónica","Comprobantes electrónicos y reportes","+US$2","al mes")}
 {precio("book","Contabilidad","Asientos, cuentas por cobrar y planilla","+US$6","al mes")}
 {precio("star","Planes de lealtad","Puntos, recompensas y billetera","+US$2","al mes")}
</div>
<div class="col" style="bottom:56px"><div class="yel" style="font-size:36px">30 días gratis · sin tarjeta</div>{wa_box(32)}</div>""", shade=False))

# ---------- Miércoles 9: SwiftGo carrusel (4) ----------
def chip(t):
    return f'<div style="background:#e8f1fe;color:#0b2d6b;font-weight:700;border-radius:999px;padding:12px 26px;font-size:30px">{t}</div>'

P["2026-12-09_swiftgo_1.png"] = (1080, 1350, page(1080, 1350, None, f"""
<div class="col" style="top:56px">{logo_pill()}
 <div class="label" style="font-size:58px;margin-top:6px">Cobra sin<br>perseguir</div>
 <div class="box" style="font-size:38px">Conoce <b>SwiftGo</b></div></div>
<div class="col" style="top:560px">
 <div class="card" style="flex-direction:column;gap:26px;padding:38px 34px">
  <div class="ct" style="font-size:36px;text-align:center">Para negocios que cobran todos los meses<br><span style="color:#1463e6">a los mismos clientes</span></div>
  <div style="display:flex;flex-wrap:wrap;gap:14px;justify-content:center">
   {chip("Colegios")}{chip("Academias")}{chip("Gimnasios")}{chip("Condominios")}{chip("Clubes")}{chip("Servicios profesionales")}
  </div>
 </div></div>
<div class="col" style="bottom:70px"><div class="yel" style="font-size:34px">Desliza →</div></div>
<div class="num">1/4</div>""", shade=False))

def lam(n, icon, title, sub, extra="", top=300):
    return (1080, 1350, page(1080, 1350, None, f"""
<div class="col" style="top:56px">{logo_pill()}</div>
<div class="col" style="top:{top}px;gap:30px">
 <div class="card" style="flex-direction:column;align-items:flex-start;padding:50px;gap:28px">
  <div class="ico" style="width:150px;height:150px;border-radius:34px">{I[icon].replace('<svg','<svg style="width:90px;height:90px"')}</div>
  <div class="ct" style="font-size:60px">{title}</div>
  <div class="cs" style="font-size:35px;color:#2b3752;font-weight:500">{sub}</div>
 </div>{extra}</div>
<div class="num">{n}/4</div>""", shade=False))

P["2026-12-09_swiftgo_2.png"] = lam(2, "bell", "Recordatorios y mora, solos",
    "Aviso antes y después del vencimiento. Mora por porcentaje o monto fijo, con días de gracia.",
    '<div class="card" style="justify-content:center;gap:16px;padding:26px 20px">' + chip("Por vencer") + '<span class="ct" style="font-size:34px">→</span>' + chip("Vencido") + '<span class="ct" style="font-size:34px">→</span>' + chip("Mora") + '</div>', top=300)
P["2026-12-09_swiftgo_3.png"] = lam(3, "link", "Pagan en un clic",
    "Por enlace o en tu portal, con tu logo y colores. Con tarjeta o cuenta bancaria, sin crear usuario ni contraseña.",
    '<div class="card" style="flex-wrap:wrap;justify-content:center;gap:14px;padding:26px 20px">' + chip("Tarjeta con 3-D Secure") + chip("Cuenta bancaria (IBAN)") + chip("Colones y dólares") + '</div>', top=270)
P["2026-12-09_swiftgo_4.png"] = lam(4, "repeat", "Cobro automático",
    "Tu cliente lo activa en el portal y SwiftGo cobra cada documento al vencer. Desde el plan Profesional.",
    f'<div class="box" style="font-size:34px">Planes desde <b>USD 8 al mes</b></div><div class="yel" style="font-size:34px">Afíliate en swiftpay.one</div>{wa_box()}', top=300)

# ---------- Viernes 11: SwiftRent ----------
P["2026-12-11_swiftrent.png"] = (1080, 1350, page(1080, 1350, None, f"""
<div class="col" style="top:56px">{logo_pill()}
 <div class="label" style="font-size:48px;margin-top:6px">¿Tienes un negocio<br>de alquiler?</div>
 <div class="box" style="font-size:34px">Renta, cobra y controla con <b>SwiftRent</b></div></div>
<div class="col" style="top:490px;gap:20px">
 {card("users","App para tu personal","Y app para tus clientes",40,27)}
 {card("card","Reservan y pagan con tarjeta","Desde la app para clientes",40,27)}
 {card("key","Una sola cuenta","Para todas las empresas de alquiler afiliadas",40,27)}
 <div class="box" style="font-size:34px"><b>US$3</b> por usuario al mes</div>
</div>
<div class="col" style="bottom:56px"><div class="yel" style="font-size:36px">30 días gratis · sin tarjeta</div>{wa_box(32)}</div>""", shade=False))

render(P, sys.argv[1])
print("\n".join(P))
