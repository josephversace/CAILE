using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using IIM.Shared.Models;

namespace IIM.Application.Urls
{

	public interface IAriaSnapshotParser
	{
		AriaTree Parse(string ariaSnapshot);
	}


	public sealed class AriaSnapshotParser : IAriaSnapshotParser
	{
		private static readonly Regex HeadingRegex =
			new Regex(
				@"heading\s+""(?<text>[^""]+)""\s*\[level=(?<level>\d+)\]",
				RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public AriaTree Parse(string ariaSnapshot)
		{
			var headings = new List<AriaHeading>();
			var order = 0;

			foreach (var line in ariaSnapshot.Split('\n'))
			{
				var match = HeadingRegex.Match(line);
				if (!match.Success)
					continue;

				headings.Add(new AriaHeading
				{
					Text = match.Groups["text"].Value.Trim(),
					Level = int.Parse(match.Groups["level"].Value),
					Order = order++
				});
			}

			return new AriaTree
			{
				Headings = headings
			};
		}
	}

}
