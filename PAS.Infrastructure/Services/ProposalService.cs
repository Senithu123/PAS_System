using Microsoft.EntityFrameworkCore;
using PAS.Core.Interfaces;
using PAS.Core.Enums;
using PAS.Infrastructure.Data;

namespace PAS.Infrastructure.Services
{
    public class ProposalService : IProposalService
    {
        private readonly ApplicationDbContext _context;

        public ProposalService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ConfirmMatchAsync(int proposalId)
        {
            var proposal = await _context.ProjectProposals
                .FirstOrDefaultAsync(p => p.Id == proposalId);

            if (proposal == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(proposal.MatchedSupervisorId))
            {
                return false;
            }

            proposal.Status = ProposalStatus.Matched;
            proposal.IsIdentityRevealed = true;
            proposal.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}