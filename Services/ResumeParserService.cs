using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace RecruitmentPortal.Services
{
    public class ResumeParserService : IResumeParserService
    {
        private readonly ILogger<ResumeParserService> _logger;
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB limit
        private static readonly string[] AllowedExtensions = { ".pdf", ".docx" };

        public ResumeParserService(ILogger<ResumeParserService> logger)
        {
            _logger = logger;
        }

        public async Task<ResumeParseResult> ParseResumeAsync(IFormFile? file, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                return new ResumeParseResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Please select a valid resume file. The uploaded file is empty."
                };
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return new ResumeParseResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Resume file size exceeds the allowed limit of 10 MB (File size: {file.Length / (1024 * 1024.0):F1} MB)."
                };
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return new ResumeParseResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Unsupported file format '{extension}'. Only PDF and DOCX files are supported."
                };
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            return await ParseResumeStreamAsync(memoryStream, file.FileName, cancellationToken);
        }

        public Task<ResumeParseResult> ParseResumeStreamAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
        {
            if (stream == null || stream.Length == 0)
            {
                return Task.FromResult(new ResumeParseResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Resume file stream is empty."
                });
            }

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return Task.FromResult(new ResumeParseResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Unsupported file format '{extension}'. Only PDF and DOCX files are supported."
                });
            }

            try
            {
                string rawText;
                if (extension == ".pdf")
                {
                    rawText = ExtractPdfText(stream);
                }
                else
                {
                    rawText = ExtractDocxText(stream);
                }

                var normalizedText = NormalizeExtractedText(rawText);

                if (string.IsNullOrWhiteSpace(normalizedText) || normalizedText.Length < 30)
                {
                    return Task.FromResult(new ResumeParseResult
                    {
                        IsSuccess = false,
                        FileName = fileName,
                        FileExtension = extension,
                        FileSizeBytes = stream.Length,
                        ErrorMessage = extension == ".pdf"
                            ? "Could not extract readable text from the PDF. The file may be an image-only scan or password-protected. Please upload a searchable text-based PDF or DOCX."
                            : "Could not extract readable text from the Word document. The file may be blank or contain only images."
                    });
                }

                _logger.LogInformation("Successfully parsed resume '{FileName}' ({Length} chars extracted).", fileName, normalizedText.Length);

                return Task.FromResult(new ResumeParseResult
                {
                    IsSuccess = true,
                    ExtractedText = normalizedText,
                    FileName = fileName,
                    FileExtension = extension,
                    FileSizeBytes = stream.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse resume file '{FileName}'", fileName);
                return Task.FromResult(new ResumeParseResult
                {
                    IsSuccess = false,
                    FileName = fileName,
                    FileExtension = extension,
                    FileSizeBytes = stream.Length,
                    ErrorMessage = $"Failed to parse {extension.ToUpperInvariant()} resume: {ex.Message}"
                });
            }
        }

        private string ExtractPdfText(Stream stream)
        {
            stream.Position = 0;
            var sb = new StringBuilder();

            using var document = PdfDocument.Open(stream);
            foreach (var page in document.GetPages())
            {
                var pageText = page.Text;
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    sb.AppendLine(pageText);
                }
            }

            return sb.ToString();
        }

        private string ExtractDocxText(Stream stream)
        {
            stream.Position = 0;
            var sb = new StringBuilder();

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
            foreach (var p in paragraphs)
            {
                var texts = p.Descendants(w + "t").Select(t => t.Value);
                var paragraphText = string.Concat(texts);
                if (!string.IsNullOrWhiteSpace(paragraphText))
                {
                    sb.AppendLine(paragraphText.Trim());
                }
            }

            return sb.ToString();
        }

        private string NormalizeExtractedText(string rawText)
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
