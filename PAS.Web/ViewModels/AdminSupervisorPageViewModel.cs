using System.Collections.Generic;

namespace PAS.Web.ViewModels
{
    public class AdminSupervisorPageViewModel
    {
        public int TotalSupervisors { get; set; }
        public string? SearchTerm { get; set; }
        public List<AdminSupervisorViewModel> Supervisors { get; set; } = new();
    }
}