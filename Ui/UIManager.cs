using System.ComponentModel;

namespace GTAVNativesWrapper.Ui
{
	/// <summary>
	/// Bridge betwen <see cref="Wrapper"/> and <see cref="MainWindow"/>
	/// </summary>
	public class UIManager : INotifyPropertyChanged
	{
		private static UIManager instance;
		private delegate void LogCallback(string line);
		private delegate void BoolCallback(bool value);

		public static void LogLine(string line)
		{
			if(instance.window.CheckAccess())
			{
				if(line.StartsWith("[INFO]"))
					line = "~b~[INFO]~0~" + line.Substring(6);
				else if(line.StartsWith("[WARN]"))
					line = "~o~[WARN]~0~" + line.Substring(6);
				else if(line.StartsWith("[ERROR]"))
					line = "~r~[ERROR]~0~" + line.Substring(7);

				instance.window.Output.Inlines.AddRange(ColorFormatter.GetFormattedText(line + '\n'));
				instance.window.OutputBox.ScrollToEnd();
			}
			else
				instance.window.Dispatcher.Invoke(new LogCallback(LogLine), line);
		}

		private MainWindow window;
		private UserConfig settings;

		public event PropertyChangedEventHandler PropertyChanged;

		private string windowTitle = "GTA V Natives Wrapper";
		public string WindowTitle
		{
			get { return this.windowTitle; }
			set
			{
				if(this.windowTitle != value)
				{
					this.windowTitle = value;
					this.NotifyPropertyChanged("WindowTitle");
				}
			}
		}

		private bool buttonsEnabled = false;
		public bool ButtonsEnabled
		{
			get { return this.buttonsEnabled; }
			set
			{
				if(this.buttonsEnabled != value)
				{
					this.buttonsEnabled = value;
					this.NotifyPropertyChanged("ButtonsEnabled");

					if(this.buttonsEnabled)
						this.SetFileNameFieldEnabled(!this.UseSeparatedFiles);
					else
						this.SetFileNameFieldEnabled(false);
				}
			}
		}

		public bool VisibilityPublic
		{
			get { return this.settings.VisibilityPublic; }
			set
			{
				if(this.settings.VisibilityPublic != value)
				{
					this.settings.VisibilityPublic = value;
					this.NotifyPropertyChanged("VisibilityPublic");
				}
			}
		}

		public bool VisibilityInternal
		{
			get { return this.settings.VisibilityInternal; }
			set
			{
				if(this.settings.VisibilityInternal != value)
				{
					this.settings.VisibilityInternal = value;
					this.NotifyPropertyChanged("VisibilityInternal");
				}
			}
		}

		public bool VisibilityProtected
		{
			get { return this.settings.VisibilityProtected; }
			set
			{
				if(this.settings.VisibilityProtected != value)
				{
					this.settings.VisibilityProtected = value;
					this.NotifyPropertyChanged("VisibilityProtected");
				}
			}
		}

		public string OutputFolder
		{
			get { return this.settings.OutputFolder; }
			set
			{
				if(this.settings.OutputFolder != value)
				{
					this.settings.OutputFolder = value;
					this.NotifyPropertyChanged("OutputFolder");
				}
			}
		}

		public bool UseSeparatedFiles
		{
			get { return this.settings.UseSeparatedFiles; }
			set
			{
				if(this.settings.UseSeparatedFiles != value)
				{
					this.settings.UseSeparatedFiles = value;
					this.NotifyPropertyChanged("UseSeparatedFiles");
					this.SetFileNameFieldEnabled(!value);
				}
			}
		}

		public string FileName
		{
			get { return this.settings.FileName; }
			set
			{
				if(this.settings.FileName != value)
				{
					this.settings.FileName = value;
					this.NotifyPropertyChanged("FileName");
				}
			}
		}

		public string Namespace
		{
			get { return this.settings.Namespace; }
			set
			{
				if(this.settings.Namespace != value)
				{
					this.settings.Namespace = value;
					this.NotifyPropertyChanged("Namespace");
				}
			}
		}

		public string ClassesPrefix
		{
			get { return this.settings.ClassesPrefix; }
			set
			{
				if(this.settings.ClassesPrefix != value)
				{
					this.settings.ClassesPrefix = value;
					this.NotifyPropertyChanged("ClassesPrefix");
				}
			}
		}

		public string ClassesSuffix
		{
			get { return this.settings.ClassesSuffix; }
			set
			{
				if(this.settings.ClassesSuffix != value)
				{
					this.settings.ClassesSuffix = value;
					this.NotifyPropertyChanged("ClassesSuffix");
				}
			}
		}

		public bool Comments
		{
			get { return this.settings.Comments; }
			set
			{
				if(this.settings.Comments != value)
				{
					this.settings.Comments = value;
					this.NotifyPropertyChanged("Comments");
				}
			}
		}

		public bool Compact
		{
			get { return this.settings.Compact; }
			set
			{
				if(this.settings.Compact != value)
				{
					this.settings.Compact = value;
					this.NotifyPropertyChanged("Compact");
				}
			}
		}

		public bool UseLogFile
		{
			get { return this.settings.UseLogFile; }
			set
			{
				if(this.settings.UseLogFile != value)
				{
					this.settings.UseLogFile = value;
					this.NotifyPropertyChanged("UseLogFile");
				}
			}
		}

		public bool CheckUpdates
		{
			get { return this.settings.CheckUpdates; }
			set
			{
				if(this.settings.CheckUpdates != value)
				{
					this.settings.CheckUpdates = value;
					this.NotifyPropertyChanged("CheckUpdates");
				}
			}
		}

		public UIManager(MainWindow window, UserConfig settings)
		{
			instance = this;
			this.window = window;
			this.settings = settings;
		}

		private void SetFileNameFieldEnabled(bool enabled)
		{
			if(this.window.CheckAccess())
			{
				this.window.FileName.IsEnabled = enabled;
			}
			else
				this.window.Dispatcher.Invoke(new BoolCallback(SetFileNameFieldEnabled), enabled);
		}

		private void NotifyPropertyChanged(string propertyName)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
