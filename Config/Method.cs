using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAVNativesWrapper.Config
{
	public class Method
	{
		public string Native { get; private set; }
		public string Name { get; private set; }
		public bool HasCustomName { get { return this.Name != null; } }
		public Dictionary<string,string> Params { get; private set; }

		public Method(string native) : this(native, null) {}

		public Method(string native, string name)
		{
			this.Native = native;
			this.Name = name;
			this.Params = new Dictionary<string, string>();
		}
	}
}
