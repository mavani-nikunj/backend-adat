using AdatHisabdubai.Dto;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AdatHisabdubai.Service
{
    public class PartyLedgerPdfDocument : IDocument
    {
        private readonly List<Ladjerdto> _ledger;

        public PartyLedgerPdfDocument(List<Ladjerdto> ledger)
        {
            _ledger = ledger;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {

            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Content().Column(col =>
                {
                    col.Item().AlignCenter().Text("PARTY LEDGER")
                        .Bold()
                        .FontSize(14);

                    col.Item().Height(10);

                    col.Item().PaddingVertical(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);   // Date
                            columns.RelativeColumn(3);    // Remark
                            columns.ConstantColumn(70);   // Debit
                            columns.ConstantColumn(70);   // Credit
                            columns.ConstantColumn(90);   // Amount
                            columns.ConstantColumn(80);   // Balance
                        });

                        // HEADER
                        table.Header(h =>
                        {
                            h.Cell().PaddingBottom(10).Text("Date").Bold();
                            h.Cell().Text("Remark").Bold();
                            h.Cell().AlignRight().Text("DisplayAmount").Bold();
                            h.Cell().AlignRight().Text("Credit").Bold();
                            h.Cell().AlignRight().Text("Debit").Bold();

                            h.Cell().AlignRight().Text("Balance").Bold();
                        });

                        // ROWS
                        foreach (var row in _ledger)
                        {
                            bool isExtra = row.DisplayAmount.HasValue && row.DisplayAmount > 0;

                            table.Cell().PaddingBottom(10).Text(row.Date.ToString("dd-MM-yyyy"));
                            table.Cell().Text(row.Remark);
                            table.Cell().AlignRight().Text(
                                isExtra ? row.DisplayAmount.Value.ToString("N2") : ""
                            );
                            table.Cell().AlignRight().Text(
                                isExtra ? "" : row.Credit > 0 ? row.Credit.Value.ToString("N2") : ""
                            );

                            table.Cell().AlignRight().Text(
                                isExtra ? "" : row.Debit > 0 ? row.Debit.Value.ToString("N2") : ""
                            );



                            table.Cell().AlignRight().Text(
    isExtra
        ? ""
        : row.Balance.HasValue
            ? row.Balance.Value.ToString("N2")
            : ""
);

                        }
                    });
                });
            });
        }
    }
}
