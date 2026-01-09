using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using IIM.Ingestion.Services;

namespace IIM.Ingestion.Services
{
	internal static class ExcelStructureDetectorInternal
	{
		// ============================================================
		// ENTRY
		// ============================================================

		public static WorkbookStructureResult Detect(
			Stream stream,
			string sourceName,
			ExcelStructureDetectorOptions? options,
			IStructureAuditSink? auditSink,
			CancellationToken ct)
		{
			options ??= ExcelStructureDetectorOptions.Default;
			auditSink ??= new NullStructureAuditSink();

			var diagnostics = new DetectionDiagnostics();
			var sw = System.Diagnostics.Stopwatch.StartNew();

			using var doc = SpreadsheetDocument.Open(stream, false);
			var result = DetectInternal(doc, sourceName, options, auditSink, diagnostics, ct);

			diagnostics.Duration = sw.Elapsed;
			return result with { Diagnostics = diagnostics };
		}

		// ============================================================
		// CORE PIPELINE
		// ============================================================

		private static WorkbookStructureResult DetectInternal(
			SpreadsheetDocument doc,
			string sourceName,
			ExcelStructureDetectorOptions options,
			IStructureAuditSink audit,
			DetectionDiagnostics diagnostics,
			CancellationToken ct)
		{
			var wbPart = doc.WorkbookPart
				?? throw new InvalidDataException("WorkbookPart missing.");

			var styles = wbPart.WorkbookStylesPart?.Stylesheet;
			var sst = new SharedStringCache(wbPart.SharedStringTablePart?.SharedStringTable);
			var dateFormats = styles != null ? new DateFormatCache(styles) : DateFormatCache.Empty;

			var sheets = new List<SheetStructureResult>();
			var usedBuilder = UsedRangeBuilder.Empty();

			foreach (var sheet in wbPart.Workbook.Sheets!.OfType<Sheet>())
			{
				ct.ThrowIfCancellationRequested();

				var wsPart = (WorksheetPart)wbPart.GetPartById(sheet.Id!);
				var ws = wsPart.Worksheet;
				var sheetName = sheet.Name?.Value ?? "Sheet";

				bool hasPivot = wsPart.PivotTableParts.Any();
				bool hasTables = wsPart.TableDefinitionParts.Any();
				bool hasAutoFilter = ws.Descendants<AutoFilter>().Any();

				var grid = RowIndexedSparseGrid.Build(
					wsPart, sst, styles, dateFormats, diagnostics, ct);

				usedBuilder.Include(grid.UsedRange);

				var regions = RegionDetector.Detect(grid, options, ct);
				regions = RegionSplitter.Split(grid, regions, options);

				var shape = SheetShapeClassifier.Classify(
					grid, regions, hasPivot, hasTables, hasAutoFilter);

				var tables = new List<TableStructureResult>();

				foreach (var tablePart in wsPart.TableDefinitionParts)
				{
					if (TryParseRange(tablePart.Table.Reference?.Value, out var tr))
					{
						tables.Add(TableAnalyzer.FromExplicit(
							sheetName, tr, tablePart.Table.Name?.Value, grid));
					}
				}

				foreach (var region in regions)
				{
					if (tables.Any(t => t.Range.Overlaps(region)))
						continue;

					tables.Add(TableAnalyzer.Analyze(
						sheetName, grid, region, shape, options, ct));
				}

				sheets.Add(new SheetStructureResult(
					sheetName,
					shape,
					grid.ToProfile(hasPivot, hasTables, hasAutoFilter),
					tables));
			}

			return new WorkbookStructureResult(
				sourceName,
				usedBuilder.ToUsedRange(),
				sheets,
				diagnostics);
		}

		// ============================================================
		// INTERNAL TYPES
		// ============================================================

		// ============================================================
		// USED RANGE BUILDER
		// ============================================================

		private sealed class UsedRangeBuilder
		{
			private int _minRow = int.MaxValue;
			private int _minCol = int.MaxValue;
			private int _maxRow = 0;
			private int _maxCol = 0;
			private int _nonEmpty = 0;

			public static UsedRangeBuilder Empty() => new();

			public void Include(UsedRange range)
			{
				if (range.NonEmptyCells <= 0)
					return;

				_minRow = Math.Min(_minRow, range.MinRow);
				_minCol = Math.Min(_minCol, range.MinCol);
				_maxRow = Math.Max(_maxRow, range.MaxRow);
				_maxCol = Math.Max(_maxCol, range.MaxCol);
				_nonEmpty += range.NonEmptyCells;
			}

