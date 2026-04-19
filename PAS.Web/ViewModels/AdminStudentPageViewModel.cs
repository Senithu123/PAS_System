using System.Collections.Generic;

namespace PAS.Web.ViewModels
{
    public class AdminStudentPageViewModel
    {
        public int TotalStudents { get; set; }
        public string? SearchTerm { get; set; }
        public List<AdminStudentViewModel> Students { get; set; } = new();
    }
}