using AdatHisabdubai.Data;
using AdatHisabdubai.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdatHisabdubai.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]

    public class ExtrainvoiceController : ControllerBase
    {
        private readonly AdatHisabAppContext _context;
        public ExtrainvoiceController(AdatHisabAppContext context)
        {
            _context = context;
        }

        // GET ALL (with optional pagination)
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll(int? page, int? limit, string? partyName)
        {
            try
            {
                int clientId = User.GetClientId();
                int yearId = User.GetYearId();

                var query = _context.Invoicedetails
                    .Where(x => x.ClientId == clientId && x.YearId == yearId && x.IsExtra == true && x.IsOpening == false)
                    .Join(_context.Partymasters,
           invoice => invoice.ShiperId,
           party => party.Id,
           (invoice, party) => new
           {
               invoice.Id,
               invoice.InvoiceNo,
               invoice.Invoicedate,
               invoice.Carat,
               invoice.Amount,
               invoice.FinalAmount,
               invoice.ShiperId,
               invoice.Remark,
               invoice.Terms,
               invoice.AdatPercent,
               invoice.AdatAmount,
               invoice.Duedate,
               invoice.UserId,

               PartyName = new { party.Name, ShiperId = invoice.ShiperId },
               AdatpartyDetail = new
               {
                   Name = _context.Partymasters.Where(z => z.Id == invoice.AdatpartyId).Select(z => z.Name).FirstOrDefault(),
                   Id = invoice.AdatpartyId
               }

           })
                    .OrderByDescending(x => x.Id)
                    .AsQueryable();
                // ✅ OPTIONAL PARTY ID FILTER
                if (!string.IsNullOrWhiteSpace(partyName))
                {
                    query = query.Where(x => x.PartyName.Name.Contains(partyName));
                }
                int totalRecords = await query.CountAsync();

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
                        Message = "No extra invoices found"
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Extra invoice list fetched successfully",
                    TotalRecords = totalRecords,
                    Page = page,
                    Limit = limit,
                    TotalPages = (page.HasValue && limit.HasValue && limit > 0)
                        ? (int)Math.Ceiling(totalRecords / (double)limit)
                        : 1,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        // GET BY ID
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                int clientId = User.GetClientId();
                int yearId = User.GetYearId();

                var extra = await _context.Invoicedetails
                    .Where(x => x.Id == id && x.ClientId == clientId && x.YearId == yearId && x.IsExtra == true)
                    .Select(z => new
                    {
                        z.Id,

                        z.Carat,
                        z.Amount,
                        z.AdatPercent,
                        z.AdatAmount,
                        z.FinalAmount,
                        z.Invoicedate,
                        z.Cdate,
                        z.Udate,
                        z.UserId,
                        z.Duedate,

                        z.Remark,
                        Currency = new
                        {
                            z.CurrencyId,

                            CurrencyName = _context.Currencymsts.Where(c => c.Id == z.CurrencyId).Select(c => c.Name).FirstOrDefault()
                        },
                        PartyName = new { Name = _context.Partymasters.Where(t => t.Id == z.ShiperId).Select(z => z.Name).FirstOrDefault(), ShiperId = z.ShiperId },
                        AdatpartyDetail = new
                        {
                            Name = _context.Partymasters.Where(t => t.Id == z.AdatpartyId).Select(z => z.Name).FirstOrDefault(),
                            Id = z.AdatpartyId
                        }
                    })
                    .FirstOrDefaultAsync();

                if (extra == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Extra invoice not found"
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Extra invoice fetched successfully",
                    Data = extra
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        // ADD
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Add([FromBody] Invoicedetail model)
        {
            await using var dbTran = await _context.Database.BeginTransactionAsync();

            try
            {
                if (model == null)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Invalid data"
                    });
                }
                model.IsExtra = true; // Mark as extra invoice
                model.InvoiceNo = "EXTRA-" + Random.Shared.Next(1, 1000); // Generate unique invoice number
                model.Terms = 0; // No payment terms for extra invoice
                model.Duedate = model.Invoicedate; // Due date same as invoice date for extra invoice
                model.IsOpening = false; // Not an opening balance
                model.ClientId = User.GetClientId();
                model.YearId = User.GetYearId();
                model.Cdate = DateTime.UtcNow;
                model.Udate = null;
                model.UserId = User.GetUserId();

                // 1️⃣ Save main table first
                _context.Invoicedetails.Add(model);
                await _context.SaveChangesAsync();   // ID generated here

                // 2️⃣ Now use generated ID manually
                var adatdetail = new Adatdetail
                {
                    PartyId = model.ShiperId,
                    Amount = model.AdatAmount,
                    InvoiceId = model.Id,   // manual linking (NO FK)
                    Remark = model.Remark,
                    AdatPartyId = model.AdatpartyId,
                    Cdate = DateTime.UtcNow,
                    ClientId = model.ClientId,
                    YearId = model.YearId,
                    UserId = model.UserId,
                    Invoicedate = model.Invoicedate,
                };

                _context.Adatdetails.Add(adatdetail);
                await _context.SaveChangesAsync();

                // 3️⃣ Commit transaction
                await dbTran.CommitAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Extra invoice added successfully",
                    Data = model
                });
            }
            catch (Exception)
            {
                await dbTran.RollbackAsync();

                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Failed to save extra invoice"
                });
            }
        }


        // UPDATE
        [HttpPut]
        [Authorize(Roles = "DevAdmin,Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] Invoicedetail model)
        {
            try
            {
                if (model == null)
                    return StatusCode(500, new { Success = false, Message = "Invalid data" });

                int clientId = User.GetClientId();
                int yearId = User.GetYearId();

                var existing = await _context.Invoicedetails
                    .FirstOrDefaultAsync(x => x.Id == id && x.ClientId == clientId && x.YearId == yearId);

                if (existing == null)
                    return StatusCode(500, new { Success = false, Message = "Extra invoice not found" });

                var extraInvoiceExitsinTransaction = await _context.Transactions.Where(x => x.InvoiceId == id && x.PartyId == existing.ShiperId && x.ClientId == clientId && x.YearId == yearId).FirstOrDefaultAsync();
                if (extraInvoiceExitsinTransaction != null)
                {
                    return StatusCode(500, new { Success = false, Message = "Cannot update extra invoice linked to transactions" });
                }

                // update permitted fields
                existing.ShiperId = model.ShiperId ?? existing.ShiperId;
                existing.Carat = model.Carat ?? existing.Carat;
                existing.Amount = model.Amount ?? existing.Amount;
                existing.AdatPercent = model.AdatPercent ?? existing.AdatPercent;
                existing.AdatAmount = model.AdatAmount ?? existing.AdatAmount;
                existing.FinalAmount = model.FinalAmount ?? existing.FinalAmount;
                existing.Invoicedate = model.Invoicedate ?? existing.Invoicedate;
                existing.Udate = DateTime.UtcNow;
                existing.UserId = User.Identity?.Name;
                existing.Remark = model.Remark ?? existing.Remark;
                existing.CurrencyId = model.CurrencyId ?? existing.CurrencyId;
                existing.AdatpartyId = model.AdatpartyId ?? existing.AdatpartyId;

                _context.Invoicedetails.Update(existing);
                await _context.SaveChangesAsync();
                // ====================================================
                // 🔥 UPDATE OR INSERT ADATDETAIL (LINKED BY InvoiceId)
                // ====================================================

                if (existing.AdatAmount != null && existing.AdatAmount > 0)
                {
                    var adatdetail = await _context.Adatdetails
                        .FirstOrDefaultAsync(x =>
                            x.InvoiceId == existing.Id &&
                            x.ClientId == clientId &&
                            x.YearId == yearId);

                    // ======================
                    // UPDATE EXISTING
                    // ======================
                    if (adatdetail != null)
                    {
                        adatdetail.PartyId = existing.ShiperId;
                        adatdetail.Amount = existing.AdatAmount;
                        adatdetail.Remark = existing.Remark;
                        adatdetail.AdatPartyId = existing.AdatpartyId;
                        adatdetail.Cdate = DateTime.UtcNow;
                        adatdetail.UserId = User.Identity?.Name;
                        adatdetail.Invoicedate = existing.Invoicedate;

                        _context.Adatdetails.Update(adatdetail);
                    }
                    else
                    {
                        // ======================
                        // INSERT NEW
                        // ======================
                        var newAdat = new Adatdetail
                        {
                            PartyId = existing.ShiperId,
                            Amount = existing.AdatAmount,
                            InvoiceId = existing.Id,  // manual link
                            Remark = existing.Remark,
                            AdatPartyId = existing.AdatpartyId,
                            Cdate = DateTime.UtcNow,
                            ClientId = clientId,
                            YearId = yearId,
                            UserId = User.Identity?.Name,
                            Invoicedate = existing.Invoicedate
                        };

                        _context.Adatdetails.Add(newAdat);
                    }

                    await _context.SaveChangesAsync();
                }
                return Ok(new
                {
                    Success = true,
                    Message = "Extra invoice updated successfully",
                    Data = existing
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        // DELETE
        [HttpDelete]
        [Authorize(Roles = "DevAdmin,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                int clientId = User.GetClientId();
                int yearId = User.GetYearId();

                var existing = await _context.Invoicedetails
                    .FirstOrDefaultAsync(x => x.Id == id && x.ClientId == clientId && x.YearId == yearId);

                if (existing == null)
                    return Ok(new { Success = false, Message = "Extra invoice not found" });

                _context.Invoicedetails.Remove(existing);
                await _context.SaveChangesAsync();
                var adatdetail = _context.Adatdetails.Where(z => z.InvoiceId == id).FirstOrDefault();
                _context.Adatdetails.Remove(adatdetail);
                _context.SaveChanges();
                return Ok(new { Success = true, Message = "Extra invoice deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }
    }
}