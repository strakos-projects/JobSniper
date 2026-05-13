using JobSniper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
namespace JobSniper.Models
{
    public class JobOffer
    {
        //public string JobId { get; set; } = Guid.NewGuid().ToString("N");
        private string _jobId;

        private string _pairingUrl;
        public string PairingUrl
        {
            get => _pairingUrl;
            set
            {
                _pairingUrl = value;
                _jobId = null; 
            }
        }

        public string JobId
        {
            get
            {
                if (string.IsNullOrEmpty(_jobId))
                {
                    string urlToHash = !string.IsNullOrWhiteSpace(PairingUrl) ? PairingUrl : Url;

                    if (!string.IsNullOrEmpty(urlToHash))
                    {
                        using (var md5 = System.Security.Cryptography.MD5.Create())
                        {
                            var bytes = System.Text.Encoding.UTF8.GetBytes(urlToHash);
                            var hash = md5.ComputeHash(bytes);
                            _jobId = Convert.ToHexString(hash).ToLower();
                        }
                    }
                    else
                    {
                        _jobId = Guid.NewGuid().ToString("N");
                    }
                }
                return _jobId;
            }
            set => _jobId = value;
        }
        public int Id { get; set; }
        public string CrmCompanyId { get; set; }
        public string ExternalId { get; set; }
        public string Title { get; set; }
        public string Company { get; set; }
        public string Url { get; set; }
        public DateTime DateScraped { get; set; } = DateTime.Now; 
        public DateTime LastSeen { get; set; } = DateTime.Now;

        public bool IsProbablyInactive => (DateTime.Now - LastSeen).TotalDays > 2;
        public bool IsNewOffer => (DateTime.Now - DateScraped).TotalHours <= 48;

        [JsonIgnore]
        public bool IsJustScraped { get; set; }
        

        public int Status { get; set; }
        public int CrmReputation { get; set; } = 0;
        public string PortalName { get; set; } = string.Empty;
        public string Location { get; set; }
        public string Salary { get; set; }

        [JsonIgnore] public AiEvaluation Evaluation { get; set; }

        [JsonIgnore]
        public int SortableAiScore => Evaluation?.StrategicScore ?? -1;
        public JobOffer()
        {
            // PROZATÍM (DEMO) NAPLNÍME MOCK DATY
            //Evaluation = AiEvaluation.GetDemoPosudek();
        }
    }
}