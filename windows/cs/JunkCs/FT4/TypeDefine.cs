using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT4 {
	/// <summary>
	/// T4テンプレートで生成するクラス内要素の型定義
	/// </summary>
	public class TypeDefine {
		public static readonly TypeDefine[] AllTypes = new TypeDefine[] {
			new TypeDefine("int", "System.Int32", "i", 4, false),
			new TypeDefine("float", "System.Single", "f", 4, true),
			new TypeDefine("double", "System.Double", "d", 8, true),
		};

		/// <summary>
		/// <see cref="int"/>等のエイリアス名
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// <see cref="System.Int32"/>等のフル名称
		/// </summary>
		public readonly string FullName;

		/// <summary>
		/// 超短縮名称、iやfなど
		/// </summary>
		public readonly string ShortName;

		/// <summary>
		/// バイト数で型のサイズ
		/// </summary>
		public readonly int Size;

		/// <summary>
		/// 実数かどうか
		/// </summary>
		public readonly bool IsReal;


		TypeDefine(string name, string fullName, string shortName, int size, bool isReal) {
			this.Name = name;
			this.FullName = fullName;
			this.ShortName = shortName;
			this.Size = size;
			this.IsReal = isReal;
		}

		/// <summary>
		/// エイリアス名から型定義を取得する
		/// </summary>
		/// <param name="name"><see cref="int"/>等のエイリアス名</param>
		/// <returns>型定義またはnull</returns>
		public static TypeDefine FromName(string name) {
			var t = (from td in AllTypes where td.Name == name select td).FirstOrDefault();
			if(t == null)
				throw new NotImplementedException("\"" + name + "\" type is not supported.");
			return t;
		}

		/// <summary>
		/// 指定された型定義以外を取得する
		/// </summary>
		/// <param name="td">型定義</param>
		/// <returns>型定義配列</returns>
		public static TypeDefine[] Other(TypeDefine td) {
			return (from td2 in AllTypes where td != td2 select td2).ToArray();
		}
	}
}
