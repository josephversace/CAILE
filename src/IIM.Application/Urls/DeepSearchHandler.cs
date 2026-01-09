using IIM.Application.Urls;
using IIM.Shared.Mediator;
using Microsoft.Extensions.Logging;

public record DeepSearchCommand(string Query, Guid WorkspaceId) : IRequest<List<Guid>>;

public class DeepSearchHandler : IRequestHandler<DeepSearchCommand, List<Guid>>
{
	private readonly ISearchService _search;
	private readonly IMediator _mediator;
	private readonly ILogger<DeepSearchHandler> _logger;

	public DeepSearchHandler(ISearchService search, IMediator mediator, ILogger<DeepSearchHandler> logger)
	{
		_search = search;
		_mediator = mediator;
		_logger = logger;
	}

	public async Task<List<Guid>> Handle(DeepSearchCommand request, CancellationToken ct)
	{
		// 1. Get URLs from SearXNG
		var results = await _search.SearchAsync(request.Query, limit: 3, ct);
		var ingestedIds = new List<Guid>();

		// 2. Process each URL through our existing IngestUrlCommand logic
		// This leverages Playwright for the Accessibility View and IFileStore for deduplication
		foreach (var result in results)
		{
			var ingestResult = await _mediator.Send(new IngestUrlCommand(result.Url, request.WorkspaceId), ct);
			if (ingestResult.Success && ingestResult.VirtualFileId.HasValue)
			{
				ingestedIds.Add(ingestResult.VirtualFileId.Value);
			}
		}

		return ingestedIds;
	}
}