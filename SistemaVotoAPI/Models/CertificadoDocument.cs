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