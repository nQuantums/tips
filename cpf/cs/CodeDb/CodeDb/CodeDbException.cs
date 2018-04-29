using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.Serialization;
using System.Text;

namespace CodeDb {
	public class CodeDbException : Exception {
		public CodeDbException() { }
		public CodeDbException(string message) : base(message) { }
		public CodeDbException(SerializationInfo info, StreamingContext context) : base(info, context) { }
		public CodeDbException(string message, Exception innerException) : base(message, innerException) { }
	}

	public abstract class CodeDbEnvironmentException : CodeDbException {
		public CodeDbEnvironmentException() { }
		public CodeDbEnvironmentException(string message) : base(message) { }
		public CodeDbEnvironmentException(SerializationInfo info, StreamingContext context) : base(info, context) { }
		public CodeDbEnvironmentException(string message, Exception innerException) : base(message, innerException) { }

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
