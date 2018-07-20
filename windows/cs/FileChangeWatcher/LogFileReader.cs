﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace FileChangeWatcher {
	/// <summary>
	/// ログファイル内容を読み込み保持する
	/// </summary>
	public class LogFileReader {
		/// <summary>
		/// ログファイルパス名
		/// </summary>
		public string FilePath;

		/// <summary>
		/// ファイルのエンコード
		/// </summary>
		public Encoding Encoding;

		/// <summary>
		/// ログファイル内容データ、実データよりサイズが大きいことがある
		/// </summary>
		public byte[] Content;

		/// <summary>
		/// ログファイル内容の実データサイズ
		/// </summary>
		public int ContentLength;

		/// <summary>
		/// コンストラクタ、ファイルパス名を指定して初期化する
		/// </summary>
		/// <param name="filePath">ログファイルパス名</param>
		public LogFileReader(string filePath) {
			this.FilePath = filePath;
		}

		/// <summary>
		/// ログファイルの増加分のみ読み込む
		/// </summary>
		/// <param name="retryCount">読み込み試行回数</param>
		/// <param name="retryWait">読み込み再試行前の待機時間(ms)</param>
		/// <returns>読み込んだデータの文字列または null </returns>
		public string ReadDelta(int retryCount = 100, int retryWait = 10) {
			var filePath = this.FilePath;
			var content = this.Content;
			var contentLength = this.ContentLength;

			// 一定回数読み込みを試行する
			string text = null;
			for (int i = 0; i < retryCount; i++) {
				try {
					using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
						var fileLength = fs.Length;
						if (content == null || fileLength < contentLength) {
							// 既存データが存在しない、または既存データサイズより小さくなるならファイル切り替わったかもしれないので全体読み込み
							content = new byte[fileLength];
							contentLength = fs.Read(content, 0, (int)fileLength);
							if (contentLength != fileLength) {
								Array.Resize(ref content, contentLength);
							}
							var enc = GetCode(content);
							text = enc.GetString(content);
							this.Encoding = enc;
						} else if (contentLength < fileLength) {
							// 既存データよりファイルサイズが大きくなるなら増加分のみ読み込み
							fs.Seek(contentLength, SeekOrigin.Begin);
							if (content.Length < fileLength) {
								Array.Resize(ref content, (int)fileLength * 2);
							}
							var length = fs.Read(content, contentLength, (int)fileLength - contentLength);
							text = this.Encoding.GetString(content, contentLength, length);
							contentLength += length;
						} else {
							// サイズが変わらない場合は何もしない
						}
					}

					// 読み込まれた内容を保持する
					this.Content = content;
					this.ContentLength = contentLength;

					break;
				} catch (IOException ex) {
					if (ex.HResult == -2147024864) {
						Thread.Sleep(retryWait);
					} else {
						throw;
					}
				}
			}

			return text;
		}

		/// <summary>
		/// 文字コードを判別する
		/// </summary>
		/// <remarks>
		/// Jcode.pmのgetcodeメソッドを移植したものです。
		/// Jcode.pm(http://openlab.ring.gr.jp/Jcode/index-j.html)
		/// Jcode.pmの著作権情報
		/// Copyright 1999-2005 Dan Kogai <dankogai@dan.co.jp>
		/// This library is free software; you can redistribute it and/or modify it
		///  under the same terms as Perl itself.
		/// </remarks>
		/// <param name="bytes">文字コードを調べるデータ</param>
		/// <returns>適当と思われるEncodingオブジェクト。
		/// 判断できなかった時はnull。</returns>
		public static System.Text.Encoding GetCode(byte[] bytes) {
			const byte bEscape = 0x1B;
			const byte bAt = 0x40;
			const byte bDollar = 0x24;
			const byte bAnd = 0x26;
			const byte bOpen = 0x28;    //'('
			const byte bB = 0x42;
			const byte bD = 0x44;
			const byte bJ = 0x4A;
			const byte bI = 0x49;

			int len = bytes.Length;
			byte b1, b2, b3, b4;

			//Encode::is_utf8 は無視

			bool isBinary = false;
			for (int i = 0; i < len; i++) {
				b1 = bytes[i];
				if (b1 <= 0x06 || b1 == 0x7F || b1 == 0xFF) {
					//'binary'
					isBinary = true;
					if (b1 == 0x00 && i < len - 1 && bytes[i + 1] <= 0x7F) {
						//smells like raw unicode
						return System.Text.Encoding.Unicode;
					}
				}
			}
			if (isBinary) {
				return null;
			}

			//not Japanese
			bool notJapanese = true;
			for (int i = 0; i < len; i++) {
				b1 = bytes[i];
				if (b1 == bEscape || 0x80 <= b1) {
					notJapanese = false;
					break;
				}
			}
			if (notJapanese) {
				return System.Text.Encoding.ASCII;
			}

			for (int i = 0; i < len - 2; i++) {
				b1 = bytes[i];
				b2 = bytes[i + 1];
				b3 = bytes[i + 2];

				if (b1 == bEscape) {
					if (b2 == bDollar && b3 == bAt) {
						//JIS_0208 1978
						//JIS
						return System.Text.Encoding.GetEncoding(50220);
					} else if (b2 == bDollar && b3 == bB) {
						//JIS_0208 1983
						//JIS
						return System.Text.Encoding.GetEncoding(50220);
					} else if (b2 == bOpen && (b3 == bB || b3 == bJ)) {
						//JIS_ASC
						//JIS
						return System.Text.Encoding.GetEncoding(50220);
					} else if (b2 == bOpen && b3 == bI) {
						//JIS_KANA
						//JIS
						return System.Text.Encoding.GetEncoding(50220);
					}
					if (i < len - 3) {
						b4 = bytes[i + 3];
						if (b2 == bDollar && b3 == bOpen && b4 == bD) {
							//JIS_0212
							//JIS
							return System.Text.Encoding.GetEncoding(50220);
						}
						if (i < len - 5 &&
							b2 == bAnd && b3 == bAt && b4 == bEscape &&
							bytes[i + 4] == bDollar && bytes[i + 5] == bB) {
							//JIS_0208 1990
							//JIS
							return System.Text.Encoding.GetEncoding(50220);
						}
					}
				}
			}

			//should be euc|sjis|utf8
			//use of (?:) by Hiroki Ohzaki <ohzaki@iod.ricoh.co.jp>
			int sjis = 0;
			int euc = 0;
			int utf8 = 0;
			for (int i = 0; i < len - 1; i++) {
				b1 = bytes[i];
				b2 = bytes[i + 1];
				if (((0x81 <= b1 && b1 <= 0x9F) || (0xE0 <= b1 && b1 <= 0xFC)) &&
					((0x40 <= b2 && b2 <= 0x7E) || (0x80 <= b2 && b2 <= 0xFC))) {
					//SJIS_C
					sjis += 2;
					i++;
				}
			}
			for (int i = 0; i < len - 1; i++) {
				b1 = bytes[i];
				b2 = bytes[i + 1];
				if (((0xA1 <= b1 && b1 <= 0xFE) && (0xA1 <= b2 && b2 <= 0xFE)) ||
					(b1 == 0x8E && (0xA1 <= b2 && b2 <= 0xDF))) {
					//EUC_C
					//EUC_KANA
					euc += 2;
					i++;
				} else if (i < len - 2) {
					b3 = bytes[i + 2];
					if (b1 == 0x8F && (0xA1 <= b2 && b2 <= 0xFE) &&
						(0xA1 <= b3 && b3 <= 0xFE)) {
						//EUC_0212
						euc += 3;
						i += 2;
					}
				}
			}
			for (int i = 0; i < len - 1; i++) {
				b1 = bytes[i];
				b2 = bytes[i + 1];
				if ((0xC0 <= b1 && b1 <= 0xDF) && (0x80 <= b2 && b2 <= 0xBF)) {
					//UTF8
					utf8 += 2;
					i++;
				} else if (i < len - 2) {
					b3 = bytes[i + 2];
					if ((0xE0 <= b1 && b1 <= 0xEF) && (0x80 <= b2 && b2 <= 0xBF) &&
						(0x80 <= b3 && b3 <= 0xBF)) {
						//UTF8
						utf8 += 3;
						i += 2;
					}
				}
			}
			//M. Takahashi's suggestion
			//utf8 += utf8 / 2;

			System.Diagnostics.Debug.WriteLine(
				string.Format("sjis = {0}, euc = {1}, utf8 = {2}", sjis, euc, utf8));
			if (euc > sjis && euc > utf8) {
				//EUC
				return System.Text.Encoding.GetEncoding(51932);
			} else if (sjis > euc && sjis > utf8) {
				//SJIS
				return System.Text.Encoding.GetEncoding(932);
			} else if (utf8 > euc && utf8 > sjis) {
				//UTF8
				return System.Text.Encoding.UTF8;
			}

			return null;
		}
	}
}