using System.IO.Compression;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using RecruitmentPortal.Services;
using Xunit;

namespace RecruitmentPortal.Tests.Services
{
    public class ResumeParserServiceTests
    {
        private readonly ResumeParserService _service;

        public ResumeParserServiceTests()
        {
            var loggerMock = new Mock<ILogger<ResumeParserService>>();
            _service = new ResumeParserService(loggerMock.Object);
        }

        [Fact]
        public async Task ParseResumeAsync_ShouldReturnError_WhenFileIsNull()
        {
            var result = await _service.ParseResumeAsync(null);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("empty");
        }

        [Fact]
        public async Task ParseResumeAsync_ShouldReturnError_WhenFileIsEmpty()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(0);

            var result = await _service.ParseResumeAsync(fileMock.Object);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("empty");
        }

        [Fact]
        public async Task ParseResumeAsync_ShouldReturnError_WhenFileExceedsMaxSize()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("large.pdf");
            fileMock.Setup(f => f.Length).Returns(11 * 1024 * 1024); // 11 MB

            var result = await _service.ParseResumeAsync(fileMock.Object);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("10 MB");
        }

        [Theory]
        [InlineData("resume.txt")]
        [InlineData("resume.exe")]
        [InlineData("resume.jpg")]
        [InlineData("resume.png")]
        public async Task ParseResumeAsync_ShouldReturnError_WhenFileExtensionIsUnsupported(string fileName)
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(100);

            var result = await _service.ParseResumeAsync(fileMock.Object);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Unsupported file format");
        }

        [Fact]
        public async Task ParseResumeStreamAsync_ShouldExtractText_FromValidDocxStream()
        {
            // Build an in-memory valid docx zip archive
            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                var entry = archive.CreateEntry("word/document.xml");
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<w:document xmlns:w=""http://schemas.openxmlformats.org/wordprocessingml/2006/main"">
  <w:body>
    <w:p>
      <w:r>
        <w:t>John Doe - Senior Software Engineer</w:t>
      </w:r>
    </w:p>
    <w:p>
      <w:r>
        <w:t>Over 8 years experience building cloud native .NET microservices and RESTful APIs.</w:t>
      </w:r>
    </w:p>
  </w:body>
</w:document>");
            }

            ms.Position = 0;

            var result = await _service.ParseResumeStreamAsync(ms, "candidate.docx");

            result.IsSuccess.Should().BeTrue();
            result.ExtractedText.Should().Contain("John Doe - Senior Software Engineer");
            result.ExtractedText.Should().Contain(".NET microservices");
        }

        [Fact]
        public async Task ParseResumeStreamAsync_ShouldReturnError_WhenDocxHasNoDocumentXml()
        {
            // Build zip without word/document.xml
            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                var entry = archive.CreateEntry("dummy.txt");
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream);
                writer.Write("not a docx");
            }

            ms.Position = 0;

            var result = await _service.ParseResumeStreamAsync(ms, "corrupt.docx");

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Invalid DOCX");
        }
    }
}
