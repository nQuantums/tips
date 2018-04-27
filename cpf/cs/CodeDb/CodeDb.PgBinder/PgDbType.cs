using System;
using System.Collections.Generic;
using System.Text;
using NpgsqlTypes;

namespace CodeDb.PgBinder {
	/// <summary>
	/// PostgreSQLの型
	/// </summary>
	public class PgDbType : IDbType {
		static readonly Dictionary<Type, NpgsqlDbType> _TypeToNpgsqlDbType = new Dictionary<Type, NpgsqlDbType>() {
			{ typeof(bool), NpgsqlDbType.Boolean },
			{ typeof(bool?), NpgsqlDbType.Boolean },
			{ typeof(bool[]), NpgsqlDbType.Boolean | NpgsqlDbType.Array },
			{ typeof(char), NpgsqlDbType.Char },
			{ typeof(char?), NpgsqlDbType.Char },
			{ typeof(char[]), NpgsqlDbType.Char | NpgsqlDbType.Array },
			{ typeof(int), NpgsqlDbType.Integer },
			{ typeof(int?), NpgsqlDbType.Integer },
			{ typeof(int[]), NpgsqlDbType.Integer | NpgsqlDbType.Array },
			{ typeof(long), NpgsqlDbType.Bigint },
			{ typeof(long?), NpgsqlDbType.Bigint },
			{ typeof(long[]), NpgsqlDbType.Bigint | NpgsqlDbType.Array },
			{ typeof(double), NpgsqlDbType.Double },
			{ typeof(double?), NpgsqlDbType.Double },
			{ typeof(double[]), NpgsqlDbType.Double | NpgsqlDbType.Array },
			{ typeof(string), NpgsqlDbType.Text },
			{ typeof(string[]), NpgsqlDbType.Text | NpgsqlDbType.Array },
			{ typeof(Guid), NpgsqlDbType.Uuid },
			{ typeof(Guid?), NpgsqlDbType.Uuid },
			{ typeof(Guid[]), NpgsqlDbType.Uuid | NpgsqlDbType.Array },
			{ typeof(DateTime), NpgsqlDbType.Timestamp },
			{ typeof(DateTime?), NpgsqlDbType.Timestamp },
			{ typeof(DateTime[]), NpgsqlDbType.Timestamp | NpgsqlDbType.Array },
		};

		public Type _Type;

		public Type Type => _Type;
		public NpgsqlDbType DbType { get; private set; }

		public PgDbType(string udtName) : this(ToNpgsqlDbType(udtName)) {
		}

		public PgDbType(Type type) {
			NpgsqlDbType dbType;
			if (!_TypeToNpgsqlDbType.TryGetValue(type, out dbType)) {
				throw new ApplicationException($"'{type.FullName}' type is not supported.");
			}
			_Type = type;
			this.DbType = dbType;
		}

		public PgDbType(NpgsqlDbType type) {
			Type t = null;
			foreach (var kvp in _TypeToNpgsqlDbType) {
				if (kvp.Value == type) {
					t = kvp.Key;
					break;
				}
			}
			if (t == null) {
				throw new ApplicationException($"'{type}' type is not supported.");
			}
			this.DbType = type;
		}

		public override string ToString() {
			return ToDbTypeString();
		}

