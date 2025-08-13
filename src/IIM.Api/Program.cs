
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net.Http.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("qdrant", c => c.BaseAddress = new Uri(builder.Configuration["Qdrant:BaseUrl"] ?? "http://localhost:6333"));
builder.Services.AddHttpClient("embed",  c => c.BaseAddress = new Uri(builder.Configuration["EmbedService:BaseUrl"] ?? "http://localhost:8081"));

var app = builder.Build();

app.MapGet("/healthz", () => Results.Text("ok"));
app.MapGet("/v1/gpu", () => Results.Json(new { vendor = "AMD/NVIDIA/CPU", provider = "directml", vramGb = 8 }));

var running = new Dictionary<string, object>();
app.MapPost("/v1/run", (string id, string modelPath, string engine, string provider) => { running[id] = new { modelPath, engine, provider }; return Results.Ok(new { started = id, engine, provider }); });
app.MapPost("/v1/generate", (HttpRequest req) => {
    var id = req.Query["id"].ToString();
    var prompt = req.Query["prompt"].ToString();
    if (!running.ContainsKey(id)) return Results.NotFound();
    return Results.Json(new { text = $"[IIM MOCK] Answer to: {prompt}" });
});
app.MapPost("/v1/stop-all", () => { running.Clear(); return Results.Ok(); });

// --- RAG components ---
var collection = "rag_index";

app.MapPost("/rag/index", async (HttpRequest req, IHttpClientFactory f) =>
{
    var mode = req.Query["mode"].ToString();
    var useSentences = string.Equals(mode, "sentences", StringComparison.OrdinalIgnoreCase);

    var body = await JsonSerializer.DeserializeAsync<IndexBody>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new IndexBody();
    var chunks = useSentences ? ChunkBySentences(body.text ?? "") : ChunkFixed(body.text ?? "");

    foreach (var ch in chunks) {
        var vec = await EmbedAsync(f, ch.text);
        await UpsertAsync(f, collection, body.id ?? Guid.NewGuid().ToString("N"), vec, new Dictionary<string, object>{{"chunk", ch.text}});
    }
    return Results.Ok(new { status = "indexed", mode = useSentences ? "sentences" : "fixed" });
});

app.MapPost("/rag/upload", async (HttpRequest req, IHttpClientFactory f) =>
{
    var mode = req.Query["mode"].ToString();
    var useSentences = string.Equals(mode, "sentences", StringComparison.OrdinalIgnoreCase);

    if (!req.HasFormContentType) return Results.BadRequest("multipart/form-data required");
    var form = await req.ReadFormAsync();
    int count = 0;
    foreach (var file in form.Files)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext not in [".txt", ".md", ".pdf", ".docx"]) continue;

        string text;
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        ms.Position = 0;
        if (ext == ".pdf") text = ExtractTextFromPdf(ms);
        else if (ext == ".docx") text = ExtractTextFromDocx(ms);
        else {
            using var sr = new StreamReader(ms, Encoding.UTF8, true);
            text = await sr.ReadToEndAsync();
        }

        var chunks = useSentences ? ChunkBySentences(text) : ChunkFixed(text);
        int idx = 0;
        foreach (var ch in chunks)
        {
            var vec = await EmbedAsync(f, ch.text);
            await UpsertAsync(f, collection, $"{file.FileName}:{idx++}", vec, new Dictionary<string, object>{{"file", file.FileName},{"chunk", ch.text}});
        }
        count++;
    }
    return Results.Ok(new { status = "indexed", files = count, mode = useSentences ? "sentences" : "fixed" });
});

