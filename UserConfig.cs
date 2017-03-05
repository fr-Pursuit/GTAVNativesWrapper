using PursuitLib;
using PursuitLib.IO;
using PursuitLib.WPF;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace GTAVNativesWrapper
{
	[Serializable]
	public class UserConfig : ConfigFile
	{
		public bool VisibilityPublic { get; set; } = true;
		public bool VisibilityInternal { get; set; } = false;
		public bool VisibilityProtected { get; set; } = false;
		public string OutputFolder { get; set; } = "";
		public bool UseSeparatedFiles { get; set; } = false;
		public string FileName { get; set; } = "Natives.cs";
		public string Namespace { get; set; } = "GTAV.Natives";
		public string ClassesPrefix { get; set; } = "";
		public string ClassesSuffix { get; set; } = "Natives";
		public bool Comments { get; set; } = true;
		public bool Compact { get; set; } = true;
		public bool UseLogFile { get; set; } = true;
		public bool CheckUpdates { get; set; } = true;

		public UserConfig() : base(WPFApp.UserConfigFile, true) {}
	}
}