		public static NpgsqlDbType ToNpgsqlDbType(string udtName) {
			switch (udtName) {
			case "char":
				return NpgsqlDbType.Char;
			case "bytea":
				return NpgsqlDbType.Bytea;
			case "bit":
				return NpgsqlDbType.Bit;
			case "bool":
				return NpgsqlDbType.Boolean;
			case "timestamp":
				return NpgsqlDbType.Timestamp;
			case "timestamptz":
				return NpgsqlDbType.TimestampTZ;
			case "date":
				return NpgsqlDbType.Date;
			case "money":
				return NpgsqlDbType.Money;
			case "numeric":
				return NpgsqlDbType.Numeric;
			case "float8":
				return NpgsqlDbType.Double;
			case "uuid":
				return NpgsqlDbType.Uuid;
			case "int2":
				return NpgsqlDbType.Smallint;
			case "_int2":
				return NpgsqlDbType.Array | NpgsqlDbType.Smallint;
			case "int4":
				return NpgsqlDbType.Integer;
			case "_int4":
				return NpgsqlDbType.Array | NpgsqlDbType.Integer;
			case "int8":
				return NpgsqlDbType.Bigint;
			case "array":
				return NpgsqlDbType.Array;
			case "Circle":
				return NpgsqlDbType.Circle;
			case "inet":
				return NpgsqlDbType.Inet;
			case "interval":
				return NpgsqlDbType.Interval;
			case "Line":
				return NpgsqlDbType.Line;
			case "LSeg":
				return NpgsqlDbType.LSeg;
			case "Path":
				return NpgsqlDbType.Path;
			case "Point":
				return NpgsqlDbType.Point;
			case "Polygon":
				return NpgsqlDbType.Polygon;
			case "Box":
				return NpgsqlDbType.Box;
			case "float4":
				return NpgsqlDbType.Real;
			case "text":
				return NpgsqlDbType.Text;
			case "varchar":
				return NpgsqlDbType.Varchar;
			//case NpgsqlDbType.Text:
			//	return "bpchar";
			//case NpgsqlDbType.Array | NpgsqlDbType.Text:
			//	return "_bpchar";
			case "_varchar":
				return NpgsqlDbType.Array | NpgsqlDbType.Varchar;
			case "_text":
				return NpgsqlDbType.Array | NpgsqlDbType.Text;
			case "time":
				return NpgsqlDbType.Time;
			//case NpgsqlDbType.Time:
			//	return "timetz";
			case "xml":
				return NpgsqlDbType.Xml;
			default:
				throw new ApplicationException();
			}
		}

		public static string ToUdtName(NpgsqlDbType dbType) {
			switch (dbType) {
			case NpgsqlDbType.Char:
				return "char";
			case NpgsqlDbType.Bytea:
				return "bytea";
			case NpgsqlDbType.Bit:
				return "bit";
			case NpgsqlDbType.Boolean:
				return "bool";
			case NpgsqlDbType.Timestamp:
				return "timestamp";
			case NpgsqlDbType.TimestampTZ:
				return "timestamptz";
			case NpgsqlDbType.Date:
				return "date";
			case NpgsqlDbType.Money:
				return "money";
			case NpgsqlDbType.Numeric:
				return "numeric";
			case NpgsqlDbType.Double:
				return "float8";
			case NpgsqlDbType.Uuid:
				return "uuid";
			case NpgsqlDbType.Smallint:
				return "int2";
			case NpgsqlDbType.Array | NpgsqlDbType.Smallint:
				return "int2[]";
			case NpgsqlDbType.Integer:
				return "int4";
			case NpgsqlDbType.Array | NpgsqlDbType.Integer:
				return "int4[]";
			case NpgsqlDbType.Bigint:
				return "int8";
			case NpgsqlDbType.Array:
				return "array";
			case NpgsqlDbType.Circle:
				return "Circle";
			case NpgsqlDbType.Inet:
				return "inet";
			case NpgsqlDbType.Interval:
				return "interval";
			case NpgsqlDbType.Line:
				return "Line";
			case NpgsqlDbType.LSeg:
				return "LSeg";
			case NpgsqlDbType.Path:
				return "Path";
			case NpgsqlDbType.Point:
				return "Point";
			case NpgsqlDbType.Polygon:
				return "Polygon";
			case NpgsqlDbType.Box:
				return "Box";
			case NpgsqlDbType.Real:
				return "float4";
			case NpgsqlDbType.Text:
				return "text";
			case NpgsqlDbType.Varchar:
				return "varchar";
			//case NpgsqlDbType.Text:
			//	return "bpchar";
			//case NpgsqlDbType.Array | NpgsqlDbType.Text:
			//	return "_bpchar";
			case NpgsqlDbType.Array | NpgsqlDbType.Varchar:
				return "varchar[]";
			case NpgsqlDbType.Array | NpgsqlDbType.Text:
				return "text[]";
			case NpgsqlDbType.Time:
				return "time";
			//case NpgsqlDbType.Time:
			//	return "timetz";
			case NpgsqlDbType.Xml:
				return "xml";
			default:
				throw new ApplicationException();
			}
		}

