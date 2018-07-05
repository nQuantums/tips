using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace FT4 {
	/// <summary>
	/// ソースコード生成の基本クラス
	/// </summary>
	[DataContract]
	public class CodeGen {
		public string UsingForInlining() {
			return "using System.Runtime.CompilerServices;";
		}

		public string Inlining() {
			return "[MethodImpl(MethodImplOptions.AggressiveInlining)]";
		}
	}
}
