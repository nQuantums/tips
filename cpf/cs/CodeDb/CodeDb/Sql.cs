using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using CodeDb.Query;
using CodeDb.Internal;

namespace CodeDb {
	/// <summary>
	/// クエリの土台
	/// </summary>
	public class Sql : IQueryNode {
		#region プロパティ
		/// <summary>
		/// ノードが属するSQLオブジェクト
		/// </summary>
		public Sql Owner => this;

		/// <summary>
		/// 親ノード
		/// </summary>
		public IQueryNode Parent => null;

		/// <summary>
		/// DB接続環境
		/// </summary>
		public DbEnvironment Environment { get; private set; }

		/// <summary>
		/// 子ノード一覧
		/// </summary>
		public List<IQueryNode> Children { get; private set; }
		IEnumerable<IQueryNode> IQueryNode.Children => this.Children;

		/// <summary>
		/// 使用可能な全ての列
		/// </summary>
		public ColumnMap AllColumns { get; private set; }

		/// <summary>
		/// 使用可能な全てのテーブル
		/// </summary>
		public HashSet<ITable> AllTables { get; private set; }
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ、DB接続環境を指定して初期化する
		/// </summary>
		public Sql(DbEnvironment environment) {
			this.Environment = environment;
			this.Children = new List<IQueryNode>();
			this.AllColumns = new ColumnMap();
			this.AllTables = new HashSet<ITable>();
		}
		#endregion

		#region 公開メソッド
		/// <summary>
		/// テーブルを使用できるよう登録する
		/// </summary>
		/// <param name="table">テーブル</param>
		public void Register(ITable table) {
			if (!this.AllTables.Contains(table)) {
				this.AllColumns.Include(table.ColumnMap);
			}
		}

		public From<TColumns> From<TColumns>(ISelect<TColumns> tableOrSelect) {
			var node = new From<TColumns>(this, tableOrSelect);
			this.Children.Add(node);
			return node;
		}

		public InsertInto<TColumns, TColumnsOrder> InsertInto<TColumns, TColumnsOrder>(TableDef<TColumns> table, Expression<Func<TColumns, TColumnsOrder>> columnsExpression) {
			var node = new InsertInto<TColumns, TColumnsOrder>(this, table, columnsExpression);
			this.Children.Add(node);
			return node;
		}

		public static bool Like(string text, string pattern) {
			return default(bool);
		}

		public static bool Exists(ISelect select) {
			return default(bool);
		}

		public static bool NotExists(ISelect select) {
			return default(bool);
		}

		public void BuildSql(ElementCode context) {
			foreach (var node in this.Children) {
				node.BuildSql(context);
			}
		}
		#endregion
	}
}
