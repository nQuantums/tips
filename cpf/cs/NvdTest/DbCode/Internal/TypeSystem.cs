using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using System.Linq;
using DbCode.Internal;

namespace DbCode.Internal {
	public static class TypeSystem {
		public static Boolean IsAnonymousType(Type type) {
			var hasCompilerGeneratedAttribute = type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Any();
			var nameContainsAnonymousType = type.FullName.Contains("AnonymousType");
			var isAnonymousType = hasCompilerGeneratedAttribute && nameContainsAnonymousType;
			return isAnonymousType;
		}
	}
}
