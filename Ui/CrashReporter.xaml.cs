using PursuitLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace GTAVNativesWrapper.Ui
{
	public partial class CrashReporter : Window
	{
		private const string ReportSite = "github.com";
		private const string ReportURL = "https://github.com/fr-Pursuit/GTAVNativesWrapper/issues/new";

		private readonly Exception exception;

		public CrashReporter(Exception exception, bool createInterface)
		{
			Log.Error("Caught unhandled exception: " + exception);
			this.exception = exception;

			if(createInterface)
			{
				this.InitializeComponent();
				this.Message.Content = ((string)this.Message.Content).Replace("{0}", ReportSite);
				Log.Info("Created crash report interface successfully.");
			}
			else
			{
				MessageBox.Show("The program has encountered an unexpected error and cannot continue.\nPlease report the following error at " + ReportSite + ". Sorry for any inconvenience caused.", "Fatal error", MessageBoxButton.OK, MessageBoxImage.Error);
				Log.Warn("Unable to create crash report interface.");
			}

			string message = this.FillCrashReport();

			this.Report.Text = message;

			string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Pursuit\\GTA V Natives Wrapper");
			string filePath = Path.Combine(dir, "crash-" + String.Format("{0:dd-MM-yyyy-HH.mm.ss}", DateTime.Now) + ".txt");

			Log.Info("Creating " + filePath + "...");

			if(!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			using(StreamWriter writer = new StreamWriter(filePath))
			{
				writer.Write(message);
			}
		}

		private string FillCrashReport()
		{
			StringBuilder report = new StringBuilder();
			report.Append("---- GTA V Natives Wrapper " + Wrapper.Version + " crash report ----\n");
			report.Append("Generated on " + String.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTime.Now) + "\n\n\n");

			report.Append("-- Thrown exception --\n");
			report.Append(this.exception.ToString() + "\n\n");

			report.Append("-- Program state --\n");
			try
			{
				report.Append("Is initialized: " + (Wrapper.Instance != null) + '\n');
				report.Append("Is window initialized: " + (Wrapper.Instance.Window != null) + '\n');
				report.Append("Are buttons enabled: " + Wrapper.Instance.UiManager.ButtonsEnabled + '\n');
			}
			catch(Exception)
			{
				report.Append("~Unexpected error~\n");
			}

			return report.ToString();
		}

		private void Close(object sender, EventArgs e)
		{
			this.Close();
		}

		private void ReportCrash(object sender, EventArgs e)
		{
			Process.Start(ReportURL);
		}
	}
}
