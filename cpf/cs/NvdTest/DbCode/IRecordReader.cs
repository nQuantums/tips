using System;
using System.Collections.Generic;
using System.Text;

namespace DbCode {
	/// <summary>
	/// <see cref="IDbCodeDataReader"/>からデータを読み取り<typeparamref name="T"/>型のレコードを列挙するオブジェクト
	/// </summary>
	/// <typeparam name="T">レコード型</typeparam>
	public interface IRecordReader<T> {
		/// <summary>
		/// レコードの列挙子を取得する
		/// </summary>
		/// <param name="dataReader">レコード作成元データを読み取る<see cref="IDbCodeDataReader"/></param>
		/// <returns>レコード列挙子</returns>
		IEnumerable<T> Enumerate(IDbCodeDataReader dataReader);
	}
}