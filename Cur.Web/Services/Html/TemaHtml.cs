using Cur.Web.Models;

namespace Cur.Web.Services.Html;

/// <summary>
/// Apariencia y variantes de estructura de cada plantilla en la exportacion HTML.
/// Los colores replican los de la plantilla equivalente en PDF.
/// </summary>
internal sealed record TemaHtml(
    string Clase,
    bool MostrarFoto,
    bool BarraLateral,
    string Css)
{
    public static TemaHtml Para(PlantillaCv plantilla) => plantilla switch
    {
        PlantillaCv.Ejecutiva => new("hv-ejecutiva", MostrarFoto: true, BarraLateral: true, CssEjecutiva),
        // Igual que en el PDF: la minimal renuncia a la foto para favorecer los filtros ATS.
        PlantillaCv.Minimal => new("hv-minimal", MostrarFoto: false, BarraLateral: false, CssMinimal),
        PlantillaCv.Timeline => new("hv-timeline", MostrarFoto: true, BarraLateral: false, CssTimeline),
        _ => new("hv-clasica", MostrarFoto: true, BarraLateral: false, CssClasica)
    };

    /// <summary>Estilos compartidos por las cuatro plantillas.</summary>
    public const string CssBase = """
    *, *::before, *::after { box-sizing: border-box; }
    html { -webkit-text-size-adjust: 100%; }
    body {
      margin: 0;
      padding: 24px 16px;
      background: #F3F4F6;
      font-family: "Segoe UI", system-ui, -apple-system, "Helvetica Neue", Arial, sans-serif;
      font-size: 14px;
      line-height: 1.45;
      color: #111827;
      -webkit-print-color-adjust: exact;
      print-color-adjust: exact;
    }
    h1, h2, h3 { margin: 0; font-weight: 600; }
    p { margin: 0; }
    ul { margin: 0; padding: 0; list-style: none; }
    img { max-width: 100%; }
    .hv { max-width: 860px; margin: 0 auto; background: #FFFFFF; box-shadow: 0 1px 4px rgba(17, 24, 39, .14); }
    .hv-cuerpo { padding: 28px 40px 20px; }
    .hv-seccion + .hv-seccion { margin-top: 24px; }
    .hv-seccion-titulo { font-size: 11px; font-weight: 700; letter-spacing: .09em; text-transform: uppercase; }
    .hv-lista { margin-top: 10px; }
    .hv-item { break-inside: avoid; page-break-inside: avoid; }
    .hv-item + .hv-item { margin-top: 14px; }
    .hv-item-fila { display: flex; justify-content: space-between; align-items: baseline; gap: 18px; }
    .hv-item-titulo { font-size: 15px; font-weight: 600; }
    .hv-item-sub { font-size: 13.5px; }
    .hv-meta { font-size: 12.5px; margin-top: 2px; }
    .hv-fechas { text-align: right; font-size: 12px; white-space: nowrap; }
    .hv-perfil { margin-top: 8px; text-align: justify; }
    .hv-logros { margin-top: 6px; }
    .hv-logros li { display: flex; gap: 8px; font-size: 13px; margin-top: 3px; }
    .hv-vineta { flex: 0 0 10px; }
    .hv-logro-nombre { font-weight: 600; }
    .hv-tabla { width: 100%; border-collapse: collapse; font-size: 12.5px; margin-top: 8px; }
    .hv-tabla th { text-align: left; font-weight: 600; padding: 6px; }
    .hv-tabla td { padding: 6px; vertical-align: top; }
    .hv-pie { display: flex; justify-content: space-between; gap: 16px; font-size: 11px; }
    .hv-portada { padding: 34px 40px 30px; }
    .hv-portada p { margin-top: 12px; white-space: pre-wrap; text-align: justify; }
    @page { size: A4; margin: 1.4cm; }
    @media print {
      body { background: #FFFFFF; padding: 0; }
      .hv { max-width: none; box-shadow: none; }
      .hv-portada { break-after: page; page-break-after: always; }
    }
    @media (max-width: 640px) {
      body { padding: 0; }
      .hv-cuerpo { padding: 22px 18px; }
      .hv-item-fila { flex-direction: column; }
      .hv-fechas { text-align: left; }
    }
    """;

    private const string CssClasica = """
    .hv-clasica .hv-encabezado { display: flex; align-items: flex-start; gap: 18px; padding: 32px 40px 0; }
    .hv-clasica .hv-foto { width: 96px; height: 96px; object-fit: cover; border: 2px solid #1D4ED8; }
    .hv-clasica h1 { font-size: 26px; color: #1D4ED8; }
    .hv-clasica .hv-profesion { font-size: 15px; color: #6B7280; margin-top: 2px; }
    .hv-clasica .hv-contacto { margin-top: 8px; font-size: 12px; color: #6B7280; }
    .hv-clasica .hv-contacto li { display: inline; }
    .hv-clasica .hv-contacto li + li::before { content: "   \2022   "; white-space: pre; }
    .hv-clasica .hv-contacto b { font-weight: 600; }
    .hv-clasica .hv-regla { height: 1.5px; background: #1D4ED8; margin: 14px 40px 0; }
    .hv-clasica .hv-seccion-titulo { color: #1D4ED8; border-bottom: 1px solid #E5E7EB; padding-bottom: 4px; }
    .hv-clasica .hv-item-sub, .hv-clasica .hv-fechas, .hv-clasica .hv-meta { color: #6B7280; }
    .hv-clasica .hv-vineta::before { content: "\2022"; color: #1D4ED8; }
    .hv-clasica .hv-tabla th { background: #F3F4F6; border-bottom: 1px solid #E5E7EB; }
    .hv-clasica .hv-tabla td { border-bottom: 1px solid #E5E7EB; }
    .hv-clasica .hv-pie { color: #6B7280; border-top: 1px solid #E5E7EB; margin: 0 40px; padding: 10px 0 24px; }
    """;

    private const string CssEjecutiva = """
    .hv-ejecutiva .hv-cuerpo { display: grid; grid-template-columns: 250px 1fr; padding: 0; }
    .hv-ejecutiva .hv-lateral { background: #1E293B; color: #E2E8F0; padding: 28px 24px; }
    .hv-ejecutiva .hv-principal { padding: 30px 32px; }
    .hv-ejecutiva .hv-foto { width: 110px; height: 110px; object-fit: cover; border: 2px solid #38BDF8; margin-bottom: 16px; }
    .hv-ejecutiva h1 { font-size: 22px; color: #FFFFFF; }
    .hv-ejecutiva .hv-profesion { font-size: 13px; color: #38BDF8; margin-top: 3px; }
    .hv-ejecutiva .hv-lateral-titulo { font-size: 10px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: #38BDF8; margin-top: 22px; }
    .hv-ejecutiva .hv-dato { margin-top: 9px; }
    .hv-ejecutiva .hv-dato span { display: block; font-size: 10.5px; color: #94A3B8; }
    .hv-ejecutiva .hv-dato b { display: block; font-weight: 400; font-size: 12.5px; color: #E2E8F0; overflow-wrap: anywhere; }
    .hv-ejecutiva .hv-comp-lateral li { margin-top: 9px; font-size: 12px; }
    .hv-ejecutiva .hv-comp-lateral small { display: block; font-size: 10.5px; color: #94A3B8; }
    .hv-ejecutiva .hv-seccion-titulo { color: #1D4ED8; border-bottom: 1px solid #E5E7EB; padding-bottom: 4px; }
    .hv-ejecutiva .hv-item-sub, .hv-ejecutiva .hv-fechas, .hv-ejecutiva .hv-meta { color: #6B7280; }
    .hv-ejecutiva .hv-vineta::before { content: "\25AA"; color: #1D4ED8; }
    .hv-ejecutiva .hv-pie { color: #6B7280; border-top: 1px solid #E5E7EB; margin: 0 32px; padding: 10px 0 24px; }
    .hv-ejecutiva .hv-portada { padding: 30px 32px 26px; }
    @media (max-width: 640px) {
      .hv-ejecutiva .hv-cuerpo { grid-template-columns: 1fr; }
      .hv-ejecutiva .hv-principal { padding: 24px 18px; }
      .hv-ejecutiva .hv-pie { margin: 0 18px; }
    }
    """;

    private const string CssMinimal = """
    .hv-minimal { color: #1A1A1A; }
    .hv-minimal .hv-encabezado { padding: 36px 44px 0; }
    .hv-minimal h1 { font-size: 27px; letter-spacing: -.01em; }
    .hv-minimal .hv-profesion { font-size: 14px; color: #707070; margin-top: 3px; }
    .hv-minimal .hv-contacto { margin-top: 10px; font-size: 12px; color: #707070; }
    .hv-minimal .hv-contacto li { display: inline; }
    .hv-minimal .hv-contacto li + li::before { content: "  \00B7  "; white-space: pre; }
    .hv-minimal .hv-contacto b { font-weight: 600; }
    .hv-minimal .hv-regla { height: 1px; background: #D4D4D4; margin: 16px 44px 0; }
    .hv-minimal .hv-cuerpo { padding: 26px 44px 20px; }
    .hv-minimal .hv-seccion-titulo { color: #707070; border-bottom: 1px solid #D4D4D4; padding-bottom: 4px; }
    .hv-minimal .hv-item-sub, .hv-minimal .hv-fechas, .hv-minimal .hv-meta { color: #707070; }
    .hv-minimal .hv-vineta::before { content: "\2014"; color: #707070; }
    .hv-minimal .hv-tabla th { color: #707070; border-bottom: 1px solid #D4D4D4; }
    .hv-minimal .hv-tabla td { border-bottom: 1px solid #D4D4D4; }
    .hv-minimal .hv-pie { color: #707070; border-top: 1px solid #D4D4D4; margin: 0 44px; padding: 10px 0 24px; }
    .hv-minimal .hv-portada { padding: 36px 44px 30px; }
    """;

    private const string CssTimeline = """
    .hv-timeline .hv-encabezado { display: flex; align-items: flex-start; gap: 18px; padding: 32px 40px 0; }
    .hv-timeline .hv-foto { width: 96px; height: 96px; object-fit: cover; border-radius: 50%; border: 2px solid #0F766E; }
    .hv-timeline h1 { font-size: 25px; color: #0F766E; }
    .hv-timeline .hv-profesion { font-size: 14px; color: #14B8A6; margin-top: 2px; }
    .hv-timeline .hv-contacto { margin-top: 8px; font-size: 12px; color: #6B7280; }
    .hv-timeline .hv-contacto li { display: inline; }
    .hv-timeline .hv-contacto li + li::before { content: "   \2022   "; white-space: pre; }
    .hv-timeline .hv-contacto b { font-weight: 600; }
    .hv-timeline .hv-regla { height: 2px; background: #14B8A6; margin: 14px 40px 0; }
    .hv-timeline .hv-seccion-titulo { color: #0F766E; border-bottom: 1px solid #E5E7EB; padding-bottom: 4px; }
    .hv-timeline .hv-lista { position: relative; padding-left: 24px; margin-top: 12px; }
    .hv-timeline .hv-lista::before { content: ""; position: absolute; left: 4px; top: 7px; bottom: 5px; width: 2px; background: #E5E7EB; }
    .hv-timeline .hv-lista .hv-item { position: relative; }
    .hv-timeline .hv-lista .hv-item + .hv-item { margin-top: 16px; }
    .hv-timeline .hv-lista .hv-item::before {
      content: ""; position: absolute; left: -24px; top: 6px;
      width: 10px; height: 10px; border-radius: 50%; background: #14B8A6;
    }
    .hv-timeline .hv-item-sub, .hv-timeline .hv-fechas, .hv-timeline .hv-meta { color: #6B7280; }
    .hv-timeline .hv-vineta::before { content: "\2022"; color: #14B8A6; }
    .hv-timeline .hv-tabla th { background: #F3F4F6; border-bottom: 1px solid #E5E7EB; }
    .hv-timeline .hv-tabla td { border-bottom: 1px solid #E5E7EB; }
    .hv-timeline .hv-pie { color: #6B7280; border-top: 1px solid #E5E7EB; margin: 0 40px; padding: 10px 0 24px; }
    """;
}
