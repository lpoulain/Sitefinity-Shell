using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SitefinitySupport.Logs
{
	class Util
	{
		// Reads a file even if it is locked
		public static IEnumerable<string> ReadLines(string path)
		{
			using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0x1000, FileOptions.SequentialScan))
			using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
			{
				string line;
				while ((line = sr.ReadLine()) != null)
				{
					yield return line;
				}
			}
		}
	}
}
