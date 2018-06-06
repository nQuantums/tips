 
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Runtime.CompilerServices;
using DbCode.Query;
using DbCode.Internal;

namespace DbCode {
	/// <summary>
	/// 列配列基本クラス
	/// </summary>
	/// <remarks>プロパティは全て列として扱われる</remarks>
	public class ColumnsBase {
		#region 列定義用メソッド
		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected bool As(Func<bool> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected bool[] As(Func<bool[]> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected bool? As(Func<bool?> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected char As(Func<char> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected char[] As(Func<char[]> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected char? As(Func<char?> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected int As(Func<int> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected int[] As(Func<int[]> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected int? As(Func<int?> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected long As(Func<long> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected long[] As(Func<long[]> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected long? As(Func<long?> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected double As(Func<double> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected double[] As(Func<double[]> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected double? As(Func<double?> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected string As(Func<string> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected string[] As(Func<string[]> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}
		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected Guid As(Func<Guid> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected Guid[] As(Func<Guid[]> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected Guid? As(Func<Guid?> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected DateTime As(Func<DateTime> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected DateTime[] As(Func<DateTime[]> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		/// <summary>
		/// プロパティを<see cref="Column"/>に結びつける、プロパティの get 内から呼び出す必要がある
		/// </summary>
		/// <param name="getter">列定義シングルトンのプロパティ取得を呼び出す処理</param>
		/// <param name="flags">列定義のオプションフラグ</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>ダミー値</returns>
		protected DateTime? As(Func<DateTime?> getter, ColumnFlags flags = 0, [CallerMemberName] string propertyName = null) {
			Mediator.ColumnFlags = flags;
			Mediator.PropertyName = propertyName;
			return getter();
		}

		#endregion
	}
}
