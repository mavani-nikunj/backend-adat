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

    public class TransactionController : ControllerBase
    {
        private readonly AdatHisabAppContext _context;
        public TransactionController(AdatHisabAppContext context)
        {
            _context = context;
        }
        // ✅ Get JVID
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetJvId()
        {
            int clientId = User.GetClientId();
            int yearId = User.GetYearId();
            var last = await _context.Transactions
                .Where(x => x.ClientId == clientId && x.YearId == yearId && x.JvId != null)
                .OrderByDescending(x => x.JvId)
                .FirstOrDefaultAsync();
            int newJvId = 1;
            if (last != null && last.JvId.HasValue)
            {
                newJvId = last.JvId.Value + 1;
            }
            return Ok(new
            {
                Success = true,
                Message = "New JvId generated successfully",
                JvId = newJvId
            });
        }
        // ✅ GET ALL TRANSACTIONS (WITH PAGINATION)
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll(int? page, int? limit, string? paymenttype, string? partyName, DateOnly? creatDate)
        {
            int clientId = User.GetClientId();
            int yearId = User.GetYearId();

            // base query (apply tenant/year scoping first)
            var baseQuery = _context.Transactions
                .Where(t => t.ClientId == clientId && t.YearId == yearId);

            // filter by payment type (case-insensitive)
            if (!string.IsNullOrWhiteSpace(paymenttype))
            {
                var pt = paymenttype.Trim().ToLower();
                baseQuery = baseQuery.Where(t => t.PaymentType != null && t.PaymentType.ToLower() == pt);
            }

            // filter by party name (case-insensitive, contains)
            if (!string.IsNullOrWhiteSpace(partyName))
            {
                var pn = partyName.Trim().ToLower();
                baseQuery = baseQuery.Where(t =>
       _context.Partymasters
           .Any(p => p.Id == t.PartyId
                  && p.ClientId == clientId
                  && p.YearId == yearId
                  && p.Name.Contains(pn))); // EF Core translates this to SQL LIKE '%Both Bhai%'
            }

            // filter by creation date (DateOnly equality)
            if (creatDate.HasValue)
            {
                baseQuery = baseQuery.Where(t => t.CreatDate.HasValue && t.CreatDate == creatDate.Value);
            }

            // total after filters
            int totalRecords = await baseQuery.CountAsync();

            // projection & ordering
            var query = baseQuery
                .OrderByDescending(t => t.Id)
                .Select(t => new
                {
                    t.Id,
                    t.JvId,

                    Party = new
                    {
                        Id = t.PartyId,
                        Name = _context.Partymasters
                            .Where(p => p.Id == t.PartyId)
                            .Select(p => p.Name)
                            .FirstOrDefault()
                    },

                    Currency = new
                    {
                        Id = t.CurrencyId,
                        Name = _context.Currencymsts
                            .Where(c => c.Id == t.CurrencyId)
                            .Select(c => c.Name)
                            .FirstOrDefault()
                    },

                    Client = new
                    {
                        Id = t.ClientId,
                        Name = _context.Clientmasters
                            .Where(c => c.Id == t.ClientId)
                            .Select(c => c.Name)
                            .FirstOrDefault()
                    },

                    t.YearId,
                    t.CreatDate,
                    t.Amount,
                    t.PaymentType,
                    t.ExRate,
                    t.ConvertAmount,
                    t.Remark,
                    t.Cdate,
                    t.Udate,
                    t.AdatAmount,
                    t.AdatPercent,
                    t.AmountWithoutAdat,
                    t.BalanceType,
                    t.InvoiceId
                })
                .AsQueryable();

            // paging
            if (page.HasValue && limit.HasValue && page > 0 && limit > 0)
            {
                query = query
                    .Skip((page.Value - 1) * limit.Value)
                    .Take(limit.Value);
            }

            var data = await query.ToListAsync();

            if (!data.Any())
            {
                return Ok(new
                {
                    Success = true,
                    Message = "No transactions found",
                    Data = data

                });
            }

            return Ok(new
            {
                Success = true,
                Message = "Transactions fetched successfully",
                TotalRecords = totalRecords,
                Page = page,
                Limit = limit,
                TotalPages = limit > 0
                    ? (int)Math.Ceiling(totalRecords / (double)limit)
                    : 1,
                Data = data
            });
        }
        // ✅ GET PARTY WISE INVOICE DETAILS
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPartyInvoiceList(int? id)
        {
            int clientId = User.GetClientId();
            int yearId = User.GetYearId();
            var invoicedetail = new List<PartywiseInvoicedetaildto>();
            if (!id.HasValue || id == 0)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Party Id is required"
                });
            }
            var partyExists = await _context.Partymasters
                .AnyAsync(x => x.Id == id.Value && x.ClientId == clientId && x.YearId == yearId);
            if (!partyExists)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Party Id not found"
                });
            }


            var invoices = await _context.Invoicedetails
            .Where(x => x.ShiperId == id && x.ClientId == clientId && x.YearId == yearId).ToListAsync();


            foreach (var item in invoices)
            {
                var recamount = await _context.Transactions
                    .Where(t => t.InvoiceId == item.Id && t.PaymentType == "Receive" && t.ClientId == clientId && t.YearId == yearId)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;
                var outstandingAmount = item.FinalAmount - recamount;
                if (outstandingAmount > 0)
                {
                    invoicedetail.Add(new PartywiseInvoicedetaildto
                    {
                        Id = item.Id,
                        Invoicedate = item.Invoicedate?.ToDateTime(TimeOnly.MinValue),
                        DueDate = item.Duedate?.ToDateTime(TimeOnly.MinValue),
                        PeymentAmount = item.FinalAmount,
                        ReceiveAmount = recamount,
                        BalanceAmount = outstandingAmount

                    });
                }
            }
            invoicedetail = invoicedetail.OrderBy(x => x.Id).ToList();
            return Ok(new
            {
                Success = true,
                Message = "Invoice List generated successfully",
                Data = invoicedetail

            });
        }
        // ✅ GET TRANSACTION BY ID
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetById(int id, int? jvId)
        {
            int clientId = User.GetClientId();
            int yearId = User.GetYearId();

            // =========================
            // CASE 1: NORMAL TRANSACTION
            // =========================
            if (!jvId.HasValue || jvId == 0)
            {
                var transaction = await _context.Transactions
                    .Where(t =>
                        t.Id == id &&
                        t.ClientId == clientId &&
                        t.YearId == yearId && t.JvId == 0)
                    .Select(t => new
                    {
                        t.Id,
                        t.JvId,

                        Party = new
                        {
                            Id = t.PartyId,
                            Name = _context.Partymasters
                                .Where(p => p.Id == t.PartyId)
                                .Select(p => p.Name)
                                .FirstOrDefault()
                        },

                        Currency = new
                        {
                            Id = t.CurrencyId,
                            Name = _context.Currencymsts
                                .Where(c => c.Id == t.CurrencyId)
                                .Select(c => c.Name)
                                .FirstOrDefault()
                        },

                        Client = new
                        {
                            Id = t.ClientId,
                            Name = _context.Clientmasters
                                .Where(c => c.Id == t.ClientId)
                                .Select(c => c.Name)
                                .FirstOrDefault()
                        },

                        t.YearId,
                        t.CreatDate,
                        t.Amount,
                        t.PaymentType,
                        t.ExRate,
                        t.ConvertAmount,
                        t.Remark,
                        t.Cdate,
                        t.Udate,
                        t.AdatAmount,
                        t.AdatPercent,
                        t.AmountWithoutAdat,
                        t.BalanceType,
                        t.InvoiceId
                    })
                    .FirstOrDefaultAsync();

                if (transaction == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Transaction not found Use in Jv"
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Transaction fetched successfully",
                    Data = transaction
                });
            }

            // =========================
            // CASE 2: JV TRANSACTIONS
            // =========================
            var jvTransactions = await _context.Transactions
                .Where(t =>
                    t.JvId == jvId &&
                    t.ClientId == clientId &&
                    t.YearId == yearId)
                .Select(t => new
                {
                    t.Id,
                    t.JvId,

                    Party = new
                    {
                        Id = t.PartyId,
                        Name = _context.Partymasters
                            .Where(p => p.Id == t.PartyId)
                            .Select(p => p.Name)
                            .FirstOrDefault()
                    },

                    Currency = new
                    {
                        Id = t.CurrencyId,
                        Name = _context.Currencymsts
                            .Where(c => c.Id == t.CurrencyId)
                            .Select(c => c.Name)
                            .FirstOrDefault()
                    },

                    Client = new
                    {
                        Id = t.ClientId,
                        Name = _context.Clientmasters
                            .Where(c => c.Id == t.ClientId)
                            .Select(c => c.Name)
                            .FirstOrDefault()
                    },

                    t.YearId,
                    t.CreatDate,
                    t.Amount,
                    t.PaymentType,
                    t.ExRate,
                    t.ConvertAmount,
                    t.Remark,
                    t.Cdate,
                    t.Udate,
                    t.AdatAmount,
                    t.AdatPercent,
                    t.AmountWithoutAdat,
                    t.BalanceType,
                    t.InvoiceId
                })
                .ToListAsync();

            if (!jvTransactions.Any())
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "JV transactions not found"
                });
            }

            return Ok(new
            {
                Success = true,
                Message = "JV transactions fetched successfully",
                Data = jvTransactions
            });
        }

        // ✅ CREATE TRANSACTION SINGLE ENTRY
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddSingle([FromBody] TransactionItemDto model)
        {
            if (User.GetYearId() <= 0)
            {
                return StatusCode(500, new { Success = false, Message = "Select Financial Year" });
            }
            if (!model.InvoiceId.HasValue)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "InvoiceId is required"
                });
            }

            if (model.InvoiceId.HasValue && model.InvoiceId > 0)
            {
                var invoiceExists = await _context.Invoicedetails
                    .AnyAsync(x => x.Id == model.InvoiceId.Value);

                if (!invoiceExists)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Invoice Id not found"
                    });
                }
            }
            if (!model.PartyId.HasValue || model.PartyId <= 0 || !await _context.Partymasters.AnyAsync(p => p.Id == model.PartyId))
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "PartyId is required Or invalid"
                });
            }

            if (!model.CurrencyId.HasValue || model.CurrencyId <= 0)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Currency is required"
                });
            }
            var party = await _context.Partymasters
     .Where(p =>
         p.Id == model.PartyId &&
         p.ClientId == User.GetClientId() &&
         p.YearId == User.GetYearId())
     .Select(p => new
     {
         p.CurrencyId,
         CurrencyName = _context.Currencymsts
             .Where(c => c.Id == p.CurrencyId)
             .Select(c => c.Name)
             .FirstOrDefault()
     })
     .FirstOrDefaultAsync();


            if (party == null)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Invalid Party"
                });
            }

            // If currency is NOT BOTH and mismatch
            if (party.CurrencyName != "BOTH" &&
                party.CurrencyId != model.CurrencyId)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "This party already has transactions with a different currency"
                });
            }


            var transaction = new Transaction
            {
                JvId = 0,
                PartyId = model.PartyId,
                CreatDate = model.CreatDate,

                CurrencyId = model.CurrencyId,
                PaymentType = model.PaymentType,
                ExRate = model.ExRate,
                ConvertAmount = model.ConvertAmount,
                //ConvertAmount = model.ExRate.HasValue
                //    ? model.Amount * model.ExRate.Value
                //    : model.Amount,
                Remark = model.Remark,
                AdatPercent = model.AdatPercent,
                AdatAmount = model.AdatAmount,
                AmountWithoutAdat = model.AmountWithoutAdat,
                Amount = model.Amount,
                InvoiceId = model.InvoiceId,
                BalanceType = model.BalanceType,

                ClientId = User.GetClientId(),
                YearId = User.GetYearId(),
                Cdate = DateTime.Now,
                RevPartyId = model.BalanceType
            };
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return Ok(new
            {
                Success = true,
                Message = "Transaction saved successfully",
                Data = transaction
            });
        }


        // ✅ CREATE TRANSACTION
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Add([FromBody] TransactionRequestDto model)
        {
            if (User.GetYearId() <= 0)
            {
                return StatusCode(500, new { Success = false, Message = "Select Financial Year" });
            }

            if (model.Receive == null || model.Payment == null)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Both Receive and Payment details are required"
                });
            }

            // ✅ Currency validation
            var partyCurrencies = await _context.Partymasters
      .Join(_context.Currencymsts,
            p => p.CurrencyId,
            c => c.Id,
            (p, c) => new
            {
                p.Id,
                p.CurrencyId,
                c.Name
            })
      .ToListAsync();


            var receiveParty = partyCurrencies
     .FirstOrDefault(x => x.Id == model.Receive.PartyId);

            var paymentParty = partyCurrencies
                .FirstOrDefault(x => x.Id == model.Payment.PartyId);

            if (receiveParty == null || paymentParty == null)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "One or more parties not found"
                });
            }

            // 🔹 Receive Currency Check
            if (receiveParty.Name != "BOTH" &&
                receiveParty.CurrencyId != model.Receive.CurrencyId)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Receive currency must match Receive party currency"
                });
            }

            // 🔹 Payment Currency Check
            if (paymentParty.Name != "BOTH" &&
                paymentParty.CurrencyId != model.Payment.CurrencyId)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Payment currency must match Payment party currency"
                });
            }



            // ✅ Amount validation
            var receiveAmount = model.Receive.ExRate.HasValue
                ? model.Receive.Amount * model.Receive.ExRate.Value
                : model.Receive.Amount;

            //if (receiveAmount != model.Payment.Amount)
            //{
            //    return StatusCode(500, new
            //    {
            //        Success = false,
            //        Message = "Debit and Credit amount must be same"
            //    });
            //}
            int clientId = User.GetClientId();
            int yearId = User.GetYearId();
            var last = await _context.Transactions
                .Where(x => x.ClientId == clientId && x.YearId == yearId && x.JvId != null)
                .OrderByDescending(x => x.JvId)
                .FirstOrDefaultAsync();
            int newJvId = 1;
            if (last != null && last.JvId.HasValue)
            {
                newJvId = last.JvId.Value + 1;
            }
            // ✅ JV Entry
            //if (model.JvId > 0)
            //{
            var jventry = new Jventry
            {
                Jvid = newJvId,
                Jvdate = model.TransactionDate,
                Remark = model.Remark,
                ClientId = User.GetClientId(),
                YearId = User.GetYearId(),
                Cdate = DateTime.Now
            };

            _context.Jventries.Add(jventry);
            await _context.SaveChangesAsync();
            //}

            // ✅ Create Transactions
            var transactions = new List<Transaction>
    {
        new Transaction
        {
            JvId = newJvId,
            PartyId = model.Receive.PartyId,
            CreatDate = model.Receive.CreatDate,
            Amount = model.Receive.Amount,
            CurrencyId = model.Receive.CurrencyId,
            PaymentType = "Receive",
            ExRate = model.Receive.ExRate,
            ConvertAmount = model.Receive.ConvertAmount,
            Remark = model.Receive.Remark,
            ClientId = User.GetClientId(),
            YearId = User.GetYearId(),
            Cdate = DateTime.Now,
            BalanceType=0,
            InvoiceId=model.Receive.InvoiceId ?? 0,
            RevPartyId=model.Payment.PartyId


        },

        new Transaction
        {
            JvId = newJvId,
            PartyId = model.Payment.PartyId,
            CreatDate = model.Payment.CreatDate,
            Amount = model.Payment.Amount,
            CurrencyId = model.Payment.CurrencyId,
            PaymentType = "Payment",
            ExRate = model.Payment.ExRate,
            ConvertAmount = model.Payment.ConvertAmount,
            Remark = model.Payment.Remark,
            ClientId = User.GetClientId(),
            YearId = User.GetYearId(),
            Cdate = DateTime.Now,
            BalanceType=0,
             InvoiceId=model.Payment.InvoiceId??0,
             RevPartyId=model.Receive.PartyId
        }
    };

            _context.Transactions.AddRange(transactions);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = "Journal voucher saved successfully",
                Data = transactions
            });
        }

        [HttpPut]
        [Authorize]
        public async Task<IActionResult> UpdateSingle(int id,
                    [FromBody] UpdateSingletransactionDto model)

        {
            int clientId = User.GetClientId();
            int yearId = User.GetYearId();

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.ClientId == clientId &&
                    x.YearId == yearId);

            if (transaction == null)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Transaction not found"
                });
            }

            // 🔹 Update only fields that came
            if (model.PartyId.HasValue)
                transaction.PartyId = model.PartyId.Value;

            if (model.InvoiceId.HasValue)
            {
                if (model.InvoiceId.Value > 0)
                {
                    bool invoiceExists = await _context.Invoicedetails
                        .AnyAsync(x => x.Id == model.InvoiceId.Value);

                    if (!invoiceExists)
                    {
                        return StatusCode(500, new
                        {
                            Success = false,
                            Message = "Invoice Id not found"
                        });
                    }
                }

                transaction.InvoiceId = model.InvoiceId.Value;
            }

            if (model.Amount.HasValue)
                transaction.Amount = model.Amount.Value;

            if (model.CreatDate.HasValue)
                transaction.CreatDate = model.CreatDate.Value;
            if (model.AdatPercent.HasValue)
                transaction.AdatPercent = model.AdatPercent.Value;
            if (model.AdatAmount.HasValue)
                transaction.AdatAmount = model.AdatAmount.Value;
            if (model.AmountWithoutAdat.HasValue)
                transaction.AmountWithoutAdat = model.AmountWithoutAdat.Value;

            if (model.CurrencyId.HasValue)
                transaction.CurrencyId = model.CurrencyId.Value;

            if (!string.IsNullOrWhiteSpace(model.PaymentType))
                transaction.PaymentType = model.PaymentType;

            if (model.BalanceType.HasValue)
                transaction.BalanceType = model.BalanceType.Value;
            transaction.RevPartyId = model.BalanceType.Value;
            if (model.ExRate.HasValue)
                transaction.ExRate = model.ExRate.Value;

            if (model.ConvertAmount.HasValue)
                transaction.ConvertAmount = model.ConvertAmount.Value;

            if (!string.IsNullOrWhiteSpace(model.Remark))
                transaction.Remark = model.Remark;


            transaction.Udate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = "Transaction updated successfully"
            });
        }


        [HttpPut]
        [Authorize]
        public async Task<IActionResult> Update([FromBody] TransactionRequestDto model)
        {
            int clientId = User.GetClientId();
            int yearId = User.GetYearId();

            if (yearId <= 0)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Select Financial Year"
                });
            }

            if (model.Receive == null || model.Payment == null)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Both Receive and Payment details are required"
                });
            }
            if (model.Receive.CurrencyId <= 0 || model.Payment.CurrencyId <= 0)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Currency is required"
                });
            }

            // ============================
            // Amount Validation
            // ============================
            var receiveAmount = model.Receive.ExRate.HasValue
                ? model.Receive.Amount * model.Receive.ExRate.Value
                : model.Receive.Amount;
            // ✅ Currency validation
            var partyCurrencies = await _context.Partymasters
     .Where(p =>
         (p.Id == model.Receive.PartyId || p.Id == model.Payment.PartyId) &&
         p.ClientId == clientId &&
         p.YearId == yearId
     )
     .Select(p => new
     {
         p.Id,
         p.CurrencyId
     })
     .ToListAsync();

            // Get BOTH currency Id dynamically
            var bothCurrencyId = await _context.Currencymsts
                .Where(x => x.Name == "BOTH")
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            var receivePartyCurrency = partyCurrencies
                .FirstOrDefault(x => x.Id == model.Receive.PartyId)?.CurrencyId;

            var paymentPartyCurrency = partyCurrencies
                .FirstOrDefault(x => x.Id == model.Payment.PartyId)?.CurrencyId;

            if (receivePartyCurrency == null || paymentPartyCurrency == null)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "One or more parties not found"
                });
            }

            // ✅ RECEIVE VALIDATION
            if (receivePartyCurrency != bothCurrencyId &&
                receivePartyCurrency != model.Receive.CurrencyId)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Receive currency must match Receive party currency"
                });
            }

            // ✅ PAYMENT VALIDATION
            if (paymentPartyCurrency != bothCurrencyId &&
                paymentPartyCurrency != model.Payment.CurrencyId)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Payment currency must match Payment party currency"
                });
            }



            //if (receiveAmount != model.Payment.Amount)
            //{
            //    return StatusCode(500, new
            //    {
            //        Success = false,
            //        Message = "Debit and Credit amount must be same"
            //    });
            //}

            // ============================
            // Update JV Entry
            // ============================
            var jvEntry = await _context.Jventries.FirstOrDefaultAsync(x =>
                x.Jvid == model.JvId &&
                x.ClientId == clientId &&
                x.YearId == yearId);

            if (jvEntry == null)
            {
                return Ok(new
                {
                    Success = true,
                    Message = "JV entry not found"
                });
            }

            jvEntry.Jvdate = model.TransactionDate;
            jvEntry.Remark = model.Remark;
            jvEntry.Udate = DateTime.Now;
            _context.Jventries.Update(jvEntry);
            await _context.SaveChangesAsync();
            // ============================
            // Remove Old Transactions
            // ============================
            var oldTransactions = await _context.Transactions
                .Where(t =>
                    t.JvId == model.JvId &&
                    t.ClientId == clientId &&
                    t.YearId == yearId)
                .ToListAsync();

            if (!oldTransactions.Any())
            {
                return Ok(new
                {
                    Success = false,
                    Message = "JV transactions not found"
                });
            }

            _context.Transactions.RemoveRange(oldTransactions);

            // ============================
            // Insert Updated Transactions
            // ============================
            var transactions = new List<Transaction>
    {
        new Transaction
        {
            JvId = model.JvId,
            PartyId = model.Receive.PartyId,
            CreatDate = model.Receive.CreatDate,
            Amount = model.Receive.Amount,
            CurrencyId = model.Receive.CurrencyId,
            PaymentType = "Receive",
            ExRate = model.Receive.ExRate,
            ConvertAmount = model.Receive.ConvertAmount,
            Remark = model.Receive.Remark,
            InvoiceId=0,
            ClientId = clientId,
            YearId = yearId,
            Udate = DateTime.Now,
            RevPartyId=model.Payment.PartyId
        },

        new Transaction
        {
            JvId = model.JvId,
            PartyId = model.Payment.PartyId,
            CreatDate = model.Payment.CreatDate,
            Amount = model.Payment.Amount,
            CurrencyId = model.Payment.CurrencyId,
            PaymentType = "Payment",
            ExRate = model.Payment.ExRate,
            ConvertAmount = model.Payment.ConvertAmount,
            Remark = model.Payment.Remark,
            ClientId = clientId,
            YearId = yearId,
            Udate = DateTime.Now,
            InvoiceId=0,
            RevPartyId=model.Receive.PartyId
        }
    };

            _context.Transactions.AddRange(transactions);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = "Journal voucher updated successfully"
            });
        }
        [HttpDelete("{id?}")]
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            int clientId = User.GetClientId();
            int yearId = User.GetYearId();

            if (yearId <= 0)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Select Financial Year"
                });
            }
            if (!id.HasValue || id <= 0)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Id is required"
                });
            }

            // ============================
            // Find Transaction by ID
            // ============================
            var transaction = await _context.Transactions.FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.ClientId == clientId &&
                t.YearId == yearId);

            if (transaction == null)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Transaction not found"
                });
            }

            // ============================
            // CASE 1: NORMAL TRANSACTION (JVId == 0)
            // ============================
            if (!transaction.JvId.HasValue || transaction.JvId == 0)
            {
                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Transaction deleted successfully"
                });
            }

            // ============================
            // CASE 2: JV TRANSACTION (JVId > 0)
            // ============================
            int jvId = transaction.JvId.Value;

            var jvEntry = await _context.Jventries.FirstOrDefaultAsync(x =>
                x.Jvid == jvId &&
                x.ClientId == clientId &&
                x.YearId == yearId);

            if (jvEntry == null)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "JV entry not found"
                });
            }

            var jvTransactions = await _context.Transactions
                .Where(t =>
                    t.JvId == jvId &&
                    t.ClientId == clientId &&
                    t.YearId == yearId)
                .ToListAsync();

            if (!jvTransactions.Any())
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "JV transactions not found"
                });
            }

            // ============================
            // Atomic Delete
            // ============================
            using var dbTran = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Transactions.RemoveRange(jvTransactions);
                _context.Jventries.Remove(jvEntry);

                await _context.SaveChangesAsync();
                await dbTran.CommitAsync();
            }
            catch
            {
                await dbTran.RollbackAsync();
                throw;
            }

            return Ok(new
            {
                Success = true,
                Message = "Journal voucher deleted successfully"
            });
        }





    }
}
