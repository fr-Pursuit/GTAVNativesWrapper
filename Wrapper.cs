using GTAVNativesWrapper.Ui;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PursuitLib;
using PursuitLib.WPF;
using PursuitLib.WPF.Dialogs;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Media;

namespace GTAVNativesWrapper
{
	/// <summary>
	/// Main class
	/// </summary>
	public class Wrapper
	{
		public static Version Version { get; } = Versions.GetTypeVersion(typeof(Wrapper));
		public static Wrapper Instance { get; internal set; }
		private delegate void UpdateCallback(MainWindow window, JObject obj);

		public MainWindow Window { get; internal set; }
		public UserConfig Settings { get; internal set; }
		public WorkManager WorkManager { get; private set; }
		public UIManager UiManager { get; internal set; }

		public Wrapper(MainWindow window)
		{
			Instance = this;
			this.WorkManager = new WorkManager();

			this.Window = window;
			this.Settings = new UserConfig();
			this.UiManager = new UIManager(window, this.Settings);

			if(this.Settings.UseLogFile)
				Log.SetLogFile(Path.Combine(WPFApp.UserDataDirectory, "latest.log"));
			Log.Info("GTA V Natives Wrapper " + Version);
			Log.Info("Using PursuitLib " + Versions.GetTypeVersion(typeof(Log)));
		}

		public void InitUI()
		{
			Log.Info("Initializing user interface...");
			Log.ShowDate = false;
			Log.LogMethod = UIManager.LogLine;

			ColorFormatter.RegisterColor('0', Colors.Black);
			ColorFormatter.RegisterColor('r', Colors.Red);
			ColorFormatter.RegisterColor('g', Colors.Green);
			ColorFormatter.RegisterColor('b', Colors.Blue);
			ColorFormatter.RegisterColor('o', Colors.Orange);

			this.Window.Closing += OnWindowClosing;
			this.Window.GenerateButton.Click += StartCodeGeneration;
			this.Window.BrowseButton.Click += BrowseFolder;
			this.Window.AboutButton.Click += ShowAboutPopup;
			this.UiManager.WindowTitle += " " + Version + " " + Versions.GetVersionType(Version);
			this.UiManager.ButtonsEnabled = true;

			if(this.Settings.CheckUpdates)
				this.WorkManager.StartWork(CheckUpdates);
		}

		private void CheckUpdates()
		{
			JObject obj = this.IsUpToDate();
			if(obj != null)
				this.Window.Dispatcher.Invoke(new UpdateCallback(ShowUpdatePopup), this.Window, obj);
		}


		/// <summary>
		/// Checks whether the software is up to date or not
		/// </summary>
		/// <returns>A JObject representing the latest update, or null if it's up to date</returns>
		public JObject IsUpToDate()
		{
			try
			{
				HttpWebRequest request = WebRequest.CreateHttp("https://api.github.com/repos/fr-Pursuit/GTAVNativesWrapper/releases/latest");
				request.UserAgent = "GTAVNativesWrapper-" + Version;

				using(HttpWebResponse response = request.GetResponse() as HttpWebResponse)
				{
					if(response.StatusCode == HttpStatusCode.OK)
					{
						using(StreamReader streamReader = new StreamReader(response.GetResponseStream()))
						using(JsonReader reader = new JsonTextReader(streamReader))
						{
							JObject obj = JObject.Load(reader);
							if(new Version(obj["tag_name"].ToString()) > Version)
							{
								Log.Info("New update found (" + obj["name"] + ')');
								return obj;
							}
						}
					}
					else
						Log.Warn("Unable to check for updates. Response code was " + response.StatusCode + " (" + response.StatusDescription + ')');
				}
			}
			catch(Exception)
			{
				Log.Warn("Unable to check for updates.");
			}

			return null;
		}

