using AdatHisabdubai.Data;
using AdatHisabdubai.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JangadHisabApp.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PartyMasterController : ControllerBase
    {
        private readonly AdatHisabAppContext _context;

        public PartyMasterController(AdatHisabAppContext context)
        {
            _context = context;
        }
        // ✅ GET ALL GROUP PARTIES
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllGroupParties()
        {
            var data = await _context.Partymasters
                .Where(p => p.ClientId == User.GetClientId() && p.IsGroupParty == true)
                .Select(p => new
                {
                    p.Id,
                    p.Name
                })
                .ToListAsync();
            if (!data.Any())
            {
                return Ok(new
                {
                    Success = true,
                    Message = "No group parties found",
                    Data = data
                });
            }
            return Ok(new
            {
                Success = true,
                Message = "Group parties fetched successfully",
                Data = data
            });
        }
        // ✅ GET ALL CURRENCY
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetallCurrency()
        {
            var data = await _context.Currencymsts.ToListAsync();
            return Ok(new
            {
                Success = true,
                Message = "Currency fetched successfully",
                Data = data
            });

        }
        // ✅ GET ALL PARTIES
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll1(int page = 1, int limit = 10, string? partyName = null)
        {
            try
            {


                // 1. Ensure page and limit are valid
                if (page < 1) page = 1;
                if (limit < 1) limit = 10;

                // 2. Start with the base query (Filtering)
                var query = _context.Partymasters
                    .Where(p => p.ClientId == User.GetClientId());

                // 3. Apply Search Filter if partyName is provided
                if (!string.IsNullOrWhiteSpace(partyName))
                {
                    query = query.Where(p => p.Name.Contains(partyName));
                }

                // 4. Get Total Count for metadata before paging
                var totalRecords = await query.CountAsync();

                // 5. Apply Sorting and Paging
                var data = await query
                    .OrderByDescending(p => p.Id)
                    .Skip((page - 1) * limit) // Skip previous pages
                    .Take(limit)              // Take only the current page size
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.PhoneNo,
                        p.Email,
                        p.Addresss,
                        p.Remark,
                        Client = _context.Clientmasters
                            .Where(c => c.Id == p.ClientId)
                            .Select(c => new
                            {
                                id = c.Id,
                                Name = c.Name,
                                Userid = c.UserName
                            }).FirstOrDefault(),
                        p.YearId,
                        p.IsGroupParty,
                        GroupParty = p.IsGroupParty == false ? _context.Partymasters
                            .Where(gp => gp.Id == p.GroupPartyId)
                            .Select(gp => new
                            {
                                id = gp.Id,
                                Name = gp.Name
                            }).FirstOrDefault() : null,
                        p.Cdate,
                        p.Udate
                    })
                    .ToListAsync();

                // 6. Return response with metadata
                return Ok(new
                {
                    Success = true,
                    Message = data.Any() ? "Party list fetched successfully" : "No party found",
                    TotalRecords = totalRecords,
                    CurrentPage = page,
                    PageSize = limit,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / limit),
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
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllMainSubPartyList()
        {
            var clientId = User.GetClientId();
            var yearId = User.GetYearId();

            var parties = await _context.Partymasters
                .Where(x => x.ClientId == clientId && x.YearId == yearId && x.IsGroupParty != false)
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
        public async Task<IActionResult> GetAll(int? page, int? limit, string? search)
        {
            try
            {
                var compId = User.GetClientId();

                var query = _context.Partymasters
                    .Where(p => p.ClientId == compId)
                    .Join(
                        _context.Clientmasters,
                        p => p.ClientId,
                        c => c.Id,
                        (p, c) => new
                        {
                            p.Id,
                            p.Name,
                            p.Addresss,
                            p.Remark,
                            p.PhoneNo,
                            p.Email,

                            Client = new
                            {
                                c.Name,
                                c.ClientId,
                                c.UserName
                            },

                            p.IsGroupParty,

                            GroupParty = p.IsGroupParty == false
                                ? _context.Partymasters
                                    .Where(gp => gp.Id == p.GroupPartyId)
                                    .Select(gp => new
                                    {
                                        id = gp.Id,
                                        Name = gp.Name
                                    })
                                    .FirstOrDefault()
                                : null,

                            p.YearId,
                            p.Cdate,

                            Currency = _context.Currencymsts
                                .Where(cur => cur.Id == p.CurrencyId)
                                .Select(cur => new
                                {
                                    cur.Id,
                                    cur.Name
                                })
                                .FirstOrDefault(),

                            p.PartyType
                        }
                    )
                    .OrderBy(x => x.Name)
                    .AsQueryable();

                // ================= SEARCH BY NAME =================
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim();

                    query = query.Where(x =>
                        x.Name.Contains(search));
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

                var parties = await query.ToListAsync();

                if (!parties.Any())
                {
                    return Ok(new
                    {
                        Success = true,
                        Message = "No parties found"
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Parties fetched successfully",
                    TotalRecords = totalRecords,
                    Page = page,
                    Limit = limit,
                    TotalPages = (page.HasValue && limit.HasValue && limit > 0)
                        ? (int)Math.Ceiling(totalRecords / (double)limit)
                        : 1,
                    Data = parties
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

        // ✅ GET PARTY BY ID
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {


                var data = await _context.Partymasters
                    .Where(p => p.Id == id &&
                                p.ClientId == User.GetClientId() &&
                                p.YearId == User.GetYearId())
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.PhoneNo,
                        p.Email,
                        p.Addresss,
                        p.Remark,
                        p.Cdate,
                        p.Udate,
                        Client = _context.Clientmasters
                                .Where(z => z.Id == p.ClientId)
                                .Select(z => new
                                {
                                    z.Name,
                                    z.ClientId,
                                    z.UserName
                                })
                                .FirstOrDefault(),
                        p.YearId,
                        CurrencyId = _context.Currencymsts
                                .Where(cur => cur.Id == p.CurrencyId)
                                .Select(cur => new
                                {
                                    cur.Id,
                                    cur.Name


                                })
                                .FirstOrDefault(),
                        p.IsGroupParty,
                        GroupParty = p.IsGroupParty == false ? _context.Partymasters
                            .Where(gp => gp.Id == p.GroupPartyId)
                            .Select(gp => new
                            {
                                id = gp.Id,
                                Name = gp.Name
                            }).FirstOrDefault() : null,
                        PartyType = _context.Partymasters.Where(pt => pt.Id == p.Id).Select(pt => pt.PartyType).FirstOrDefault()
                    })
                    .FirstOrDefaultAsync();

                if (data == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Party not found",
                        Data = new List<object>()
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Party fetched successfully",
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

        // ✅ CREATE PARTY
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Add([FromBody] Partymaster model)
        {
            try
            {

                if (User.GetYearId() == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Select Financial Year"
                    });
                }
                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Party name is required"
                    });
                }
                var currencyId = await _context.Currencymsts.Where(c => c.Id == model.CurrencyId).FirstOrDefaultAsync();
                if (currencyId == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Invalid CurrencyId"
                    });
                }
                // 🔁 DUPLICATE CHECK
                bool exists = await _context.Partymasters.AnyAsync(p =>
                    p.ClientId == User.GetClientId() &&

                    p.Name == model.Name);

                if (exists)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Party already exists"
                    });
                }
                //if (!string.IsNullOrEmpty(model.Email))
                //{
                //    bool emailExists = await _context.Partymasters.AnyAsync(p =>
                //        p.ClientId == User.GetClientId() &&
                //        p.YearId == User.GetYearId() &&
                //        p.Email == model.Email);

                //    if (emailExists)
                //        return StatusCode(500, new { Success = false, Message = "Email already exists" });
                //}

                //if (!string.IsNullOrEmpty(model.PhoneNo))
                //{
                //    bool phoneExists = await _context.Partymasters.AnyAsync(p =>
                //        p.ClientId == User.GetClientId() &&
                //        p.YearId == User.GetYearId() &&
                //        p.PhoneNo == model.PhoneNo);

                //    if (phoneExists)
                //        return StatusCode(500, new { Success = false, Message = "Phone number already exists" });
                //}

                // 🔁 CurrencyID CHECK
                if (!model.CurrencyId.HasValue || model.CurrencyId <= 0)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "CurrencyID is required"
                    });
                }

                // 🔁 PARTYTYPE CHECK
                if (string.IsNullOrWhiteSpace(model.PartyType))
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "PartyType is required"
                    });
                }

                model.ClientId = User.GetClientId();
                model.YearId = User.GetYearId();
                model.Cdate = DateTime.Now;

                _context.Partymasters.Add(model);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Party created successfully",
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

        // ✅ UPDATE PARTY
        [HttpPut]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] Partymaster dto)
        {
            try
            {
                var party = await _context.Partymasters
                    .FirstOrDefaultAsync(p =>
                        p.Id == id &&
                        p.ClientId == User.GetClientId()
                       );

                if (party == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Party not found",
                        Data = new List<object>()
                    });
                }

                bool currencyExists = await _context.Currencymsts
    .AnyAsync(z => z.Id == dto.CurrencyId);

                if (!currencyExists)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Currency not found"
                    });
                }




                var finalName = string.IsNullOrWhiteSpace(dto.Name)
      ? party.Name
      : dto.Name;

                var finalEmail = string.IsNullOrWhiteSpace(dto.Email)
                    ? party.Email
                    : dto.Email;

                var finalPhone = string.IsNullOrWhiteSpace(dto.PhoneNo)
                    ? party.PhoneNo
                    : dto.PhoneNo;

                // 🔁 DUPLICATE CHECK (NAME / EMAIL / PHONE)
                bool exists = await _context.Partymasters.AnyAsync(p =>
                    p.Id != id &&
                    p.ClientId == User.GetClientId() &&
                    p.YearId == User.GetYearId() &&
                    (
                        p.Name == finalName ||
                        (!string.IsNullOrEmpty(finalEmail) && p.Email == finalEmail) ||
                        (!string.IsNullOrEmpty(finalPhone) && p.PhoneNo == finalPhone)
                    )
                );

                if (exists)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Party with same Name, Email, or Phone already exists"
                    });
                }
                bool isCurrencyChanged =

                            dto.CurrencyId != party.CurrencyId;

                if (isCurrencyChanged)
                {
                    bool hasTransaction = await _context.Transactions
                        .AnyAsync(t => t.PartyId == id && t.ClientId == User.GetClientId());

                    if (hasTransaction)
                    {
                        return BadRequest(new
                        {
                            Success = false,
                            Message = "Cannot update CurrencyID with existing transactions"
                        });
                    }
                }

                party.Name = finalName;
                party.PhoneNo = dto.PhoneNo ?? party.PhoneNo;
                party.Email = dto.Email ?? party.Email;
                party.Addresss = dto.Addresss ?? party.Addresss;
                party.Remark = dto.Remark ?? party.Remark;
                party.CurrencyId = dto.CurrencyId ?? party.CurrencyId;
                party.PartyType = dto.PartyType ?? party.PartyType;
                party.IsGroupParty = dto.IsGroupParty ?? party.IsGroupParty;
                party.GroupPartyId = dto.GroupPartyId ?? party.GroupPartyId;


                party.Udate = DateTime.Now;





                //party.CurrencyId = dto.CurrencyId ?? party.CurrencyId;
                //party.PartyType = dto.PartyType ?? party.PartyType;


                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Party updated successfully",
                    Data = party
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

        // ✅ DELETE PARTY
        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var eixtsTransaction = await _context.Transactions.Where(t => t.PartyId == id && t.ClientId == User.GetClientId()).AnyAsync();
                if (eixtsTransaction)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Cannot delete party with existing transactions"
                    });
                }

                var party = await _context.Partymasters
                    .FirstOrDefaultAsync(p =>
                        p.Id == id &&
                        p.ClientId == User.GetClientId() &&
                        p.YearId == User.GetYearId());

                if (party == null)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "Party not found",
                        Data = (string?)null
                    });
                }

                _context.Partymasters.Remove(party);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Party deleted successfully",
                    Data = party
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
