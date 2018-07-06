using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace FT4
{
	[DataContract]
	public class VectorDefines : CodeGen {
		[DataMember]
		public VectorDefine[] defines;

		/// <summary>
		/// 指定Jsonファイルからベクトル定義一覧を生成する
		/// </summary>
		/// <param name="path">Jsonファイルパス名</param>
		/// <returns>ベクトル定義一覧</returns>
		public static VectorDefines FromJsonFile(string path) {
			var s = File.ReadAllText(path, Encoding.UTF8);
			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(s))) {
				var serializer = new DataContractJsonSerializer(typeof(VectorDefines));
				return serializer.ReadObject(ms) as VectorDefines;
			}
		}

		/// <summary>
		/// 指定の出力先にベクトルクラスソースを生成する
		/// </summary>
		/// <param name="outputDir">ベクトルクラスソース出力先ディレクトリ</param>
		/// <param name="generationEnvironment">T4テンプレートが生成中に使用する<see cref="StringBuilder"/></param>
		/// <param name="genProc">ベクトル定義とT4テンプレートから<see cref="generationEnvironment"/>にソースを生成するデリゲート</param>
		public void Generate(string outputDir, StringBuilder generationEnvironment, Action<VectorDefine> genProc) {
			foreach (var d in this.defines) {
				var outputFile = Path.Combine(outputDir, d.ClassName + ".cs");
				try {
					genProc(d);
					File.WriteAllText(outputFile, generationEnvironment.ToString());
				} catch (Exception ex) {
					generationEnvironment.AppendLine();
					generationEnvironment.AppendLine("Failed to process template\n" + ex.StackTrace);
				} finally {
					generationEnvironment.Clear();
				}
			}
		}
	}

	[DataContract]
	public class VectorDefine : CodeGen {
		[DataMember]
		public string type;
		[DataMember]
		public int length;

		TypeDefine _TypeDefine;


		public TypeDefine TypeDefine {
			get => _TypeDefine ?? (_TypeDefine = TypeDefine.FromName(this.type));
		}

		public VectorDefine[] OtherTypes {
			get {
				return (from td in TypeDefine.AllTypes where td != this.TypeDefine select this.Clone(td)).ToArray();
			}
		}

		public string FullType {
			get {
				return this.TypeDefine.FullName;
			}
		}

		public int ElementSize {
			get {
				return this.TypeDefine.Size;
			}
		}

		public int FullSize {
			get {
				return this.ElementSize * this.length;
			}
		}

		public string Postfix {
			get {
				return this.TypeDefine.ShortName;
			}
		}

		public string[] Fields {
			get {
				switch (this.length) {
				case 2:
					return new string[] { "X", "Y" };
				case 3:
					return new string[] { "X", "Y", "Z" };
				case 4:
					return new string[] { "X", "Y", "Z", "W" };
				default:
					throw new NotImplementedException("Vector length " + this.length + " is not supported.");
				}
			}
		}

		public string ClassName {
			get {
				return "Vector" + this.length + this.Postfix;
			}
		}

		public string Axis(int i) {
			var sb = new StringBuilder();
			for (int j = 0; j < this.length; j++) {
				if (j != 0)
					sb.Append(", ");
				sb.Append(j == i ? "1" : "0");
			}
			return sb.ToString();
		}

		public string DefElems(string indent) {
			var sb = new StringBuilder();
			var offset = 0;
			var size = this.ElementSize;
			foreach (var f in this.Fields) {
				sb.AppendLine(indent + "[FieldOffset(" + offset + ")]");
				sb.AppendLine(indent + "public element " + f + ";");
				offset += size;
			}
			return sb.ToString();
		}

		public string Args() {
			var sb = new StringBuilder();
			var fields = this.Fields;
			for (int i = 0; i < fields.Length; i++) {
				if (i != 0)
					sb.Append(", ");
				sb.Append("element " + fields[i].ToLower());
			}
			return sb.ToString();
		}

		public string Repeat(string name) {
			var sb = new StringBuilder();
			for (int i = 0, n = this.length; i < n; i++) {
				if (i != 0)
					sb.Append(", ");
				sb.Append(name);
			}
			return sb.ToString();
		}

		public VectorDefine Clone(TypeDefine type) {
			var c = this.MemberwiseClone() as VectorDefine;
			c.type = type.Name;
			c._TypeDefine = type;
			return c;
		}

		/// <summary>
		/// 要素毎の処理コードを生成する
		/// </summary>
		/// <param name="template">要素毎に対応するコードを生成する</param>
		/// <param name="delimiter">null指定可能、要素毎に対応する区切りコードを生成する、<see cref="postfix"/>がnull以外ならインデックスとして要素数が渡る</param>
		/// <param name="prefix">null指定可能、要素毎コードの前に配置されるコード</param>
		/// <param name="postfix">null指定可能、最終要素コードの後に配置されるコード</param>
		/// <param name="terminator">null指定可能、生成したコードの終端コード</param>
		/// <param name="startIndex">要素列挙の開始インデックス</param>
		/// <returns>生成されたコード</returns>
		public string ElementWise(string template, string delimiter = "\n", string prefix = null, string postfix = null, string terminator = null, int startIndex = 0) {
			return this.ElementWise(
				(i) => template,
				delimiter != null ? (i) => delimiter : (Func<int, string>)null,
				prefix != null ? () => prefix : (Func<string>)null,
				postfix != null ? () => postfix : (Func<string>)null,
				terminator != null ? () => terminator : (Func<string>)null,
				startIndex);
		}

		/// <summary>
		/// 要素毎の処理コードを生成する
		/// </summary>
		/// <param name="template">要素毎に要素インデックスが渡りそれに対応するコードを生成する</param>
		/// <param name="delimiter">null指定可能、要素毎に要素インデックスが渡りそれに対応する区切りコードを生成する、<see cref="postfix"/>がnull以外ならインデックスとして要素数が渡る</param>
		/// <param name="prefix">null指定可能、要素毎コードの前に配置されるコードを生成する</param>
		/// <param name="postfix">null指定可能、最終要素コードの後に配置されるコードを生成する</param>
		/// <param name="terminator">null指定可能、生成したコードの終端コードを生成する</param>
		/// <param name="startIndex">要素列挙の開始インデックス</param>
		/// <returns>生成されたコード</returns>
		public string ElementWise(Func<int, string> template, Func<int, string> delimiter = null, Func<string> prefix = null, Func<string> postfix = null, Func<string> terminator = null, int startIndex = 0) {
			var sb = new StringBuilder();
			var fields = this.Fields;
			if (prefix != null)
				sb.Append(prefix());
			for (int i = startIndex; i < fields.Length; i++) {
				if (delimiter != null && i != startIndex)
					sb.Append(delimiter(i));

				var t = new Template {
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
				sb.Append(t.Generate(template(i), i));
			}
			if (postfix != null) {
				if (delimiter != null && sb.Length != 0)
					sb.Append(delimiter(fields.Length));
				sb.Append(postfix());
			}
			if (terminator != null)
				sb.Append(terminator());
			return sb.ToString();
		}
	}
}
