using AdatHisabdubai.Data;


namespace JangadHisabApp.Service
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(AdatHisabAppContext context)
        {
            // Ensure DB created


            // 🔹 CHECK IF DATA EXISTS
            if (!context.Clientmasters.Any(x => x.UserName == "bnsoft"))
            {
                var demoCompany = new Clientmaster
                {
                    Name = "Bhavin",
                    Email = "bhavinsoft@yahoo.co.in",
                    Phoneno = "9376240271",
                    Address = "katargam",
                    Remark = "Default demo company-pass-1234",
                    UserPassword = BCrypt.Net.BCrypt.HashPassword("1234"),
                    Udate = DateTime.Now,
                    UserRoleId = 1,
                    Cdate = DateTime.Now,
                    UserName = "bnsoft",
                    Isdelete = false

                };

                context.Clientmasters.Add(demoCompany);
                await context.SaveChangesAsync();
                demoCompany.ClientId = demoCompany.Id;
                context.Clientmasters.Update(demoCompany);
                await context.SaveChangesAsync();
            }

        }
    }
}
