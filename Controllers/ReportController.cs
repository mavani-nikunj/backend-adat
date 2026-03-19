using AdatHisabdubai.Data;
using AdatHisabdubai.Dto;
using AdatHisabdubai.Extensions;
using AdatHisabdubai.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace AdatHisabdubai.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly AdatHisabAppContext _context;
        public ReportController(AdatHisabAppContext context)
        {
            _context = context;
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> PartywiseBalance(int partyId)
        {
            int clientId = User.GetClientId();
            int yearId = User.GetYearId();
            var partyBalances = await _context.Partymasters
                .Where(p => p.ClientId == clientId)
                .Select(p => new
                {
                    PartyId = p.Id,
                    PartyName = p.Name,
                    Balance = _context.Invoicedetails
                        .Where(i => i.ShiperId == p.Id && i.ClientId == clientId && i.YearId == yearId)
                        .Sum(i => (i.FinalAmount ?? 0) * (i.OpeningType == "Payment" ? -1 : 1)) +
                              _context.Transactions
                        .Where(t => t.PartyId == p.Id && t.ClientId == clientId && t.YearId == yearId)
                        .Sum(t => t.PaymentType == "Payment" ? -t.Amount : t.Amount)
                })
                .ToListAsync();
            return Ok(new
            {
                Success = true,
                Message = "Party balances retrieved successfully",
                Data = partyBalances
            });
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> PartyLager(int partyId, DateOnly? fromdate, DateOnly? todate)
        {
            int clientId = User.GetClientId();
            int yearId = User.GetYearId();

            decimal runningBalance = 0;
            decimal currencyrunningBalance = 0;
            var ledger = new List<Ladjerdto>();

            // ======================
            // INVOICES
            // ======================
            var invoices = await _context.Invoicedetails
     .Where(x =>
         (x.ShiperId == partyId || x.ConsignId == partyId) &&
         x.ClientId == clientId &&
         x.YearId == yearId &&

         (
             // 🔹 Always include Opening
             x.IsOpening == true ||

             // 🔹 Date filter for normal invoices
             (
                 x.IsOpening != true &&
                 (fromdate == null || x.Invoicedate >= fromdate) &&
                 (todate == null || x.Invoicedate <= todate)
             )
         )
     )
     .ToListAsync();

            var adatList = await _context.Adatdetails
    .Where(t => t.AdatPartyId == partyId &&
                t.ClientId == clientId &&
                t.YearId == yearId && (fromdate == null || t.Invoicedate >= fromdate) &&
        (todate == null || t.Invoicedate <= todate))
    .ToListAsync();

            foreach (var inv in invoices)
            {
                // 🔹 OPENING BALANCE (detected by OpeningType)
                if (inv.Carat == null && inv.Rate == null && inv.Amount == null && inv.IsOpening == true && inv.Invoicedate >= fromdate && inv.Invoicedate <= todate)
                {
                    ledger.Add(new Ladjerdto
                    {
                        Id = inv.Id,
                        Date = inv.Invoicedate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue,
                        Remark = inv.Remark,
                        PaymentType = "Opening",
                        Debit = inv.OpeningType == "Payment" ? inv.FinalAmount : 0,
                        Credit = inv.OpeningType == "Receive" ? inv.FinalAmount : 0,
                        Currency = await _context.Currencymsts
                            .Where(x => x.Id == inv.CurrencyId)
                            .Select(x => x.Name)
                            .FirstOrDefaultAsync(),
                        Carat = 0,
                        Invamount = 0,
                        ExtraInvAmount = 0

                    });
                }

                // 🔹 EXTRA INVOICE (only amount shown, no balance impact)
                else if (inv.FinalAmount > 0 && inv.Rate == null && inv.Invoicedate >= fromdate && inv.Invoicedate <= todate)
                {
                    ledger.Add(new Ladjerdto
                    {
                        Id = inv.Id,
                        Date = inv.Invoicedate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue,
                        Remark = inv.Remark,
                        PaymentType = "Extra",          // 👈 NOT SHOWN
                        Debit = 0,
                        Credit = 0,
                        DisplayAmount = inv.FinalAmount,
                        Currency = await _context.Currencymsts
                            .Where(x => x.Id == inv.CurrencyId)
                            .Select(x => x.Name)
                            .FirstOrDefaultAsync(),
                        Carat = inv.Carat,
                        Invamount = 0,
                        ExtraInvAmount = inv.FinalAmount

                    });

                    //var isextra = await _context.Adatdetails.Where(t => t.PartyId == partyId && t.ClientId == clientId && t.YearId == yearId).ToListAsync();
                    //foreach (var extra in isextra)
                    //{
                    //    ledger.Add(new Ladjerdto
                    //    {
                    //        Id = extra.Id,
                    //        Date = extra.Cdate.Value,
                    //        Remark = extra.Remark,
                    //        PaymentType = "Adat",          // 👈 NOT SHOWN
                    //        Debit = extra.Amount,
                    //        Credit = 0,
                    //        DisplayAmount = 0,
                    //        Currency = await _context.Currencymsts
                    //            .Where(x => x.Id == inv.CurrencyId)
                    //            .Select(x => x.Name)
                    //            .FirstOrDefaultAsync(),
                    //        Carat = 0,
                    //        Invamount = 0,
                    //        ExtraInvAmount = 0
                    //    });
                    //}

                }

                // 🔹 NORMAL INVOICE
                else
                {
                    bool isShipper = inv.ShiperId == partyId;
                    bool isConsignee = inv.ConsignId == partyId;

                    // 🔹 Select correct expense
                    decimal expenseAmount = isShipper
                        ? (inv.ShiparExpenseAmount ?? 0)
                        : (inv.ConsignExpenseAmount ?? 0);

                    ledger.Add(new Ladjerdto
                    {
                        Id = inv.Id,
                        Date = inv.Invoicedate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue,

                        // 🔹 Show expense in remark
                        Remark = $"{inv.Remark} (Invoice Amt {inv.FinalAmount}, Expense {expenseAmount})",

                        PaymentType = "Invoice",

                        // 🔹 Debit/Credit based on role
                        Debit = 0,
                        Credit = isShipper ? inv.ShiparExpenseAmount : inv.ConsignExpenseAmount,

                        // 🔹 Show selected expense amount
                        DisplayAmount = expenseAmount,

                        Currency = await _context.Currencymsts
                            .Where(x => x.Id == inv.CurrencyId)
                            .Select(x => x.Name)
                            .FirstOrDefaultAsync(),

                        Carat = inv.Carat,
                        Invamount = inv.FinalAmount,
                        ExtraInvAmount = expenseAmount// 👈 put here if you want separate column

                    });
                }
            }

            foreach (var extra in adatList)
            {
                ledger.Add(new Ladjerdto
                {
                    Id = extra.Id,
                    Date = extra.Cdate.Value,
                    Remark = extra.Remark,
                    PaymentType = "Adat",
                    Debit = extra.Amount,
                    Credit = 0,
                    DisplayAmount = 0,
                    Currency = await _context.Currencymsts
                        .Where(x => x.Id == 1)
                        .Select(x => x.Name)
                        .FirstOrDefaultAsync(),
                    Carat = 0,
                    Invamount = 0,
                    ExtraInvAmount = 0

                });
            }



            // ======================
            // TRANSACTIONS (ALWAYS LOAD)
            // ======================
            var transactions = await _context.Transactions
                .Where(x =>
                    (x.PartyId == partyId || x.BalanceType == partyId) &&
                    x.ClientId == clientId &&
                    x.YearId == yearId
                    && (fromdate == null || x.CreatDate >= fromdate) &&
                    (todate == null || x.CreatDate <= todate))
                .Select(x => new
                {
                    x.Id,
                    x.CreatDate,
                    x.Amount,
                    x.PaymentType,
                    x.CurrencyId,
                    x.Remark,
                    x.BalanceType,
                    x.AmountWithoutAdat,
                    x.PartyId,
                    x.JvId,
                    x.RevPartyId,
                    x.ConvertCurrencyId,
                    x.ExRate,
                    x.ConvertAmount


                })
                .ToListAsync();

            foreach (var tr in transactions)
            {
                bool isMainParty = tr.PartyId == partyId;
                bool isBalanceParty = tr.BalanceType == partyId;

                decimal amountToUse =
                    isBalanceParty
                        ? (tr.AmountWithoutAdat ?? 0)
                        : (tr.Amount ?? 0);

                decimal debit = 0;
                decimal credit = 0;

                // ======================
                // MAIN PARTY → NORMAL ENTRY
                // ======================
                if (isMainParty)
                {
                    debit = tr.PaymentType == "Payment" ? amountToUse : 0;
                    credit = tr.PaymentType == "Receive" ? amountToUse : 0;
                }

                // ======================
                // BALANCE PARTY → REVERSE ENTRY
                // ======================
                else if (isBalanceParty)
                {
                    debit = tr.PaymentType == "Receive" ? amountToUse : 0;
                    credit = tr.PaymentType == "Payment" ? amountToUse : 0;
                }

                var currencyName = await _context.Currencymsts
                    .Where(x => x.Id == tr.CurrencyId)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync();

                ledger.Add(new Ladjerdto
                {
                    Id = tr.Id,
                    Date = tr.CreatDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue,
                    Remark = tr.Remark,
                    PaymentType = tr.PaymentType,
                    Debit = debit,
                    Credit = credit,
                    Currency = currencyName,
                    WithParty = await _context.Partymasters.Where(x => x.Id == (isMainParty ? tr.RevPartyId : tr.PartyId)).Select(x => x.Name).FirstOrDefaultAsync(),
                    ConverRemark = $"{_context.Currencymsts.Where(z => z.Id == tr.ConvertCurrencyId).Select(z => z.Name).FirstOrDefault()}-{tr.ExRate}"


                });
            }

            // ======================
            // SORT & RUNNING BALANCE
            // ======================
            ledger = ledger
                .OrderBy(x => x.Date)

                .ToList();
            // Dictionary to store running balance per currency
            var currencyBalances = new Dictionary<string, decimal>();

            foreach (var row in ledger)
            {
                string currency = row.Currency ?? "UNKNOWN";
                if (!currencyBalances.ContainsKey(currency))
                    currencyBalances[currency] = 0;

                currencyBalances[currency] += (row.Credit ?? 0) - (row.Debit ?? 0);

                row.CurrencyWiseBalance = currencyBalances[currency];

                //runningBalance += (row.Credit ?? 0) - (row.Debit ?? 0);
                //row.Balance = runningBalance;
                // 🔹 Use same balance for both
                row.CurrencyWiseBalance = currencyBalances[currency];
                row.Balance = currencyBalances[currency];   // ✅ FIXED HERE
            }
            var currencyBalanceList = currencyBalances
                .Select(x => new
                {
                    Currency = x.Key,
                    Balance = x.Value
                })
                .ToList();

            var partyName = await _context.Partymasters
                .Where(x => x.Id == partyId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();

            //var currencyList = await _context.Transactions.Where(x => x.PartyId == partyId && x.ClientId == clientId && x.YearId == yearId).Select(x => x.CurrencyId).Distinct().ToListAsync();
            //foreach (var currency in currencyList)
            //{
            //    var currencyName = await _context.Currencymsts.Where(x => x.Id == currency).Select(x => x.Name).FirstOrDefaultAsync();
            //    foreach (var row in ledger.Where(x => x.Currency == currencyName)) { row.Currency = currencyName; }

            //}

            return Ok(new
            {
                Success = true,
                Message = "Ledger Data generated successfully",
                PartyName = partyName,
                Data = ledger,
                CurrencyBalances = currencyBalanceList

            });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> PartyLedgerPdf(int partyId)
        {
            int clientId = User.GetClientId();
            int yearId = User.GetYearId();

            decimal runningBalance = 0;
            var ledger = new List<Ladjerdto>();

            // ======================
            // INVOICES
            // ======================
            var invoices = await _context.Invoicedetails
    .Where(x =>
        x.ShiperId == partyId &&
        x.ClientId == clientId &&
        x.YearId == yearId)

    .ToListAsync();


            var adatList = await _context.Adatdetails
    .Where(t => t.PartyId == partyId &&
                t.ClientId == clientId &&
                t.YearId == yearId)
    .ToListAsync();

            foreach (var inv in invoices)
            {
                // 🔹 OPENING BALANCE (detected by OpeningType)
                if (inv.Carat == null && inv.Rate == null && inv.Amount == null)
                {
                    ledger.Add(new Ladjerdto
                    {
                        Id = inv.Id,
                        Date = inv.Invoicedate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue,
                        Remark = inv.Remark,
                        PaymentType = "Opening",
                        Debit = inv.OpeningType == "Payment" ? inv.FinalAmount : 0,
                        Credit = inv.OpeningType == "Receive" ? inv.FinalAmount : 0,
                        Currency = await _context.Currencymsts
                            .Where(x => x.Id == 1)
                            .Select(x => x.Name)
                            .FirstOrDefaultAsync()
                    });
                }

                // 🔹 EXTRA INVOICE (only amount shown, no balance impact)
                else if (inv.FinalAmount > 0 && inv.Rate == null)
                {
                    ledger.Add(new Ladjerdto
                    {
                        Id = inv.Id,
                        Date = inv.Invoicedate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue,
                        Remark = inv.Remark,
                        PaymentType = "Extra",          // 👈 NOT SHOWN
                        Debit = 0,
                        Credit = 0,
                        DisplayAmount = inv.FinalAmount,
                        Currency = await _context.Currencymsts
                            .Where(x => x.Id == 1)
                            .Select(x => x.Name)
                            .FirstOrDefaultAsync()
                    });

                    //var isextra = await _context.Adatdetails.Where(t => t.PartyId == partyId && t.ClientId == clientId && t.YearId == yearId).ToListAsync();
                    //foreach (var extra in isextra)
                    //{
                    //    ledger.Add(new Ladjerdto
                    //    {
                    //        Id = extra.Id,
                    //        Date = extra.Cdate.Value,
                    //        Remark = extra.Remark,
                    //        PaymentType = "Adat",          // 👈 NOT SHOWN
                    //        Debit = extra.Amount,
                    //        Credit = 0,
                    //        DisplayAmount = 0,
                    //        Currency = await _context.Currencymsts
                    //            .Where(x => x.Id == 1)
                    //            .Select(x => x.Name)
                    //            .FirstOrDefaultAsync()
                    //    });
                    //}

                }

                // 🔹 NORMAL INVOICE
                else
                {
                    ledger.Add(new Ladjerdto
                    {
                        Id = inv.Id,
                        Date = inv.Invoicedate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue,
                        Remark = inv.Remark,
                        PaymentType = "Invoice",
                        Debit = inv.FinalAmount,
                        Credit = 0,
                        Currency = await _context.Currencymsts
                            .Where(x => x.Id == 1)
                            .Select(x => x.Name)
                            .FirstOrDefaultAsync()
                    });
                }
            }

            foreach (var extra in adatList)
            {
                ledger.Add(new Ladjerdto
                {
                    Id = extra.Id,
                    Date = extra.Cdate.Value,
                    Remark = extra.Remark,
                    PaymentType = "Adat",
                    Debit = extra.Amount,
                    Credit = 0,
                    DisplayAmount = 0,
                    Currency = await _context.Currencymsts
                        .Where(x => x.Id == 1)
                        .Select(x => x.Name)
                        .FirstOrDefaultAsync(),
                    Carat = 0,
                    Invamount = 0,
                    ExtraInvAmount = 0
                });
            }



            // ======================
            // TRANSACTIONS (ALWAYS LOAD)
            // ======================
            var transactions = await _context.Transactions
                .Where(x =>
                    x.PartyId == partyId &&
                    x.ClientId == clientId &&
                    x.YearId == yearId)
                .Select(x => new
                {
                    x.Id,
                    x.CreatDate,
                    x.Amount,
                    x.PaymentType,
                    x.CurrencyId,
                    x.Remark
                })
                .ToListAsync();

            foreach (var tr in transactions)
            {
                ledger.Add(new Ladjerdto
                {
                    Id = tr.Id,
                    Date = tr.CreatDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue,
                    Remark = tr.Remark,
                    PaymentType = tr.PaymentType,
                    Debit = tr.PaymentType == "Payment" ? tr.Amount : 0,
                    Credit = tr.PaymentType == "Receive" ? tr.Amount : 0,
                    Currency = await _context.Currencymsts
                        .Where(x => x.Id == tr.CurrencyId)
                        .Select(x => x.Name)
                        .FirstOrDefaultAsync()
                });
            }

            // ======================
            // SORT & RUNNING BALANCE
            // ======================
            ledger = ledger
                .OrderBy(x => x.Date)

                .ToList();

            foreach (var row in ledger)
            {
                runningBalance += (row.Credit ?? 0) - (row.Debit ?? 0);
                row.Balance = runningBalance;
            }

            var partyName = await _context.Partymasters
                .Where(x => x.Id == partyId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();

            // ======================
            // GENERATE PDF
            // ======================
            var pdf = new PartyLedgerPdfDocument(ledger);
            var bytes = pdf.GeneratePdf();

            return File(bytes, "application/pdf", "PartyLedger.pdf");
        }



    }
}