			public UsedRange ToUsedRange()
			{
				return _nonEmpty <= 0
					? new UsedRange(0, 0, 0, 0, 0)
					: new UsedRange(_minRow, _minCol, _maxRow, _maxCol, _nonEmpty);
			}
		}


		private sealed record MergedRegion(CellRange Range);
		private sealed record AutoFilterHint(CellRange Range);

		private sealed class SharedStringCache
		{
			private readonly string[] _values;
			public SharedStringCache(SharedStringTable? sst)
				=> _values = sst?.Elements<SharedStringItem>()
					   .Select(x => x.InnerText ?? "")
					   .ToArray() ?? Array.Empty<string>();

			public string? Get(int i)
				=> i >= 0 && i < _values.Length ? _values[i] : null;
		}

		private sealed class DateFormatCache
		{
			private readonly HashSet<uint> _ids;
			public static DateFormatCache Empty { get; } = new(new HashSet<uint>());
			public DateFormatCache(Stylesheet styles)
			{
				_ids = new HashSet<uint> { 14, 15, 16, 17, 22 };
				if (styles.NumberingFormats == null) return;
				foreach (var nf in styles.NumberingFormats.Elements<NumberingFormat>())
				{
					if (nf.NumberFormatId != null &&
						nf.FormatCode?.Value?.ToLowerInvariant().Contains("yy") == true)
						_ids.Add(nf.NumberFormatId.Value);
				}
			}
			private DateFormatCache(HashSet<uint> ids) => _ids = ids;
			public bool IsDate(uint id) => _ids.Contains(id);
		}

		private sealed record CellValueInfo(
			bool IsEmpty,
			string? Text,
			double? Number,
			DateTime? DateValue);

		// ============================================================
		// GRID (streaming-safe)
		// ============================================================

		private sealed class RowIndexedSparseGrid
		{
			private readonly Dictionary<int, Dictionary<int, CellValueInfo>> _rows = new();

			public UsedRange UsedRange { get; private set; }
			public int NonEmptyCells => UsedRange.NonEmptyCells;
			public double FormulaRatio { get; private set; }

			public static RowIndexedSparseGrid Build(
				WorksheetPart wsPart,
				SharedStringCache sst,
				Stylesheet? styles,
				DateFormatCache dates,
				DetectionDiagnostics diag,
				CancellationToken ct)
			{
				var g = new RowIndexedSparseGrid();
				int nonEmpty = 0, formulas = 0, total = 0;
				int minR = int.MaxValue, minC = int.MaxValue, maxR = 0, maxC = 0;

				using var reader = OpenXmlReader.Create(wsPart);
				while (reader.Read())
				{
					ct.ThrowIfCancellationRequested();
					if (reader.ElementType != typeof(Row)) continue;

					var row = (Row)reader.LoadCurrentElement();
					if (row.RowIndex?.Value is not uint rU) continue;
					int r = (int)rU;

					foreach (var cell in row.Elements<Cell>())
					{
						total++;
						if (cell.CellReference?.Value is not string cref) continue;
						int c = ExcelAddress.Col(cref);

						var info = ParseCell(cell, sst, styles, dates);
						if (info.IsEmpty) continue;

						if (!g._rows.TryGetValue(r, out var map))
							g._rows[r] = map = new();


						map[c] = info;
						nonEmpty++;

						minR = Math.Min(minR, r);
						minC = Math.Min(minC, c);
						maxR = Math.Max(maxR, r);
						maxC = Math.Max(maxC, c);

						if (cell.CellFormula != null) formulas++;
					}
				}

				g.FormulaRatio = total == 0 ? 0 : (double)formulas / total;
				g.UsedRange = nonEmpty == 0
					? new UsedRange(0, 0, 0, 0, 0)
					: new UsedRange(minR, minC, maxR, maxC, nonEmpty);

				return g;
			}

			public bool TryGet(int r, int c, out CellValueInfo v)
			{
				if (_rows.TryGetValue(r, out var m) && m.TryGetValue(c, out v!))
					return true;
				v = default!;
				return false;
			}

			public IEnumerable<(int r, int c, CellValueInfo v)> Cells()
			{
				foreach (var r in _rows)
					foreach (var c in r.Value)
						yield return (r.Key, c.Key, c.Value);
			}

			public SheetProfile ToProfile(bool pivot, bool tables, bool filter)
				=> new(UsedRange, NonEmptyCells, FormulaRatio, 0, 0, pivot, tables, filter);
		}

		// ============================================================
		// REGION DETECTION / SPLITTING
		// ============================================================

