using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RecruitmentPortal.Data;
using RecruitmentPortal.Models;
using RecruitmentPortal.Models.DTOs;
using RecruitmentPortal.Models.Enums;
using RecruitmentPortal.Services;
using Xunit;

namespace RecruitmentPortal.Tests.Services
{
    public class KnowledgeBaseIngestionServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IDocumentTextExtractor> _extractorMock;
        private readonly Mock<ITextChunkingService> _chunkingMock;
        private readonly Mock<IOpenAIEmbeddingService> _embeddingMock;
        private readonly Mock<IPineconeVectorService> _pineconeMock;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly KnowledgeBaseIngestionService _service;
        private readonly string _tempFolder;

        public KnowledgeBaseIngestionServiceTests()
        {
            var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"KbIngestionTests_{Guid.NewGuid()}")
                .Options;

            _dbContext = new ApplicationDbContext(dbOptions);

            _tempFolder = Path.Combine(Path.GetTempPath(), $"KbTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempFolder);

            _extractorMock = new Mock<IDocumentTextExtractor>();
            _chunkingMock = new Mock<ITextChunkingService>();
            _embeddingMock = new Mock<IOpenAIEmbeddingService>();
            _pineconeMock = new Mock<IPineconeVectorService>();
            _envMock = new Mock<IWebHostEnvironment>();

            _envMock.Setup(e => e.ContentRootPath).Returns(_tempFolder);

            _pineconeMock.Setup(p => p.BuildNamespace(It.IsAny<int>()))
                .Returns<int>(id => $"kb-{id}");
            _pineconeMock.Setup(p => p.BuildVectorId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<int, int, int>((kb, doc, idx) => $"kb-{kb}-doc-{doc}-chunk-{idx}");

            var kbOptions = Options.Create(new KnowledgeBaseOptions
            {
                StoragePath = "App_Data/KnowledgeBaseFiles",
                MaxFileSizeMB = 25,
                ChunkSizeTokens = 800,
                ChunkOverlapTokens = 120
            });

            _service = new KnowledgeBaseIngestionService(
                _dbContext,
                _extractorMock.Object,
                _chunkingMock.Object,
                _embeddingMock.Object,
                _pineconeMock.Object,
                kbOptions,
                _envMock.Object,
                NullLogger<KnowledgeBaseIngestionService>.Instance);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            if (Directory.Exists(_tempFolder))
            {
                try { Directory.Delete(_tempFolder, true); } catch { }
            }
        }

        [Fact]
        public async Task IngestDocumentAsync_ShouldFail_WhenFileIsEmpty()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(0);
            fileMock.Setup(f => f.FileName).Returns("empty.pdf");

            var result = await _service.IngestDocumentAsync(1, fileMock.Object);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("empty");
        }

        [Fact]
        public async Task IngestDocumentAsync_ShouldFail_WhenExtensionIsUnsupported()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(100);
            fileMock.Setup(f => f.FileName).Returns("script.ps1");

            var result = await _service.IngestDocumentAsync(1, fileMock.Object);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Unsupported file format");
        }

        [Fact]
        public async Task IngestDocumentAsync_ShouldFail_WhenFileSizeExceedsLimit()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(26 * 1024 * 1024); // 26MB
            fileMock.Setup(f => f.FileName).Returns("large.pdf");

            var result = await _service.IngestDocumentAsync(1, fileMock.Object);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("exceeds maximum allowed limit");
        }

        [Fact]
        public async Task IngestDocumentAsync_ShouldMarkFailedInDatabase_WhenExtractionFails()
        {
            // Seed a knowledge base
            var kb = new KnowledgeBase { Name = "Engineering Policies" };
            _dbContext.KnowledgeBases.Add(kb);
            await _dbContext.SaveChangesAsync();

            var content = "dummy pdf content";
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            using var stream = new MemoryStream(bytes);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(bytes.Length);
            fileMock.Setup(f => f.FileName).Returns("corrupt.pdf");
            fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns<Stream, CancellationToken>((s, ct) => s.WriteAsync(bytes, 0, bytes.Length, ct));

            _extractorMock.Setup(e => e.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExtractedDocument
                {
                    IsSuccess = false,
                    ErrorMessage = "Encrypted or unreadable PDF"
                });

            var result = await _service.IngestDocumentAsync(kb.Id, fileMock.Object);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Encrypted or unreadable PDF");

            // Verify status in DB is Failed
            var docInDb = await _dbContext.KnowledgeBaseDocuments.FirstAsync();
            docInDb.Status.Should().Be(KnowledgeBaseDocumentStatus.Failed);
            docInDb.ErrorMessage.Should().Contain("Encrypted or unreadable PDF");
        }

        [Fact]
        public async Task IngestDocumentAsync_ShouldSuccessfullyIndexDocument_WhenAllStepsSucceed()
        {
            var kb = new KnowledgeBase { Name = "Engineering Policies" };
            _dbContext.KnowledgeBases.Add(kb);
            await _dbContext.SaveChangesAsync();

            var content = "valid docx content";
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(bytes.Length);
            fileMock.Setup(f => f.FileName).Returns("guide.docx");
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns<Stream, CancellationToken>((s, ct) => s.WriteAsync(bytes, 0, bytes.Length, ct));

            _extractorMock.Setup(e => e.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExtractedDocument
                {
                    IsSuccess = true,
                    FullText = "Software engineer guidelines and hiring practices.",
                    Pages = new List<DocumentPageText> { new() { PageNumber = 1, Text = "Guidelines" } }
                });

            _chunkingMock.Setup(c => c.ChunkDocument(It.IsAny<ExtractedDocument>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new List<TextChunk>
                {
                    new() { ChunkIndex = 0, Content = "Software engineer guidelines", PageNumber = 1, TokenCount = 5 }
                });

            _embeddingMock.Setup(e => e.GenerateEmbeddingsBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<List<float>> { new() { 0.1f, 0.2f, 0.3f } });

            var result = await _service.IngestDocumentAsync(kb.Id, fileMock.Object);

            result.IsSuccess.Should().BeTrue();
            result.ChunkCount.Should().Be(1);

            var docInDb = await _dbContext.KnowledgeBaseDocuments.Include(d => d.Chunks).FirstAsync();
            docInDb.Status.Should().Be(KnowledgeBaseDocumentStatus.Indexed);
            docInDb.Chunks.Should().HaveCount(1);
            docInDb.Chunks.First().VectorId.Should().Be($"kb-{kb.Id}-doc-{docInDb.Id}-chunk-0");

            _pineconeMock.Verify(p => p.UpsertVectorsAsync(
                $"kb-{kb.Id}",
                It.Is<IEnumerable<PineconeVectorRecord>>(v => v.Count() == 1),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
