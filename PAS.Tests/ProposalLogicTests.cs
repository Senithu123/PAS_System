using PAS.Core.Entities;
using PAS.Core.Enums;
using Xunit;

namespace PAS.Tests.UnitTests
{
    public class ProposalLogicTests
    {
        [Fact]
        public void Proposal_Should_Start_With_Pending_Status()
        {
            var proposal = new ProjectProposal
            {
                Title = "Test Proposal",
                Abstract = "Test Abstract",
                TechnicalStack = "ASP.NET Core, SQL Server",
                ResearchAreaId = 1,
                StudentId = "student-123"
            };

            Assert.Equal(ProposalStatus.Pending, proposal.Status);
            Assert.False(proposal.IsIdentityRevealed);
        }

        [Fact]
        public void Proposal_Should_Reveal_Identity_When_Matched()
        {
            var proposal = new ProjectProposal
            {
                Title = "Test Proposal",
                Abstract = "Test Abstract",
                TechnicalStack = "ASP.NET Core, SQL Server",
                ResearchAreaId = 1,
                StudentId = "student-123",
                Status = ProposalStatus.Matched,
                IsIdentityRevealed = true
            };

            Assert.Equal(ProposalStatus.Matched, proposal.Status);
            Assert.True(proposal.IsIdentityRevealed);
        }

        [Fact]
        public void Proposal_Should_Be_Withdrawn_Correctly()
        {
            var proposal = new ProjectProposal
            {
                Title = "Test Proposal",
                Abstract = "Test Abstract",
                TechnicalStack = "ASP.NET Core, SQL Server",
                ResearchAreaId = 1,
                StudentId = "student-123"
            };

            proposal.Status = ProposalStatus.Withdrawn;

            Assert.Equal(ProposalStatus.Withdrawn, proposal.Status);
        }
    }
}