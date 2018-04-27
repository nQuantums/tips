﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeDb {
	/// <summary>
	/// SQL内変数、@variable などの変数に置き換わる
	/// </summary>
	public class Variable {
		public readonly object Value;

		public Variable(object value) {
			this.Value = value;
		}

		public override string ToString() {
			var value = this.Value;
			if (value == null) {
				return "null";
			} else {
				return value.ToString();
			}
		}

		public override bool Equals(object obj) {
			return object.ReferenceEquals(this.Value, obj);
		}

		public override int GetHashCode() {
			var value = this.Value;
			return value != null ? value.GetHashCode() : 0;
		}

		// 明示的キャスト
		public static bool operator true(Variable variable) => !(variable is null);
		public static bool operator false(Variable variable) => variable is null;

		// 暗黙的キャスト
		public static implicit operator bool(Variable variable) => (bool)variable.Value;
		public static implicit operator bool?(Variable variable) => (bool?)variable.Value;
		public static implicit operator char(Variable variable) => (char)variable.Value;
		public static implicit operator char?(Variable variable) => (char?)variable.Value;
		public static implicit operator int(Variable variable) => (int)variable.Value;
		public static implicit operator int?(Variable variable) => (int?)variable.Value;
		public static implicit operator long(Variable variable) => (long)variable.Value;
		public static implicit operator long?(Variable variable) => (long?)variable.Value;
		public static implicit operator double(Variable variable) => (double)variable.Value;
		public static implicit operator double?(Variable variable) => (double?)variable.Value;
		public static implicit operator string(Variable variable) => (string)variable.Value;
		public static implicit operator Guid(Variable variable) => (Guid)variable.Value;
		public static implicit operator Guid?(Variable variable) => (Guid?)variable.Value;
		public static implicit operator DateTime(Variable variable) => (DateTime)variable.Value;
		public static implicit operator DateTime?(Variable variable) => (DateTime?)variable.Value;

		// 配列としてアクセス
		public Variable this[int index] {
			get {
				return default(Variable);
			}
		}

		// 以下演算子オーバーロード
		public static bool operator +(bool l, Variable r) => default(bool);
		public static bool operator +(Variable l, bool r) => default(bool);
		public static bool? operator +(bool? l, Variable r) => default(bool?);
		public static bool? operator +(Variable l, bool? r) => default(bool?);
		public static char operator +(char l, Variable r) => default(char);
		public static char operator +(Variable l, char r) => default(char);
		public static char? operator +(char? l, Variable r) => default(char?);
		public static char? operator +(Variable l, char? r) => default(char?);
		public static int operator +(int l, Variable r) => default(int);
		public static int operator +(Variable l, int r) => default(int);
		public static int? operator +(int? l, Variable r) => default(int?);
		public static int? operator +(Variable l, int? r) => default(int?);
		public static long operator +(long l, Variable r) => default(long);
		public static long operator +(Variable l, long r) => default(long);
		public static long? operator +(long? l, Variable r) => default(long?);
		public static long? operator +(Variable l, long? r) => default(long?);
		public static double operator +(double l, Variable r) => default(double);
		public static double operator +(Variable l, double r) => default(double);
		public static double? operator +(double? l, Variable r) => default(double?);
		public static double? operator +(Variable l, double? r) => default(double?);
		public static string operator +(string l, Variable r) => default(string);
		public static string operator +(Variable l, string r) => default(string);
		public static Guid operator +(Guid l, Variable r) => default(Guid);
		public static Guid operator +(Variable l, Guid r) => default(Guid);
		public static Guid? operator +(Guid? l, Variable r) => default(Guid?);
		public static Guid? operator +(Variable l, Guid? r) => default(Guid?);
		public static DateTime operator +(DateTime l, Variable r) => default(DateTime);
		public static DateTime operator +(Variable l, DateTime r) => default(DateTime);
		public static DateTime? operator +(DateTime? l, Variable r) => default(DateTime?);
		public static DateTime? operator +(Variable l, DateTime? r) => default(DateTime?);
		public static Variable operator +(Variable l, Variable r) => default(Variable);
		public static bool operator -(bool l, Variable r) => default(bool);
		public static bool operator -(Variable l, bool r) => default(bool);
		public static bool? operator -(bool? l, Variable r) => default(bool?);
		public static bool? operator -(Variable l, bool? r) => default(bool?);
		public static char operator -(char l, Variable r) => default(char);
		public static char operator -(Variable l, char r) => default(char);
		public static char? operator -(char? l, Variable r) => default(char?);
		public static char? operator -(Variable l, char? r) => default(char?);
		public static int operator -(int l, Variable r) => default(int);
		public static int operator -(Variable l, int r) => default(int);
		public static int? operator -(int? l, Variable r) => default(int?);
		public static int? operator -(Variable l, int? r) => default(int?);
		public static long operator -(long l, Variable r) => default(long);
		public static long operator -(Variable l, long r) => default(long);
		public static long? operator -(long? l, Variable r) => default(long?);
		public static long? operator -(Variable l, long? r) => default(long?);
		public static double operator -(double l, Variable r) => default(double);
		public static double operator -(Variable l, double r) => default(double);
		public static double? operator -(double? l, Variable r) => default(double?);
		public static double? operator -(Variable l, double? r) => default(double?);
		public static string operator -(string l, Variable r) => default(string);
		public static string operator -(Variable l, string r) => default(string);
		public static Guid operator -(Guid l, Variable r) => default(Guid);
		public static Guid operator -(Variable l, Guid r) => default(Guid);
		public static Guid? operator -(Guid? l, Variable r) => default(Guid?);
		public static Guid? operator -(Variable l, Guid? r) => default(Guid?);
		public static DateTime operator -(DateTime l, Variable r) => default(DateTime);
		public static DateTime operator -(Variable l, DateTime r) => default(DateTime);
		public static DateTime? operator -(DateTime? l, Variable r) => default(DateTime?);
		public static DateTime? operator -(Variable l, DateTime? r) => default(DateTime?);
		public static Variable operator -(Variable l, Variable r) => default(Variable);
		public static bool operator *(bool l, Variable r) => default(bool);
		public static bool operator *(Variable l, bool r) => default(bool);
		public static bool? operator *(bool? l, Variable r) => default(bool?);
		public static bool? operator *(Variable l, bool? r) => default(bool?);
		public static char operator *(char l, Variable r) => default(char);
		public static char operator *(Variable l, char r) => default(char);
		public static char? operator *(char? l, Variable r) => default(char?);
		public static char? operator *(Variable l, char? r) => default(char?);
		public static int operator *(int l, Variable r) => default(int);
		public static int operator *(Variable l, int r) => default(int);
		public static int? operator *(int? l, Variable r) => default(int?);
		public static int? operator *(Variable l, int? r) => default(int?);
		public static long operator *(long l, Variable r) => default(long);
		public static long operator *(Variable l, long r) => default(long);
		public static long? operator *(long? l, Variable r) => default(long?);
		public static long? operator *(Variable l, long? r) => default(long?);
		public static double operator *(double l, Variable r) => default(double);
		public static double operator *(Variable l, double r) => default(double);
		public static double? operator *(double? l, Variable r) => default(double?);
		public static double? operator *(Variable l, double? r) => default(double?);
		public static string operator *(string l, Variable r) => default(string);
		public static string operator *(Variable l, string r) => default(string);
		public static Guid operator *(Guid l, Variable r) => default(Guid);
		public static Guid operator *(Variable l, Guid r) => default(Guid);
		public static Guid? operator *(Guid? l, Variable r) => default(Guid?);
		public static Guid? operator *(Variable l, Guid? r) => default(Guid?);
		public static DateTime operator *(DateTime l, Variable r) => default(DateTime);
		public static DateTime operator *(Variable l, DateTime r) => default(DateTime);
		public static DateTime? operator *(DateTime? l, Variable r) => default(DateTime?);
		public static DateTime? operator *(Variable l, DateTime? r) => default(DateTime?);
		public static Variable operator *(Variable l, Variable r) => default(Variable);
		public static bool operator /(bool l, Variable r) => default(bool);
		public static bool operator /(Variable l, bool r) => default(bool);
		public static bool? operator /(bool? l, Variable r) => default(bool?);
		public static bool? operator /(Variable l, bool? r) => default(bool?);
		public static char operator /(char l, Variable r) => default(char);
		public static char operator /(Variable l, char r) => default(char);
		public static char? operator /(char? l, Variable r) => default(char?);
		public static char? operator /(Variable l, char? r) => default(char?);
		public static int operator /(int l, Variable r) => default(int);
		public static int operator /(Variable l, int r) => default(int);
		public static int? operator /(int? l, Variable r) => default(int?);
		public static int? operator /(Variable l, int? r) => default(int?);
		public static long operator /(long l, Variable r) => default(long);
		public static long operator /(Variable l, long r) => default(long);
		public static long? operator /(long? l, Variable r) => default(long?);
		public static long? operator /(Variable l, long? r) => default(long?);
		public static double operator /(double l, Variable r) => default(double);
		public static double operator /(Variable l, double r) => default(double);
		public static double? operator /(double? l, Variable r) => default(double?);
		public static double? operator /(Variable l, double? r) => default(double?);
		public static string operator /(string l, Variable r) => default(string);
		public static string operator /(Variable l, string r) => default(string);
		public static Guid operator /(Guid l, Variable r) => default(Guid);
		public static Guid operator /(Variable l, Guid r) => default(Guid);
		public static Guid? operator /(Guid? l, Variable r) => default(Guid?);
		public static Guid? operator /(Variable l, Guid? r) => default(Guid?);
		public static DateTime operator /(DateTime l, Variable r) => default(DateTime);
		public static DateTime operator /(Variable l, DateTime r) => default(DateTime);
		public static DateTime? operator /(DateTime? l, Variable r) => default(DateTime?);
		public static DateTime? operator /(Variable l, DateTime? r) => default(DateTime?);
		public static Variable operator /(Variable l, Variable r) => default(Variable);
		public static bool operator <(bool l, Variable r) => default(bool);
		public static bool operator <(Variable l, bool r) => default(bool);
		public static bool operator <(bool? l, Variable r) => default(bool);
		public static bool operator <(Variable l, bool? r) => default(bool);
		public static bool operator <(char l, Variable r) => default(bool);
		public static bool operator <(Variable l, char r) => default(bool);
		public static bool operator <(char? l, Variable r) => default(bool);
		public static bool operator <(Variable l, char? r) => default(bool);
		public static bool operator <(int l, Variable r) => default(bool);
		public static bool operator <(Variable l, int r) => default(bool);
		public static bool operator <(int? l, Variable r) => default(bool);
		public static bool operator <(Variable l, int? r) => default(bool);
		public static bool operator <(long l, Variable r) => default(bool);
		public static bool operator <(Variable l, long r) => default(bool);
		public static bool operator <(long? l, Variable r) => default(bool);
		public static bool operator <(Variable l, long? r) => default(bool);
		public static bool operator <(double l, Variable r) => default(bool);
		public static bool operator <(Variable l, double r) => default(bool);
		public static bool operator <(double? l, Variable r) => default(bool);
		public static bool operator <(Variable l, double? r) => default(bool);
		public static bool operator <(string l, Variable r) => default(bool);
		public static bool operator <(Variable l, string r) => default(bool);
		public static bool operator <(Guid l, Variable r) => default(bool);
		public static bool operator <(Variable l, Guid r) => default(bool);
		public static bool operator <(Guid? l, Variable r) => default(bool);
		public static bool operator <(Variable l, Guid? r) => default(bool);
		public static bool operator <(DateTime l, Variable r) => default(bool);
		public static bool operator <(Variable l, DateTime r) => default(bool);
		public static bool operator <(DateTime? l, Variable r) => default(bool);
		public static bool operator <(Variable l, DateTime? r) => default(bool);
		public static bool operator <(Variable l, Variable r) => default(bool);
		public static bool operator >(bool l, Variable r) => default(bool);
		public static bool operator >(Variable l, bool r) => default(bool);
		public static bool operator >(bool? l, Variable r) => default(bool);
		public static bool operator >(Variable l, bool? r) => default(bool);
		public static bool operator >(char l, Variable r) => default(bool);
		public static bool operator >(Variable l, char r) => default(bool);
		public static bool operator >(char? l, Variable r) => default(bool);
		public static bool operator >(Variable l, char? r) => default(bool);
		public static bool operator >(int l, Variable r) => default(bool);
		public static bool operator >(Variable l, int r) => default(bool);
		public static bool operator >(int? l, Variable r) => default(bool);
		public static bool operator >(Variable l, int? r) => default(bool);
		public static bool operator >(long l, Variable r) => default(bool);
		public static bool operator >(Variable l, long r) => default(bool);
		public static bool operator >(long? l, Variable r) => default(bool);
		public static bool operator >(Variable l, long? r) => default(bool);
		public static bool operator >(double l, Variable r) => default(bool);
		public static bool operator >(Variable l, double r) => default(bool);
		public static bool operator >(double? l, Variable r) => default(bool);
		public static bool operator >(Variable l, double? r) => default(bool);
		public static bool operator >(string l, Variable r) => default(bool);
		public static bool operator >(Variable l, string r) => default(bool);
		public static bool operator >(Guid l, Variable r) => default(bool);
		public static bool operator >(Variable l, Guid r) => default(bool);
		public static bool operator >(Guid? l, Variable r) => default(bool);
		public static bool operator >(Variable l, Guid? r) => default(bool);
		public static bool operator >(DateTime l, Variable r) => default(bool);
		public static bool operator >(Variable l, DateTime r) => default(bool);
		public static bool operator >(DateTime? l, Variable r) => default(bool);
		public static bool operator >(Variable l, DateTime? r) => default(bool);
		public static bool operator >(Variable l, Variable r) => default(bool);
		public static bool operator <=(bool l, Variable r) => default(bool);
		public static bool operator <=(Variable l, bool r) => default(bool);
		public static bool operator <=(bool? l, Variable r) => default(bool);
		public static bool operator <=(Variable l, bool? r) => default(bool);
		public static bool operator <=(char l, Variable r) => default(bool);
		public static bool operator <=(Variable l, char r) => default(bool);
		public static bool operator <=(char? l, Variable r) => default(bool);
		public static bool operator <=(Variable l, char? r) => default(bool);
		public static bool operator <=(int l, Variable r) => default(bool);
		public static bool operator <=(Variable l, int r) => default(bool);
		public static bool operator <=(int? l, Variable r) => default(bool);
		public static bool operator <=(Variable l, int? r) => default(bool);
		public static bool operator <=(long l, Variable r) => default(bool);
		public static bool operator <=(Variable l, long r) => default(bool);
		public static bool operator <=(long? l, Variable r) => default(bool);
		public static bool operator <=(Variable l, long? r) => default(bool);
		public static bool operator <=(double l, Variable r) => default(bool);
		public static bool operator <=(Variable l, double r) => default(bool);
		public static bool operator <=(double? l, Variable r) => default(bool);
		public static bool operator <=(Variable l, double? r) => default(bool);
		public static bool operator <=(string l, Variable r) => default(bool);
		public static bool operator <=(Variable l, string r) => default(bool);
		public static bool operator <=(Guid l, Variable r) => default(bool);
		public static bool operator <=(Variable l, Guid r) => default(bool);
		public static bool operator <=(Guid? l, Variable r) => default(bool);
		public static bool operator <=(Variable l, Guid? r) => default(bool);
		public static bool operator <=(DateTime l, Variable r) => default(bool);
		public static bool operator <=(Variable l, DateTime r) => default(bool);
		public static bool operator <=(DateTime? l, Variable r) => default(bool);
		public static bool operator <=(Variable l, DateTime? r) => default(bool);
		public static bool operator <=(Variable l, Variable r) => default(bool);
		public static bool operator >=(bool l, Variable r) => default(bool);
		public static bool operator >=(Variable l, bool r) => default(bool);
		public static bool operator >=(bool? l, Variable r) => default(bool);
		public static bool operator >=(Variable l, bool? r) => default(bool);
		public static bool operator >=(char l, Variable r) => default(bool);
		public static bool operator >=(Variable l, char r) => default(bool);
		public static bool operator >=(char? l, Variable r) => default(bool);
		public static bool operator >=(Variable l, char? r) => default(bool);
		public static bool operator >=(int l, Variable r) => default(bool);
		public static bool operator >=(Variable l, int r) => default(bool);
		public static bool operator >=(int? l, Variable r) => default(bool);
		public static bool operator >=(Variable l, int? r) => default(bool);
		public static bool operator >=(long l, Variable r) => default(bool);
		public static bool operator >=(Variable l, long r) => default(bool);
		public static bool operator >=(long? l, Variable r) => default(bool);
		public static bool operator >=(Variable l, long? r) => default(bool);
		public static bool operator >=(double l, Variable r) => default(bool);
		public static bool operator >=(Variable l, double r) => default(bool);
		public static bool operator >=(double? l, Variable r) => default(bool);
		public static bool operator >=(Variable l, double? r) => default(bool);
		public static bool operator >=(string l, Variable r) => default(bool);
		public static bool operator >=(Variable l, string r) => default(bool);
		public static bool operator >=(Guid l, Variable r) => default(bool);
		public static bool operator >=(Variable l, Guid r) => default(bool);
		public static bool operator >=(Guid? l, Variable r) => default(bool);
		public static bool operator >=(Variable l, Guid? r) => default(bool);
		public static bool operator >=(DateTime l, Variable r) => default(bool);
		public static bool operator >=(Variable l, DateTime r) => default(bool);
		public static bool operator >=(DateTime? l, Variable r) => default(bool);
		public static bool operator >=(Variable l, DateTime? r) => default(bool);
		public static bool operator >=(Variable l, Variable r) => default(bool);
		public static bool operator ==(bool l, Variable r) => default(bool);
		public static bool operator ==(Variable l, bool r) => default(bool);
		public static bool operator ==(bool? l, Variable r) => default(bool);
		public static bool operator ==(Variable l, bool? r) => default(bool);
		public static bool operator ==(char l, Variable r) => default(bool);
		public static bool operator ==(Variable l, char r) => default(bool);
		public static bool operator ==(char? l, Variable r) => default(bool);
		public static bool operator ==(Variable l, char? r) => default(bool);
		public static bool operator ==(int l, Variable r) => default(bool);
		public static bool operator ==(Variable l, int r) => default(bool);
		public static bool operator ==(int? l, Variable r) => default(bool);
		public static bool operator ==(Variable l, int? r) => default(bool);
		public static bool operator ==(long l, Variable r) => default(bool);
		public static bool operator ==(Variable l, long r) => default(bool);
		public static bool operator ==(long? l, Variable r) => default(bool);
		public static bool operator ==(Variable l, long? r) => default(bool);
		public static bool operator ==(double l, Variable r) => default(bool);
		public static bool operator ==(Variable l, double r) => default(bool);
		public static bool operator ==(double? l, Variable r) => default(bool);
		public static bool operator ==(Variable l, double? r) => default(bool);
		public static bool operator ==(string l, Variable r) => default(bool);
		public static bool operator ==(Variable l, string r) => default(bool);
		public static bool operator ==(Guid l, Variable r) => default(bool);
		public static bool operator ==(Variable l, Guid r) => default(bool);
		public static bool operator ==(Guid? l, Variable r) => default(bool);
		public static bool operator ==(Variable l, Guid? r) => default(bool);
		public static bool operator ==(DateTime l, Variable r) => default(bool);
		public static bool operator ==(Variable l, DateTime r) => default(bool);
		public static bool operator ==(DateTime? l, Variable r) => default(bool);
		public static bool operator ==(Variable l, DateTime? r) => default(bool);
		public static bool operator ==(Variable l, Variable r) => default(bool);
		public static bool operator !=(bool l, Variable r) => default(bool);
		public static bool operator !=(Variable l, bool r) => default(bool);
		public static bool operator !=(bool? l, Variable r) => default(bool);
		public static bool operator !=(Variable l, bool? r) => default(bool);
		public static bool operator !=(char l, Variable r) => default(bool);
		public static bool operator !=(Variable l, char r) => default(bool);
		public static bool operator !=(char? l, Variable r) => default(bool);
		public static bool operator !=(Variable l, char? r) => default(bool);
		public static bool operator !=(int l, Variable r) => default(bool);
		public static bool operator !=(Variable l, int r) => default(bool);
		public static bool operator !=(int? l, Variable r) => default(bool);
		public static bool operator !=(Variable l, int? r) => default(bool);
		public static bool operator !=(long l, Variable r) => default(bool);
		public static bool operator !=(Variable l, long r) => default(bool);
		public static bool operator !=(long? l, Variable r) => default(bool);
		public static bool operator !=(Variable l, long? r) => default(bool);
		public static bool operator !=(double l, Variable r) => default(bool);
		public static bool operator !=(Variable l, double r) => default(bool);
		public static bool operator !=(double? l, Variable r) => default(bool);
		public static bool operator !=(Variable l, double? r) => default(bool);
		public static bool operator !=(string l, Variable r) => default(bool);
		public static bool operator !=(Variable l, string r) => default(bool);
		public static bool operator !=(Guid l, Variable r) => default(bool);
		public static bool operator !=(Variable l, Guid r) => default(bool);
		public static bool operator !=(Guid? l, Variable r) => default(bool);
		public static bool operator !=(Variable l, Guid? r) => default(bool);
		public static bool operator !=(DateTime l, Variable r) => default(bool);
		public static bool operator !=(Variable l, DateTime r) => default(bool);
		public static bool operator !=(DateTime? l, Variable r) => default(bool);
		public static bool operator !=(Variable l, DateTime? r) => default(bool);
		public static bool operator !=(Variable l, Variable r) => default(bool);
		public static bool operator |(bool l, Variable r) => default(bool);
		public static bool operator |(Variable l, bool r) => default(bool);
		public static bool? operator |(bool? l, Variable r) => default(bool?);
		public static bool? operator |(Variable l, bool? r) => default(bool?);
		public static char operator |(char l, Variable r) => default(char);
		public static char operator |(Variable l, char r) => default(char);
		public static char? operator |(char? l, Variable r) => default(char?);
		public static char? operator |(Variable l, char? r) => default(char?);
		public static int operator |(int l, Variable r) => default(int);
		public static int operator |(Variable l, int r) => default(int);
		public static int? operator |(int? l, Variable r) => default(int?);
		public static int? operator |(Variable l, int? r) => default(int?);
		public static long operator |(long l, Variable r) => default(long);
		public static long operator |(Variable l, long r) => default(long);
		public static long? operator |(long? l, Variable r) => default(long?);
		public static long? operator |(Variable l, long? r) => default(long?);
		public static double operator |(double l, Variable r) => default(double);
		public static double operator |(Variable l, double r) => default(double);
		public static double? operator |(double? l, Variable r) => default(double?);
		public static double? operator |(Variable l, double? r) => default(double?);
		public static string operator |(string l, Variable r) => default(string);
		public static string operator |(Variable l, string r) => default(string);
		public static Guid operator |(Guid l, Variable r) => default(Guid);
		public static Guid operator |(Variable l, Guid r) => default(Guid);
		public static Guid? operator |(Guid? l, Variable r) => default(Guid?);
		public static Guid? operator |(Variable l, Guid? r) => default(Guid?);
		public static DateTime operator |(DateTime l, Variable r) => default(DateTime);
		public static DateTime operator |(Variable l, DateTime r) => default(DateTime);
		public static DateTime? operator |(DateTime? l, Variable r) => default(DateTime?);
		public static DateTime? operator |(Variable l, DateTime? r) => default(DateTime?);
		public static Variable operator |(Variable l, Variable r) => default(Variable);
		public static bool operator &(bool l, Variable r) => default(bool);
		public static bool operator &(Variable l, bool r) => default(bool);
		public static bool? operator &(bool? l, Variable r) => default(bool?);
		public static bool? operator &(Variable l, bool? r) => default(bool?);
		public static char operator &(char l, Variable r) => default(char);
		public static char operator &(Variable l, char r) => default(char);
		public static char? operator &(char? l, Variable r) => default(char?);
		public static char? operator &(Variable l, char? r) => default(char?);
		public static int operator &(int l, Variable r) => default(int);
		public static int operator &(Variable l, int r) => default(int);
		public static int? operator &(int? l, Variable r) => default(int?);
		public static int? operator &(Variable l, int? r) => default(int?);
		public static long operator &(long l, Variable r) => default(long);
		public static long operator &(Variable l, long r) => default(long);
		public static long? operator &(long? l, Variable r) => default(long?);
		public static long? operator &(Variable l, long? r) => default(long?);
		public static double operator &(double l, Variable r) => default(double);
		public static double operator &(Variable l, double r) => default(double);
		public static double? operator &(double? l, Variable r) => default(double?);
		public static double? operator &(Variable l, double? r) => default(double?);
		public static string operator &(string l, Variable r) => default(string);
		public static string operator &(Variable l, string r) => default(string);
		public static Guid operator &(Guid l, Variable r) => default(Guid);
		public static Guid operator &(Variable l, Guid r) => default(Guid);
		public static Guid? operator &(Guid? l, Variable r) => default(Guid?);
		public static Guid? operator &(Variable l, Guid? r) => default(Guid?);
		public static DateTime operator &(DateTime l, Variable r) => default(DateTime);
		public static DateTime operator &(Variable l, DateTime r) => default(DateTime);
		public static DateTime? operator &(DateTime? l, Variable r) => default(DateTime?);
		public static DateTime? operator &(Variable l, DateTime? r) => default(DateTime?);
		public static Variable operator &(Variable l, Variable r) => default(Variable);
		public static bool operator !(Variable op) => op is null;
	}
}
