using System.Security.Claims;

namespace AdatHisabdubai.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetClientId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("ClientId")?.Value;
            if (string.IsNullOrEmpty(claim))
                return 1; // Default ClientId set to 1 for safety
            return int.Parse(claim);
        }
        public static int GetRoleId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("UserRoleId")?.Value;
            if (string.IsNullOrEmpty(claim))
                throw new UnauthorizedAccessException("UserRoleId claim missing");
            return int.Parse(claim);
        }
        public static int GetYearId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("YearId")?.Value;
            if (string.IsNullOrEmpty(claim))
                return 1; // Default YearId set to 1 for safety
            return int.Parse(claim);
        }
        public static string GetUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(claim))
                throw new UnauthorizedAccessException("UserName claim missing");
            return claim;
        }
        public static string GetEmail(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("Email")?.Value;
            if (string.IsNullOrEmpty(claim))
                throw new UnauthorizedAccessException("Email claim missing");
            return claim;
        }

        public static string GetRole(this ClaimsPrincipal user)
       => user.FindFirst(ClaimTypes.Role)?.Value ?? "";
    }
}
