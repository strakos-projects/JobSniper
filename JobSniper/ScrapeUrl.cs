using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobSniper.Models
{
    class ScrapeUrl
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public string PortalName { get; set; } 
        public bool IsActive { get; set; } 
    }
}
