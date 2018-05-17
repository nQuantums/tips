 
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using DbCode;
using DbCode.Internal;
using Npgsql;
using NpgsqlTypes;

namespace DbCode.PgBind {
	/// <summary>
	/// Npgsql接続環境クラス
	/// </summary>
	public class PgEnvironment : PgEnvironmentBase {
		public override bool Bool(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(bool)), flags | Mediator.ColumnFlags);
			return default(bool);
		}
		public override bool[] BoolArray(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(bool[])), flags | Mediator.ColumnFlags);
			return default(bool[]);
		}
		public override bool? BoolNull(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(bool?)), flags | Mediator.ColumnFlags | ColumnFlags.Nullable);
			return default(bool?);
		}
		public override char Char(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(char)), flags | Mediator.ColumnFlags);
			return default(char);
		}
		public override char[] CharArray(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(char[])), flags | Mediator.ColumnFlags);
			return default(char[]);
		}
		public override char? CharNull(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(char?)), flags | Mediator.ColumnFlags | ColumnFlags.Nullable);
			return default(char?);
		}
		public override int Int32(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(int)), flags | Mediator.ColumnFlags);
			return default(int);
		}
		public override int[] Int32Array(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(int[])), flags | Mediator.ColumnFlags);
			return default(int[]);
		}
		public override int? Int32Null(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(int?)), flags | Mediator.ColumnFlags | ColumnFlags.Nullable);
			return default(int?);
		}
		public override long Int64(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(long)), flags | Mediator.ColumnFlags);
			return default(long);
		}
		public override long[] Int64Array(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(long[])), flags | Mediator.ColumnFlags);
			return default(long[]);
		}
		public override long? Int64Null(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(long?)), flags | Mediator.ColumnFlags | ColumnFlags.Nullable);
			return default(long?);
		}
		public override double Real64(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(double)), flags | Mediator.ColumnFlags);
			return default(double);
		}
		public override double[] Real64Array(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(double[])), flags | Mediator.ColumnFlags);
			return default(double[]);
		}
		public override double? Real64Null(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(double?)), flags | Mediator.ColumnFlags | ColumnFlags.Nullable);
			return default(double?);
		}
		public override string String(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(string)), flags | Mediator.ColumnFlags);
			return default(string);
		}
		public override string[] StringArray(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(string[])), flags | Mediator.ColumnFlags);
			return default(string[]);
		}
		public override Guid Uuid(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(Guid)), flags | Mediator.ColumnFlags);
			return default(Guid);
		}
		public override Guid[] UuidArray(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(Guid[])), flags | Mediator.ColumnFlags);
			return default(Guid[]);
		}
		public override Guid? UuidNull(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(Guid?)), flags | Mediator.ColumnFlags | ColumnFlags.Nullable);
			return default(Guid?);
		}
		public override DateTime DateTime(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(DateTime)), flags | Mediator.ColumnFlags);
			return default(DateTime);
		}
		public override DateTime[] DateTimeArray(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(DateTime[])), flags | Mediator.ColumnFlags);
			return default(DateTime[]);
		}
		public override DateTime? DateTimeNull(string name, ColumnFlags flags = 0) {
			Mediator.Column = Mediator.Table.BindColumn(Mediator.PropertyName, name, new PgDbType(typeof(DateTime?)), flags | Mediator.ColumnFlags | ColumnFlags.Nullable);
			return default(DateTime?);
		}
	}
}
