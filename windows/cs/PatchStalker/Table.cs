using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatchStalker {
	public class Table {
		public enum CellType {
			Th,
			Td,
		}

		public int RowCount { get; private set; }
		public int ColCount { get; private set; }
		public Cell[,] Cells { get; private set; }

		public class Cell {
			public CellType Type;
			public int NodeId;
			public string Text;
			public readonly List<int> LinkIds = new List<int>();

			public override string ToString() {
				return this.Text;
			}
		}

		public Table(object[] tableData) {
			int rowCount = tableData.Length;
			int colCount = 0;
			for (int i = 0; i < rowCount; i++) {
				var cols = tableData[i] as object[];
				int colIndex = 0;
				for (int j = 0; j < cols.Length; j++) {
					var values = cols[j] as object[];
					var colSpan = Convert.ToInt32(values[3]);
					colIndex += colSpan;
				}
				if (colCount < colIndex) {
					colCount = colIndex;
				}
			}

			var cells = new Cell[rowCount, colCount];
			for (int i = 0; i < rowCount; i++) {
				for (int j = 0; j < colCount; j++) {
					cells[i, j] = new Cell();
				}
			}

			for (int i = 0; i < rowCount; i++) {
				var cols = tableData[i] as object[];
				int colIndex = 0;
				for (int j = 0; j < cols.Length; j++) {
					while (colIndex < colCount) {
						if (cells[i, colIndex].Text is null) {
							break;
						}
						colIndex++;
					}
					var values = cols[j] as object[];
					var nodeName = values[0] as string;
					var nodeId = Convert.ToInt32(values[1]);
					var rowSpan = Convert.ToInt32(values[2]);
					var colSpan = Convert.ToInt32(values[3]);
					var text = values[4] as string;
					var nodeType = nodeName == "th" ? CellType.Th : CellType.Td;

					var linkIds = new int[values.Length - 5];
					for (int k = 5; k < values.Length; k++) {
						linkIds[k - 5] = Convert.ToInt32(values[k]);
					}

					for (int r = 0; r < rowSpan; r++) {
						var rowIndex = i + r;
						for (int c = 0; c < colSpan; c++) {
							var cell = cells[rowIndex, colIndex + c];
							cell.Type = nodeType;
							cell.NodeId = nodeId;
							cell.Text = text;
							for (int k = 0; k < linkIds.Length; k++) {
								cell.LinkIds.Add(linkIds[k]);
							}
						}
					}

					colIndex += colSpan;
				}
			}

			this.RowCount = rowCount;
			this.ColCount = colCount;
			this.Cells = cells;
		}
	}
}