app.MapPost("/agents/rag/query", async (HttpRequest req, IHttpClientFactory f) =>
{
    var body = await JsonSerializer.DeserializeAsync<QueryBody>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new QueryBody();
    var qv = await EmbedAsync(f, body.query ?? "");
    var hits = await SearchAsync(f, collection, qv, body.k > 0 ? body.k : 3);
    var context = string.Join("\n---\n", hits.Select(h => h.payload.TryGetValue("chunk", out var c) ? c?.ToString() : ""));
    var answer = $"[RAG MOCK]\nQ: {body.query}\nA (grounded on {hits.Count} chunks):\n{context}";
    var citations = hits.Select(h => h.id).ToList();
    return Results.Json(new { answer, citations });
});

// Helpers
static IEnumerable<(int idx, string text)> ChunkFixed(string text, int maxLen=800, int overlap=100)
{
    var clean = Regex.Replace(text ?? "", "\s+", " ").Trim();
    int i=0; int idx=0;
    while (i < clean.Length) {
        int end = Math.Min(i + maxLen, clean.Length);
        string slice = clean[i:end];
        yield return (idx++, slice);
        if (end == clean.Length) break;
        i = end - overlap;
        if (i < 0) i = 0;
    }
}

static IEnumerable<(int idx, string text)> ChunkBySentences(string text, int maxChars=800)
{
    var sents = Regex.Split(text ?? "", r"(?<=[\.!?])\s+");
    var sb = new StringBuilder();
    int idx=0;
    foreach (var s in sents)
    {
        if (sb.Length + s.Length + 1 > maxChars && sb.Length > 0) {
            yield return (idx++, sb.ToString().Trim());
            sb.Clear();
        }
        sb.Append(s).Append(' ');
    }
    if (sb.Length > 0) yield return (idx++, sb.ToString().Trim());
}

static string ExtractTextFromPdf(Stream stream)
{
    var sb = new StringBuilder();
    using (var doc = PdfDocument.Open(stream))
    {
        foreach (var page in doc.GetPages())
            sb.AppendLine(page.Text);
    }
    return sb.ToString();
}

static string ExtractTextFromDocx(Stream stream)
{
    var sb = new StringBuilder();
    using (var doc = WordprocessingDocument.Open(stream, false))
    {
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body != null)
        {
            foreach (var para in body.Descendants<Paragraph>())
                sb.AppendLine(string.Concat(para.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text)));
        }
    }
    return sb.ToString();
}

static async Task<float[]> EmbedAsync(IHttpClientFactory f, string text)
{
    var http = f.CreateClient("embed");
    var res = await http.PostAsJsonAsync("/embed", new { text });
    res.EnsureSuccessStatusCode();
    return await res.Content.ReadFromJsonAsync<float[]>() ?? Array.Empty<float>();
}

static async Task UpsertAsync(IHttpClientFactory f, string collection, string id, float[] vec, Dictionary<string, object> payload)
{
    var http = f.CreateClient("qdrant");
    // create collection (idempotent)
    var create = new { vectors = new { size = vec.Length, distance = "Cosine" } };
    await http.PutAsJsonAsync($"/collections/{collection}", create);
    // upsert
    var body = new { points = new[] { new { id, vector = vec, payload } } };
    await http.PutAsJsonAsync($"/collections/{collection}/points", body);
}

static async Task<List<(string id, Dictionary<string, object> payload)>> SearchAsync(IHttpClientFactory f, string collection, float[] qv, int k)
{
    var http = f.CreateClient("qdrant");
    var body = new { vector = qv, limit = k, with_payload = true };
    var res = await http.PostAsJsonAsync($"/collections/{collection}/points/search", body);
    var json = await res.Content.ReadFromJsonAsync<QdrantSearchResponse>() ?? new();
    return json.result.Select(r => (r.id, r.payload ?? new())).ToList();
}

app.Run("http://localhost:5080");

// Records for JSON
record IndexBody(string id, string text);
record QueryBody(string query, int k);
record QdrantSearchResponse(List<Item> result) { public QdrantSearchResponse() : this(new()){} public class Item { public string id {get;set;}=string.Empty; public Dictionary<string,object>? payload {get;set;} } }
