using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Infrastructure.Models;
	public static class LocalModelStoragePaths
	{
		public static string GetBaseDir()
		{
			var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			return Path.Combine(home, ".caile", "models");
		}

		public static string GetSlotDir(string slot)
		{
			return Path.Combine(GetBaseDir(), slot.ToLowerInvariant());
		}

		public static string GetModelDir(string slot, string modelName)
		{
			return Path.Combine(GetSlotDir(slot), modelName);
		}
	}


