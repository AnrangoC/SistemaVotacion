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
            // --- PÁGINA 1: ANVERSO (AJUSTADO) ---
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(25);
                page.PageColor(Colors.White);

                page.Content().Column(col =>
                {
                    col.Spacing(5);

                    // 1. ENCABEZADO
                    col.Item().Row(row =>
                    {
                        row.ConstantItem(250).AlignLeft().Row(r => {
                            r.AutoItem().Height(80).Image(CertificadoVotacion.RutaEscudo).FitHeight();
                            r.AutoItem().PaddingLeft(10).Height(80).Image(CertificadoVotacion.RutaCne).FitHeight();
                        });

                        row.RelativeItem().PaddingTop(10).Column(c => {
                            c.Item().AlignCenter().Text("CERTIFICADO DE VOTACIÓN").FontSize(30).ExtraBold().FontColor(Colors.Blue.Darken3);
                            c.Item().AlignCenter().Background(Colors.Blue.Medium).PaddingHorizontal(25).PaddingVertical(3)
                                    .Text(CertificadoVotacion.FechaElectoral).FontSize(15).Bold().FontColor(Colors.White);
                        });

                        row.ConstantItem(140).AlignRight().Height(100).Image(CertificadoVotacion.RutaQr).FitHeight();
                    });

                    col.Item().PaddingVertical(5).LineHorizontal(2f).LineColor(Colors.Blue.Medium);

                    // 2. CUERPO AJUSTADO
                    col.Item().Row(row =>
                    {
                        // CÓDIGO DE BARRAS MÁS LARGO (Hasta el nivel del CC)
                        row.ConstantItem(50).PaddingVertical(2).Column(c =>
                        {
                            // Aumentamos a 45 repeticiones para que baje hasta el nivel de la cédula
                            for (int i = 0; i < 45; i++)
                            {
                                c.Item().Height(3).Background(Colors.Black);
                                c.Item().Height(2);
                                c.Item().Height(1.5f).Background(Colors.Black);
                                c.Item().Height(2);
                            }
                        });

                        row.RelativeItem().PaddingLeft(25).Column(mainCol =>
                        {
                            // Nombre del Votante
                            mainCol.Item().PaddingBottom(5).Text(CertificadoVotacion.Nombre).FontSize(34).ExtraBold();

                            mainCol.Item().Row(contentRow =>
                            {
                                // DATOS IZQUIERDA (Alineados con la foto)
                                contentRow.RelativeItem().PaddingRight(10).PaddingTop(35).Column(info =>
                                {
                                    // Espaciado mayor entre bloques para separar Provincia, Cantón y Parroquia
                                    info.Spacing(22);

                                    var fields = new[] {
                            ("PROVINCIA", CertificadoVotacion.Provincia),
                            ("CIRCUNSCRIPCIÓN", "ÚNICA"),
                            ("CANTÓN", CertificadoVotacion.Canton),
                            ("PARROQUIA", CertificadoVotacion.Parroquia)
                            // Se eliminaron ZONA y JUNTA
                        };

                                    foreach (var (label, value) in fields)
                                    {
                                        info.Item().Text(t => {
                                            t.Span($"{label}: ").FontSize(19).Bold().FontColor(Colors.Blue.Medium);
                                            t.Span(value).FontSize(19).Bold().FontColor(Colors.Black);
                                        });
                                    }
                                });

                                // FOTO DERECHA Y NÚMEROS
                                contentRow.ConstantItem(220).AlignRight().Column(fCol =>
                                {
                                    // Número de certificado (Referencia para el inicio de los campos)
                                    fCol.Item().PaddingBottom(5).AlignRight().Text(t => {
                                        t.Span("N° ").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                                        t.Span(CertificadoVotacion.CertificadoNo).FontSize(18).Bold();
                                    });

                                    // Foto
                                    fCol.Item().Width(190).Height(250).Border(2f).BorderColor(Colors.Black).Image(CertificadoVotacion.FotoBytes).FitArea();

                                    // CC debajo de la foto
                                    fCol.Item().AlignCenter().Width(190).PaddingTop(8).Text(t => {
                                        t.Span("CC N°: ").FontSize(17).Bold().FontColor(Colors.Blue.Medium);
                                        t.Span(CertificadoVotacion.Cedula).FontSize(18).ExtraBold();
                                    });
                                });
                            });
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
