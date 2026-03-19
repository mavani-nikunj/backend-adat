using AdatHisabdubai.Data;
using AdatHisabdubai.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JangadHisabApp.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class OpeningBalanceController : ControllerBase
    {
        private readonly AdatHisabAppContext _context;

        public OpeningBalanceController(AdatHisabAppContext context)
        {
            _context = context;
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllCurrency()
        {
            try
            {


                var data = await _context.Currencymsts

                    .OrderBy(c => c.Name)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,

                    })
                    .ToListAsync();
                if (!data.Any())
                {
                    return Ok(new
                    {
                        Success = false,
                        Message = "No currency found",
                        Data = new List<object>()
                    });
                }
                return Ok(new
                {
                    Success = true,
                    Message = "Currencies fetched successfully",
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
        // ✅ GET ALL OPENING BALANCES
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            try
            {


                var data = await _context.Invoicedetails
                    .Where(o => o.ClientId == User.GetClientId() && o.YearId == User.GetYearId() && o.IsOpening == true)
                    .OrderByDescending(o => o.Id)
                    .Select(o => new
                    {
                        o.Id,
                        o.Invoicedate,
                        o.ShiperId,
                        PartyName = _context.Partymasters.Where(z => z.Id == o.ShiperId).Select(z => z.Name).FirstOrDefault(),
                        Currency = new
                        {
                            Id = o.CurrencyId,
                            Name = _context.Currencymsts.Where(z => z.Id == o.CurrencyId).Select(z => z.Name).FirstOrDefault()
                        },
                        o.FinalAmount,
                        Year = new
                        {
                            Id = o.YearId,
                            FromYear = _context.Yearmasters
                                .Where(y => y.Id == o.YearId)
                                .Select(y => y.Fromyear)
                                .FirstOrDefault(),
                            ToYear = _context.Yearmasters
                                .Where(y => y.Id == o.YearId)
                                .Select(y => y.Toyear)
                                .FirstOrDefault()
                        },
                        o.OpeningType,
                        o.Cdate,
                        o.Udate,
                        o.Remark
                    })
                    .ToListAsync();

                if (!data.Any())
                {
                    return Ok(new
                    {
                        Success = true,
                        Message = "No opening balance found",
                        Data = new List<object>()
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Opening balances fetched successfully",
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


                var data = await _context.Invoicedetails
                    .Where(o => o.Id == id && o.ClientId == User.GetClientId() && o.YearId == User.GetYearId() && o.IsOpening == true)
                    .Select(o => new
                    {
                        o.Id,
                        o.ShiperId,
                        o.Invoicedate,
                        PartyName = _context.Partymasters.Where(z => z.Id == o.ShiperId).Select(z => z.Name).FirstOrDefault(),
                        Currency = new
                        {
                            Id = o.CurrencyId,
                            Name = _context.Currencymsts.Where(z => z.Id == o.CurrencyId).Select(z => z.Name).FirstOrDefault()
                        },
                        o.FinalAmount,
                        Year = new
                        {
                            Id = o.YearId,
                            FromYear = _context.Yearmasters
                                .Where(y => y.Id == o.YearId)
                                .Select(y => y.Fromyear)
                                .FirstOrDefault(),
                            ToYear = _context.Yearmasters
                                .Where(y => y.Id == o.YearId)
                                .Select(y => y.Toyear)
                                .FirstOrDefault()
                        },

                        o.OpeningType,
                        o.YearId,
                        o.Cdate,
                        o.Udate
                    })
                    .FirstOrDefaultAsync();

                if (data == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Opening balance not found",
                        Data = new List<object>()
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Opening balance fetched successfully",
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

        // ✅ CREATE
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Add([FromBody] Invoicedetail model)
        {
            try
            {
                var partyExists = await _context.Partymasters
                    .AnyAsync(p => p.Id == model.ShiperId && p.ClientId == User.GetClientId());

                if (!partyExists)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Invalid Party"
                    });
                }

                if (!model.ShiperId.HasValue || model.ShiperId <= 0)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Party is required"
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

                var partyCurrencyId = await _context.Partymasters
                                        .Where(p =>
                                            p.Id == model.ShiperId &&
                                            p.ClientId == User.GetClientId() &&
                                            p.YearId == User.GetYearId()
                                        )
                                        .Select(p => p.CurrencyId)
                                        .FirstOrDefaultAsync();


                if (partyCurrencyId == null)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Party not found"
                    });
                }

                if (partyCurrencyId != model.CurrencyId)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Opening currency must match party currency"
                    });
                }




                if (!model.FinalAmount.HasValue || model.FinalAmount <= 0)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Amount is required"
                    });
                }

                if (model.Invoicedate == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Creatdate is required"
                    });
                }
                var currencyid = await _context.Currencymsts
                    .Where(z => z.Id == model.CurrencyId)
                    .Select(o => o.Id)
                    .FirstOrDefaultAsync();

                if (currencyid == 0)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Invalid Currency"
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
                if (string.IsNullOrWhiteSpace(model.OpeningType) ||
                        !(model.OpeningType.Equals("Receive", StringComparison.OrdinalIgnoreCase) ||
                          model.OpeningType.Equals("Payment", StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Opening Type must be either Receive or Payment"
                    });
                }

                model.IsExtra = false;
                model.ClientId = User.GetClientId();
                model.IsOpening = true;
                model.Cdate = DateTime.Now;
                model.YearId = User.GetYearId();
                model.UserId = User.GetUserId();

                _context.Invoicedetails.Add(model);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Opening balance created successfully",
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
                var idexitintransaction = await _context.Transactions.Where(z => z.InvoiceId == id && z.ClientId == User.GetClientId() && z.YearId == User.GetYearId()).AnyAsync();
                if (idexitintransaction)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Cannot update opening balance as it is linked to existing transactions"
                    });
                }

                //var currencyid = await _context.Currencymsts
                //    .Where(z => z.Id == dto.CurrencyId)
                //    .Select(o => o.Id)
                //    .FirstOrDefaultAsync();

                //if (currencyid == 0)
                //{
                //    return StatusCode(500, new
                //    {
                //        Success = false,
                //        Message = "Invalid Currency"
                //    });
                //}
                var opening = await _context.Invoicedetails
                    .FirstOrDefaultAsync(o => o.Id == id && o.ClientId == User.GetClientId() && o.YearId == User.GetYearId() && o.IsOpening == true);

                if (opening == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Opening balance not found",
                        Data = new List<object>()
                    });
                }
                if (dto.CurrencyId.HasValue &&
                        dto.CurrencyId != opening.CurrencyId &&
                        !await _context.Currencymsts.AnyAsync(x => x.Id == dto.CurrencyId))
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Invalid Currency"
                    });
                }
                var partyCurrency = await _context.Partymasters
                                             .Where(p =>
                                                 p.Id == dto.ShiperId &&
                                                 p.ClientId == User.GetClientId() &&
                                                 p.YearId == User.GetYearId()
                                             )
                                             .Select(p => p.CurrencyId)
                                             .FirstOrDefaultAsync();

                if (partyCurrency == null)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Party not found"
                    });
                }

                if (dto.CurrencyId != dto.CurrencyId)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Party currency mismatch"
                    });
                }

                var finalCurrency = dto.CurrencyId ?? opening.CurrencyId;
                var finalAmount = dto.FinalAmount ?? opening.FinalAmount;
                var finalYearId = User.GetYearId();
                var creatdate = dto.Invoicedate ?? opening.Invoicedate;


                opening.ShiperId = dto.ShiperId ?? opening.ShiperId;
                opening.CurrencyId = finalCurrency;
                opening.FinalAmount = finalAmount;
                opening.YearId = finalYearId;
                opening.OpeningType = dto.OpeningType ?? opening.OpeningType;
                opening.Udate = DateTime.Now;
                opening.Remark = dto.Remark ?? opening.Remark;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Opening balance updated successfully",
                    Data = opening
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

        // ✅ DELETE (HARD / OPTIONAL SOFT)
        [HttpDelete]
        [Authorize(Roles = "DevAdmin,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var idexitintransaction = await _context.Transactions.Where(z => z.InvoiceId == id && z.ClientId == User.GetClientId() && z.YearId == User.GetYearId()).AnyAsync();
            if (idexitintransaction)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Cannot delete opening balance as it is linked to existing transactions"
                });
            }
            var opening = await _context.Invoicedetails
                .FirstOrDefaultAsync(o => o.Id == id && o.ClientId == User.GetClientId() && o.YearId == User.GetYearId() && o.IsOpening == true);

            if (opening == null)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Opening balance not found",
                    Data = (string?)null
                });
            }

            _context.Invoicedetails.Remove(opening);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = "Opening balance deleted successfully",
                Data = opening
            });
        }
    }
}
