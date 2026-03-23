using AdatHisabdubai.Data;
using AdatHisabdubai.Dto;
using AdatHisabdubai.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdatHisabdubai.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly AdatHisabAppContext _context;
        public HomeController(AdatHisabAppContext context)
        {
            _context = context;
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetExchangRate()
        {
            var exchangeRates = await _context.Exchangerates


                .ToListAsync();
            return Ok(new
            {
                Success = true,
                Message = "Exchange Rates fetched successfully",
                Data = exchangeRates
            });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetBalance1()
        {
            int clientId = User.GetClientId();
            int yearId = User.GetYearId();
            var balanceDetails = new List<Dto.Balancedetaildto>();
            decimal netbalance = 0;
            var balance = await _context.Openingbalances
                .Where(x => x.ClientId == clientId && x.YearId == yearId).Select(x => x.CurrencyId)
                .Distinct()
                .ToListAsync();
            foreach (var currencyId in balance)
            {
                var openamount = await _context.Openingbalances
                    .Where(x => x.ClientId == clientId && x.YearId == yearId && x.CurrencyId == currencyId)
                    .SumAsync(x => x.Amount) ?? 0;
                var totalCredit = await _context.Transactions
                    .Where(x => x.ClientId == clientId && x.YearId == yearId && x.CurrencyId == currencyId && x.PaymentType == "Receive")
                    .SumAsync(x => x.Amount) ?? 0;
                var totalDedit = await _context.Transactions
                   .Where(x => x.ClientId == clientId && x.YearId == yearId && x.CurrencyId == currencyId && x.PaymentType == "Payment")
                   .SumAsync(x => x.Amount) ?? 0;
                netbalance = openamount + totalCredit - totalDedit;
                balanceDetails.Add(new Dto.Balancedetaildto
                {
                    CurrencyId = currencyId,
                    CurrencyName = (await _context.Currencymsts.FindAsync(currencyId))?.Name,
                    NetBalance = netbalance,

                });
                // You can store or return the netBalance for each currency as needed
            }
            return Ok(new
            {
                Success = true,
                Message = "Balance List fetched successfully",
                Data = balanceDetails
            });

        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetBalance()
        {
            int clientId = User.GetClientId();
            int yearId = User.GetYearId();
            var balanceDetails = new List<Homebalancedto>();
            decimal netbalance = 0;
            var balanceparty = await _context.Partymasters.Where(z => z.PartyType == "BALANCE").ToListAsync();
            foreach (var item in balanceparty)
            {
                var totalrec = await _context.Transactions.Where(z => z.PartyId == item.Id && z.ClientId == clientId && z.YearId == yearId && z.PaymentType == "Receive").SumAsync(z => z.Amount) ?? 0;

                var totalpay = await _context.Transactions.Where(z => z.PartyId == item.Id && z.ClientId == clientId && z.YearId == yearId && z.PaymentType == "Payment").SumAsync(z => z.Amount) ?? 0;

                var receive = await _context.Transactions.Where(z => z.BalanceType == item.Id && z.ClientId == clientId && z.YearId == yearId && z.PaymentType == "Receive").SumAsync(z => z.AmountWithoutAdat);
                var payment = await _context.Transactions.Where(z => z.BalanceType == item.Id && z.ClientId == clientId && z.YearId == yearId && z.PaymentType == "Payment").SumAsync(z => z.AmountWithoutAdat);
                //var shiperreceiveinvoiceexpense = await _context.Invoicedetails.Where(z => z.ShiperId == item.Id && z.ClientId == clientId && z.YearId == yearId).SumAsync(z => z.ShiparExpenseAmount) ?? 0;
                //var consignreceiveinvoiceexpense = await _context.Invoicedetails.Where(z => z.ConsignId == item.Id && z.ClientId == clientId && z.YearId == yearId).SumAsync(z => z.ConsignExpenseAmount) ?? 0;
                //netbalance = (decimal)(totalpay - payment - totalrec - receive);
                netbalance = (decimal)(totalrec + receive - totalpay - payment);
                balanceDetails.Add(new Homebalancedto
                {
                    CurrencyName = (await _context.Currencymsts.FindAsync(item.CurrencyId))?.Name,
                    PartyName = item.Name,
                    NetBalance = netbalance
                });

            }
            return Ok(new
            {
                Success = true,
                Message = "Balance List fetched successfully",
                Data = balanceDetails
            });
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetBalanceboth()
        {
            int clientId = User.GetClientId();
            int yearId = User.GetYearId();

            var result = new List<object>();

            var balanceparty = await _context.Partymasters
                .Where(z => z.PartyType == "BALANCE")
                .ToListAsync();

            foreach (var item in balanceparty)
            {
                // ======================
                // GET PARTY CURRENCY NAME
                // ======================
                var partyCurrencyName = await _context.Currencymsts
                    .Where(c => c.Id == item.CurrencyId)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync();

                // ======================
                // GET TRANSACTIONS (may be empty)
                // ======================
                var transactions = await _context.Transactions
                    .Where(z =>
                        (z.PartyId == item.Id || z.BalanceType == item.Id) &&
                        z.ClientId == clientId &&
                        z.YearId == yearId)
                    .ToListAsync();

                // ======================
                // IF NO TRANSACTIONS → ZERO BALANCE
                // ======================
                if (!transactions.Any())
                {
                    result.Add(new
                    {
                        CurrencyName = partyCurrencyName,
                        NetBalance = 0m,
                        PartyName = item.Name
                    });

                    continue;
                }

                // ======================
                // GROUP BY CURRENCY
                // ======================
                var currencyGroups = transactions.GroupBy(t => t.CurrencyId);

                var details = new List<object>();
                decimal totalBalance = 0;

                foreach (var g in currencyGroups)
                {
                    decimal totalReceiveParty = g
                        .Where(x => x.PartyId == item.Id && x.PaymentType == "Receive")
                        .Sum(x => x.Amount ?? 0);

                    decimal totalPaymentParty = g
                        .Where(x => x.PartyId == item.Id && x.PaymentType == "Payment")
                        .Sum(x => x.Amount ?? 0);

                    decimal totalReceiveBalance = g
                        .Where(x => x.BalanceType == item.Id && x.PaymentType == "Receive")
                        .Sum(x => x.Amount ?? 0);

                    decimal totalPaymentBalance = g
                        .Where(x => x.BalanceType == item.Id && x.PaymentType == "Payment")
                        .Sum(x => x.Amount ?? 0);

                    decimal netBalance =
                        totalReceiveParty +
                        totalReceiveBalance -
                        totalPaymentParty -
                        totalPaymentBalance;

                    totalBalance += netBalance;

                    var currencyName = await _context.Currencymsts
                        .Where(c => c.Id == g.Key)
                        .Select(c => c.Name)
                        .FirstOrDefaultAsync();

                    details.Add(new
                    {
                        CurrencyName = currencyName,
                        NetBalance = netBalance,
                        PartyName = item.Name
                    });
                }

                // ======================
                // BOTH CURRENCY PARTY
                // ======================
                if (partyCurrencyName == "BOTH")
                {
                    result.Add(new
                    {
                        CurrencyName = "BOTH",

                        Detail = details
                    });
                }
                else
                {
                    // SINGLE CURRENCY PARTY → show per currency result
                    foreach (var d in details)
                        result.Add(d);
                }
            }

            return Ok(new
            {
                Success = true,
                Message = "Balance List fetched successfully",
                Data = result
            });
        }
        public async Task<IActionResult> GetBalancebothdiffrece()
        {
            int clientId = User.GetClientId();
            int yearId = User.GetYearId();

            var result = new List<object>();

            var balanceparty = await _context.Partymasters
                .Where(z => z.PartyType == "BALANCE")
                .ToListAsync();

            foreach (var item in balanceparty)
            {
                // ======================
                // PARTY CURRENCY NAME
                // ======================
                var partyCurrencyName = await _context.Currencymsts
                    .Where(c => c.Id == item.CurrencyId)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync();

                // ======================
                // GET TRANSACTIONS (may be empty)
                // ======================
                var transactions = await _context.Transactions
                    .Where(z =>
                        (z.PartyId == item.Id || z.BalanceType == item.Id) &&
                        z.ClientId == clientId &&
                        z.YearId == yearId)
                    .ToListAsync();

                // ======================
                // NO TRANSACTIONS → ZERO BALANCE
                // ======================
                if (!transactions.Any())
                {
                    result.Add(new
                    {
                        CurrencyName = partyCurrencyName,
                        NetBalance = 0m,
                        PartyName = item.Name
                    });

                    continue;
                }

                // ======================
                // GROUP BY CURRENCY
                // ======================
                var currencyGroups = transactions.GroupBy(t => t.CurrencyId);

                var details = new List<object>();
                decimal totalBalance = 0;

                foreach (var g in currencyGroups)
                {
                    decimal totalReceiveParty = g
                        .Where(x => x.PartyId == item.Id && x.PaymentType == "Receive")
                        .Sum(x => x.Amount ?? 0);

                    decimal totalPaymentParty = g
                        .Where(x => x.PartyId == item.Id && x.PaymentType == "Payment")
                        .Sum(x => x.Amount ?? 0);

                    decimal totalReceiveBalance = g
                        .Where(x => x.BalanceType == item.Id && x.PaymentType == "Receive")
                        .Sum(x => x.AmountWithoutAdat ?? 0);

                    decimal totalPaymentBalance = g
                        .Where(x => x.BalanceType == item.Id && x.PaymentType == "Payment")
                        .Sum(x => x.AmountWithoutAdat ?? 0);

                    //decimal netBalance =
                    //    totalReceiveParty +
                    //    totalReceiveBalance -
                    //    totalPaymentParty -
                    //    totalPaymentBalance;
                    decimal netBalance =
                       totalReceiveParty +
                       totalPaymentBalance -
                       totalPaymentParty -
                       totalReceiveBalance;

                    totalBalance += netBalance;

                    var currencyName = await _context.Currencymsts
                        .Where(c => c.Id == g.Key)
                        .Select(c => c.Name)
                        .FirstOrDefaultAsync();

                    details.Add(new
                    {
                        CurrencyName = currencyName,
                        NetBalance = netBalance,
                        PartyName = item.Name
                    });
                }

                // ======================
                // CURRENCY = BOTH
                // ======================
                if (partyCurrencyName == "BOTH")
                {
                    result.Add(new
                    {
                        CurrencyName = "BOTH",
                        NetBalance = totalBalance,
                        PartyName = item.Name,
                        Detail = details
                    });
                }
                else
                {
                    // SINGLE CURRENCY → show per-currency rows
                    result.AddRange(details);
                }
            }

            return Ok(new
            {
                Success = true,
                Message = "Balance List fetched successfully",
                Data = result
            });
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetBalanceById(int? partyId)
        {
            if (partyId == null)
                return BadRequest("PartyId required");

            int clientId = User.GetClientId();
            int yearId = User.GetYearId();

            var item = await _context.Partymasters
                .FirstOrDefaultAsync(z => z.Id == partyId);

            if (item == null)
                return NotFound("Party not found");

            // ======================
            // GET ALL RELEVANT TRANSACTIONS
            // ======================
            var transactions = await _context.Transactions
                .Where(z =>
                    (z.PartyId == item.Id || z.BalanceType == item.Id) &&
                    z.ClientId == clientId &&
                    z.YearId == yearId)
                .ToListAsync();

            // ======================
            // GROUP BY CURRENCY
            // ======================
            var currencyBalances = transactions
                .GroupBy(t => t.CurrencyId)
                .Select(g =>
                {
                    decimal totalReceiveParty = g
                        .Where(x => x.PartyId == item.Id && x.PaymentType == "Receive")
                        .Sum(x => x.Amount ?? 0);

                    decimal totalPaymentParty = g
                        .Where(x => x.PartyId == item.Id && x.PaymentType == "Payment")
                        .Sum(x => x.Amount ?? 0);

                    decimal totalReceiveBalance = g
                        .Where(x => x.BalanceType == item.Id && x.PaymentType == "Receive")
                        .Sum(x => x.AmountWithoutAdat ?? 0);

                    decimal totalPaymentBalance = g
                        .Where(x => x.BalanceType == item.Id && x.PaymentType == "Payment")
                        .Sum(x => x.AmountWithoutAdat ?? 0);

                    //decimal netBalance =
                    //    totalReceiveParty +
                    //    totalReceiveBalance -
                    //    totalPaymentParty -
                    //    totalPaymentBalance;
                    decimal netBalance = totalReceiveParty + totalPaymentBalance - totalPaymentParty -
                       totalReceiveBalance;


                    return new
                    {
                        CurrencyId = g.Key,
                        NetBalance = netBalance
                    };
                })
                .ToList();

            // ======================
            // GET CURRENCY NAMES
            // ======================
            var result = new List<Homebalancedto>();

            foreach (var bal in currencyBalances)
            {
                var currencyName = await _context.Currencymsts
                    .Where(c => c.Id == bal.CurrencyId)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync();

                result.Add(new Homebalancedto
                {
                    PartyName = item.Name,
                    CurrencyName = currencyName,
                    NetBalance = bal.NetBalance
                });
            }

            return Ok(new
            {
                Success = true,
                Message = "Balance List fetched successfully",
                Data = result
            });
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> OutstandingInvoice(DateOnly? fromdate, DateOnly? todata)
        {
            var outstandingInvoices = new List<Outstandinginvoicedto>();
            var clientId = User.GetClientId();
            var yearId = User.GetYearId();
            var invoices = await _context.Invoicedetails
     .Where(x => x.ClientId == clientId && x.YearId == yearId &&
         (!fromdate.HasValue || x.Duedate >= fromdate.Value) &&
         (!todata.HasValue || x.Duedate <= todata.Value))
     .ToListAsync();
            Console.WriteLine($"From--------------: {fromdate}, To: {todata}");
            if (invoices.Count == 0)
            {
                return Ok(new
                {
                    Success = true,
                    Message = "No Invoices found",
                    Data = outstandingInvoices
                });
            }
            foreach (var invoice in invoices)
            {
                var receivedAmount = await _context.Transactions
                    .Where(x => x.ClientId == clientId && x.YearId == yearId && x.InvoiceId == invoice.Id && x.PaymentType == "Receive")
                    .SumAsync(x => x.Amount) ?? 0;
                var outstandingAmount =
      (invoice.IsExtra == true ? (invoice.Amount ?? 0) : (invoice.FinalAmount ?? 0))
      - receivedAmount;
                if (outstandingAmount > 0)
                {
                    var today = DateOnly.FromDateTime(DateTime.Now);

                    var daysOutstanding = invoice.Duedate.HasValue
                        ? (today.DayNumber - invoice.Duedate.Value.DayNumber)
                        : 0;

                    outstandingInvoices.Add(new Outstandinginvoicedto
                    {
                        InvoiceId = invoice.Id,
                        PartyName = (await _context.Partymasters.FindAsync(invoice.ShiperId))?.Name,
                        InvoiceDate = invoice.Invoicedate,
                        DueDate = invoice.Duedate,
                        InvoiceAmount = invoice.IsExtra == true
                                        ? (invoice.Amount ?? 0)
                                            : (invoice.FinalAmount ?? 0),
                        ReceiveAmount = receivedAmount,
                        OutstandingAmount = outstandingAmount,
                        Days = daysOutstanding,
                        SubParty = (await _context.Partymasters.FindAsync(invoice.ShiperCompanyId))?.Name
                    });
                }
            }
            var adatList = await _context.Adatdetails
    .Where(x => x.ClientId == clientId && x.YearId == yearId &&
        (!fromdate.HasValue || x.Invoicedate >= fromdate.Value) &&
        (!todata.HasValue || x.Invoicedate <= todata.Value))
    .ToListAsync();

            foreach (var adat in adatList)
            {
                var receivedAmount = await _context.Transactions
                    .Where(x => x.ClientId == clientId &&
                                x.YearId == yearId &&
                                x.InvoiceId == adat.InvoiceId && x.PartyId == adat.AdatPartyId && // 🔥 IMPORTANT (InvoiceId → AdatId)
                                x.PaymentType == "Payment")
                    .SumAsync(x => x.Amount) ?? 0;

                var adatAmount = adat.Amount ?? 0;

                var outstandingAmount = adatAmount - receivedAmount;

                if (outstandingAmount > 0)
                {
                    var today = DateOnly.FromDateTime(DateTime.Now);

                    var daysOutstanding = adat.Invoicedate.HasValue
                        ? (today.DayNumber - adat.Invoicedate.Value.DayNumber)
                        : 0;

                    outstandingInvoices.Add(new Outstandinginvoicedto
                    {
                        InvoiceId = adat.Id, // you can rename later if needed
                        PartyName = (await _context.Partymasters.FindAsync(adat.AdatPartyId))?.Name,
                        InvoiceDate = adat.Invoicedate,
                        DueDate = adat.Invoicedate,
                        InvoiceAmount = adatAmount,
                        ReceiveAmount = receivedAmount,
                        OutstandingAmount = outstandingAmount,
                        Days = daysOutstanding,
                        SubParty = null // if not applicable

                    });
                }
            }
            return Ok(new
            {
                Success = true,
                Message = "Outstanding Invoice fetched successfully",
                Data = outstandingInvoices

            });
        }
    }
}
