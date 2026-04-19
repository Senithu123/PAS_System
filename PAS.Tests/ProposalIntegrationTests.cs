using Microsoft.EntityFrameworkCore;
using PAS.Core.Entities;
using PAS.Core.Enums;
using PAS.Infrastructure.Data;
using Xunit;

namespace PAS.Tests.IntegrationTests
{
    public class ProposalIntegrationTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Should_Save_ProjectProposal_To_Database()
        {
            using var context = GetInMemoryDbContext();

            var researchArea = new ResearchArea
            {
                Name = "Artificial Intelligence",
                Description = "AI related projects"
            };

            context.ResearchAreas.Add(researchArea);
            await context.SaveChangesAsync();

            var proposal = new ProjectProposal
            {
                Title = "AI-Based Student Performance Prediction System",
                Abstract = "A system to predict academic performance using machine learning.",
                TechnicalStack = "ASP.NET Core MVC, SQL Server, EF Core",
                ResearchAreaId = researchArea.Id,
                StudentId = "student-001",
                Status = ProposalStatus.Pending,
                IsIdentityRevealed = false
            };

            context.ProjectProposals.Add(proposal);
            await context.SaveChangesAsync();

            var savedProposal = await context.ProjectProposals.FirstOrDefaultAsync();

            Assert.NotNull(savedProposal);
            Assert.Equal("AI-Based Student Performance Prediction System", savedProposal!.Title);
            Assert.Equal(ProposalStatus.Pending, savedProposal.Status);
            Assert.False(savedProposal.IsIdentityRevealed);
        }

        [Fact]
        public async Task Should_Update_Proposal_Status_To_Withdrawn()
        {
            using var context = GetInMemoryDbContext();

            var proposal = new ProjectProposal
            {
                Title = "Test Proposal",
                Abstract = "Test Abstract",
                TechnicalStack = "ASP.NET Core",
                ResearchAreaId = 1,
                StudentId = "student-001",
                Status = ProposalStatus.Pending
            };

            context.ProjectProposals.Add(proposal);
            await context.SaveChangesAsync();

            proposal.Status = ProposalStatus.Withdrawn;
            await context.SaveChangesAsync();

            var updatedProposal = await context.ProjectProposals.FirstOrDefaultAsync();

            Assert.NotNull(updatedProposal);
            Assert.Equal(ProposalStatus.Withdrawn, updatedProposal!.Status);
        }
    }
}