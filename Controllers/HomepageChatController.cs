using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RecruitmentPortal.Data;
using RecruitmentPortal.Models.DTOs;
using RecruitmentPortal.Services;

namespace RecruitmentPortal.Controllers
{
    [ApiController]
    [Route("api/homepage-chat")]
    [AllowAnonymous]
    [EnableRateLimiting("HomepageChatPolicy")]
    public class HomepageChatController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IRagChatService _ragChatService;
        private readonly IOpenAITranscriptionService _transcriptionService;
        private readonly HomepageChatOptions _options;
        private readonly ILogger<HomepageChatController> _logger;

        public HomepageChatController(
            ApplicationDbContext dbContext,
            IRagChatService ragChatService,
            IOpenAITranscriptionService transcriptionService,
            IOptions<HomepageChatOptions> options,
            ILogger<HomepageChatController> logger)
        {
            _dbContext = dbContext;
            _ragChatService = ragChatService;
            _transcriptionService = transcriptionService;
            _options = options.Value;
            _logger = logger;
        }

        // GET: /api/homepage-chat/config
        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            return Ok(new HomepageChatConfigDto
            {
                Enabled = _options.Enabled,
                EnableVoiceInput = _options.EnableVoiceInput,
                MaxMessageLength = _options.MaxMessageLength,
                MaxAudioSizeMB = _options.MaxAudioSizeMB,
                FallbackMessage = _options.FallbackMessage
            });
        }

        // POST: /api/homepage-chat/message
        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] HomepageChatMessageRequest request, CancellationToken cancellationToken)
        {
            if (!_options.Enabled)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new HomepageChatMessageResponse
                {
                    Success = false,
                    ErrorMessage = "Homepage chat is currently disabled.",
                    IsFallback = true,
                    Answer = _options.FallbackMessage
                });
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new HomepageChatMessageResponse
                {
                    Success = false,
                    ErrorMessage = "Question cannot be empty."
                });
            }

            var cleanMessage = request.Message.Trim();
            if (cleanMessage.Length > _options.MaxMessageLength)
            {
                return BadRequest(new HomepageChatMessageResponse
                {
                    Success = false,
                    ErrorMessage = $"Question cannot exceed {_options.MaxMessageLength} characters."
                });
            }

            // Determine knowledge base to query
            var kb = await _dbContext.KnowledgeBases
                .FirstOrDefaultAsync(k => k.Id == _options.DefaultKnowledgeBaseId && k.IsActive, cancellationToken);

            if (kb == null)
            {
                // Fallback to first active knowledge base if configured ID is not found
                kb = await _dbContext.KnowledgeBases
                    .FirstOrDefaultAsync(k => k.IsActive, cancellationToken);
            }

            if (kb == null)
            {
                _logger.LogWarning("No active knowledge base found for homepage chat.");
                return Ok(new HomepageChatMessageResponse
                {
                    Success = true,
                    SessionId = request.SessionId,
                    Answer = _options.FallbackMessage,
                    Citations = new List<HomepageCitationDto>(),
                    IsFallback = true
                });
            }

            try
            {
                var result = await _ragChatService.AskQuestionAsync(
                    kb.Id,
                    cleanMessage,
                    request.SessionId,
                    userId: null,
                    cancellationToken: cancellationToken,
                    includeCitations: false);

                var isFallback = !result.IsSuccess
                    || string.IsNullOrWhiteSpace(result.Answer)
                    || result.Answer.Contains(RagChatService.DefaultNotFoundMessage, StringComparison.OrdinalIgnoreCase);

                var cleanAnswer = isFallback 
                    ? _options.FallbackMessage 
                    : RagChatService.StripCitationReferences(result.Answer);

                return Ok(new HomepageChatMessageResponse
                {
                    Success = true,
                    SessionId = result.SessionId > 0 ? result.SessionId : request.SessionId,
                    MessageId = result.MessageId > 0 ? result.MessageId : (int?)null,
                    Answer = cleanAnswer,
                    Citations = new List<HomepageCitationDto>(),
                    IsFallback = isFallback,
                    ErrorMessage = !result.IsSuccess ? result.ErrorMessage : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing public chat message for KB {KbId}", kb.Id);
                return Ok(new HomepageChatMessageResponse
                {
                    Success = true,
                    SessionId = request.SessionId,
                    Answer = _options.FallbackMessage,
                    Citations = new List<HomepageCitationDto>(),
                    IsFallback = true,
                    ErrorMessage = "An internal error occurred."
                });
            }
        }

        // POST: /api/homepage-chat/transcribe
        [HttpPost("transcribe")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> TranscribeAudio([FromForm] IFormFile? file, CancellationToken cancellationToken)
        {
            if (!_options.Enabled)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new HomepageVoiceTranscribeResponse
                {
                    Success = false,
                    ErrorMessage = "Homepage chat is currently disabled."
                });
            }

            if (!_options.EnableVoiceInput)
            {
                return BadRequest(new HomepageVoiceTranscribeResponse
                {
                    Success = false,
                    ErrorMessage = "Voice input is currently disabled."
                });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new HomepageVoiceTranscribeResponse
                {
                    Success = false,
                    ErrorMessage = "Audio file is required."
                });
            }

            var maxSizeBytes = (long)_options.MaxAudioSizeMB * 1024 * 1024;
            if (file.Length > maxSizeBytes)
            {
                return BadRequest(new HomepageVoiceTranscribeResponse
                {
                    Success = false,
                    ErrorMessage = $"Audio file exceeds the maximum allowed size of {_options.MaxAudioSizeMB} MB."
                });
            }

            // Validate audio content type / extension
            var contentType = file.ContentType ?? string.Empty;
            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
            var isAudio = contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)
                || ext is ".webm" or ".ogg" or ".wav" or ".mp3" or ".m4a" or ".mp4";

            if (!isAudio)
            {
                return BadRequest(new HomepageVoiceTranscribeResponse
                {
                    Success = false,
                    ErrorMessage = "Unsupported file format. Please submit a valid audio recording."
                });
            }

            try
            {
                // Raw audio is streamed directly in-memory to OpenAI without being stored on disk or DB
                using var stream = file.OpenReadStream();
                var result = await _transcriptionService.TranscribeAudioAsync(
                    stream,
                    file.FileName ?? "recording.webm",
                    contentType,
                    cancellationToken);

                if (!result.IsSuccess)
                {
                    return BadRequest(new HomepageVoiceTranscribeResponse
                    {
                        Success = false,
                        ErrorMessage = result.ErrorMessage ?? "Speech transcription failed."
                    });
                }

                return Ok(new HomepageVoiceTranscribeResponse
                {
                    Success = true,
                    Transcript = result.Text
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing audio transcription.");
                return StatusCode(StatusCodes.Status500InternalServerError, new HomepageVoiceTranscribeResponse
                {
                    Success = false,
                    ErrorMessage = "An error occurred during audio transcription."
                });
            }
        }
    }
}
