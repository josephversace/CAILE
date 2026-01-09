namespace IIM.Ingestion.Services;

public static class IngestionStepIds
{
	public const string CoreDedupCheck = "core.dedup.check";
	public const string CoreFileRead = "core.file.read";

	public const string MetaExifFast = "meta.exif.fast";

	public const string DocExtractText = "doc.extract.text";
	public const string DocShapeDetect = "doc.shape.detect";
	public const string AiTextAnalysis = "ai.text.analysis";

	public const string ExcelStructureDetect = "excel.structure.detect";
	public const string ExcelCanonicalize = "excel.canonicalize.tabletext";

	public const string ChunkBuild = "chunk.build";
	public const string EmbedIndexQdrant = "embed.index.qdrant";

	public const string IocRegexExtract = "ioc.regex.extract";

	public const string AiImageDescribe = "ai.image.describe";

	// Future examples:
	public const string VisionFaceExtract = "vision.face.extract";
	public const string VisionFaceEmbed = "vision.face.embed";
}
