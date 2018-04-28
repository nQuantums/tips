﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using CodeDb.Query;
using CodeDb.Internal;

namespace CodeDb {
	/// <summary>
	/// テーブル定義基本クラス
	/// </summary>
	/// <typeparam name="TColumns"><see cref="ColumnsBase"/>を継承しプロパティを列として扱うクラス</typeparam>
	public class TableDef<TColumns> : ITable<TColumns>, ITableDef {
		#region スタティック要素
		static Action<TColumns> _AllColumnsBinder;

		/// <summary>
		/// <see cref="TColumns"/>の全プロパティを取得することで<see cref="ITable.BindColumn"/>を呼び出し<see cref="Column"/>を結びつけるアクション
		/// </summary>
		protected static Action<TColumns> AllColumnsBinder {
			get {
				if (_AllColumnsBinder == null) {
					var type = typeof(TColumns);
					var param = Expression.Parameter(type);
					var properties = type.GetProperties();
					var expressions = new Expression[properties.Length];
					for (int i = 0; i < properties.Length; i++) {
						expressions[i] = Expression.Property(param, properties[i]);
					}
					var expr = Expression.Lambda<Action<TColumns>>(Expression.Block(expressions), param);
					_AllColumnsBinder = (Action<TColumns>)expr.Compile();
				}
				return _AllColumnsBinder;
			}
		}

		/// <summary>
		/// プライマリキー定義を生成する、<see cref="GetPrimaryKey"/>内で呼び出す
		/// </summary>
		/// <param name="getters"><see cref="Columns"/>のプロパティを呼び出す処理を指定する</param>
		/// <returns>プライマリキー定義</returns>
		protected virtual IPrimaryKeyDef MakePrimaryKey(params Func<object>[] getters) {
			Mediator.Table = this;
			Mediator.TableName = this.Name;
			try {
				var colDefs = new IColumnDef[getters.Length];
				for (int i = 0; i < getters.Length; i++) {
					colDefs[i] = Mediator.GetFrom(getters[i]);
				}
				return new PrimaryKeyDef(colDefs);
			} finally {
				Mediator.Table = null;
				Mediator.TableName = null;
			}
		}

		/// <summary>
		/// インデックス定義を生成する
		/// </summary>
		/// <param name="flags">インデックスに設定するフラグ</param>
		/// <param name="getters"><see cref="Columns"/>のプロパティを呼び出す処理を指定する</param>
		/// <returns>インデックス定義</returns>
		protected virtual IIndexDef MakeIndex(IndexFlags flags, params Func<object>[] getters) {
			Mediator.Table = this;
			Mediator.TableName = this.Name;
			try {
				var colDefs = new IColumnDef[getters.Length];
				for (int i = 0; i < getters.Length; i++) {
					colDefs[i] = Mediator.GetFrom(getters[i]);
				}
				return new IndexDef(flags, colDefs);
			} finally {
				Mediator.Table = null;
				Mediator.TableName = null;
			}
		}

		/// <summary>
		/// インデックス定義列を生成する、<see cref="GetIndices"/>内で呼び出す
		/// </summary>
		/// <param name="idxs">インデックス定義列</param>
		/// <returns>インデックス定義列</returns>
		protected static IIndexDef[] MakeIndices(params IIndexDef[] idxs) {
			return idxs;
		}
		#endregion

		#region プロパティ
		/// <summary>
		/// DB接続環境
		/// </summary>
		public DbEnvironment Environment { get; private set; }

		/// <summary>
		/// テーブル名
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// 列をプロパティとして持つオブジェクト
		/// </summary>
		public TColumns Columns { get; private set; }

		/// <summary>
		/// 列をプロパティとして持つオブジェクト
		/// </summary>
		public TColumns _ => this.Columns;

		/// <summary>
		/// テーブルが直接保持する列定義の取得
		/// </summary>
		public ColumnMap ColumnMap { get; private set; }

		/// <summary>
		/// テーブルが直接保持する列定義の取得
		/// </summary>
		public IEnumerable<IColumnDef> ColumnDefs => this.ColumnMap;
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ、DB接続環境とテーブル名を指定して初期化する
		/// </summary>
		/// <param name="environment">DB接続環境</param>
		/// <param name="name">DB上のテーブル名</param>
		public TableDef(DbEnvironment environment, string name) {
			this.Environment = environment;
			this.Name = name;
			this.ColumnMap = new ColumnMap();

			// プロパティと列をバインドする
			Mediator.Table = this;
			try {
				this.Columns = TypeWiseCache<TColumns>.Creator();

				// 全プロパティを一度呼び出す事でバインドされる
				AllColumnsBinder(this.Columns);
			} finally {
				Mediator.Table = null;
			}
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
		public Column BindColumn(string propertyName, string name, IDbType dbType, ColumnFlags flags = 0, ElementCode source = null) {
			var column = this.ColumnMap.TryGetByPropertyName(propertyName);
			if (column == null) {
				this.ColumnMap.Add(column = new Column(this.Environment, this.Columns, typeof(TColumns).GetProperty(propertyName), this, name, dbType, flags, source));
			}
			return column;
		}

		/// <summary>
		/// プライマリキー定義を取得する、派生先クラスでオーバーライドする必要がある
		/// </summary>
		/// <returns>プライマリキー定義</returns>
		public virtual IPrimaryKeyDef GetPrimaryKey() {
			return null;
		}

		/// <summary>
		/// インデックス定義を取得する、派生先クラスでオーバーライドする必要がある
		/// </summary>
		/// <returns>インデックス定義列</returns>
		public virtual IEnumerable<IIndexDef> GetIndices() {
			return new IIndexDef[0];
		}

		/// <summary>
		/// エイリアス用にクローンを作成する
		/// </summary>
		/// <returns>クローン</returns>
		public TableDef<TColumns> AliasedClone() {
			var c = this.MemberwiseClone() as TableDef<TColumns>;
			ColumnMap map;
			TColumns columns;
			c.ColumnMap = map = new ColumnMap();
			c.Columns = columns = TypeWiseCache<TColumns>.Creator();
			foreach (var column in this.ColumnMap) {
				map.Add(column.AliasedClone(columns, c));
			}
			return c;
		}

		/// <summary>
		/// SQL文を生成する
		/// </summary>
		/// <param name="context">生成先のコンテキスト</param>
		public void BuildSql(ElementCode context) {
			context.Concat(this.Name);
		}
		public override string ToString() {
			return this.Name;
		}
		#endregion

		#region 非公開メソッド
		ITable<TColumns> ITable<TColumns>.AliasedClone() => this.AliasedClone();
		ITable ITable.AliasedClone() => this.AliasedClone();
		#endregion
	}
}
