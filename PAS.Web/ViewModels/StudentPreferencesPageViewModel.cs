using System.Collections.Generic;

namespace PAS.Web.ViewModels
{
    public class StudentPreferencesPageViewModel
    {
        public StudentPreferenceInputViewModel Input { get; set; } = new();
        public List<StudentPreferenceItemViewModel> Preferences { get; set; } = new();
    }
}