using AdatHisabdubai.Data;
using AdatHisabdubai.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdatHisabdubai.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class InvoiceDetailController : ControllerBase
    {
        private readonly AdatHisabAppContext _context;
        public InvoiceDetailController(AdatHisabAppContext context)
        {
            _context = context;
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetSubCompony(int ShiperId)
        {
            try
            {
                var groupParty = await _context.Partymasters.Where(x => x.Id == ShiperId).FirstOrDefaultAsync();
                int clientId = User.GetClientId();
                int yearId = User.GetYearId();
                var subCompany = await _context.Partymasters
                    .Where(x => x.GroupPartyId == ShiperId &&
                    x.ClientId == clientId && x.YearId == yearId)
                    .Select(x => new
                    {
                        x.Id,
                        x.Name
                    })
                    .ToListAsync();

                if (subCompany != null)
                {
                    return Ok(new
                    {
                        Success = true,
                        Message = "Sub Company fetched successfully",
                        GroupPartyName = groupParty.Name,
                        Data = subCompany
                    });
                }
                else
                {
                    return Ok(new
                    {
                        Success = true,
                        Message = "Sub Company not found"
                    });

                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllMainSubPartyList()
        {
            var clientId = User.GetClientId();
            var yearId = User.GetYearId();

            var parties = await _context.Partymasters
                .Where(x => x.ClientId == clientId && x.YearId == yearId && x.IsGroupParty != true)
                .ToListAsync();

            var result = parties
                .Select(main => new
                {
                    MainPartyId = main.Id,
                    MainPartyName = main.Name,
                    partyType = main.PartyType,

                    SubParties = parties
                        .Where(sub => sub.GroupPartyId == main.Id)
                        .Select(sub => new
                        {
                            sub.Id,
                            sub.Name
                        })
                        .ToList()
                })
                .ToList();

            return Ok(new
            {
                Success = true,
                Message = "Main and Sub Parties fetched successfully",
                Data = result
            });
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllMainSubPartyList1()
        {
            var clientId = User.GetClientId();
            var yearId = User.GetYearId();

            var parties = await _context.Partymasters
                .Where(x => x.ClientId == clientId && x.YearId == yearId)
                .ToListAsync();

            var result = parties
                .Select(main => new
                {
                    MainPartyId = main.Id,
                    MainPartyName = main.Name,

                    SubParties = parties
                        .Where(sub => sub.GroupPartyId == main.Id)
                        .Select(sub => new
                        {
                            sub.Id,
                            sub.Name
                        })
                        .ToList()
                })
                .ToList();

            return Ok(new
            {
                Success = true,
                Message = "Main and Sub Parties fetched successfully",
                Data = result
            });
        }
        // ✅ GET ALL (WITH PAGINATION)
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll(int? page, int? limit, string? partyName)
        {
            try
            {
                int clientId = User.GetClientId();
                int yearId = User.GetYearId();

                var query = _context.Invoicedetails
                    .Where(x => x.ClientId == clientId &&
                                x.YearId == yearId &&
                                x.IsOpening == false &&
                                x.IsExtra == false)

                    .Select(invoice => new
                    {
                        invoice.Id,
                        invoice.InvoiceNo,
                        invoice.Invoicedate,
                        invoice.Carat,
                        invoice.Amount,
                        invoice.FinalAmount,

                        invoice.Remark,
                        invoice.Terms,
                        invoice.AdatPercent,
                        invoice.AdatAmount,
                        invoice.Duedate,

                        // 🔹 Shipper Name
                        ShipperDetail = new
                        {
                            Name = _context.Partymasters
                            .Where(p => p.Id == invoice.ShiperId)
                            .Select(p => p.Name)
                            .FirstOrDefault(),
                            Id = invoice.ShiperId
                        },
                        ShipperComponyDetail = new
                        {
                            Name = _context.Partymasters
                            .Where(p => p.Id == invoice.ShiperCompanyId)
                            .Select(p => p.Name)
                            .FirstOrDefault(),
                            Id = invoice.ShiperCompanyId
                        },

                        // 🔹 Consignee Name
                        ConsigneeDetail = new
                        {
                            Name = _context.Partymasters
                            .Where(p => p.Id == invoice.ConsignId)
                            .Select(p => p.Name)
                            .FirstOrDefault(),
                            Id = invoice.ConsignId
                        },
                        // 🔹 ConsigneeComponyName
                        ConsigneeComponyDetail = new
                        {
                            Name = _context.Partymasters
                            .Where(p => p.Id == invoice.ConsignCompanyId)
                            .Select(p => p.Name)
                            .FirstOrDefault(),
                            Id = invoice.ConsignCompanyId
                        }
                    })

                    .OrderByDescending(x => x.Id)
                    .AsQueryable();

                // ================= SEARCH =================
                if (!string.IsNullOrWhiteSpace(partyName))
                {
                    query = query.Where(x =>
                        (x.ShipperDetail.Name != null && x.ShipperDetail.Name.Contains(partyName)) ||
                        (x.ConsigneeDetail.Name != null && x.ConsigneeDetail.Name.Contains(partyName)));
                }

                // ================= COUNT =================
                int totalRecords = await query.CountAsync();

                // ================= PAGINATION =================
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
                        Success = false,
                        Message = "No invoices found"
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Invoice list fetched successfully",
                    TotalRecords = totalRecords,
                    Page = page,
                    Limit = limit,
                    TotalPages = (page.HasValue && limit.HasValue && limit > 0)
                        ? (int)Math.Ceiling(totalRecords / (double)limit.Value)
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

        // ✅ GET BY ID
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {


                var invoice = await _context.Invoicedetails
                    .Where(x =>
                        x.Id == id &&
                        x.ClientId == User.GetClientId() &&
                        x.YearId == User.GetYearId())
                    .Select(z => new
                    {
                        z.Id,

                        z.Invoicedate,
                        z.Carat,
                        z.Rate,
                        z.AdatPercent,
                        z.Amount,
                        z.Terms,
                        z.Duedate,
                        z.Remark,
                        z.AdatAmount,
                        z.FinalAmount,
                        z.YearId,
                        z.InvoiceNo,
                        Client = new
                        {
                            z.ClientId,
                            ClientName = _context.Clientmasters.Where(c => c.Id == z.ClientId).Select(c => c.Name).FirstOrDefault()
                        },
                        // 🔹 Shipper Name
                        ShipperDetail = new
                        {
                            Name = _context.Partymasters
                            .Where(p => p.Id == z.ShiperId)
                            .Select(p => p.Name)
                            .FirstOrDefault(),
                            Id = z.ShiperId
                        },
                        ShipperComponyDetail = new
                        {
                            Name = _context.Partymasters
                            .Where(p => p.Id == z.ShiperCompanyId)
                            .Select(p => p.Name)
                            .FirstOrDefault(),
                            Id = z.ShiperCompanyId
                        },

                        // 🔹 Consignee Name
                        ConsigneeDetail = new
                        {
                            Name = _context.Partymasters
                            .Where(p => p.Id == z.ConsignId)
                            .Select(p => p.Name)
                            .FirstOrDefault(),
                            Id = z.ConsignId
                        },
                        // 🔹 ConsigneeComponyName
                        ConsigneeComponyDetail = new
                        {
                            Name = _context.Partymasters
                            .Where(p => p.Id == z.ConsignCompanyId)
                            .Select(p => p.Name)
                            .FirstOrDefault(),
                            Id = z.ConsignCompanyId
                        },
                        z.ShiparExpensePercent,
                        z.ShiparExpenseAmount,
                        z.ConsignExpensePercent,
                        z.ConsignExpenseAmount,
                        z.IsExtra,
                        z.IsOpening
                    })
                    .FirstOrDefaultAsync();

                if (invoice == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Invoice not found"
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Invoice fetched successfully",
                    Data = invoice
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

        // ✅ CREATE
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Add([FromBody] Invoicedetail model)
        {
            try
            {


                if (User.GetYearId() <= 0)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Select Financial Year"
                    });
                }

                if (model.ShiperId <= 0)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Party is required"
                    });
                }

                if (model.Invoicedate == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Invoice date is required"
                    });
                }

                // Financial year validation
                var year = await _context.Yearmasters
                    .FirstOrDefaultAsync(y =>
                        y.Id == User.GetYearId() &&
                        y.ClientId == User.GetClientId() &&
                        y.Isdelete == false);

                if (year == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Invalid financial year"
                    });
                }

                if (model.Invoicedate < year.Fromyear || model.Invoicedate > year.Toyear)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Opening balance date is outside the financial year"
                    });
                }
                var ShiperId = await _context.Partymasters.Where(y => y.Id == model.ShiperId).FirstOrDefaultAsync();
                if (ShiperId == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Invalid ShiperId"
                    });
                }
                if (model.Carat <= 0)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Carat must be greater than 0"
                    });
                }

                if (model.Amount <= 0)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Amount must be greater than 0"
                    });
                }


                model.IsOpening = false;
                model.IsExtra = false;
                model.Cdate = DateTime.Now;
                model.Udate = DateTime.Now;
                model.ClientId = User.GetClientId();
                model.YearId = User.GetYearId();
                model.UserId = User.GetUserId();
                model.CurrencyId = 1;
                //model.CurrencyId = _context.Partymasters.Where(z => z.Id == model.ShiperId).Select(z => z.CurrencyId).FirstOrDefault();
                _context.Invoicedetails.Add(model);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Invoice created successfully",
                    Data = model
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

        // ✅ UPDATE
        [HttpPut]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] Invoicedetail dto)
        {
            try
            {


                var invoice = await _context.Invoicedetails
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        x.ClientId == User.GetClientId() &&
                        x.YearId == User.GetYearId());

                if (invoice == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Invoice not found"
                    });
                }

                // Financial year validation
                var year = await _context.Yearmasters
                    .FirstOrDefaultAsync(y =>
                        y.Id == User.GetYearId() &&
                        y.ClientId == User.GetClientId() &&
                        y.Isdelete == false);

                if (year == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Invalid financial year"
                    });
                }

                if (dto.Invoicedate < year.Fromyear || dto.Invoicedate > year.Toyear)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Opening balance date is outside the financial year"
                    });
                }
                var partyExists = await _context.Partymasters
                            .Where(p => p.Id == dto.ShiperId).FirstOrDefaultAsync();

                if (partyExists == null)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Invalid ShiperId"
                    });
                }
                invoice.ShiperId = dto.ShiperId ?? invoice.ShiperId;
                invoice.ConsignId = dto.ConsignId ?? invoice.ConsignId;
                invoice.Invoicedate = dto.Invoicedate ?? invoice.Invoicedate;
                invoice.Carat = dto.Carat ?? invoice.Carat;
                invoice.Rate = dto.Rate ?? invoice.Rate;
                invoice.Amount = dto.Amount ?? invoice.Amount;
                invoice.Terms = dto.Terms ?? invoice.Terms;
                invoice.Duedate = dto.Duedate ?? invoice.Duedate;
                invoice.Remark = dto.Remark ?? invoice.Remark;
                invoice.AdatAmount = dto.AdatAmount ?? invoice.AdatAmount;
                invoice.AdatPercent = dto.AdatPercent ?? invoice.AdatPercent;
                invoice.FinalAmount = dto.FinalAmount ?? invoice.FinalAmount;
                invoice.Udate = DateTime.Now;
                invoice.InvoiceNo = dto.InvoiceNo ?? invoice.InvoiceNo;
                invoice.ShiperCompanyId = dto.ShiperCompanyId ?? invoice.ShiperCompanyId;
                invoice.ConsignCompanyId = dto.ConsignCompanyId ?? invoice.ConsignCompanyId;

                invoice.ShiparExpensePercent = dto.ShiparExpensePercent ?? invoice.ShiparExpensePercent;
                invoice.ShiparExpenseAmount = dto.ShiparExpenseAmount ?? invoice.ShiparExpenseAmount;
                invoice.ConsignExpensePercent = dto.ConsignExpensePercent ?? invoice.ConsignExpensePercent;
                invoice.ConsignExpenseAmount = dto.ConsignExpenseAmount ?? invoice.ConsignExpenseAmount;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Invoice updated successfully",
                    Data = invoice
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

        // ✅ DELETE
        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {


                var invoice = await _context.Invoicedetails
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        x.ClientId == User.GetClientId() &&
                        x.YearId == User.GetYearId());

                if (invoice == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Invoice not found"
                    });
                }

                _context.Invoicedetails.Remove(invoice);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Invoice deleted successfully",
                    Data = invoice
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
    }
}
