using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace RecruitmentPortal.Services
{
    public class DocumentTextExtractor : IDocumentTextExtractor
    {
        private readonly ILogger<DocumentTextExtractor> _logger;
        private static readonly string[] AllowedExtensions = { ".pdf", ".docx" };

        public DocumentTextExtractor(ILogger<DocumentTextExtractor> logger)
        {
            _logger = logger;
        }

        public async Task<ExtractedDocument> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return new ExtractedDocument
                {
                    IsSuccess = false,
                    ErrorMessage = $"File does not exist: '{filePath}'"
                };
            }

            var fileName = Path.GetFileName(filePath);
            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return await ExtractAsync(fileStream, fileName, cancellationToken);
        }

        public Task<ExtractedDocument> ExtractAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
        {
            if (stream == null || stream.Length == 0)
            {
                return Task.FromResult(new ExtractedDocument
                {
                    IsSuccess = false,
                    FileName = fileName ?? string.Empty,
                    ErrorMessage = "Document stream is empty."
                });
            }

            var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return Task.FromResult(new ExtractedDocument
                {
                    IsSuccess = false,
                    FileName = fileName ?? string.Empty,
                    FileExtension = extension,
                    FileSizeBytes = stream.Length,
                    ErrorMessage = $"Unsupported file format '{extension}'. Only PDF and DOCX files are supported."
                });
            }

            try
            {
                List<DocumentPageText> pages;
                if (extension == ".pdf")
                {
                    pages = ExtractPdfPages(stream);
                }
                else
                {
                    pages = ExtractDocxPages(stream);
                }

                // Filter out empty pages and normalize
                var cleanedPages = new List<DocumentPageText>();
                var fullTextSb = new StringBuilder();

                foreach (var page in pages)
                {
                    var normalizedPageText = NormalizeText(page.Text);
                    if (!string.IsNullOrWhiteSpace(normalizedPageText))
                    {
                        cleanedPages.Add(new DocumentPageText
                        {
                            PageNumber = page.PageNumber,
                            Text = normalizedPageText
                        });
                        fullTextSb.AppendLine(normalizedPageText);
                    }
                }

                var fullText = fullTextSb.ToString().Trim();

                if (string.IsNullOrWhiteSpace(fullText) || fullText.Length < 10)
                {
                    return Task.FromResult(new ExtractedDocument
                    {
                        IsSuccess = false,
                        FileName = fileName ?? string.Empty,
                        FileExtension = extension,
                        FileSizeBytes = stream.Length,
                        ErrorMessage = extension == ".pdf"
                            ? "Could not extract readable text from the PDF. The file may be an image-only scan or password-protected. Please upload a searchable text-based PDF or DOCX."
                            : "Could not extract readable text from the Word document. The file may be blank or contain only images."
                    });
                }

                _logger.LogInformation("Successfully extracted {PageCount} pages ({CharCount} chars) from '{FileName}'",
                    cleanedPages.Count, fullText.Length, fileName);

                return Task.FromResult(new ExtractedDocument
                {
                    IsSuccess = true,
                    FileName = fileName ?? string.Empty,
                    FileExtension = extension,
                    FileSizeBytes = stream.Length,
                    FullText = fullText,
                    Pages = cleanedPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract text from '{FileName}'", fileName);
                return Task.FromResult(new ExtractedDocument
                {
                    IsSuccess = false,
                    FileName = fileName ?? string.Empty,
                    FileExtension = extension,
                    FileSizeBytes = stream.Length,
                    ErrorMessage = $"Failed to parse {extension.ToUpperInvariant()} document: {ex.Message}"
                });
            }
        }

        private List<DocumentPageText> ExtractPdfPages(Stream stream)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            var pages = new List<DocumentPageText>();
            using var document = PdfDocument.Open(stream);

            foreach (var page in document.GetPages())
            {
                var pageText = page.Text ?? string.Empty;
                pages.Add(new DocumentPageText
                {
                    PageNumber = page.Number,
                    Text = pageText
                });
            }

            return pages;
        }

        private List<DocumentPageText> ExtractDocxPages(Stream stream)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            var documentEntry = archive.GetEntry("word/document.xml");
            if (documentEntry == null)
            {
                throw new InvalidOperationException("Invalid DOCX structure: word/document.xml entry not found.");
            }

            using var entryStream = documentEntry.Open();
            var xDoc = XDocument.Load(entryStream);
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            var paragraphs = xDoc.Descendants(w + "p");
            var sb = new StringBuilder();

            foreach (var p in paragraphs)
            {
                var texts = p.Descendants(w + "t").Select(t => t.Value);
                var paragraphText = string.Concat(texts);
                if (!string.IsNullOrWhiteSpace(paragraphText))
                {
                    sb.AppendLine(paragraphText.Trim());
                }
            }

            // Word documents don't have rigid physical pages in raw XML, so we treat as page 1
            return new List<DocumentPageText>
            {
                new DocumentPageText
                {
                    PageNumber = 1,
                    Text = sb.ToString()
                }
            };
        }

        public string NormalizeText(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return string.Empty;

            // Normalize line breaks
            var text = rawText.Replace("\r\n", "\n").Replace('\r', '\n');

            // Strip non-printable control characters except newline and tab
            var chars = text.Where(c => c == '\n' || c == '\t' || !char.IsControl(c)).ToArray();
            text = new string(chars);

            // Split into lines, trim each, and collapse consecutive blank lines
            var lines = text.Split('\n');
            var cleanedLines = new List<string>();
            bool previousWasEmpty = false;

            foreach (var rawLine in lines)
            {
                var line = Regex.Replace(rawLine.Trim(), @"[ \t]+", " ");
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (!previousWasEmpty)
                    {
                        cleanedLines.Add(string.Empty);
                        previousWasEmpty = true;
                    }
                }
                else
                {
                    cleanedLines.Add(line);
                    previousWasEmpty = false;
                }
            }

            return string.Join("\n", cleanedLines).Trim();
        }
    }
}