		/// <summary>
		/// Shows a popup telling the user a new update is out.
		/// </summary>
		/// <param name="obj">A JObject representing the update</param>
		public void ShowUpdatePopup(Window window, JObject obj)
		{
			if(this.Window.CheckAccess())
			{
				if(MessageDialog.Show(window, "A new update is available.\n\n"+obj["name"]+"\n\nChangelog:\n"+obj["body"]+"\n\nDo you want to download it ?", "Update", TaskDialogStandardIcon.Information, TaskDialogStandardButtons.Yes | TaskDialogStandardButtons.No) == TaskDialogResult.Yes)
				{
					Process.Start(obj["assets"][0]["browser_download_url"].ToString());
					this.Window.Close();
				}
			}
			else this.Window.Dispatcher.Invoke(new UpdateCallback(ShowUpdatePopup), window, obj);
		}

		private void OnWindowClosing(object sender, CancelEventArgs e)
		{
			if(/*!this.closeRequested && */this.WorkManager.IsWorking())
			{
				e.Cancel = true;
				MessageDialog.Show(this.Window, "The program is currently working. Please wait.", "Impossible action", TaskDialogStandardIcon.Information, TaskDialogStandardButtons.Ok);
			}
			//else
				//this.Closed = true;
		}

		private void StartCodeGeneration(object sender, RoutedEventArgs e)
		{
			if(this.UiManager.OutputFolder.Length == 0)
				MessageDialog.Show(this.Window, "Please provide an output folder", "Invalid settings", TaskDialogStandardIcon.Information);
			else if(!this.UiManager.UseSeparatedFiles && this.UiManager.FileName.Length == 0)
				MessageDialog.Show(this.Window, "Please provide an output file", "Invalid settings", TaskDialogStandardIcon.Information);
			else if(this.UiManager.Namespace.Length == 0)
				MessageDialog.Show(this.Window, "Please provide a namespace", "Invalid settings", TaskDialogStandardIcon.Information);
			else if(this.UiManager.ClassesPrefix.Length == 0 && this.UiManager.ClassesSuffix.Length == 0)
				MessageDialog.Show(this.Window, "Please provide a class prefix and / or a class suffix", "Invalid settings", TaskDialogStandardIcon.Information);
			else
			{
				if(!Directory.Exists(this.UiManager.OutputFolder))
				{
					try
					{
						Directory.CreateDirectory(this.UiManager.OutputFolder);
					}
					catch(IOException)
					{
						MessageDialog.Show(this.Window, "Please provide a valid output folder", "Invalid settings", TaskDialogStandardIcon.Information);
						return;
					}
				}
				
				if(!this.UiManager.UseSeparatedFiles && !File.Exists(this.UiManager.FileName))
				{
					try
					{
						File.Create(Path.Combine(this.UiManager.OutputFolder, this.UiManager.FileName));
					}
					catch(IOException)
					{
						MessageDialog.Show(this.Window, "Please provide a valid output file", "Invalid settings", TaskDialogStandardIcon.Information);
						return;
					}
				}

				this.UiManager.ButtonsEnabled = false;
				this.Settings.Save();
				this.WorkManager.StartWork(GenerateCode);
			}
		}

		private void BrowseFolder(object sender, RoutedEventArgs e)
		{
			CommonOpenFileDialog dialog = new CommonOpenFileDialog();
			dialog.DefaultDirectory = String.IsNullOrWhiteSpace(this.Settings.OutputFolder) ? WinApp.AppDirectory : this.Settings.OutputFolder;
			dialog.IsFolderPicker = true;
			dialog.Title = "Select output directory";
			if(dialog.ShowDialog() == CommonFileDialogResult.Ok)
				this.UiManager.OutputFolder = dialog.FileName;
		}

		private void ShowAboutPopup(object sender, RoutedEventArgs e)
		{
			MessageDialog.Show(this.Window, "GTA V Natives Wrapper "+Version+ "\n\nCreated by Pursuit.\nBased on an idea by jitsuin.\nThanks to MulleDK19, alexguirre and leftas !", "About", TaskDialogStandardIcon.Information);
		}

		private void GenerateCode()
		{
			try
			{
				new Parser().ParseAndGenerateCode();
			}
			catch(ApplicationException e)
			{
				Log.Error(e.Message);
			}
			catch(Exception e)
			{
				Log.Error("Unable to generate code: " + e);
			}

			this.UiManager.ButtonsEnabled = true;
		}
	}
}
