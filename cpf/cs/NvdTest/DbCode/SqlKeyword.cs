using System;
using System.Collections.Generic;
using System.Text;

namespace DbCode {
	/// <summary>
	/// SQL文内で使うキーワード一覧
	/// </summary>
	public enum SqlKeyword {
		AddColumn,
		AddConstraint,
		AlterTable,
		And,
		As,
		Asterisk,
		CreateDatabase,
		CreateIndex,
		CreateRole,
		CreateTable,
		CurrentTimestamp,
		Default,
		DropColumn,
		DropConstraint,
		DropIndex,
		DropTable,
		Exists,
		From,
		GroupBy,
		IfExists,
		IfNotExists,
		In,
		InnerJoin,
		InsertInto,
		IsNotNull,
		IsNull,
		LeftJoin,
		Like,
		Limit,
		Not,
		NotExists,
		NotNull,
		Null,
		On,
		Or,
		OrderBy,
		Owner,
		Password,
		PrimaryKey,
		RightJoin,
		Select,
		Unique,
		Using,
		Values,
		Where,
	}
}
