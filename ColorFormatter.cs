using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;

namespace GTAVNativesWrapper
{
	/// <summary>
	/// That class allows us to use GTA formatting codes like ~r~Red text
	/// </summary>
	public static class ColorFormatter
	{
		private static Dictionary<char, Color> colors = new Dictionary<char, Color>();
		
		/// <summary>
		/// Register a color that will be used in <see cref="GetFormattedText(string)"/>
		/// </summary>
		/// <param name="char">The char assigned to the color</param>
		/// <param name="color">The color the text will be colored into</param>
		public static void RegisterColor(char @char, Color color)
		{
			colors.Add(@char, color);
		}

		public static Color GetColor(char @char)
		{
			return colors.ContainsKey(@char) ? colors[@char] : Colors.Black;
		}

		/// <summary>
		/// Returns a formatted text (a text colored using the known color codes, <see cref="RegisterColor(char, Color)"/>)
		/// </summary>
		/// <param name="text">The unformatted text</param>
		/// <returns>The formatted text</returns>
		public static List<Run> GetFormattedText(string text)
		{
			List<Run> runs = new List<Run>();
			char[] chars = text.ToArray();

			Color currentColor = Colors.Black;
			StringBuilder currentText = new StringBuilder();

			for(int i = 0; i < chars.Length; i++)
			{
				if(i < chars.Length - 3 && chars[i] == '~' && chars[i + 2] == '~' && Char.IsLetterOrDigit(chars[i + 1]))
				{
					if(currentText.Length != 0)
					{
						Run run = new Run(currentText.ToString());
						run.Foreground = new SolidColorBrush(currentColor);
						runs.Add(run);
						currentText = new StringBuilder();
					}

					currentColor = GetColor(chars[i+1]);

					i+=2;
				}
				else currentText.Append(chars[i]);
			}

			if(currentText.Length != 0)
			{
				Run run = new Run(currentText.ToString());
				run.Foreground = new SolidColorBrush(currentColor);
				runs.Add(run);
			}

			return runs;
		}
	}
}
