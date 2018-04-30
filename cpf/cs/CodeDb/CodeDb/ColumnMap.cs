using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using CodeDb.Query;

namespace CodeDb {
	/// <summary>
	/// 列定義マップ、<see cref="Column"/>のリスト、実際の列の順番と一致している
	/// </summary>
	public class ColumnMap : List<Column> {
		/// <summary>
		/// 指定されたインスタンスのプロパティから対応する列定義を取得する
		/// </summary>
		/// <param name="instance">インスタンス</param>
		/// <param name="property">プロパティ</param>
		/// <returns>列定義</returns>
		public Column TryGet(object instance, PropertyInfo property) {
			for (int i = 0, n = this.Count; i < n; i++) {
				var col = this[i];
				if (col.Instance == instance && col.Property == property) {
					return col;
				}
			}
			return null;
		}

		/// <summary>
		/// 指定された名前の列定義を取得する
		/// </summary>
		/// <param name="name">列名</param>
		/// <returns>列定義</returns>
		public Column TryGetByName(string name) {
			for (int i = 0, n = this.Count; i < n; i++) {
				var col = this[i];
				if (col.Name == name) {
					return col;
				}
			}
			return null;
		}

		/// <summary>
		/// 指定された名前のプロパティから対応する列定義を取得する
		/// </summary>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>列定義</returns>
		public Column TryGetByPropertyName(string propertyName) {
			for (int i = 0, n = this.Count; i < n; i++) {
				var col = this[i];
				if (col.Property.Name == propertyName) {
					return col;
				}
			}
			return null;
		}

		/// <summary>
		/// 同じ列定義を保持していない場合のみ追加する
		/// </summary>
		/// <param name="colDef">列定義</param>
		public void Include(Column colDef) {
			if (!this.Contains(colDef)) {
				base.Add(colDef);
			}
		}

		/// <summary>
		/// 同じ列定義を保持していない場合のみ追加する
		/// </summary>
		/// <param name="map">列定義マップ</param>
		public void Include(ColumnMap map) {
			for (int i = 0, n = map.Count; i < n; i++) {
				Include(map[i]);
			}
		}

		/// <summary>
		/// ２つの列定義マップの共有を作成する
		/// </summary>
		/// <param name="a">列定義マップ a</param>
		/// <param name="b">列定義マップ b</param>
		/// <returns>列定義マップ</returns>
		public static ColumnMap Union(ColumnMap a, ColumnMap b) {
			var result = new ColumnMap();
			for (int i = 0, n = a.Count; i < n; i++) {
				result.Include(a[i]);
			}
			for (int i = 0, n = b.Count; i < n; i++) {
				result.Include(b[i]);
			}
			return result;
		}
	}
}
