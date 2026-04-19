using PAS.Core.Entities;

namespace PAS.Core.Interfaces
{
    public interface IProposalService
    {
        Task<bool> ConfirmMatchAsync(int proposalId);
    }
}