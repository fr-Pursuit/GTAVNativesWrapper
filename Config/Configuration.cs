using PursuitLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace GTAVNativesWrapper.Config
{
	public class Configuration
	{
		public List<string> SuppressWarnings { get; } = new List<string>();
		public List<string> Namespaces { get; } = new List<string>();
		private readonly Dictionary<string, string> types;
		private readonly Dictionary<string, Class> classes;
		private readonly Dictionary<string, string> castMethods;

		public Configuration()
		{
			this.types = new Dictionary<string, string>();
			this.classes = new Dictionary<string, Class>();
			this.castMethods = new Dictionary<string, string>();

			XDocument document = null;

			if(File.Exists("Configuration.xml"))
			{
				Log.Info("Local Configuration.xml found.");
				document = XDocument.Parse(File.ReadAllText("Configuration.xml"));
			}
			else
			{
				Log.Info("Downloading configuration...");
				HttpWebRequest request = WebRequest.CreateHttp("https://raw.githubusercontent.com/fr-Pursuit/GTAVNativesWrapper/master/Configuration.xml");
				request.UserAgent = "GTAVNativesWrapper-" + Wrapper.Version;

				using(HttpWebResponse response = request.GetResponse() as HttpWebResponse)
				{
					if(response.StatusCode == HttpStatusCode.OK)
					{
						using(Stream stream = response.GetResponseStream())
						using(StreamReader reader = new StreamReader(stream))
						{
							StringBuilder builder = new StringBuilder();

							//Reads the whole file using StringBuilder
							while(!reader.EndOfStream)
								builder.Append(reader.ReadLine() + '\n');

							document = XDocument.Parse(builder.ToString());
						}
					}
					else
						throw new ApplicationException("Unable to download configuration. Please try again.");
				}
			}

			if(document != null)
			{
				XElement root = document.Element("ParserConfig");
				if(root != null)
				{
					if(root.HasElements)
					{
						this.ParseHeader(root.Element("Header"));
						this.ParseTypes(root.Element("Types"));
						this.ParseClasses(root.Element("Classes"));
						this.ParseCastMethods(root.Element("CastMethods"));
					}
					else
						throw new ApplicationException("Invalid configuration: ParserConfig is empty");
				}
				else
					throw new ApplicationException("Invalid configuration: root element must be 'ParserConfig'.");
			}
			else
				throw new ApplicationException("Unable to read configuration.");
		}

		private void ParseHeader(XElement types)
		{
			if(types != null && types.HasElements)
			{
				IEnumerable<XElement> elements = types.Elements();

				foreach(XElement element in elements)
				{
					if(element.Name == "Using" && element.HasAttributes)
					{
						string @namespace = element.Attribute("Namespace")?.Value;
						if(@namespace != null)
							this.Namespaces.Add(@namespace);
					}
					else if(element.Name == "Suppress" && element.HasAttributes)
					{
						string warn = element.Attribute("Warning")?.Value;
						if(warn != null)
							this.SuppressWarnings.Add(warn);
					}
				}
			}
		}

		private void ParseTypes(XElement types)
		{
			if(types != null && types.HasElements)
			{
				IEnumerable<XElement> elements = types.Elements();

				foreach(XElement element in elements)
					this.ParseType(element);
			}
		}

		private void ParseType(XElement element)
		{
			if(element.HasAttributes)
			{
				string native = element.Attribute("Native")?.Value;
				string name = element.Attribute("Name")?.Value;

				if(native != null && name != null)
					this.types.Add(native, name);
			}
		}

		private void ParseClasses(XElement classes)
		{
			if(classes != null && classes.HasElements)
			{
				IEnumerable<XElement> elements = classes.Elements();

				foreach(XElement element in elements)
					this.ParseClass(element);
			}
		}

		private void ParseClass(XElement @class)
		{
			if(@class.HasAttributes && @class.HasElements)
			{
				string name = @class.Attribute("Name")?.Value;

				if(name != null)
				{
					Class clazz = new Class();

					IEnumerable<XElement> elements = @class.Elements();
					foreach(XElement element in elements)
						this.ParseMethod(element, clazz);

					this.classes.Add(name, clazz);
				}
			}
		}

		private void ParseMethod(XElement method, Class @class)
		{
			if(method.HasAttributes)
			{
				string native = method.Attribute("Native")?.Value;
				string name = method.Attribute("Name")?.Value;

				if(native != null)
				{
					Method m = new Method(native, name);
					
					if(method.HasElements)
					{
						foreach(XElement element in method.Elements())
						{
							if(element.Name == "Param" && element.HasAttributes)
							{
								string paramName = element.Attribute("Name")?.Value;
								string type = element.Attribute("Type")?.Value;

								if(paramName != null && type != null)
									m.Params.Add(paramName, type);
							}
						}
					}

					@class.AddMethod(m);
				}
			}
		}

		private void ParseCastMethods(XElement element)
		{
			if(element != null && element.HasElements)
			{
				foreach(XElement method in element.Elements())
				{
					if(method.HasAttributes)
					{
						string from = method.Attribute("From")?.Value;
						string to = method.Attribute("To")?.Value;
						string theMethod = method.Attribute("Method")?.Value;

						if(from != null && to != null && theMethod != null)
							this.castMethods.Add(from + '>' + to, theMethod);
					}
				}
			}
		}

		public string GetType(string nativeType)
		{
			string type = null;
			this.types.TryGetValue(nativeType, out type);
			return type;
		}

		public Class GetClass(string name)
		{
			Class @class = null;
			this.classes.TryGetValue(name, out @class);
			return @class;
		}

		public bool HasCastMethod(string from, string to)
		{
			return this.castMethods.ContainsKey(from + '>' + to);
		}

		public string GetCastMethod(string from, string to)
		{
			return this.castMethods[from + '>' + to];
		}
	}
}
