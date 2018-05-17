 
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using DbCode.Query;

namespace DbCode {
	/// <summary>
	/// DB接続環境基本クラス
	/// </summary>
	public abstract class DbEnvironment {
		/// <summary>
		/// 新規SQLオブジェクトを生成する
		/// </summary>
		/// <returns>SQLオブジェクト</returns>
		public abstract Sql NewSql();

		/// <summary>
		/// DBへのコネクションの作成
		/// </summary>
		/// <param name="connectionString">接続文字列</param>
		/// <returns>コネクション</returns>
		public abstract IDbCodeConnection CreateConnection(string connectionString);

		/// <summary>
		/// ログインロールの作成
		/// </summary>
		/// <param name="connection">コネクション</param>
		/// <param name="roleName">ロール名</param>
		/// <param name="password">パスワード</param>
		public abstract void CreateRole(IDbCodeConnection connection, string roleName, string password);

		/// <summary>
		/// データベースの作成
		/// </summary>
		/// <param name="connection">コネクション</param>
		/// <param name="databaseName">データベース名</param>
		/// <param name="owner">データベースの所有者ロール名</param>
		public abstract void CreateDatabase(IDbCodeConnection connection, string databaseName, string owner);

		/// <summary>
		/// 指定のコネクションで接続中のデータベース情報の取得、データベースから実際に定義を読み込む
		/// </summary>
		/// <param name="connection">コネクション</param>
		/// <returns>データベース定義</returns>
		public abstract IDatabaseDef ReadDatabaseDef(IDbCodeConnection connection);

		/// <summary>
		/// 指定クラスからデータベース定義を生成する、<see cref="ITableDef"/>インターフェースを取得できるスタティックプロパティがテーブルとなる
		/// </summary>
		/// <param name="databaseDefType">データベース定義生成元タイプ</param>
		/// <param name="databaseName">データベース名</param>
		/// <returns>データベース定義</returns>
		public abstract IDatabaseDef GenerateDatabaseDef(Type databaseDefType, string databaseName);

		/// <summary>
		/// データベース変化分情報の取得
		/// </summary>
		/// <param name="currentDatabaseDef">現在のデータベース定義、<see cref="ReadDatabaseDef"/>で取得可能</param>
		/// <param name="targetDatabaseDef">目標とするデータベース定義、<see cref="GenerateDatabaseDef"/>で取得可能</param>
		/// <returns>データベース変化分情報</returns>
		public abstract IDatabaseDelta GetDatabaseDelta(IDatabaseDef currentDatabaseDef, IDatabaseDef targetDatabaseDef);

		/// <summary>
		/// プログラム上での型から対応するDB上での型を作成する
		/// </summary>
		/// <param name="type">プログラム上での型</param>
		/// <returns>DB上での型</returns>
		public abstract IDbType CreateDbTypeFromType(Type type);

		/// <summary>
		/// 列名などを予約後と認識されないようにする
		/// </summary>
		/// <param name="name">名前</param>
		/// <returns>名前</returns>
		public abstract string Quote(string name);

		/// <summary>
		/// データベースに変化分を適用する
		/// </summary>
		/// <param name="context">コマンド生成先のバッファ</param>
		/// <param name="databaseDelta">データベース変化分</param>
		public abstract void ApplyDatabaseDelta(ElementCode context, IDatabaseDelta databaseDelta);

		/// <summary>
		/// レコード読み取りオブジェクトを作成する
		/// </summary>
		/// <param name="propertiesToAssign">列をバインドする<typeparamref name="T"/>内のプロパティ情報配列、読み取られる列の並びに一致している、<typeparamref name="T"/>がプリミティブ型の場合、または全プロパティを使用するには null を指定する</param>
		/// <typeparam name="T">レコードの型</typeparam>
		/// <returns>レコード読み取りオブジェクト</returns>
		public abstract IRecordReader<T> CreateRecordReader<T>(PropertyInfo[] propertiesToAssign);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract bool Bool(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract bool[] BoolArray(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract bool? BoolNull(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract char Char(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract char[] CharArray(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract char? CharNull(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract int Int32(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract int[] Int32Array(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract int? Int32Null(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract long Int64(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract long[] Int64Array(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract long? Int64Null(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract double Real64(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract double[] Real64Array(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract double? Real64Null(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract string String(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract string[] StringArray(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract Guid Uuid(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract Guid[] UuidArray(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract Guid? UuidNull(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract DateTime DateTime(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract DateTime[] DateTimeArray(string name, ColumnFlags flags = 0);

		/// <summary>
		/// 列を定義する、<see cref="DbCode.Internal.Mediator.Table"/>の<see cref="ITable.BindColumn"/>を呼び出し結果の列定義が<see cref="DbCode.Internal.Mediator.Column"/>に代入される
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <returns>ダミー値</returns>
		public abstract DateTime? DateTimeNull(string name, ColumnFlags flags = 0);
	}
}
