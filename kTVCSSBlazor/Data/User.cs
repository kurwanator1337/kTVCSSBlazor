using kTVCSSBlazor.Models;
using System.Security.Claims;

namespace kTVCSSBlazor.Data
{
    public class User : UserCookie
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public List<string> Roles { get; set; } = [];
        public int InLobbyWithPlayerID { get; set; } = 0;
        public DateTime StartSearchDateTime { get; set; }
        public DateTime AliveDt { get; set; } = DateTime.Now;

        public ClaimsPrincipal ToClaimsPrincipal() => new(new ClaimsIdentity(new Claim[]
        {
        new (ClaimTypes.Name, Username),
        new (ClaimTypes.Hash, Password),
        new (nameof(VkUid), VkUid ?? ""),
        new (nameof(SteamId), SteamId ?? ""),
        new (nameof(Name), Name ?? ""),
        new (nameof(TeamName), TeamName ?? ""),
        new (nameof(TeamID), TeamID ?? ""),
        new (nameof(AvatarUrl), AvatarUrl ?? ""),
        new (nameof(TeamPicture), TeamPicture ?? ""),
        new (nameof(CurrentMMR), CurrentMMR.ToString() ?? "0"),
        new (nameof(MaxMMR), MaxMMR.ToString() ?? "0"),
        new (nameof(Id), Id.ToString() ?? "0"),
        new (nameof(MinMMR), MinMMR.ToString() ?? "0"),
        new (nameof(TotalMatches), TotalMatches.ToString() ?? "0"),
        new (nameof(Tier), Tier.ToString() ?? "2"),
        }.Concat(Roles.Select(r => new Claim(ClaimTypes.Role, r)).ToArray()),
        "kTVCSS"));

        public static User FromClaimsPrincipal(ClaimsPrincipal principal) => new()
        {
            Username = principal.FindFirstValue(ClaimTypes.Name) ?? "",
            Password = principal.FindFirstValue(ClaimTypes.Hash) ?? "",
            VkUid = principal.FindFirstValue(nameof(VkUid)) ?? "",
            SteamId = principal.FindFirstValue(nameof(SteamId)) ?? "",
            Name = principal.FindFirstValue(nameof(Name)) ?? "",
            TeamName = principal.FindFirstValue(nameof(TeamName)) ?? "",
            TeamID = principal.FindFirstValue(nameof(TeamID)) ?? "",
            AvatarUrl = principal.FindFirstValue(nameof(AvatarUrl)) ?? "",
            TeamPicture = principal.FindFirstValue(nameof(TeamPicture)) ?? "",
            CurrentMMR = Convert.ToInt32(principal.FindFirstValue(nameof(CurrentMMR))),
            MaxMMR = Convert.ToInt32(principal.FindFirstValue(nameof(MaxMMR))),
            Id = Convert.ToInt32(principal.FindFirstValue(nameof(Id))),
            MinMMR = Convert.ToInt32(principal.FindFirstValue(nameof(MinMMR))),
            TotalMatches = Convert.ToInt32(principal.FindFirstValue(nameof(TotalMatches))),
            Tier = Convert.ToInt32(principal.FindFirstValue(nameof(Tier))),
            Roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
        };
    }
}
