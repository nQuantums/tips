using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Npgsql;

namespace DbCode.PgBind {
	public class PgEnvironmentException : DbCodeEnvironmentException {
		public PgEnvironmentException(PostgresException inner) : base(inner.MessageText, inner) { }

		public override DbEnvironmentErrorType ErrorType {
			get {
				var iex = this.InnerException as PostgresException;
				switch (iex.SqlState) {
				case "42P04":
					return DbEnvironmentErrorType.DuplicateDatabase;
				case "42710":
					return DbEnvironmentErrorType.DuplicateObject;
				case "23505":
					return DbEnvironmentErrorType.DuplicateKey;
				default:
					return DbEnvironmentErrorType.Unknown;
				}
			}
		}
	}
}
