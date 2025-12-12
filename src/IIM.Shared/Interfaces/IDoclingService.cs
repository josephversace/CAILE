using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Shared.Interfaces;

public interface IDoclingService
{
	Task<DoclingDocument> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default);
}