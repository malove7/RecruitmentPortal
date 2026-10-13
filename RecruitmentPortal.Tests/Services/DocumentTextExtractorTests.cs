using System.IO.Compression;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RecruitmentPortal.Services;
using Xunit;

namespace RecruitmentPortal.Tests.Services
{
    public class DocumentTextExtractorTests
    {
        private readonly DocumentTextExtractor _extractor;

        public DocumentTextExtractorTests()
        {
            _extractor = new DocumentTextExtractor(NullLogger<DocumentTextExtractor>.Instance);
        }

        [Fact]
        public async Task ExtractAsync_ShouldReturnError_WhenStreamIsEmpty()
        {
            using var ms = new MemoryStream();
            var result = await _extractor.ExtractAsync(ms, "test.pdf");

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("empty");
        }

        [Theory]
        [InlineData("file.exe")]
        [InlineData("file.png")]
        [InlineData("file.txt")]
        public async Task ExtractAsync_ShouldReturnError_WhenExtensionIsNotSupported(string fileName)
        {
            using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
            var result = await _extractor.ExtractAsync(ms, fileName);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Unsupported file format");
        }

        [Fact]
        public async Task ExtractAsync_ShouldExtractDocxContent_Successfully()
        {
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
        <w:t>Engineering Candidate Evaluation Policy.</w:t>
      </w:r>
    </w:p>
    <w:p>
      <w:r>
        <w:t>All software engineers must pass technical coding interview and system design interview.</w:t>
      </w:r>
    </w:p>
  </w:body>
</w:document>");
            }

            ms.Position = 0;
            var result = await _extractor.ExtractAsync(ms, "policy.docx");

            result.IsSuccess.Should().BeTrue();
            result.FullText.Should().Contain("Engineering Candidate Evaluation Policy");
            result.FullText.Should().Contain("system design interview");
            result.Pages.Should().HaveCount(1);
            result.Pages[0].PageNumber.Should().Be(1);
        }

        [Fact]
        public async Task ExtractAsync_ShouldReturnError_WhenDocxHasInvalidStructure()
        {
            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                var entry = archive.CreateEntry("other.xml");
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream);
                writer.Write("<xml>test</xml>");
            }

            ms.Position = 0;
            var result = await _extractor.ExtractAsync(ms, "corrupt.docx");

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Invalid DOCX");
        }

        [Fact]
        public void NormalizeText_ShouldCleanWhitespaceAndControlCharacters()
        {
            var raw = "Line 1\r\n\r\n\r\n   Line   2   \t\nLine 3";
            var normalized = _extractor.NormalizeText(raw);

            normalized.Should().Be("Line 1\n\nLine 2\nLine 3");
        }
    }
}
