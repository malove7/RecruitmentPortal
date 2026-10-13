using FluentAssertions;
using RecruitmentPortal.Services;
using Xunit;

namespace RecruitmentPortal.Tests.Services
{
    public class TextChunkingServiceTests
    {
        private readonly TextChunkingService _service;

        public TextChunkingServiceTests()
        {
            _service = new TextChunkingService();
        }

        [Fact]
        public void EstimateTokenCount_ShouldReturnSensibleEstimate()
        {
            var text = "The quick brown fox jumps over the lazy dog.";
            var count = _service.EstimateTokenCount(text);

            count.Should().BeInRange(9, 20);
        }

        [Fact]
        public void ChunkText_ShouldReturnSingleChunk_WhenTextIsShort()
        {
            var text = "Short recruitment policy statement.";
            var chunks = _service.ChunkText(text, 1, 800, 120);

            chunks.Should().HaveCount(1);
            chunks[0].ChunkIndex.Should().Be(0);
            chunks[0].PageNumber.Should().Be(1);
            chunks[0].Content.Should().Be(text);
        }

        [Fact]
        public void ChunkText_ShouldCreateMultipleOverlappingChunks_WhenTextExceedsChunkSize()
        {
            // Build text of ~10,000 chars (> 2,500 tokens)
            var sentences = new List<string>();
            for (int i = 1; i <= 80; i++)
            {
                sentences.Add($"Paragraph {i}: The candidate must demonstrate proficiency in ASP.NET Core, C#, EF Core, and SQL Server databases. All evaluations should be documented.");
            }
            var text = string.Join("\n\n", sentences);

            var chunks = _service.ChunkText(text, 1, 300, 50);

            chunks.Count.Should().BeGreaterThan(1);
            chunks.First().ChunkIndex.Should().Be(0);

            // Verify sequential index
            for (int i = 0; i < chunks.Count; i++)
            {
                chunks[i].ChunkIndex.Should().Be(i);
                chunks[i].TokenCount.Should().BeGreaterThan(0);
                chunks[i].PageNumber.Should().Be(1);
            }
        }

        [Fact]
        public void ChunkDocument_ShouldRetainPageNumbers_FromMultiPageDocument()
        {
            var doc = new ExtractedDocument
            {
                IsSuccess = true,
                FullText = "Page 1 Content\n\nPage 2 Content",
                Pages = new List<DocumentPageText>
                {
                    new() { PageNumber = 1, Text = "Page 1 Content on Candidate Screening Policy." },
                    new() { PageNumber = 2, Text = "Page 2 Content on Offer Approval Flow." }
                }
            };

            var chunks = _service.ChunkDocument(doc, 800, 120);

            chunks.Should().NotBeEmpty();
            chunks.Any(c => c.PageNumber == 1).Should().BeTrue();
            chunks.Any(c => c.PageNumber == 2).Should().BeTrue();
        }

        [Fact]
        public void ChunkDocument_ShouldReturnEmpty_WhenDocumentTextIsEmpty()
        {
            var doc = new ExtractedDocument
            {
                IsSuccess = true,
                FullText = ""
            };

            var chunks = _service.ChunkDocument(doc);
            chunks.Should().BeEmpty();
        }
    }
}
