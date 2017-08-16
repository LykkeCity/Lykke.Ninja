using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Ninja.Web.Models
{
    public class IsAliveResponse
    {
        public string Version { get; set; }
        public IEnumerable<IssueIndicator> IssueIndicators { get; set; }

        public class IssueIndicator
        {
            public string Type { get; set; }
            public string Value { get; set; }
        }
    }
}
