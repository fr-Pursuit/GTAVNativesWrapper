using GTAVNativesWrapper.Config;
using PursuitLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace GTAVNativesWrapper
{
	public class Parser
	{
		private readonly Wrapper wrapper;
		private readonly Configuration config;
		private FileStream fStream = null;
		private StreamWriter writer = null;
		private Class activeClass = null;
		private string currentClassName = null;
		private bool firstMethod = true;

		public Parser()
		{
			this.wrapper = Wrapper.Instance;
			this.config = new Configuration();
		}

		/// <summary>
		/// This method downloads reference.html and parses it while generating the code
		/// </summary>
		public void ParseAndGenerateCode()
		{
			Log.Info("Starting code generation...");
			//Downloads reference.html
			Log.Info("Downloading reference.html...");
			HttpWebRequest request = WebRequest.CreateHttp("http://www.dev-c.com/nativedb/reference.html");
			request.Headers.Add("Accept-Encoding", "gzip, deflate");
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

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

						Log.Info("Parsing reference.html...");
						using(StringReader r = new StringReader(builder.ToString()))
						{
							try
							{
								if(this.wrapper.UiManager.UseSeparatedFiles)
								{
									fStream = new FileStream(Path.Combine(this.wrapper.UiManager.OutputFolder, "Hash.cs"), FileMode.Create);
									writer = new StreamWriter(fStream);

									this.WriteHeader();
									writer.WriteLine();
									this.WriteHeaderComment();
									writer.WriteLine("namespace " + this.wrapper.UiManager.Namespace);
									writer.WriteLine('{');
									this.WriteHashStruct();
									writer.WriteLine("}");

									writer.Close();
									fStream.Close();

									writer = null;
									fStream = null;
								}

								string line;
								int lineNum = 0;
								while((line = r.ReadLine()) != null)
								{
									lineNum++;
									if(lineNum < 37) //HTML header
										continue;
									else if(lineNum > 162) //HTML footer
										break;
									else if((lineNum - 39) % 3 == 0)
										this.ParseClassContent(line);
									else if((lineNum - 37) % 3 == 0)
										this.ParseClassDefinition(line);
								}

								writer.WriteLine("	}");
								writer.WriteLine("}");

								Log.Info("Parsing complete");
							}
							catch(Exception e)
							{
								Log.Error("Unable to parse reference.html. Has the page layout changed ?");
								Log.Error("Thrown exception: " + e);
							}

							if(writer != null)
								writer.Dispose();
							if(fStream != null)
								fStream.Dispose();
						}
					}
				}
				else
					throw new ApplicationException("Unable to download reference.html. Please try again.");
			}
		}

		private void ParseClassContent(string line)
		{
			int index = 0;
			int declIndex = line.IndexOf("fndecl", index);

			while(declIndex != -1)
			{
				//Method info

				int indexb = index;
				string returnType = this.Parse(line, "fntype\">", "<", ref index, out indexb);
				bool pointer = false;
				string methodName = this.Parse(line, "n> ", "(", ref index, out indexb);
				List<ParamInfo> @params = new List<ParamInfo>();
				string hash;
				string description = null;
				Method method = null;

				if(methodName.StartsWith("*"))
				{
					pointer = true;
					methodName = methodName.Substring(1);
				}
				if(this.activeClass != null)
				{
					method = this.activeClass[methodName];
					if(method != null && method.HasCustomName)
						methodName = method.Name;
				}
				if(methodName.StartsWith("_"))
					methodName = methodName.Substring(1);
				if(methodName.StartsWith("0"))
					methodName = methodName.Substring(1);
				if(methodName.StartsWith("TASK_"))
					returnType = "Task";

				returnType = this.GetType(returnType, pointer);

				index++;
				if(line[index] != ')')
				{
					while(line[index + 7] != ')')
					{
						//params info
						ParamInfo info = new ParamInfo();
						info.Type = this.Parse(line, "fntype\">", "<", ref index, out indexb);
						bool paramPointer = line[index + 8] == '*';
						info.Name = this.Parse(line, "fnparam\">", "<", ref index, out indexb);
						info.Cast = null;

						if(info.Type == "unktype_")
							info.Name = "unk";

						info.Type = this.GetType(info.Type, paramPointer);

						if(paramPointer && info.Type != "string")
						{
							if(methodName.Contains("SET_") || methodName.Contains("REMOVE_"))
								info.Type = "ref " + info.Type;
							else
								info.Type = "out " + info.Type;
						}

						if(method != null && method.Params.ContainsKey(info.Name))
						{
							string newType = method.Params[info.Name];

							if(info.Type != newType)
							{
								info.Cast = info.Type;
								info.Type = newType;
							}
						}

						if(info.Name == "object")
							info.Name = "@object";
						else if(info.Name == "string")
							info.Name = "@string";
						else if(info.Name == "event")
							info.Name = "@event";
						else if(info.Name == "base")
							info.Name = "@base";
						else if(info.Name == "new")
							info.Name = "@new";

						@params.Add(info);
					}
				}

				//Hash and description

				indexb = line.IndexOf("// ", index) + 3;
				int space = line.IndexOf(" ", indexb);
				int arrow = line.IndexOf("<", indexb);
				index = space == -1 ? arrow : Math.Min(space, arrow);

				hash = line.Substring(indexb, index - indexb);

				declIndex = line.IndexOf("fndecl", index);
				indexb = line.IndexOf("fdesc\">", index);

				if(indexb != -1 && (declIndex == -1 || indexb < declIndex))
				{
					indexb += 7;
					index = line.IndexOf("</d", indexb);
					description = HttpUtility.HtmlDecode(line.Substring(indexb, index - indexb));
				}

				if(firstMethod)
					firstMethod = !firstMethod;
				else if(!this.wrapper.UiManager.Compact)
					writer.WriteLine();

				if(description != null && this.wrapper.UiManager.Comments)
				{
					writer.WriteLine("		/// <summary>");
					writer.WriteLine("		/// " + description.Replace("<br />", "\r\n		/// "));
					writer.WriteLine("		/// </summary>");
				}

				StringBuilder b = new StringBuilder("		" + this.GetVisibility() + " static " + returnType + ' ' + this.ToPascalCase(methodName) + '(');
				if(@params.Count != 0)
				{
					foreach(ParamInfo param in @params)
						b.Append(param.Type + ' ' + param.Name + ", ");
					b.Remove(b.Length - 2, 2);
				}
				b.Append(')');

				StringBuilder b2 = new StringBuilder();
				if(returnType != "void")
					b2.Append("return ");

				if(this.currentClassName == "AI")
					b2.Append("NativeFunction.Natives." + this.ToPascalCase(methodName));
				else
					b2.Append("NativeFunction.Natives.x" + hash);

				if(returnType != "void")
					b2.Append('<' + this.GetNativeReturnType(returnType) + '>');
				b2.Append('(');
				if(@params.Count != 0)
				{
					foreach(ParamInfo param in @params)
					{
						string name = param.Name;
						string nativeType = param.Cast != null ? param.Cast : param.Type;

						if(param.Cast != null)
						{
							if(this.config.HasCastMethod(param.Type, param.Cast))
								name = this.config.GetCastMethod(param.Type, param.Cast) + '(' + param.Name + ')';
							else
								name = '(' + param.Cast + ')' + param.Name;
						}

						if(param.Type.StartsWith("ref"))
							b2.Append("ref " + name + ", ");
						else if(param.Type.StartsWith("out"))
							b2.Append("out " + name + ", ");
						else if(nativeType == "Hash" || nativeType == "NativeArgument")
							b2.Append(name + ".Value, ");
						else
							b2.Append(name + ", ");
					}
					b2.Remove(b2.Length - 2, 2);
				}
				b2.Append(");");

				if(this.wrapper.UiManager.Compact)
				{
					b.Append("{ " + b2 + " }");
					writer.WriteLine(b);
				}
				else
				{
					writer.WriteLine(b);
					writer.WriteLine("		{");
					writer.WriteLine("			" + b2);
					writer.WriteLine("		}");
				}
			}
		}

		private void ParseClassDefinition(string line)
		{
			this.currentClassName = line.Substring(line.IndexOf("<li>") + 4);
			Log.Info("Found class: " + this.currentClassName);
			this.activeClass = this.config.GetClass(this.currentClassName);
			string className = this.GetClassName(this.currentClassName);

			if(this.wrapper.UiManager.UseSeparatedFiles)
			{
				if(fStream != null)
				{
					writer.WriteLine("	}");
					writer.WriteLine("}");
					writer.Dispose();
					fStream.Dispose();
				}

				fStream = new FileStream(Path.Combine(this.wrapper.UiManager.OutputFolder, className + ".cs"), FileMode.Create);
				writer = new StreamWriter(fStream);

				this.WriteHeader();
				writer.WriteLine();
				this.WriteHeaderComment();
				writer.WriteLine("namespace " + this.wrapper.UiManager.Namespace);
				writer.WriteLine("{");
				writer.WriteLine("	public static class " + className);
				writer.WriteLine("	{");
			}
			else
			{
				if(fStream == null)
				{
					fStream = new FileStream(Path.Combine(this.wrapper.UiManager.OutputFolder, this.wrapper.UiManager.FileName), FileMode.Create);
					writer = new StreamWriter(fStream);

					this.WriteHeader();
					writer.WriteLine();
					this.WriteHeaderComment();
					writer.WriteLine("namespace " + this.wrapper.UiManager.Namespace);
					writer.WriteLine("{");
					this.WriteHashStruct();
					writer.WriteLine();
				}
				else
				{
					writer.WriteLine("	}");
					if(!this.wrapper.UiManager.Compact)
						writer.WriteLine();
				}

				writer.WriteLine();
				writer.WriteLine("	public static class " + className);
				writer.WriteLine("	{");
			}

			firstMethod = true;
		}

		private string Parse(string text, string start, string end, ref int index, out int indexb)
		{
			if(index >= text.Length)
				throw new Exception("Invalid index");

			indexb = text.IndexOf(start, index) + start.Length;

			if(indexb == -1)
				throw new Exception("'" + start + "' was not found");

			index = text.IndexOf(end, indexb);
			if(index == -1)
				throw new Exception("'" + end + "' was not found");

			return text.Substring(indexb, index - indexb);
		}

		private string GetClassName(string originalName)
		{
			string lower = originalName.ToLower();
			return this.wrapper.UiManager.ClassesPrefix + Char.ToUpper(lower[0]) + lower.Substring(1) + this.wrapper.UiManager.ClassesSuffix;
		}

		private string GetVisibility()
		{
			return this.wrapper.UiManager.VisibilityPublic ? "public" : (this.wrapper.UiManager.VisibilityInternal ? "internal" : "protected");
		}

		private string GetType(string originalType, bool pointer)
		{
			string configType = this.config.GetType(pointer ? originalType + '*' : originalType);
			return configType != null ? configType : originalType;
		}

		private string GetNativeReturnType(string originalType)
		{
			if(originalType == "Hash")
				return "uint";
			else if(originalType == "Any")
				return "long";
			else return originalType;
		}

		private string ToPascalCase(string text)
		{
			if(text[0] == 'x')
				return text;
			else
				return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.Replace('_', ' ').ToLower()).Replace(" ", "");
		}

		private void WriteHeader()
		{
			foreach(string warn in this.config.SuppressWarnings)
				writer.WriteLine("#pragma warning disable " + warn);
			foreach(string @namespace in this.config.Namespaces)
				writer.WriteLine("using " + @namespace + ';');
		}

		private void WriteHeaderComment()
		{
			DateTime date = DateTime.Now;
			writer.WriteLine($"// Generated from dev-c using GTA V Natives Wrapper by Pursuit");
			writer.WriteLine($"// Date: {date.Day}/{date.Month}/{date.Year} {date.Hour:d2}:{date.Minute:d2}:{date.Second:d2}");
		}

		private void WriteHashStruct()
		{
			writer.WriteLine(@"	public struct Hash
	{
		public uint Value { get; set; }

		public Hash(uint value)
		{
			this.Value = value;
		}

		public Hash(string name)
		{
			this.Value = Game.GetHashKey(name);
		}

		public static implicit operator NativeArgument(Hash hash)
		{
			return new NativeArgument(hash.Value);
		}

		public static implicit operator Hash(uint value)
		{
			return new Hash(value);
		}

		public static implicit operator Hash(string name)
		{
			return new Hash(Game.GetHashKey(name));
		}
	}");
		}
	}
}
