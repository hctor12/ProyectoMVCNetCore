using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ProyectoMVCNetCore.Models;
using System.Collections.Generic;

namespace ProyectoMVCNetCore.Helpers
{
    public static class HelperQuestPDF
    {
        public static byte[] GenerateTicketSummary(Incidencia incidencia, List<Comentario> comentarios, Usuario cliente, Usuario? tecnico, string nombreEstado)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Inch);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Verdana));

                    // Header
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Row(logoRow =>
                            {
                                logoRow.AutoItem()
                                    .Width(36).Height(36)
                                    .Svg(@"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""-150 -1110 1260 1260""><rect x=""-150"" y=""-1110"" width=""1260"" height=""1260"" fill=""#ffffff"" rx=""300"" stroke=""#E2E8F0"" stroke-width=""20"" /><path fill=""#0F172A"" d=""M480-280q17 0 28.5-11.5T520-320q0-17-11.5-28.5T480-360q-17 0-28.5 11.5T440-320q0 17 11.5 28.5T480-280Zm0-160q17 0 28.5-11.5T520-480q0-17-11.5-28.5T480-520q-17 0-28.5 11.5T440-480q0 17 11.5 28.5T480-440Zm0-160q17 0 28.5-11.5T520-640q0-17-11.5-28.5T480-680q-17 0-28.5 11.5T440-640q0 17 11.5 28.5T480-600Zm320 440H160q-33 0-56.5-23.5T80-240v-160q33 0 56.5-23.5T160-480q0-33-23.5-56.5T80-560v-160q0-33 23.5-56.5T160-800h640q33 0 56.5 23.5T880-720v160q-33 0-56.5 23.5T800-480q0 33 23.5 56.5T880-400v160q0 33-23.5 56.5T800-160Zm0-80v-102q-37-22-58.5-58.5T720-480q0-43 21.5-79.5T800-618v-102H160v102q37 22 58.5 58.5T240-480q0 43-21.5 79.5T160-342v102h640ZM480-480Z""/></svg>");

                                logoRow.AutoItem().PaddingLeft(10).AlignMiddle()
                                    .Text("Soporte").FontSize(28).ExtraBold().FontColor("#0F172A");
                            });

                            col.Item().PaddingTop(15).Text($"ID Ticket: #{incidencia.IdIncidencia}").FontSize(10).SemiBold().FontColor(Colors.Grey.Medium);
                        });

                        row.RelativeItem().AlignRight().AlignMiddle().Column(col =>
                        {
                            col.Item().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(10).FontColor(Colors.Grey.Medium);
                        });
                    });

                    // Content
                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        // Info Grid
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten3).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(innerCol =>
                            {
                                innerCol.Item().Text("DETALLES DEL TICKET").FontSize(10).Bold().FontColor(Colors.Grey.Darken2);
                                innerCol.Item().PaddingTop(5).Text(incidencia.Titulo).FontSize(14).Bold();
                                innerCol.Item().PaddingTop(5).Text(incidencia.Descripcion).FontSize(11).LineHeight(1.5f);
                            });
                        });

                        col.Item().PaddingTop(20).Row(row =>
                        {
                            row.RelativeItem().Column(innerCol =>
                            {
                                innerCol.Item().Text("CLIENTE").FontSize(9).Bold().FontColor(Colors.Grey.Medium);
                                innerCol.Item().Text(cliente.Nombre).FontSize(11).Bold();
                                innerCol.Item().Text(cliente.Email).FontSize(10);
                            });

                            row.RelativeItem().Column(innerCol =>
                            {
                                innerCol.Item().Text("TÉCNICO ASIGNADO").FontSize(9).Bold().FontColor(Colors.Grey.Medium);
                                innerCol.Item().Text(tecnico?.Nombre ?? "Sin asignar").FontSize(11).Bold();
                                innerCol.Item().Text(tecnico?.Email ?? "N/A").FontSize(10);
                            });

                            row.RelativeItem().Column(innerCol =>
                            {
                                innerCol.Item().Text("ESTADO FINAL").FontSize(9).Bold().FontColor(Colors.Grey.Medium);
                                innerCol.Item().Text(nombreEstado).FontSize(11).Bold().FontColor("#27AE60");
                                innerCol.Item().Text($"Reportado: {incidencia.FechaReporte:dd/MM/yyyy}").FontSize(9);
                            });
                        });

                        // Comments Section
                        col.Item().PaddingTop(30).Text("HISTORIAL DE LA CONVERSACIÓN").FontSize(10).Bold().FontColor(Colors.Grey.Darken2);
                        col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5);

                        if (comentarios.Count == 0)
                        {
                            col.Item().PaddingTop(10).Text("No hubo comentarios adicionales en esta incidencia.").Italic().FontColor(Colors.Grey.Medium);
                        }
                        else
                        {
                            foreach (var com in comentarios)
                            {
                                col.Item().PaddingVertical(8).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten4).Row(row =>
                                {
                                    row.RelativeItem().Column(innerCol =>
                                    {
                                        innerCol.Item().Row(r =>
                                        {
                                            r.RelativeItem().Text(rowText =>
                                            {
                                                rowText.Span(com.IdUsuario == cliente.IdUsuario ? "Cliente" : "Técnico").Bold().FontSize(9).FontColor(com.IdUsuario == cliente.IdUsuario ? "#0984E3" : "#E67E22");
                                                rowText.Span(" • ").FontSize(9).FontColor(Colors.Grey.Medium);
                                                rowText.Span(com.Fecha.ToString("dd/MM/yyyy HH:mm")).FontSize(9).FontColor(Colors.Grey.Medium);
                                            });
                                        });
                                        innerCol.Item().PaddingTop(2).Text(com.Contenido).FontSize(10);
                                    });
                                });
                            }
                        }
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ").FontSize(9);
                        x.CurrentPageNumber().FontSize(9);
                        x.Span(" de ").FontSize(9);
                        x.TotalPages().FontSize(9);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
