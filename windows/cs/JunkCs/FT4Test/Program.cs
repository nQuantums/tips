using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT4Test {
	class Program {
		static void Main(string[] args) {
			var fields = new string[] {
				"X",
				"Y",
				"Z",
				"W",
			};
			var i = 0;
			var t = new FT4.Template {
				E = fields[i],
				Ep = new string[] {
						fields[(i + 1) % fields.Length],
						fields[(i + 2) % fields.Length],
					},
				Em = new string[] {
						fields[(i - 1 + fields.Length) % fields.Length],
						fields[(i - 2 + fields.Length) % fields.Length],
					},
			};
			t.BuildLower();

			Console.WriteLine(t.Generate("A_E = a_e", 0));
			//Console.WriteLine(t.Generate("_Ep1 * v._Ep2 - _Ep2 * v._Ep1", 0));
		}
	}
}
