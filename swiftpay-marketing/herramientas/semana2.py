import sys
sys.path.insert(0, __file__.rsplit("/", 1)[0])
from lib import *

I["box"] = '<svg viewBox="0 0 24 24"><path d="M21 8 12 3 3 8v8l9 5 9-5z"/><path d="M3 8l9 5 9-5M12 13v8"/></svg>'
I["sheet"] = '<svg viewBox="0 0 24 24"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M3 9h18M3 15h18M9 3v18"/></svg>'
I["truck"] = '<svg viewBox="0 0 24 24"><path d="M3 6h11v10H3zM14 9h4l3 3v4h-7"/><circle cx="7" cy="18" r="2"/><circle cx="17" cy="18" r="2"/></svg>'
I["mail"] = '<svg viewBox="0 0 24 24"><rect x="3" y="5" width="18" height="14" rx="2"/><path d="m3 7 9 6 9-6"/></svg>'
I["book"] = '<svg viewBox="0 0 24 24"><path d="M4 4h12a4 4 0 0 1 4 4v12H8a4 4 0 0 1-4-4z"/><path d="M8 9h8M8 13h6"/></svg>'
I["star"] = '<svg viewBox="0 0 24 24"><path d="m12 3 2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1-4.4-4.3 6.1-.9z"/></svg>'
I["store"] = '<svg viewBox="0 0 24 24"><path d="M3 9l2-5h14l2 5M4 9v11h16V9M3 9h18M9 20v-6h6v6"/></svg>'

def precio(icon, nombre, detalle, precio, unidad, incl=False):
    tag = '<span style="background:#ffd21f;color:#0b2d6b;border-radius:999px;padding:2px 14px;font-size:20px;margin-left:12px;vertical-align:middle">Incluido</span>' if incl else ''
    return (f'<div class="card" style="padding:20px 28px"><div class="ico" style="width:80px;height:80px">{I[icon]}</div>'
            f'<div style="flex:1"><div class="ct" style="font-size:34px">{nombre}{tag}</div><div class="cs" style="font-size:22px;color:#5d6984">{detalle}</div></div>'
            f'<div style="text-align:right"><div class="ct" style="font-size:42px;color:#1463e6">{precio}</div><div class="cs" style="font-size:20px;color:#5d6984;margin-top:0">{unidad}</div></div></div>')

P = {}
# ---------- Lunes 16: precio claro ----------
P["2026-11-16_precio.png"] = (1080, 1350, page(1080, 1350, None, f"""
<div class="col" style="top:70px">{logo_pill()}
 <div class="label" style="font-size:52px;margin-top:6px">¿Cuánto cuesta SwiftMart?</div>
 <div class="box" style="font-size:38px">Pagas <b>solo por lo que usas</b></div></div>
<div class="col" style="top:500px;gap:22px">
 {precio("store","Retail e inventario","Punto de venta, caja e inventario","US$8","por usuario al mes",True)}
 {precio("split","Facturación electrónica","Comprobantes electrónicos y reportes","+US$5","al mes")}
 {precio("book","Contabilidad","Asientos, cuentas por cobrar y planilla","+US$6","al mes")}
 {precio("star","Planes de lealtad","Puntos, recompensas y billetera","+US$4","al mes")}
</div>
<div class="col" style="bottom:60px"><div class="yel" style="font-size:36px">30 días gratis · sin tarjeta</div>{wa_box(32)}</div>""", shade=False))

# ---------- Miércoles 18: carrusel inventario ----------
P["2026-11-18_carrusel_1.png"] = (1080, 1350, f"""<html><head><meta charset='utf-8'><style>:root{{--w:1080px;--h:1350px}}{CSS}</style></head>
<body style="background:linear-gradient(180deg,#eaf4fe,#d6e9fd)">
<div class="col" style="top:56px">{logo_pill()}
 <div class="label" style="font-size:56px;margin-top:6px">Inventario<br>bajo control</div></div>
<img src="{A/'ilus_swiftmart.jpg'}" style="position:absolute;left:0;top:400px;width:1080px;border-radius:0;mask-image:linear-gradient(180deg,transparent 0,#000 8%,#000 90%,transparent 100%)">
<div class="col" style="bottom:70px"><div class="box" style="font-size:40px">Sabe cuánto vale tu inventario<br><b>en todo momento</b></div><div class="yel" style="font-size:34px">Desliza →</div></div>
<div class="num">1/4</div></body></html>""")

