using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IIM.Shared.Models;


public class DoclingResult
{
	// Primary content formats
	public string Markdown { get; set; } = "";
	public string Text { get; set; } = "";
	public string Html { get; set; } = "";

	// Full structured document (if requested)
	public DoclingDocument? Document { get; set; }

	// Processing info
	public string Status { get; set; } = "";
	public double ProcessingTimeSeconds { get; set; }
	public List<string> Errors { get; set; } = [];

	// Quick stats
	public int PageCount { get; set; }
	public int TextBlockCount { get; set; }
	public int TableCount { get; set; }
	public int PictureCount { get; set; }

	// Convenience properties
	public bool IsSuccess => Status == "success" || Status == "partial_success";
	public int BlockCount => TextBlockCount + TableCount + PictureCount;
}
/// <summary>
/// Response from docling-serve /v1/convert/file or /v1/convert/source
/// </summary>
public class DoclingResponse
{
	[JsonPropertyName("document")]
	public DoclingDocumentOutput? Document { get; set; }

	[JsonPropertyName("status")]
	public string Status { get; set; } = "";

	[JsonPropertyName("processing_time")]
	public double ProcessingTime { get; set; }

	[JsonPropertyName("timings")]
	public Dictionary<string, double>? Timings { get; set; }

	[JsonPropertyName("errors")]
	public List<string>? Errors { get; set; }

	public bool IsSuccess => Status == "success" || Status == "partial_success";
}

public class DoclingDocumentOutput
{
	[JsonPropertyName("md_content")]
	public string MarkdownContent { get; set; } = "";

	[JsonPropertyName("json_content")]
	public DoclingDocument? JsonContent { get; set; }

	[JsonPropertyName("html_content")]
	public string HtmlContent { get; set; } = "";

	[JsonPropertyName("text_content")]
	public string TextContent { get; set; } = "";

	[JsonPropertyName("doctags_content")]
	public string DocTagsContent { get; set; } = "";
}

/// <summary>
/// The DoclingDocument schema from docling-core
/// </summary>
public class DoclingDocument
{
	[JsonPropertyName("schema_name")]
	public string SchemaName { get; set; } = "DoclingDocument";

	[JsonPropertyName("version")]
	public string Version { get; set; } = "";

	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("origin")]
	public DocumentOrigin? Origin { get; set; }

	[JsonPropertyName("furniture")]
	public DocumentNode? Furniture { get; set; }

	[JsonPropertyName("body")]
	public DocumentNode? Body { get; set; }

	[JsonPropertyName("groups")]
	public List<GroupItem>? Groups { get; set; }

	[JsonPropertyName("texts")]
	public List<TextItem>? Texts { get; set; }

	[JsonPropertyName("tables")]
	public List<TableItem>? Tables { get; set; }

	[JsonPropertyName("pictures")]
	public List<PictureItem>? Pictures { get; set; }

	[JsonPropertyName("pages")]
	public Dictionary<string, PageItem>? Pages { get; set; }
}

public class DocumentOrigin
{
	[JsonPropertyName("mimetype")]
	public string MimeType { get; set; } = "";

	[JsonPropertyName("binary_hash")]
	public ulong BinaryHash { get; set; }

	[JsonPropertyName("filename")]
	public string Filename { get; set; } = "";
}

public class DocumentNode
{
	[JsonPropertyName("self_ref")]
	public string SelfRef { get; set; } = "";

	[JsonPropertyName("children")]
	public List<JsonReference>? Children { get; set; }

	[JsonPropertyName("content_layer")]
	public string? ContentLayer { get; set; }

	[JsonPropertyName("name")]
	public string? Name { get; set; }

	[JsonPropertyName("label")]
	public string? Label { get; set; }
}

public class JsonReference
{
	[JsonPropertyName("$ref")]
	public string Ref { get; set; } = "";
}

public class DocItem
{
	[JsonPropertyName("self_ref")]
	public string SelfRef { get; set; } = "";

	[JsonPropertyName("parent")]
	public JsonReference? Parent { get; set; }

	[JsonPropertyName("children")]
	public List<JsonReference>? Children { get; set; }

	[JsonPropertyName("label")]
	public string Label { get; set; } = "";

	[JsonPropertyName("prov")]
	public List<ProvenanceItem>? Provenance { get; set; }
}

public class TextItem : DocItem
{
	[JsonPropertyName("orig")]
	public string Original { get; set; } = "";

	[JsonPropertyName("text")]
	public string Text { get; set; } = "";
}

public class GroupItem : DocItem
{
	[JsonPropertyName("name")]
	public string? Name { get; set; }
}

public class TableItem : DocItem
{
	[JsonPropertyName("data")]
	public TableData? Data { get; set; }
}

public class TableData
{
	[JsonPropertyName("num_rows")]
	public int NumRows { get; set; }

	[JsonPropertyName("num_cols")]
	public int NumCols { get; set; }

	[JsonPropertyName("table_cells")]
	public List<TableCell>? Cells { get; set; }
}

public class TableCell
{
	[JsonPropertyName("row_span")]
	public int RowSpan { get; set; } = 1;

	[JsonPropertyName("col_span")]
	public int ColSpan { get; set; } = 1;

	[JsonPropertyName("start_row_offset_idx")]
	public int StartRow { get; set; }

	[JsonPropertyName("start_col_offset_idx")]
	public int StartCol { get; set; }

	[JsonPropertyName("text")]
	public string Text { get; set; } = "";
}

public class PictureItem : DocItem
{
	[JsonPropertyName("image")]
	public ImageRef? Image { get; set; }
}

public class ImageRef
{
	[JsonPropertyName("mimetype")]
	public string? MimeType { get; set; }

	[JsonPropertyName("dpi")]
	public int? Dpi { get; set; }

	[JsonPropertyName("size")]
	public ImageSize? Size { get; set; }
}

public class ImageSize
{
	[JsonPropertyName("width")]
	public int Width { get; set; }

	[JsonPropertyName("height")]
	public int Height { get; set; }
}

public class PageItem
{
	[JsonPropertyName("page_no")]
	public int PageNumber { get; set; }

	[JsonPropertyName("size")]
	public PageSize? Size { get; set; }
}

public class PageSize
{
	[JsonPropertyName("width")]
	public double Width { get; set; }

	[JsonPropertyName("height")]
	public double Height { get; set; }
}

public class ProvenanceItem
{
	[JsonPropertyName("page_no")]
	public int PageNumber { get; set; }

	[JsonPropertyName("bbox")]
	public BoundingBox? BoundingBox { get; set; }

	[JsonPropertyName("charspan")]
	public List<int>? CharSpan { get; set; }
}

public class BoundingBox
{
	[JsonPropertyName("l")]
	public double Left { get; set; }

	[JsonPropertyName("t")]
	public double Top { get; set; }

	[JsonPropertyName("r")]
	public double Right { get; set; }

	[JsonPropertyName("b")]
	public double Bottom { get; set; }

	[JsonPropertyName("coord_origin")]
	public string? CoordOrigin { get; set; }
}