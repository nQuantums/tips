using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using DbCode;
using DbCode.Query;
using DbCode.Internal;

namespace DbCode {
	/// <summary>
	/// クエリの土台
	/// </summary>
	public partial class Sql : IQueryNode {
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
		/// 指定ノードを子とする、既存の親は<see cref="RemoveChild(IQueryNode)"/>で切り離す必要がある
		/// </summary>
		/// <param name="child">子ノード</param>
		public void AddChild(IQueryNode child) {
			var parent = child.Parent;
			if(parent != null) {
				parent.RemoveChild(child);
			}
			this.Children.Add(child);
		}

		/// <summary>
		/// 指定の子ノードを取り除く
		/// </summary>
		/// <param name="child">子ノード</param>
		public void RemoveChild(IQueryNode child) {
			this.Children.Remove(child);
		}

		/// <summary>
		/// 親ノードが変更された際に呼び出される
		/// </summary>
		/// <param name="parent">新しい親ノード</param>
		public void ChangeParent(IQueryNode parent) {
		}

		/// <summary>
		/// テーブルを使用できるよう登録する
		/// </summary>
		/// <param name="table">テーブル</param>
		public void RegisterTable(ITable table) {
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
			this.AddChild(node);
			return node;
		}

		/// <summary>
		/// コードをそのままノードにする
		/// </summary>
		/// <param name="code">コード</param>
		/// <returns>ノード</returns>
		public CodeNode Code(string code) {
			var node = new CodeNode(this, new ElementCode(code));
			this.AddChild(node);
			return node;
		}

		/// <summary>
		/// 指定された名前のストアドプロシージャ（戻り値有り）を呼び出すノードを作成する
		/// </summary>
		/// <param name="funcName">ストアドプロシージャ名</param>
		/// <param name="arguments">ストアドプロシージャへの引数列</param>
		/// <returns>ノード</returns>
		public CodeNode CallFunc(string funcName, params Argument[] arguments) {
			var code = new ElementCode();
			code.Add(SqlKeyword.Select);
			code.Concat(funcName);
			code.BeginParenthesize();
			for (int i = 0; i < arguments.Length; i++) {
				if (i != 0) {
					code.AddComma();
				}
				code.Add(arguments[i]);
			}
			code.EndParenthesize();
			return this.Code(code);
		}

		/// <summary>
		/// 指定のテーブル定義を破棄する
		/// </summary>
		/// <param name="table">テーブル定義</param>
		/// <returns>DROP TABLE句ノード</returns>
		public DropTable DropTable(ITableDef table) {
			var node = new DropTable(this, table);
			this.AddChild(node);
			return node;
		}

		/// <summary>
		/// テーブルを指定してFROM句ノードを作成する
		/// </summary>
		/// <typeparam name="TColumns">プロパティを列として扱う<see cref="TableDef{TColumns}"/>のTColumnsに該当するクラス</typeparam>
		/// <param name="columnsExpression">プロパティが列指定として扱われるクラスを生成する () => new { Name = "test", ID = 1 } の様な式</param>
		/// <returns>VALUESノード</returns>
		public ListAsValues<TColumns> Values<TColumns>(Expression<Func<TColumns>> columnsExpression) {
			var node = new ListAsValues<TColumns>(this, columnsExpression);
			this.AddChild(node);
			return node;
		}

		/// <summary>
		/// テーブルを指定してFROM句ノードを作成する
		/// </summary>
		/// <typeparam name="TColumns">プロパティを列として扱う<see cref="TableDef{TColumns}"/>のTColumnsに該当するクラス</typeparam>
		/// <param name="table">入力テーブル</param>
		/// <returns>FROM句ノード</returns>
		[SqlMethod]
		public From<TColumns> From<TColumns>(TableDef<TColumns> table) {
			var node = new From<TColumns>(this, table);
			this.AddChild(node);
			return node;
		}

		/// <summary>
		/// SELECTを指定してFROM句ノードを作成する
		/// </summary>
		/// <typeparam name="TColumns">プロパティを列として扱う<see cref="TableDef{TColumns}"/>のTColumnsに該当するクラス</typeparam>
		/// <param name="select">入力元のSELECTノード</param>
		/// <returns>FROM句ノード</returns>
		[SqlMethod]
		public From<TColumns> From<TColumns>(ISelect<TColumns> select) {
			var node = new From<TColumns>(this, select);
			this.AddChild(node);
			return node;
		}

		/// <summary>
		/// 値をテーブルへ挿入する
		/// </summary>
		/// <typeparam name="TColumns">プロパティを列として扱う<see cref="TableDef{TColumns}"/>のTColumnsに該当するクラス</typeparam>
		/// <param name="table">挿入先テーブル</param>
		/// <param name="columnsExpression">列と設定値を指定する t => new[] { t.A == a, t.B == b } の様な式</param>
		/// <returns>INSERT INTO句ノード</returns>
		public InsertInto<TColumns> InsertInto<TColumns>(TableDef<TColumns> table, Expression<Func<TColumns, bool[]>> columnsExpression) {
			var node = new InsertInto<TColumns>(this, table, columnsExpression);
			this.AddChild(node);
			return node;
		}

		/// <summary>
		/// 同一値が無い場合のみ値をテーブルへ挿入する
		/// </summary>
		/// <typeparam name="TColumns">プロパティを列として扱う<see cref="TableDef{TColumns}"/>のTColumnsに該当するクラス</typeparam>
		/// <param name="table">挿入先テーブル</param>
		/// <param name="columnsExpression">列と設定値を指定する t => new[] { t.A == a, t.B == b } の様な式</param>
		/// <param name="columnCountToWhere">NOT EXISTS (SELECT * FROM t WHERE t.Name = "test") の部分で判定に使用する列数、0が指定されたら全て使用する</param>
		/// <returns>INSERT INTO句ノード</returns>
		public InsertInto<TColumns> InsertIntoIfNotExists<TColumns>(TableDef<TColumns> table, Expression<Func<TColumns, bool[]>> columnsExpression, int columnCountToWhere = 0) {
			var node = new InsertInto<TColumns>(this, table, columnsExpression);
			node.IfNotExists(columnCountToWhere);
			this.AddChild(node);
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
		/// <see cref="IDbCodeCommand"/>に渡して実行可能な形式にビルドする
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
