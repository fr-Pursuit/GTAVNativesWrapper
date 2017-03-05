using System.Collections.Generic;

namespace GTAVNativesWrapper.Config
{
	public class Class
	{
		private readonly Dictionary<string, Method> methods;

		public Class()
		{
			this.methods = new Dictionary<string, Method>();
		}

		public void AddMethod(Method method)
		{
			this.methods.Add(method.Native, method);
		}

		public Method this[string name]
		{
			get
			{
				Method method = null;
				this.methods.TryGetValue(name, out method);
				return method;
			}
		}
	}
}
