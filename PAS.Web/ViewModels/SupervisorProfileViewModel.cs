using System.ComponentModel.DataAnnotations;

namespace PAS.Web.ViewModels
{
    public class SupervisorProfileViewModel
    {
        [Required]
        [Display(Name = "Preferred Research Area")]
        public int ResearchAreaId { get; set; }
    }
}