using GTAVNativesWrapper.Ui;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace GTAVNativesWrapper
{
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs a)
		{
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
			AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
		}

		private Assembly ResolveAssembly(object sender, ResolveEventArgs args)
		{
			string assemblyName = args.Name.Substring(0, args.Name.IndexOf(',')) + ".dll";

			try
			{
				Stream stream = GetResourceStream(new Uri("/GTAVNativesWrapper;component/libs/" + assemblyName, UriKind.Relative)).Stream;

				if(stream != null)
				{
					byte[] bytes = new byte[stream.Length];
					stream.Read(bytes, 0, (int)stream.Length);
					return Assembly.Load(bytes);
				}
				else throw new IOException();
			}
			catch(Exception e)
			{
				MessageBox.Show("Unable to load " + assemblyName+"\n\n"+e, "Fatal error", MessageBoxButton.OK, MessageBoxImage.Error);
				Process.GetCurrentProcess().Kill();
				return null;
			}
		}

		private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
		{
			Exception e = (Exception)args.ExceptionObject;

			if(Wrapper.Instance != null && Wrapper.Instance.Window != null)
			{
				Wrapper.Instance.Window.Dispatcher.Invoke(delegate
				{
					Wrapper.Instance.Window.Visibility = Visibility.Hidden;
					new CrashReporter(e, true).ShowDialog();
				});
			}
			else new CrashReporter(e, false);
		}
	}
}
