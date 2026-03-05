using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JobSniper
{
    public class CompanyProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [JsonIgnore]
        public string PrimaryName => (Aliases != null && Aliases.Count > 0) ? Aliases[0] : "Neznámá firma";

        public List<string> Aliases { get; set; } = new List<string>();

        
        public int Reputation { get; set; }
        public int Potential { get; set; } = 0;
        public string InteractionHistory { get; set; } 

        public DateTime LastInteraction { get; set; } = DateTime.Now;
    }
}
