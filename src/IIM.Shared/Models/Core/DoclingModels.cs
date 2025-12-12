using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IIM.Shared.Models;


// Models matching your Python DoclingDocument
public class DoclingDocument
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("title")]
	public string? Title { get; set; }

	[JsonPropertyName("pages")]
	public List<DoclingPage> Pages { get; set; } = new();

	[JsonPropertyName("images")]
	public List<DoclingImageInfo> Images { get; set; } = new();

	[JsonPropertyName("markdown")]
	public string? Markdown { get; set; }
}

public class DoclingPage
{
	[JsonPropertyName("page_number")]
	public int PageNumber { get; set; }

	[JsonPropertyName("blocks")]
	public List<DoclingBlock> Blocks { get; set; } = new();
}

public class DoclingBlock
{
	[JsonPropertyName("block_type")]
	public string BlockType { get; set; } = string.Empty;

	[JsonPropertyName("markdown")]
	public string Markdown { get; set; } = string.Empty;

	[JsonPropertyName("section_heading")]
	public string? SectionHeading { get; set; }

	[JsonPropertyName("role")]
	public string? Role { get; set; }
}

public class DoclingImageInfo
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("page_number")]
	public int PageNumber { get; set; }

	[JsonPropertyName("kind")]
	public string Kind { get; set; } = string.Empty;

	[JsonPropertyName("path")]
	public string Path { get; set; } = string.Empty;

	[JsonPropertyName("caption")]
	public string? Caption { get; set; }
}

/// <summary>
 /// Response from docling-serve /v1/convert/file or /v1/convert/source
 /// </summary>


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