using Microsoft.EntityFrameworkCore;
using PAS.Core.Entities;
using PAS.Core.Enums;
using PAS.Infrastructure.Data;
using PAS.Infrastructure.Services;
using Xunit;

namespace PAS.Tests.UnitTests
{
    public class ProposalServiceTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task ConfirmMatchAsync_Should_Return_False_When_Proposal_Not_Found()
        {
            using var context = GetInMemoryDbContext();
            var service = new ProposalService(context);

            var result = await service.ConfirmMatchAsync(999);

            Assert.False(result);
        }

        [Fact]
        public async Task ConfirmMatchAsync_Should_Return_False_When_No_Supervisor_Is_Assigned()
        {
            using var context = GetInMemoryDbContext();

            var proposal = new ProjectProposal
            {
                Title = "Test Proposal",
                Abstract = "Test Abstract",
                TechnicalStack = "ASP.NET Core",
                ResearchAreaId = 1,
                StudentId = "student-001",
                Status = ProposalStatus.UnderReview
            };

            context.ProjectProposals.Add(proposal);
            await context.SaveChangesAsync();

            var service = new ProposalService(context);

            var result = await service.ConfirmMatchAsync(proposal.Id);

            Assert.False(result);
        }

        [Fact]
        public async Task ConfirmMatchAsync_Should_Set_Status_To_Matched_And_Reveal_Identity()
        {
            using var context = GetInMemoryDbContext();

            var proposal = new ProjectProposal
            {
                Title = "Test Proposal",
                Abstract = "Test Abstract",
                TechnicalStack = "ASP.NET Core",
                ResearchAreaId = 1,
                StudentId = "student-001",
                MatchedSupervisorId = "supervisor-001",
                Status = ProposalStatus.UnderReview,
                IsIdentityRevealed = false
            };

            context.ProjectProposals.Add(proposal);
            await context.SaveChangesAsync();

            var service = new ProposalService(context);

            var result = await service.ConfirmMatchAsync(proposal.Id);

            var updatedProposal = await context.ProjectProposals.FirstAsync();

            Assert.True(result);
            Assert.Equal(ProposalStatus.Matched, updatedProposal.Status);
            Assert.True(updatedProposal.IsIdentityRevealed);
        }
    }
}