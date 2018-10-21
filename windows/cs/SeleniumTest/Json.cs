using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.IO;

namespace SeleniumTest {
	public static class Json {
		static DataContractJsonSerializerSettings _Setting = new DataContractJsonSerializerSettings() {
			UseSimpleDictionaryFormat = true,
		};

		public static T ToObject<T>(Stream stream) {
			var serializer = new DataContractJsonSerializer(typeof(T), _Setting);
			return (T)serializer.ReadObject(stream);
		}

		public static T ToObject<T>(string source) {
			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(source))) {
				return ToObject<T>(stream);
			}
		}
	}
}
