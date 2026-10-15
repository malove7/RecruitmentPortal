using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecruitmentPortal.Data;
using RecruitmentPortal.Models;
using RecruitmentPortal.Models.DTOs;
using RecruitmentPortal.Models.Enums;

namespace RecruitmentPortal.Services
{
    public class KnowledgeBaseIngestionService : IKnowledgeBaseIngestionService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IDocumentTextExtractor _textExtractor;
        private readonly ITextChunkingService _chunkingService;
        private readonly IOpenAIEmbeddingService _embeddingService;
        private readonly IPineconeVectorService _pineconeService;
        private readonly KnowledgeBaseOptions _kbOptions;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<KnowledgeBaseIngestionService> _logger;

        private static readonly string[] AllowedExtensions = { ".pdf", ".docx" };

        public KnowledgeBaseIngestionService(
            ApplicationDbContext dbContext,
            IDocumentTextExtractor textExtractor,
            ITextChunkingService chunkingService,
            IOpenAIEmbeddingService embeddingService,
            IPineconeVectorService pineconeService,
            IOptions<KnowledgeBaseOptions> kbOptions,
            IWebHostEnvironment environment,
            ILogger<KnowledgeBaseIngestionService> logger)
        {
            _dbContext = dbContext;
            _textExtractor = textExtractor;
            _chunkingService = chunkingService;
            _embeddingService = embeddingService;
            _pineconeService = pineconeService;
            _kbOptions = kbOptions.Value;
            _environment = environment;
            _logger = logger;
        }

        public async Task<IngestionResult> IngestDocumentAsync(int knowledgeBaseId, IFormFile file, string? userId = null, CancellationToken cancellationToken = default)
        {
            var fileName = file?.FileName ?? string.Empty;

            if (file == null || file.Length == 0)
            {
                return new IngestionResult
                {
                    IsSuccess = false,
                    FileName = fileName,
                    ErrorMessage = "The uploaded file is empty."
                };
            }

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return new IngestionResult
                {
                    IsSuccess = false,
                    FileName = fileName,
                    ErrorMessage = $"Unsupported file format '{extension}'. Only PDF and DOCX files are supported."
                };
            }

            var maxBytes = (_kbOptions.MaxFileSizeMB > 0 ? _kbOptions.MaxFileSizeMB : 25) * 1024L * 1024L;
            if (file.Length > maxBytes)
            {
                return new IngestionResult
                {
                    IsSuccess = false,
                    FileName = fileName,
                    ErrorMessage = $"File size ({file.Length / (1024.0 * 1024.0):F1} MB) exceeds maximum allowed limit of {_kbOptions.MaxFileSizeMB} MB."
                };
            }

            var kb = await _dbContext.KnowledgeBases.FirstOrDefaultAsync(k => k.Id == knowledgeBaseId, cancellationToken);
            if (kb == null)
            {
                return new IngestionResult
                {
                    IsSuccess = false,
                    FileName = fileName,
                    ErrorMessage = $"Knowledge base with ID {knowledgeBaseId} was not found."
                };
            }

            // Save file locally to App_Data/KnowledgeBaseFiles
            var storageDir = Path.Combine(_environment.ContentRootPath, _kbOptions.StoragePath ?? "App_Data/KnowledgeBaseFiles");
            Directory.CreateDirectory(storageDir);

            var safeStorageName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
            var filePath = Path.Combine(storageDir, safeStorageName);

            await using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var document = new KnowledgeBaseDocument
            {
                KnowledgeBaseId = knowledgeBaseId,
                FileName = fileName,
                StorageFilePath = filePath,
                FileExtension = extension,
                FileSizeBytes = file.Length,
                ContentType = file.ContentType,
                Status = KnowledgeBaseDocumentStatus.Uploaded,
                UploadedByUserId = userId,
                UploadedAt = DateTime.UtcNow
            };

