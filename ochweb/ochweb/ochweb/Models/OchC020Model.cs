using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ochweb.Models
{
    public class OchC020View
    {
        public int SessionID { get; set; }
        public string SessionName { get; set; }
        public string SessionContent { get; set; }
        public string SessionLocation { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string CreateDateTime { get; set; }

        public string UserID { get; set; }
        public string UserType { get; set; }
        public decimal FeeAmount { get; set; }
        public string PaidYN { get; set; }
        public string RegisterTime { get; set; }
        public OchC020View()
        {
        }
    }


    public class SessionDetailViewModel
    {
        public int SessionID { get; set; }
        public string SessionName { get; set; }
        public List<RegistrantInfo> Registrants { get; set; }

        public class RegistrantInfo
        {
            public string UserID { get; set; }

            public string UserNMC { get; set; }
            
            public string UserType { get; set; }
            public decimal FeeAmount { get; set; }
            public string PaidYN { get; set; }
            public string RegisterTime { get; set; }
        }
    }
}
