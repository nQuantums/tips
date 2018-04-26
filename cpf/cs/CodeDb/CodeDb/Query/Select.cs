using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using CodeDb.Internal;

namespace CodeDb.Query {
	public class Select<TypeOfColumns> : ISelect<TypeOfColumns> {
		#region スタティック要素
		static Func<ICodeDbDataReader, IEnumerable<TypeOfColumns>> _Reader;
		#endregion

		#region プロパティ
		/// <summary>
		/// DB接続環境
		/// </summary>
		public DbEnvironment Environment { get; private set; }

		/// <summary>
		/// 生成元のFROM
		/// </summary>
		public IFrom From { get; private set; }

		/// <summary>
		/// 列をプロパティとして持つオブジェクト
		/// </summary>
		public TypeOfColumns Columns { get; private set; }

		/// <summary>
		/// 列をプロパティとして持つオブジェクト
		/// </summary>
		public TypeOfColumns _ => this.Columns;

		/// <summary>
		/// テーブルが直接保持する列定義の取得
		/// </summary>
		public ColumnMap ColumnMap { get; private set; }

		/// <summary>
		/// このテーブルを構成するのに必要な全ての列定義を取得する
		/// </summary>
		public ColumnMap SourceColumnMap => this.From.SourceColumnMap;

		/// <summary>
		/// <see cref="ICodeDbDataReader"/>から<see cref="TypeOfColumns"/>を列挙するファンクション
		/// </summary>
		public Func<ICodeDbDataReader, IEnumerable<TypeOfColumns>> Reader {
			get {
				if (_Reader == null) {
					_Reader = dr => dr.Enumerate<TypeOfColumns>();
				}
				return _Reader;
			}
		}
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="environment">DB接続環境</param>
		/// <param name="from">生成元の<see cref="IFrom"/></param>
		public Select(DbEnvironment environment, IFrom from) {
			this.Environment = environment;
			this.From = from;
			this.ColumnMap = new ColumnMap();
			this.Columns = TypeWiseCache<TypeOfColumns>.Creator();
		}
		#endregion

		#region 公開メソッド
		/// <summary>
		/// プロパティに列定義をバインドして取得する、バインド済みなら取得のみ行われる
		/// </summary>
		/// <param name="propertyName">プロパティ名</param>
		/// <param name="name">DB上での列名</param>
		/// <param name="dbType">DB上での型</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <param name="source">列を生成する基となった式</param>
		/// <returns>列定義</returns>
		public Column BindColumn(string propertyName, string name, IDbType dbType, ColumnFlags flags = 0, ExpressionInProgress source = null) {
			var column = this.ColumnMap.TryGetByPropertyName(propertyName);
			if (column == null) {
				this.ColumnMap.Add(column = new Column(this.Environment, this.Columns, typeof(TypeOfColumns).GetProperty(propertyName), this, name, dbType, flags, source));
			}
			return column;
		}

		/// <summary>
		/// エイリアス用にクローンを作成する
		/// </summary>
		/// <returns>クローン</returns>
		public Select<TypeOfColumns> AliasedClone() {
			var c = this.MemberwiseClone() as Select<TypeOfColumns>;
			ColumnMap map;
			TypeOfColumns columns;
			c.ColumnMap = map = new ColumnMap();
			c.Columns = columns = TypeWiseCache<TypeOfColumns>.Cloner(this.Columns);
			foreach (var column in this.ColumnMap) {
				map.Add(column.AliasedClone(columns, c));
			}
			// TODO: 生成元は同じなので this.SourceColumnMap はそのまま使えるかもしれない
			return c;
		}

		ITable<TypeOfColumns> ITable<TypeOfColumns>.AliasedClone() {
			return this.AliasedClone();
		}

		ITable ITable.AliasedClone() {
			return this.AliasedClone();
		}

		/// <summary>
		/// SQL文を生成する
		/// </summary>
		/// <param name="context">生成先のコンテキスト</param>
		public void BuildSql(ExpressionInProgress context) {
			context.Add(SqlKeyword.Select);
			var columns = this.ColumnMap;
			for (int i = 0, n = columns.Count; i < n; i++) {
				if (i != 0) {
					context.AddComma();
				}
				context.Add(columns[i].Source);
				context.Add(SqlKeyword.As);
				context.Concat("c" + i);
			}
			this.From.BuildSql(context);
		}
		#endregion
	}
}
