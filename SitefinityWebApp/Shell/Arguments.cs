using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SitefinitySupport.Shell
{
	public class Arguments
	{
		List<Tuple<string,string>> arguments;

		public Arguments(string args)
		{
			arguments = new List<Tuple<string, string>>();
			ParseArgs(args);
		}

		protected void ParseArgs(string args) {
			args = args.Trim() + " ";
			string arg, value;

			int len = args.Length, pos=0, argValPos = args.IndexOf("="), argNoValEnd = args.IndexOf(" ");

			while (argValPos >= 0 || argNoValEnd >= 0)
			{
				// If there is a space before a ':'
				if (argNoValEnd >= 0 && (argNoValEnd < argValPos || argValPos < 0))
				{
					arg = args.Substring(pos, argNoValEnd - pos);
					if (arg != "") arguments.Add(Tuple.Create(arg, ""));

					pos = argNoValEnd + 1;
					argNoValEnd = args.IndexOf(" ", pos);
				}
				// Otherwise there is a ':' before a space
				else
				{
					arg = args.Substring(pos, argValPos - pos);
					pos = argValPos + 1;
					argValPos = args.IndexOf("=", pos);
					if (argValPos < 0)
						argValPos = len - 1;
					else
						argValPos = args.LastIndexOf(" ", argValPos);

					if (argValPos >= 0)
						value = args.Substring(pos, argValPos - pos).Trim();
					else
						value = "";

					if (arg != "") arguments.Add(Tuple.Create(arg, value));
					pos = argValPos;
					argValPos = args.IndexOf("=", argValPos);
					argNoValEnd = args.IndexOf(" ", pos);
				}
			}

		}

		public bool ContainsKey(string arg)
		{
			return (arguments.Find(a => a.Item1 == arg) != null);
		}

		public string this[string arg]
		{
			get
			{
				var result = arguments.Find(a => a.Item1 == arg);
				if (result == null) return null;

				return result.Item2;
			}
		}

		public int Count
		{
			get
			{
				return arguments.Count;
			}
		}

		public string FirstKey
		{
			get
			{
				if (arguments.Count() == 0) return null;
				return arguments[0].Item1;
			}
		}

		public HashSet<string> Keys
		{
			get
			{
				return new HashSet<string>(arguments.Select(a => a.Item1));
			}
		}
	}
}