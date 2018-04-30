using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb {
	/// <summary>
	/// DB内での型とソース上での型との仲介機能を提供する
	/// </summary>
	public interface IDbType {
		/// <summary>
		/// 型オブジェクト取得
		/// </summary>
		Type Type { get; }

		/// <summary>
		/// DB内の型を示す文字列を取得する
		/// </summary>
		/// <param name="flags">オプションフラグ</param>
		/// <returns>DB内型名</returns>
		string ToDbTypeString(DbTypeStringFlags flags = 0);

		/// <summary>
		/// 自型が指定型と同じか調べる
		/// </summary>
		/// <param name="type">比較対象型</param>
		/// <returns>同じなら true</returns>
		bool TypeEqualsTo(IDbType type);
	}

	/// <summary>
	/// <see cref="IDbType.ToDbTypeString"/>用オプションフラグ
	/// </summary>
	public enum DbTypeStringFlags {
		/// <summary>
		/// 自動インクリメントを示す型
		/// </summary>
		Serial = 1 << 0,
	}
}
