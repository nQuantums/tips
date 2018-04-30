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

		/// <summary>
		/// コードをそのままノードにする
		/// </summary>
		/// <param name="code">コード</param>
		/// <returns>ノード</returns>
		public CodeNode Code(ElementCode code) {
			var node = new CodeNode(this, code);
			this.Children.Add(node);
			return node;
		}

		/// <summary>
		/// 指定のテーブル定義を破棄する
		/// </summary>
		/// <param name="table">テーブル定義</param>
		/// <returns>DROP TABLE句ノード</returns>
		public DropTable DropTable(ITableDef table) {
			var node = new DropTable(this, table);
			this.Children.Add(node);
			return node;
		}

		public From<TColumns> From<TColumns>(ISelect<TColumns> tableOrSelect) {
			var node = new From<TColumns>(this, tableOrSelect);
			this.Children.Add(node);
			return node;
		}

		/// <summary>
		/// 値をテーブルへ挿入する
		/// </summary>
		/// <typeparam name="TColumns">プロパティを列として扱う<see cref="TableDef{TColumns}"/>のTColumnsに該当するクラス</typeparam>
		/// <typeparam name="TColumnsOrder">列の順番を指定する匿名クラス</typeparam>
		/// <param name="table">挿入先テーブル</param>
		/// <param name="columnsAssignExpression">列への値代入を示す t => new { Name = "test", ID = 1 } の様な式、入力の t は列名参考用に使うのみ</param>
		/// <param name="columnCountToWhere">NOT EXISTS (SELECT * FROM t WHERE t.Name = "test") の部分で判定に使用する列数、0が指定されたら全て使用する</param>
		/// <returns>INSERT INTO句ノード</returns>
		public InsertInto<TColumns, TColumnsOrder> InsertInto<TColumns, TColumnsOrder>(TableDef<TColumns> table, Expression<Func<TColumns, TColumnsOrder>> columnsAssignExpression) {
			var node = new InsertInto<TColumns, TColumnsOrder>(this, table, columnsAssignExpression);
			this.Children.Add(node);
			node.Select(columnsAssignExpression);
			return node;
		}

		/// <summary>
		/// 同一の値が無い場合のみテーブルへ挿入する
		/// </summary>
		/// <typeparam name="TColumns">プロパティを列として扱う<see cref="TableDef{TColumns}"/>のTColumnsに該当するクラス</typeparam>
		/// <typeparam name="TColumnsOrder">列の順番を指定する匿名クラス</typeparam>
		/// <param name="table">挿入先テーブル</param>
		/// <param name="columnsAssignExpression">列への値代入を示す t => new { Name = "test", ID = 1 } の様な式、入力の t は列名参考用に使うのみ</param>
		/// <param name="columnCountToWhere">NOT EXISTS (SELECT * FROM t WHERE t.Name = "test") の部分で判定に使用する列数、0が指定されたら全て使用する</param>
		/// <returns>INSERT INTO句ノード</returns>
		public InsertInto<TColumns, TColumnsOrder> InsertIntoIfNotExists<TColumns, TColumnsOrder>(TableDef<TColumns> table, Expression<Func<TColumns, TColumnsOrder>> columnsAssignExpression, int columnCountToWhere = 0) {
			var node = new InsertInto<TColumns, TColumnsOrder>(this, table, columnsAssignExpression);
			this.Children.Add(node);
			node.SelectIfNotExists(columnsAssignExpression, columnCountToWhere);
			return node;
		}

		/// <summary>
		/// SQL文を生成する
		/// </summary>
		/// <param name="context">生成先のコンテキスト</param>
		public void ToElementCode(ElementCode context) {
			foreach (var node in this.Children) {
				node.ToElementCode(context);
				context.Go();
			}
		}

		/// <summary>
		/// <see cref="ICodeDbCommand"/>に渡して実行可能な形式にビルドする
		/// </summary>
		/// <returns>実行可能SQL</returns>
		public Commandable Build() {
			var context = new ElementCode();
			foreach (var node in this.Children) {
				node.ToElementCode(context);
				context.Go();
			}
			return context.Build();
		}

		/// <summary>
		/// <see cref="ICodeDbCommand"/>に渡して実行可能な形式にビルドする、<see cref="FuncCmd{T}.Execute(ICodeDbCommand)"/>を呼び出す事で指定型のレコードを列挙可能
		/// </summary>
		/// <typeparam name="T">列挙するレコードの型</typeparam>
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T> Build<T>() {
			return new FuncCmd<T>(this.Build());
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
		#endregion
	}
}
