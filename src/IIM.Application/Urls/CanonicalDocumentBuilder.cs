using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SmartReader;
using IIM.Shared.Models;

namespace IIM.Application.Urls
{
	public interface ICanonicalDocumentBuilder
	{
		CanonicalDocument Build(
			string sourceUrl,
			string? title,
			Article? article,
			AriaTree? aria,
			DocumentShapeResult shape,
			DoclingDocument? docling = null);
	}

	public sealed class CanonicalDocumentBuilder : ICanonicalDocumentBuilder
	{
		public CanonicalDocument Build(
			string sourceUrl,
			string? title,
			Article? article,
			AriaTree? aria,
			DocumentShapeResult shape,
			DoclingDocument? docling = null)
		{
			// 1. Determine the "Skeleton" of the document
			IReadOnlyList<CanonicalSection> sections;

			if (shape.Sections?.Any() == true)
			{
				// Priority 1: Use the Shape Detector's detected numeric/structural sections
				sections = BuildFromDetectedSections(article?.TextContent ?? "", shape.Sections, docling);
			}
			else if (aria?.Headings.Any() == true)
			{
				// Priority 2: Use ARIA Snapshot hierarchy as the table of contents
				sections = BuildFromAria(aria, docling, article?.TextContent);
			}
			else
			{
				// Priority 3: Narrative fallback (standard article)
				sections = BuildNarrative(article, docling);
			}

			// 2. Assemble the final Markdown
			var markdown = RenderMarkdown(title ?? article?.Title, sections, sourceUrl);

			return new CanonicalDocument
			{
				Title = title ?? article?.Title ?? ExtractTitle(sections),
				SourceUrl = sourceUrl,
				Sections = sections,
				Markdown = markdown
			};
		}

		// ─────────────────────────────────────────────────────────────
		// STRATEGY: SECTIONED (Using Shape Detector Offsets)
		// ─────────────────────────────────────────────────────────────

		private static IReadOnlyList<CanonicalSection> BuildFromDetectedSections(
			string rawText,
			IReadOnlyList<DocumentSection> detectedSections,
			DoclingDocument? docling)
		{
			var result = new List<CanonicalSection>();
			var allBlocks = docling?.Pages.SelectMany(p => p.Blocks).ToList() ?? new List<DoclingBlock>();

			foreach (var s in detectedSections)
			{
				// Attempt to find high-fidelity blocks from Docling that match this section
				var content = ExtractDoclingContentForHeading(s.Header, allBlocks);

				// Fallback to slicing the raw text if Docling didn't provide matching blocks
				if (string.IsNullOrWhiteSpace(content))
				{
					content = Slice(rawText, s.StartOffset, s.EndOffset);
				}

				result.Add(new CanonicalSection
				{
					Heading = s.Header,
					Level = InferLevel(s.Header),
					Content = Normalize(content),
					Source = docling != null ? "hybrid-docling" : "smartreader"
				});
			}

			return result;
		}

		// ─────────────────────────────────────────────────────────────
		// STRATEGY: ARIA (Using Browser Accessibility Tree)
		// ─────────────────────────────────────────────────────────────

		private static IReadOnlyList<CanonicalSection> BuildFromAria(
			AriaTree aria,
			DoclingDocument? docling,
			string? fallbackText)
		{
			var result = new List<CanonicalSection>();
			var allBlocks = docling?.Pages.SelectMany(p => p.Blocks).ToList() ?? new List<DoclingBlock>();

			foreach (var h in aria.Headings)
			{
				var content = ExtractDoclingContentForHeading(h.Text, allBlocks);

				result.Add(new CanonicalSection
				{
					Heading = h.Text,
					Level = h.Level,
					Content = Normalize(content),
					Source = "aria-docling"
				});
			}

			// If we have text but no content was mapped to headings, attach text to the first section
			if (result.Any() && string.IsNullOrEmpty(result[0].Content))
			{
				result[0] = result[0] with { Content = Normalize(fallbackText ?? "") };
			}

			return result;
		}

		// ─────────────────────────────────────────────────────────────
		// STRATEGY: NARRATIVE (Standard Article Fallback)
		// ─────────────────────────────────────────────────────────────

		private static IReadOnlyList<CanonicalSection> BuildNarrative(Article? article, DoclingDocument? docling)
		{
			// If Docling provided a full markdown export, use it as the single narrative section
			string content = docling?.Markdown ?? article?.TextContent ?? "No content extracted.";

			return new List<CanonicalSection>
			{
				new CanonicalSection
				{
					Heading = article?.Title ?? "Main Content",
					Level = 1,
					Content = Normalize(content),
					Source = docling != null ? "docling-markdown" : "smartreader"
				}
			};
		}

		// ─────────────────────────────────────────────────────────────
		// HELPERS & RENDERING
		// ─────────────────────────────────────────────────────────────

		private static string ExtractDoclingContentForHeading(string header, List<DoclingBlock> blocks)
		{
			// Logic: Find blocks where 'section_heading' matches our current header
			// and return the sequence of blocks until the next heading role appears.
			var relevantBlocks = blocks
				.Where(b => b.SectionHeading?.Equals(header, StringComparison.OrdinalIgnoreCase) == true)
				.Select(b => b.Markdown);

			return string.Join("\n\n", relevantBlocks);
		}

		private static string RenderMarkdown(string? title, IReadOnlyList<CanonicalSection> sections, string url)
		{
			var sb = new StringBuilder();

			if (!string.IsNullOrWhiteSpace(title))
			{
				sb.AppendLine("# " + title.Trim());
				sb.AppendLine($"> **Source:** [{url}]({url})");
				sb.AppendLine();
			}

			foreach (var s in sections)
			{
				// Ensure heading level is between 1 and 6
				int level = Math.Clamp(s.Level, 1, 6);
				sb.AppendLine($"{new string('#', level)} {s.Heading}");
				sb.AppendLine();
				sb.AppendLine(s.Content);
				sb.AppendLine();
			}

			return sb.ToString().Trim();
		}

		private static string Slice(string text, int start, int end)
		{
			if (string.IsNullOrEmpty(text)) return "";
			start = Math.Clamp(start, 0, text.Length);
			end = Math.Clamp(end, start, text.Length);
			return text.Substring(start, end - start);
		}

		private static string Normalize(string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return "";

			return string.Join(
				"\n",
				text.Split('\n')
					.Select(l => l.TrimEnd())
					.Where(l => !string.IsNullOrWhiteSpace(l))
			);
		}

		private static int InferLevel(string header)
		{
			// Logic: Count dots in "1.2.3" to determine depth
			var dots = header.TakeWhile(c => char.IsDigit(c) || c == '.')
							 .Count(c => c == '.');
			return Math.Clamp(dots + 1, 1, 6);
		}

		private static string ExtractTitle(IReadOnlyList<CanonicalSection> sections)
		{
			return sections.FirstOrDefault()?.Heading ?? "Document";
		}
	}
}