            _dbContext.KnowledgeBaseDocuments.Add(document);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Execute indexing pipeline
            return await ProcessDocumentIndexingAsync(document, cancellationToken);
        }

        public async Task<IngestionResult> ReindexDocumentAsync(int documentId, CancellationToken cancellationToken = default)
        {
            var document = await _dbContext.KnowledgeBaseDocuments
                .Include(d => d.Chunks)
                .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

            if (document == null)
            {
                return new IngestionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Document with ID {documentId} was not found."
                };
            }

            if (!File.Exists(document.StorageFilePath))
            {
                document.Status = KnowledgeBaseDocumentStatus.Failed;
                document.ErrorMessage = "Source document file not found on local storage.";
                await _dbContext.SaveChangesAsync(cancellationToken);

                return new IngestionResult
                {
                    IsSuccess = false,
                    DocumentId = document.Id,
                    FileName = document.FileName,
                    ErrorMessage = document.ErrorMessage
                };
            }

            // Remove existing Pinecone vectors if any
            var namespaceName = _pineconeService.BuildNamespace(document.KnowledgeBaseId);
            var existingVectorIds = document.Chunks
                .Where(c => !string.IsNullOrEmpty(c.VectorId))
                .Select(c => c.VectorId!)
                .ToList();

            if (existingVectorIds.Count > 0)
            {
                try
                {
                    await _pineconeService.DeleteVectorsAsync(namespaceName, existingVectorIds, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove previous Pinecone vectors for document {DocId}", documentId);
                }
            }

            // Remove existing chunks from SQL
            if (document.Chunks.Count > 0)
            {
                _dbContext.KnowledgeBaseChunks.RemoveRange(document.Chunks);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            document.Status = KnowledgeBaseDocumentStatus.Uploaded;
            document.ErrorMessage = null;
            document.ChunkCount = 0;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return await ProcessDocumentIndexingAsync(document, cancellationToken);
        }

        private async Task<IngestionResult> ProcessDocumentIndexingAsync(KnowledgeBaseDocument document, CancellationToken cancellationToken)
        {
            try
            {
                // Step 1: Parsing
                document.Status = KnowledgeBaseDocumentStatus.Parsing;
                await _dbContext.SaveChangesAsync(cancellationToken);

                var extraction = await _textExtractor.ExtractAsync(document.StorageFilePath, cancellationToken);
                if (!extraction.IsSuccess)
                {
                    throw new InvalidOperationException(extraction.ErrorMessage ?? "Document text extraction failed.");
                }

                document.Status = KnowledgeBaseDocumentStatus.Parsed;
                document.ExtractedCharCount = extraction.FullText.Length;
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Step 2: Chunking
                var chunkSize = _kbOptions.ChunkSizeTokens > 0 ? _kbOptions.ChunkSizeTokens : 800;
                var chunkOverlap = _kbOptions.ChunkOverlapTokens >= 0 ? _kbOptions.ChunkOverlapTokens : 120;
                var chunks = _chunkingService.ChunkDocument(extraction, chunkSize, chunkOverlap);

                if (chunks.Count == 0)
                {
                    throw new InvalidOperationException("No readable text chunks could be produced from document.");
                }

                // Step 3: Save chunks in SQL
                var chunkEntities = new List<KnowledgeBaseChunk>();
                foreach (var chunk in chunks)
                {
                    var entity = new KnowledgeBaseChunk
                    {
                        KnowledgeBaseDocumentId = document.Id,
                        ChunkIndex = chunk.ChunkIndex,
                        Content = chunk.Content,
                        PageNumber = chunk.PageNumber,
                        TokenCount = chunk.TokenCount,
                        CreatedAt = DateTime.UtcNow
                    };
                    chunkEntities.Add(entity);
                }

                _dbContext.KnowledgeBaseChunks.AddRange(chunkEntities);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Step 4: Indexing (Generate OpenAI Embeddings)
                document.Status = KnowledgeBaseDocumentStatus.Indexing;
                await _dbContext.SaveChangesAsync(cancellationToken);

                var textsToEmbed = chunkEntities.Select(c => c.Content).ToList();
                var embeddings = await _embeddingService.GenerateEmbeddingsBatchAsync(textsToEmbed, cancellationToken);

                if (embeddings.Count != chunkEntities.Count)
                {
                    throw new InvalidOperationException($"Mismatch between generated embeddings ({embeddings.Count}) and chunks ({chunkEntities.Count}).");
                }

                // Step 5: Upsert to Pinecone
                var namespaceName = _pineconeService.BuildNamespace(document.KnowledgeBaseId);
                var vectorRecords = new List<PineconeVectorRecord>();

                for (int i = 0; i < chunkEntities.Count; i++)
                {
                    var entity = chunkEntities[i];
                    var vectorId = _pineconeService.BuildVectorId(document.KnowledgeBaseId, document.Id, entity.ChunkIndex);
                    entity.VectorId = vectorId;

                    vectorRecords.Add(new PineconeVectorRecord
                    {
                        Id = vectorId,
                        Values = embeddings[i],
                        Metadata = new Dictionary<string, object>
                        {
                            ["knowledgeBaseId"] = document.KnowledgeBaseId,
                            ["documentId"] = document.Id,
                            ["chunkId"] = entity.Id,
                            ["fileName"] = document.FileName,
                            ["pageNumber"] = entity.PageNumber ?? 1,
                            ["chunkIndex"] = entity.ChunkIndex
                        }
                    });
                }

                await _pineconeService.UpsertVectorsAsync(namespaceName, vectorRecords, cancellationToken);

                // Step 6: Mark Indexed
                document.Status = KnowledgeBaseDocumentStatus.Indexed;
                document.IndexedAt = DateTime.UtcNow;
                document.ErrorMessage = null;
                document.ChunkCount = chunkEntities.Count;
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Document {DocId} ('{FileName}') indexed successfully with {ChunkCount} chunks.",
                    document.Id, document.FileName, document.ChunkCount);

                return new IngestionResult
                {
                    IsSuccess = true,
                    DocumentId = document.Id,
                    FileName = document.FileName,
                    ChunkCount = document.ChunkCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ingest document {DocId} ('{FileName}')", document.Id, document.FileName);

                document.Status = KnowledgeBaseDocumentStatus.Failed;
                document.ErrorMessage = ex.Message;
                await _dbContext.SaveChangesAsync(cancellationToken);

                return new IngestionResult
                {
                    IsSuccess = false,
                    DocumentId = document.Id,
                    FileName = document.FileName,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task DeleteDocumentAsync(int documentId, CancellationToken cancellationToken = default)
        {
            var document = await _dbContext.KnowledgeBaseDocuments
                .Include(d => d.Chunks)
                .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

            if (document == null)
                return;

            // Delete vectors from Pinecone
            var namespaceName = _pineconeService.BuildNamespace(document.KnowledgeBaseId);
            var vectorIds = document.Chunks
                .Where(c => !string.IsNullOrEmpty(c.VectorId))
                .Select(c => c.VectorId!)
                .ToList();

            if (vectorIds.Count > 0)
            {
                try
                {
                    await _pineconeService.DeleteVectorsAsync(namespaceName, vectorIds, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete Pinecone vectors for document {DocId}", documentId);
                }
            }

            // Delete local file
            try
            {
                if (File.Exists(document.StorageFilePath))
                {
                    File.Delete(document.StorageFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete local file for document {DocId}", documentId);
            }

            _dbContext.KnowledgeBaseDocuments.Remove(document);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteKnowledgeBaseAsync(int knowledgeBaseId, CancellationToken cancellationToken = default)
        {
            var kb = await _dbContext.KnowledgeBases
                .Include(k => k.Documents)
                .FirstOrDefaultAsync(k => k.Id == knowledgeBaseId, cancellationToken);

            if (kb == null)
                return;

            // Delete entire Pinecone namespace
            var namespaceName = _pineconeService.BuildNamespace(knowledgeBaseId);
            try
            {
                await _pineconeService.DeleteNamespaceAsync(namespaceName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete Pinecone namespace for KB {KbId}", knowledgeBaseId);
            }

            // Delete local files
            foreach (var doc in kb.Documents)
            {
                try
                {
                    if (File.Exists(doc.StorageFilePath))
                    {
                        File.Delete(doc.StorageFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete local file for document {DocId}", doc.Id);
                }
            }

            _dbContext.KnowledgeBases.Remove(kb);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
