namespace water
{
    public static class Extensions
    {
        public static int GetId(this System.Security.Claims.ClaimsPrincipal user)
        {
            var idClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (idClaim != null)
                return int.Parse(idClaim.Value);

            return 0;
        }
    }
}
