using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;

namespace SeleniumTest {
	public static class Json {
		static DataContractJsonSerializerSettings _JsonSerializerSettings = new DataContractJsonSerializerSettings {
			UseSimpleDictionaryFormat = true,
		};

		/// <summary>
		/// JSON 文字列のストリームからオブジェクトをデシリアライズします
		/// </summary>
		/// <param name="message">JSON 文字列</param>
		/// <returns>オブジェクト</returns>
		public static T ToObject<T>(Stream stream) {
			var serializer = new DataContractJsonSerializer(typeof(T), _JsonSerializerSettings);
			return (T)serializer.ReadObject(stream);
		}

		/// <summary>
		/// JSON 文字列からオブジェクトをデシリアライズします
		/// </summary>
		/// <param name="message">JSON 文字列</param>
		/// <returns>オブジェクト</returns>
		public static T ToObject<T>(string message) {
			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(message))) {
				var serializer = new DataContractJsonSerializer(typeof(T), _JsonSerializerSettings);
				return (T)serializer.ReadObject(stream);
			}
		}
	}
}