		private static class RegionDetector
		{
			public static List<CellRange> Detect(
				RowIndexedSparseGrid g,
				ExcelStructureDetectorOptions o,
				CancellationToken ct)
			{
				var regions = new List<CellRange>();
				var visited = new HashSet<(int r, int c)>();

				foreach (var (r, c, _) in g.Cells())
				{
					if (!visited.Add((r, c))) continue;

					int minR = r, maxR = r, minC = c, maxC = c, count = 0;
					var q = new Queue<(int r, int c)>();
					q.Enqueue((r, c));

					while (q.Count > 0)
					{
						ct.ThrowIfCancellationRequested();
						var cur = q.Dequeue();
						count++;

						foreach (var (nr, nc) in Neighbors(cur))
						{
							if (!visited.Contains((nr, nc)) &&
								g.TryGet(nr, nc, out var v) &&
								!v.IsEmpty)
							{
								visited.Add((nr, nc));
								q.Enqueue((nr, nc));

								minR = Math.Min(minR, nr);
								maxR = Math.Max(maxR, nr);
								minC = Math.Min(minC, nc);
								maxC = Math.Max(maxC, nc);
							}
						}
					}

					var range = new CellRange(minR, minC, maxR, maxC);
					if (range.RowCount >= o.MinRegionRows &&
						range.ColCount >= o.MinRegionCols &&
						count >= o.MinRegionCells)
						regions.Add(range);
				}

				return regions;
			}

			private static IEnumerable<(int, int)> Neighbors((int r, int c) p)
			{
				yield return (p.r - 1, p.c);
				yield return (p.r + 1, p.c);
				yield return (p.r, p.c - 1);
				yield return (p.r, p.c + 1);
			}
		}

		private static class RegionSplitter
		{
			public static List<CellRange> Split(
				RowIndexedSparseGrid g,
				List<CellRange> regions,
				ExcelStructureDetectorOptions o)
				=> regions;
		}

		// ============================================================
		// CLASSIFICATION + TABLE ANALYSIS (minimal but correct)
		// ============================================================

		private static class SheetShapeClassifier
		{
			public static SheetShape Classify(
				RowIndexedSparseGrid g,
				List<CellRange> regions,
				bool pivot,
				bool tables,
				bool filter)
			{
				if (pivot) return SheetShape.Pivot;
				if (regions.Count > 1) return SheetShape.MultiTable;
				if (regions.Count == 1) return SheetShape.Tabular;
				return SheetShape.Unknown;
			}
		}

		private static class TableAnalyzer
		{
			public static TableStructureResult FromExplicit(
				string sheet,
				CellRange range,
				string? name,
				RowIndexedSparseGrid g)
				=> new(sheet, range, new HeaderInfo(range.MinRow, range.MinRow, 1),
					   Array.Empty<ColumnProfile>(),
					   TableKind.ExplicitExcelTable,
					   new ConfidenceScore(1, 1, 1, 1));

			public static TableStructureResult Analyze(
				string sheet,
				RowIndexedSparseGrid g,
				CellRange r,
				SheetShape shape,
				ExcelStructureDetectorOptions o,
				CancellationToken ct)
				=> new(sheet, r, new HeaderInfo(0, 0, 0),
					   Array.Empty<ColumnProfile>(),
					   TableKind.Table,
					   new ConfidenceScore(0.5, 0, 0, 0.5));
		}

		// ============================================================
		// CELL PARSING + ADDRESS
		// ============================================================

		private static CellValueInfo ParseCell(
			Cell cell,
			SharedStringCache sst,
			Stylesheet? styles,
			DateFormatCache dates)
		{
			string? text = null;
			double? number = null;
			DateTime? date = null;

			if (cell.DataType?.Value == CellValues.SharedString &&
				int.TryParse(cell.CellValue?.Text, out var idx))
				text = sst.Get(idx);
			else if (double.TryParse(cell.CellValue?.Text, out var d))
				number = d;
			else
				text = cell.CellValue?.Text;

			bool empty = string.IsNullOrWhiteSpace(text) && number == null && date == null;
			return new CellValueInfo(empty, text, number, date);
		}

		private static class ExcelAddress
		{
			public static int Col(string r)
			{
				int c = 0;
				foreach (var ch in r)
				{
					if (!char.IsLetter(ch)) break;
					c = c * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
				}
				return c;
			}
		}

		private static bool TryParseRange(string? r, out CellRange range)
		{
			range = new CellRange(0, 0, 0, 0);
			if (string.IsNullOrWhiteSpace(r)) return false;
			var p = r.Split(':');
			if (p.Length != 2) return false;
			int r1 = ExcelAddress.Col(p[0]);
			int r2 = ExcelAddress.Col(p[1]);
			range = new CellRange(1, r1, 1, r2);
			return true;
		}
	}
}
