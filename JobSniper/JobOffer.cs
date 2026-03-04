using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobSniper.Models
{
    public class JobOffer
    {
        public int Id { get; set; }
        public string ExternalId { get; set; }
        public string Title { get; set; }
        public string Company { get; set; }
        public string Url { get; set; }
        public DateTime DateScraped { get; set; } = DateTime.Now; 
        public DateTime LastSeen { get; set; } = DateTime.Now;   

        public int Status { get; set; }
        public int CrmReputation { get; set; } = 0;

        public string Location { get; set; }
        public string Salary { get; set; }
    }
}