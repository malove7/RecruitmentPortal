using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RecruitmentPortal.Models.DTOs;
using RecruitmentPortal.Services;
using Xunit;

namespace RecruitmentPortal.Tests.Services
{
    public class PineconeVectorServiceTests
    {
        private readonly PineconeOptions _options;
        private readonly PineconeVectorService _service;

        public PineconeVectorServiceTests()
        {
            _options = new PineconeOptions
            {
                ApiKey = "dummy-pinecone-key",
                Host = "https://recruitment-knowledgebase-6540nuo.svc.aped-4627-b74a.pinecone.io",
                IndexName = "recruitment-knowledgebase",
                NamespacePrefix = "kb"
            };

            var optionsMock = Options.Create(_options);
            var httpClient = new HttpClient();
            _service = new PineconeVectorService(httpClient, optionsMock, NullLogger<PineconeVectorService>.Instance);
        }

        [Theory]
        [InlineData(1, 10, 0, "kb-1-doc-10-chunk-0")]
        [InlineData(42, 105, 3, "kb-42-doc-105-chunk-3")]
        [InlineData(100, 200, 15, "kb-100-doc-200-chunk-15")]
        public void BuildVectorId_ShouldMatchExpectedFormat(int kbId, int docId, int chunkIndex, string expected)
        {
            var vectorId = _service.BuildVectorId(kbId, docId, chunkIndex);
            vectorId.Should().Be(expected);
        }

        [Theory]
        [InlineData(1, "kb-1")]
        [InlineData(55, "kb-55")]
        public void BuildNamespace_ShouldMatchExpectedFormat(int kbId, string expected)
        {
            var ns = _service.BuildNamespace(kbId);
            ns.Should().Be(expected);
        }

        [Fact]
        public async Task QueryVectorsAsync_ShouldReturnEmpty_WhenVectorIsEmpty()
        {
            var results = await _service.QueryVectorsAsync("kb-1", new List<float>());
            results.Should().BeEmpty();
        }

        [Fact]
        public async Task UpsertVectorsAsync_ShouldThrowInvalidOperationException_WhenApiKeyIsMissing()
        {
            var emptyOptions = Options.Create(new PineconeOptions { ApiKey = "", Host = "https://recruitment-knowledgebase.pinecone.io" });
            var serviceWithoutKey = new PineconeVectorService(new HttpClient(), emptyOptions, NullLogger<PineconeVectorService>.Instance);

            var vectors = new List<PineconeVectorRecord>
            {
                new() { Id = "test-1", Values = new List<float> { 0.1f, 0.2f } }
            };

            // Clear environment variable during test if set
            var prevEnv = Environment.GetEnvironmentVariable("PINECONE_API_KEY");
            try
            {
                Environment.SetEnvironmentVariable("PINECONE_API_KEY", null);

                var act = () => serviceWithoutKey.UpsertVectorsAsync("kb-1", vectors);
                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("*Pinecone API key is missing*");
            }
            finally
            {
                if (prevEnv != null)
                {
                    Environment.SetEnvironmentVariable("PINECONE_API_KEY", prevEnv);
                }
            }
        }
    }
}
