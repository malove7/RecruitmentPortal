using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace RecruitmentPortal.Services
{
    public class ResumeParserService : IResumeParserService
    {
        private readonly IDocumentTextExtractor _extractor;
        private readonly ILogger<ResumeParserService> _logger;
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB limit
        private static readonly string[] AllowedExtensions = { ".pdf", ".docx" };

        public ResumeParserService(IDocumentTextExtractor extractor, ILogger<ResumeParserService> logger)
        {
            _extractor = extractor;
            _logger = logger;
        }

        public ResumeParserService(ILogger<ResumeParserService> logger)
            : this(new DocumentTextExtractor(NullLogger<DocumentTextExtractor>.Instance), logger)
        {
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

        public async Task<ResumeParseResult> ParseResumeStreamAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
        {
            if (stream == null || stream.Length == 0)
            {
                return new ResumeParseResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Resume file stream is empty."
                };
            }

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return new ResumeParseResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Unsupported file format '{extension}'. Only PDF and DOCX files are supported."
                };
            }

            try
            {
                var extraction = await _extractor.ExtractAsync(stream, fileName, cancellationToken);

                if (!extraction.IsSuccess)
                {
                    return new ResumeParseResult
                    {
                        IsSuccess = false,
                        FileName = fileName,
                        FileExtension = extension,
                        FileSizeBytes = stream.Length,
                        ErrorMessage = extraction.ErrorMessage
                    };
                }

                if (string.IsNullOrWhiteSpace(extraction.FullText) || extraction.FullText.Length < 30)
                {
                    return new ResumeParseResult
                    {
                        IsSuccess = false,
                        FileName = fileName,
                        FileExtension = extension,
                        FileSizeBytes = stream.Length,
                        ErrorMessage = extension == ".pdf"
                            ? "Could not extract readable text from the PDF. The file may be an image-only scan or password-protected. Please upload a searchable text-based PDF or DOCX."
                            : "Could not extract readable text from the Word document. The file may be blank or contain only images."
                    };
                }

                _logger.LogInformation("Successfully parsed resume '{FileName}' ({Length} chars extracted).", fileName, extraction.FullText.Length);

                return new ResumeParseResult
                {
                    IsSuccess = true,
                    ExtractedText = extraction.FullText,
                    FileName = fileName,
                    FileExtension = extension,
                    FileSizeBytes = stream.Length
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse resume file '{FileName}'", fileName);
                return new ResumeParseResult
                {
                    IsSuccess = false,
                    FileName = fileName,
                    FileExtension = extension,
                    FileSizeBytes = stream.Length,
                    ErrorMessage = $"Failed to parse {extension.ToUpperInvariant()} resume: {ex.Message}"
                };
            }
        }
    }
}
