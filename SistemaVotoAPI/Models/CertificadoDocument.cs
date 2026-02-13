using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SistemaVotoAPI.Models
{
    public class CertificadoDocument : IDocument
    {
        public CertificadoVotacionRequest CertificadoVotacion { get; set; }
        public CertificadoDocument(CertificadoVotacionRequest certificado)
        {
            CertificadoVotacion = certificado;
        }

        public void Compose(IDocumentContainer container)
        {
            // --- PÁGINA 1: ANVERSO ---
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(25);
                page.PageColor(Colors.White);

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    // Encabezado
                    col.Item().Row(row =>
                    {
                        row.ConstantItem(200).AlignLeft().Row(r => {
                            r.AutoItem().Height(65).Image(CertificadoVotacion.RutaEscudo);
                            r.AutoItem().PaddingLeft(15).Height(65).Image(CertificadoVotacion.RutaCne);
                        });

                        row.RelativeItem().Column(c => {
                            c.Item().AlignCenter().Text("CERTIFICADO DE VOTACIÓN").FontSize(30).Bold().FontColor(Colors.Blue.Darken3);
                            c.Item().AlignCenter().Background(Colors.Blue.Medium).PaddingHorizontal(20).PaddingVertical(5)
                                    .Text(CertificadoVotacion.FechaElectoral).FontSize(14).Bold().FontColor(Colors.White);
                        });

                        // Sección QR
                        row.ConstantItem(120).AlignRight().Column(qrCol =>
                        {
                            qrCol.Item().Width(80).Height(80).Border(0.5f).BorderColor(Colors.Grey.Lighten2).AlignCenter().AlignMiddle().Image(CertificadoVotacion.RutaQr);
                        });
                    });

                    col.Item().LineHorizontal(2f).LineColor(Colors.Blue.Medium);

                    // CUERPO
                    col.Item().Row(row =>
                    {
                        // 1. Barras grises laterales
                        row.ConstantItem(40).Background(Colors.Grey.Lighten3).AlignCenter().Column(c => {
                            for (int i = 0; i < 18; i++) c.Item().PaddingVertical(5).Text("||||||").FontSize(12).FontColor(Colors.Grey.Medium);
                        });

                        // 2. DATOS PERSONALES (Con colores mixtos)
                        row.RelativeItem().PaddingLeft(30).PaddingTop(35).Column(c =>
                        {
                            c.Item().PaddingTop(20).AlignCenter().Text(CertificadoVotacion.Nombre).FontSize(24).Bold();
                            c.Spacing(5);

                            // PROVINCIA
                            c.Item().PaddingBottom(5).Text(t => {
                                t.Span("PROVINCIA: ").FontSize(16).Bold();
                                t.Span(CertificadoVotacion.Provincia).FontSize(16).FontColor(Colors.Blue.Darken3).Bold();
                            });

                            // CIRCUNSCRIPCIÓN
                            c.Item().PaddingBottom(5).Text(t => {
                                t.Span("CIRCUNSCRIPCIÓN: ").FontSize(16).Bold();
                                t.Span("").FontSize(16).FontColor(Colors.Blue.Darken3).Bold();
                            });

                            // CANTÓN
                            c.Item().PaddingBottom(5).Text(t => {
                                t.Span("CANTÓN: ").FontSize(16).Bold();
                                t.Span(CertificadoVotacion.Canton).FontSize(16).FontColor(Colors.Blue.Darken3).Bold();
                            });

                            // PARROQUIA
                            c.Item().PaddingBottom(5).Text(t => {
                                t.Span("PARROQUIA: ").FontSize(16).Bold();
                                t.Span(CertificadoVotacion.Parroquia).FontSize(16).FontColor(Colors.Blue.Darken3).Bold();
                            });




                        });

                        // 3. FOTO Y CÉDULA
                        row.ConstantItem(220).PaddingTop(60).Column(c =>
                        {
                            c.Item().AlignRight().PaddingBottom(5).Text($"N° {CertificadoVotacion.CertificadoNo}").FontSize(11).Bold().Italic();
                            c.Item().Width(120).Height(150).Border(0.5f).Padding(2).Image(CertificadoVotacion.FotoBytes);
                            c.Item().AlignCenter().PaddingTop(5).Text($"CC N°: {CertificadoVotacion.Cedula}").FontSize(14).Bold();
                        });
                    });
                });
            });
            // --- PÁGINA 2: REVERSO ---
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(50);

                page.Content().Column(col =>
                {
                    // Encabezado Reverso
                    col.Item().AlignCenter().Row(row =>
                    {
                        row.AutoItem().Height(80).Image(CertificadoVotacion.RutaCne);
                        row.AutoItem().PaddingLeft(15).BorderLeft(1.5f).BorderColor(Colors.Grey.Lighten1).PaddingLeft(15).Column(c =>
                        {
                            c.Item().Text("ELECCIONES").FontSize(22).FontColor(Colors.Grey.Darken2);
                            c.Item().PaddingTop(-5).Text("GENERALES").FontSize(22).FontColor(Colors.Grey.Darken2);
                            c.Item().PaddingTop(2).Text("2026").FontSize(22).Bold().FontColor(Colors.Black);
                        });
                    });

                    col.Item().PaddingTop(10).AlignCenter().Text("CIUDADANA/O:").FontSize(18).Bold();

                    // Mensaje Central
                    col.Item().Background(Colors.Blue.Medium).PaddingVertical(15).Column(c =>
                    {
                        c.Item().AlignCenter().Text("ESTE DOCUMENTO ACREDITA QUE USTED SUFRAGÓ").FontSize(16).Bold().FontColor(Colors.White);
                        c.Item().AlignCenter().Text("EN LAS ELECCIONES GENERALES 2026").FontSize(16).Bold().FontColor(Colors.White);
                    });

                    // Texto Legal
                    col.Item().PaddingTop(30).PaddingHorizontal(50).AlignCenter().Text(t => {
                        t.Span("La ciudadana/o que altere cualquier documento electoral será sancionado de acuerdo a lo que establece el ").FontSize(10);
                        t.Span("artículo 275 y el artículo 279, numeral 3 de la LOEOP - Código de la Democracia.").FontSize(10).Bold();
                    });

                    col.Item().Row(r => r.RelativeItem());

                    // Firma
                    col.Item().AlignCenter().Width(300).Column(f =>
                    {
                        f.Item().AlignCenter().Height(70).Image(CertificadoVotacion.RutaFirma);
                        f.Item().PaddingTop(-5).LineHorizontal(1.5f).LineColor(Colors.Black);
                        f.Item().AlignCenter().Text("F. PRESIDENTA/E DE LA JRV").Bold().FontSize(12);
                    });

                    col.Item().AlignRight().PaddingTop(5).Text("IMP.IGM.2026.HV").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });

        }
    }
}
