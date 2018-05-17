using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.Serialization;
using System.Text;

namespace DbCode {
	public class DbCodeException : Exception {
		public DbCodeException() { }
		public DbCodeException(string message) : base(message) { }
		public DbCodeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
		public DbCodeException(string message, Exception innerException) : base(message, innerException) { }
	}

	public abstract class DbCodeEnvironmentException : DbCodeException {
		public DbCodeEnvironmentException() { }
		public DbCodeEnvironmentException(string message) : base(message) { }
		public DbCodeEnvironmentException(SerializationInfo info, StreamingContext context) : base(info, context) { }
		public DbCodeEnvironmentException(string message, Exception innerException) : base(message, innerException) { }

		public abstract DbEnvironmentErrorType ErrorType { get; }
	}

	public enum DbEnvironmentErrorType {
		Complete = 0,
		Unknown,
		DuplicateDatabase,
		DuplicateObject,
		DuplicateKey,
	}
}
