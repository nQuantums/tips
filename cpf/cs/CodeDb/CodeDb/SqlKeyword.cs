using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb {
	/// <summary>
	/// SQL文内で使うキーワード一覧
	/// </summary>
	public enum SqlKeyword {
		Null,
		Not,
		NotNull,
		CreateTable,
		DropTable,
		CreateIndex,
		Exists,
		NotExists,
		IfExists,
		IfNotExists,
		PrimaryKey,
		Select,
		From,
		Where,
		InnerJoin,
		LeftJoin,
		RightJoin,
		On,
		And,
		Or,
		GroupBy,
		OrderBy,
		InsertInto,
		Limit,
		As,
		IsNull,
		IsNotNull,
		Using,
		Like,
		In,
		Values,
		Default,
		CurrentTimestamp,
		AlterTable,
		DropConstraint,
		AddConstraint,
		DropColumn,
		AddColumn,
		CreateRole,
		Password,
		CreateDatabase,
		Owner,
	}
}