def lam(n, icon, title, sub, extra=""):
    return (1080, 1350, page(1080, 1350, "ilus_swiftmart.jpg", f"""
<div class="col" style="top:56px">{logo_pill()}</div>
<div class="col" style="top:330px;gap:30px">
 <div class="card" style="flex-direction:column;align-items:flex-start;padding:50px;gap:30px">
  <div class="ico" style="width:150px;height:150px;border-radius:34px">{I[icon].replace('<svg','<svg style="width:90px;height:90px"')}</div>
  <div class="ct" style="font-size:64px">{title}</div>
  <div class="cs" style="font-size:36px;color:#2b3752;font-weight:500">{sub}</div>
 </div>{extra}</div>
<div class="num">{n}/4</div>""", blur=True, shade=False))

P["2026-11-18_carrusel_2.png"] = lam(2, "box", "Kardex y costo promedio", "Inventario por bodega o sucursal, con costo promedio ponderado y el kardex de cada producto.")
P["2026-11-18_carrusel_3.png"] = lam(3, "sheet", "Cientos de productos en minutos", "Importa tu catálogo desde Excel o CSV, y expórtalo para editarlo y volverlo a cargar.")
P["2026-11-18_carrusel_4.png"] = lam(4, "truck", "Compras, traslados y conteos", "Las compras a proveedores actualizan el stock y el costo. Traslados entre sucursales y conteos físicos.",
    f'<div class="yel" style="font-size:36px">30 días gratis · sin tarjeta</div>{wa_box()}')

# ---------- Viernes 20: factura electrónica (post 4:5 + historia 9:16) ----------
def factura(w, h, top, mid, bottom, s=1.0, ptop=430, pw=520, ps=1.0):
    return (w, h, page(w, h, None, f"""
<div class="col" style="top:{top}px">{logo_pill(int(54*s))}
 <div class="label" style="font-size:{int(54*s)}px;margin-top:6px">Factura electrónica</div>
 <div class="box" style="font-size:{int(40*s)}px">Integrada en <b>SwiftMart</b></div></div>

<div style="position:absolute;left:50%;top:{ptop}px;width:{pw}px;transform:translateX(-50%) rotate(-4deg) scale({ps});transform-origin:top center;background:#fff;border-radius:18px;box-shadow:0 20px 50px rgba(0,0,0,.3);padding:26px 30px;font-family:Poppins;color:#0b2d6b">
 <div style="display:flex;justify-content:space-between;align-items:center"><div style="font-weight:700;font-size:26px">Factura electrónica</div>
 <div style="width:46px;height:46px;border-radius:50%;background:#22c55e;color:#fff;display:grid;place-items:center;font-size:28px;font-weight:700">✓</div></div>
 <div style="height:10px;width:60%;background:#e8f1fe;border-radius:6px;margin:16px 0 8px"></div><div style="height:10px;width:40%;background:#e8f1fe;border-radius:6px"></div>
 <div style="display:flex;justify-content:space-between;margin-top:18px;border-top:2px dashed #dbe4f0;padding-top:12px;font-weight:700;font-size:28px"><span>Total</span><span style="color:#1463e6">₡12.500</span></div>
</div>
<div class="col" style="bottom:{mid}px;gap:22px">
 {card("mail","Llega al correo de tu cliente","Factura electrónica válida, al instante",int(40*s),int(27*s))}
 {card("book","Pasa sola a tu contabilidad","Facturas, pagos e inventario sincronizados",int(40*s),int(27*s))}
</div>
<div class="col" style="bottom:{bottom}px"><div class="yel" style="font-size:{int(36*s)}px">30 días gratis · sin tarjeta</div>{wa_box(int(32*s))}</div>""", shade=False))

P["2026-11-20_factura.png"] = factura(1080, 1350, 56, 300, 60)
P["2026-11-20_factura_historia.png"] = factura(1080, 1920, 150, 480, 220, 1.15, ptop=640, pw=520, ps=1.4)

render(P, sys.argv[1])
print("\n".join(P))