		public string ToDbTypeString(DbTypeStringFlags flags = 0) {
			if ((flags & DbTypeStringFlags.Serial) != 0) {
				switch (this.DbType) {
				case NpgsqlDbType.Integer:
					return "serial4";
				case NpgsqlDbType.Bigint:
					return "serial8";
				default:
					throw new ApplicationException();
				}
			} else {
				switch (this.DbType) {
				case NpgsqlDbType.Char:
					return "char";
				case NpgsqlDbType.Bytea:
					return "bytea";
				case NpgsqlDbType.Bit:
					return "bit";
				case NpgsqlDbType.Boolean:
					return "bool";
				case NpgsqlDbType.Timestamp:
					return "timestamp";
				case NpgsqlDbType.TimestampTZ:
					return "timestamptz";
				case NpgsqlDbType.Date:
					return "date";
				case NpgsqlDbType.Money:
					return "money";
				case NpgsqlDbType.Numeric:
					return "numeric";
				case NpgsqlDbType.Double:
					return "float8";
				case NpgsqlDbType.Uuid:
					return "uuid";
				case NpgsqlDbType.Smallint:
					return "int2";
				case NpgsqlDbType.Array | NpgsqlDbType.Smallint:
					return "int2[]";
				case NpgsqlDbType.Integer:
					return "int4";
				case NpgsqlDbType.Array | NpgsqlDbType.Integer:
					return "int4[]";
				case NpgsqlDbType.Bigint:
					return "int8";
				case NpgsqlDbType.Array:
					return "array";
				case NpgsqlDbType.Circle:
					return "Circle";
				case NpgsqlDbType.Inet:
					return "inet";
				case NpgsqlDbType.Interval:
					return "interval";
				case NpgsqlDbType.Line:
					return "Line";
				case NpgsqlDbType.LSeg:
					return "LSeg";
				case NpgsqlDbType.Path:
					return "Path";
				case NpgsqlDbType.Point:
					return "Point";
				case NpgsqlDbType.Polygon:
					return "Polygon";
				case NpgsqlDbType.Box:
					return "Box";
				case NpgsqlDbType.Real:
					return "float4";
				case NpgsqlDbType.Text:
					return "text";
				case NpgsqlDbType.Varchar:
					return "varchar";
				//case NpgsqlDbType.Text:
				//	return "bpchar";
				//case NpgsqlDbType.Array | NpgsqlDbType.Text:
				//	return "_bpchar";
				case NpgsqlDbType.Array | NpgsqlDbType.Varchar:
					return "varchar[]";
				case NpgsqlDbType.Array | NpgsqlDbType.Text:
					return "text[]";
				case NpgsqlDbType.Time:
					return "time";
				//case NpgsqlDbType.Time:
				//	return "timetz";
				case NpgsqlDbType.Xml:
					return "xml";
				default:
					throw new ApplicationException();
				}
			}
		}

		public bool TypeEqualsTo(IDbType type) {
			PgDbType pdt = type as PgDbType;
			if (pdt == null) {
				return false;
			}
			return this.DbType == pdt.DbType;
		}

		public static implicit operator PgDbType(NpgsqlDbType type) {
			return new PgDbType(type);
		}
		public static implicit operator NpgsqlDbType(PgDbType type) {
			return type.DbType;
		}
	}
}
