using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PAS.Core.Entities
{
    public class SupervisorProfile : BaseEntity
    {
        public string SupervisorId { get; set; } = string.Empty;
        public ApplicationUser? Supervisor { get; set; }

        public int ResearchAreaId { get; set; }
        public ResearchArea? ResearchArea { get; set; }
    }
}
