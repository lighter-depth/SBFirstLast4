using Antlr4.Runtime.Misc;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Buffer = System.Collections.Generic.List<(string Content, string Type)>;


namespace SBFirstLast4.Dynamic;

internal class SBProcLangVisitor : SBProcLangBaseVisitor<Task<object?>>
{
	private readonly Buffer _outputBuffer;
	private readonly Action<string> _setTranslated;
	private readonly Func<Task> _update;
	private readonly CancellationToken _token;

	private static readonly Buffer _discardBuffer = new();
	private static readonly Action<string> _discardSetTranslated = _ => { };
	private static readonly Func<Task> _discardUpdate = () => Task.CompletedTask;

	internal SBProcLangVisitor(Buffer outputBuffer, Action<string> setTranslated, Func<Task> update, CancellationToken token)
		=> (_outputBuffer, _setTranslated, _update, _token) = (outputBuffer, setTranslated, update, token);

	internal SBProcLangVisitor()
		: this(_discardBuffer, _discardSetTranslated, _discardUpdate, default) { }

	public override async Task<object?> VisitScript([NotNull] SBProcLangParser.ScriptContext context)
	{
		try
		{
			foreach (var statement in context.statement())
				await Visit(statement);

			return string.Empty;
		}
		catch (Return @return)
		{
			return @return.Value;
		}
	}

	public override async Task<object?> VisitIf_else_stat([NotNull] SBProcLangParser.If_else_statContext context)
	{
		var condition = await Visit(context.expr(0));

		if (condition is bool bIf && bIf)
		{
			await Visit(context.stat_block(0));
			return "IF";
		}

		for (var i = 1; i < context.expr().Length; i++)
		{
			condition = await Visit(context.expr(i));

			if (condition is bool bElif && bElif)
			{
				await Visit(context.stat_block(i));
				return "ELIF";
			}
		}

		if (context.stat_block().Length > context.expr().Length)
		{
			await Visit(context.stat_block(context.stat_block().Length - 1));
			return "ELSE";
		}

		return "ENDIF";
	}

	public override async Task<object?> VisitDo_while_stat([NotNull] SBProcLangParser.Do_while_statContext context)
	{
		object? condition;

		do
		{
		Redo:;
			try
			{
				_token.ThrowIfCancellationRequested();
				await Visit(context.stat_block());
			}
			catch (Break)
			{
				break;
			}
			catch (Continue)
			{
				condition = await QueryAgent.EvaluateExpressionAsync(context.expr().GetText());
				continue;
			}
			catch (Redo)
			{
				goto Redo;
			}
			catch (NullReferenceException)
			{
				return "DO_WHILE";
			}

			condition = await QueryAgent.EvaluateExpressionAsync(context.expr().GetText());
		}
		while (condition is bool b && b);

		return "DO_WHILE";
	}

	public override async Task<object?> VisitWhile_stat([NotNull] SBProcLangParser.While_statContext context)
	{
		var condition = await QueryAgent.EvaluateExpressionAsync(context.expr().GetText());

		while (condition is bool b && b)
		{
		Redo:;
			try
			{
				_token.ThrowIfCancellationRequested();
				await Visit(context.stat_block());
			}
			catch (Break)
			{
				break;
			}
			catch (Continue)
			{
				condition = await QueryAgent.EvaluateExpressionAsync(context.expr().GetText());
				continue;
			}
			catch (Redo)
			{
				goto Redo;
			}

			condition = await QueryAgent.EvaluateExpressionAsync(context.expr().GetText());
		}

		return "WHILE";
	}

	public override async Task<object?> VisitDo_until_stat([NotNull] SBProcLangParser.Do_until_statContext context)
	{
		object? condition;

		do
		{
		Redo:;
			try
			{
				_token.ThrowIfCancellationRequested();
				await Visit(context.stat_block());
			}
			catch (Break)
			{
				break;
			}
			catch (Continue)
			{
				condition = await QueryAgent.EvaluateExpressionAsync(context.expr().GetText());
				continue;
			}
			catch (Redo)
			{
				goto Redo;
			}
			catch (NullReferenceException)
			{
				return "DO_UNTIL";
			}

			condition = await QueryAgent.EvaluateExpressionAsync(context.expr().GetText());
		}
		while (condition is bool b && !b);

		return "DO_UNTIL";
	}

	public override async Task<object?> VisitUntil_stat([NotNull] SBProcLangParser.Until_statContext context)
	{
		var condition = await QueryAgent.EvaluateExpressionAsync(context.expr().GetText());

		while (condition is bool b && !b)
		{
		Redo:;
			try
			{
				_token.ThrowIfCancellationRequested();
				await Visit(context.stat_block());
			}
			catch (Break)
			{
				break;
			}
			catch (Continue)
			{
				condition = await QueryAgent.EvaluateExpressionAsync(context.expr().GetText());
				continue;
			}
			catch (Redo)
			{
				goto Redo;
			}

			condition = await QueryAgent.EvaluateExpressionAsync(context.expr().GetText());
		}

		return "UNTIL";
	}

	public override async Task<object?> VisitFor_stat([NotNull] SBProcLangParser.For_statContext context)
	{
		var init = context.init;
		var cond = context.cond;
		var update = context.update;

		if (init is not null)
			await QueryAgent.RunStatementAsync(init.GetText(), _outputBuffer, _setTranslated);

		var condition = await QueryAgent.EvaluateExpressionAsync(cond?.GetText() ?? "true");


		while (condition is not bool b || b)
		{
		Redo:;
			try
			{
				_token.ThrowIfCancellationRequested();
				await Visit(context.stat_block());
			}
			catch (Break)
			{
				break;
			}
			catch (Continue)
			{
				if (update is not null)
					await QueryAgent.RunStatementAsync(update.GetText(), _outputBuffer, _setTranslated);

				condition = await QueryAgent.EvaluateExpressionAsync(cond?.GetText() ?? "true");

				continue;
			}
			catch (Redo)
			{
				goto Redo;
			}

			if (update is not null)
				await QueryAgent.RunStatementAsync(update.GetText(), _outputBuffer, _setTranslated);

			condition = await QueryAgent.EvaluateExpressionAsync(cond?.GetText() ?? "true");
		}

		return "FOR";
	}

	public override async Task<object?> VisitForeach_stat([NotNull] SBProcLangParser.Foreach_statContext context)
	{
		var wideId = context.WideID();
		var source = await QueryAgent.EvaluateExpressionAsync(context.expr()?.GetText() ?? "{}");
		var variableName = wideId?.GetText()[1..];

		if (string.IsNullOrEmpty(variableName) || source is not System.Collections.IEnumerable enumerable)
			return "FOREACH";

		foreach (var i in enumerable)
		{
			WideVariable.Variables[variableName] = i;
		Redo:;
			try
			{
				_token.ThrowIfCancellationRequested();
				await Visit(context.stat_block());
			}
			catch (Break)
			{
				break;
			}
			catch (Continue)
			{
				continue;
			}
			catch (Redo)
			{
				goto Redo;
			}
		}

		return "FOREACH";
	}

	public override async Task<object?> VisitReturn_stat([NotNull] SBProcLangParser.Return_statContext context)
	{
		var value = await QueryAgent.EvaluateExpressionAsync(context.expr()?.GetText() ?? "\"\"");
		throw new Return(value);
	}

	public override Task<object?> VisitBreak_stat([NotNull] SBProcLangParser.Break_statContext context)
		=> throw new Break();

	public override Task<object?> VisitContinue_stat([NotNull] SBProcLangParser.Continue_statContext context)
		=> throw new Continue();

	public override Task<object?> VisitRedo_stat([NotNull] SBProcLangParser.Redo_statContext context)
		=> throw new Redo();

	public override async Task<object?> VisitStat_block([NotNull] SBProcLangParser.Stat_blockContext context)
	{
		if (context.statement()?.Length > 0)
		{
			foreach (var statement in context.statement())
				await Visit(statement);
			return "STAT_BLOCK";
		}

		await Visit(context.GetChild(0));
		return "STAT_BLOCK";
	}

	public override async Task<object?> VisitWideAssignment([NotNull] SBProcLangParser.WideAssignmentContext context)
	{
		await QueryAgent.RunStatementAsync(context.GetText(), _outputBuffer, _setTranslated);
		await _update();
		return "ASSIGNMENT_WIDE";
	}

	public override async Task<object?> VisitInternalAssignment([NotNull] SBProcLangParser.InternalAssignmentContext context)
	{
		await QueryAgent.RunStatementAsync(context.GetText(), _outputBuffer, _setTranslated);
		await _update();
		return "ASSIGNMENT_INTERNAL";
	}

	public override async Task<object?> VisitMemberAssignment([NotNull] SBProcLangParser.MemberAssignmentContext context)
	{
		await QueryAgent.RunStatementAsync(context.GetText(), _outputBuffer, _setTranslated);
		await _update();
		return "MEMBER_ASSIGN";
	}

	public override async Task<object?> VisitVariableDeletion([NotNull] SBProcLangParser.VariableDeletionContext context)
	{
		await QueryAgent.RunStatementAsync(context.GetText(), _outputBuffer, _setTranslated);
		await _update();
		return "VAR_DELETION";
	}

	public override async Task<object?> VisitPrint([NotNull] SBProcLangParser.PrintContext context)
	{
		await QueryAgent.RunStatementAsync(context.GetText(), _outputBuffer, _setTranslated);
		await _update();
		return "PRINT";
	}

	public override async Task<object?> VisitClear_stat([NotNull] SBProcLangParser.Clear_statContext context)
	{
		_outputBuffer.Clear();
		await _update();
		return "CLEAR";
	}

	public override async Task<object?> VisitDelay_stat([NotNull] SBProcLangParser.Delay_statContext context)
	{
		var delay = double.Parse(context.Number().GetText());
		await Task.Delay((int)delay, _token);
		await _update();
		return "DELAY";
	}


	public override async Task<object?> VisitExpr_stat([NotNull] SBProcLangParser.Expr_statContext context)
	{
		await QueryAgent.RunStatementAsync(context.GetText(), _outputBuffer, _setTranslated);
		return "STAT_EXPR";
	}

	public override async Task<object?> VisitEmpty_stat([NotNull] SBProcLangParser.Empty_statContext context)
	{
		return await Task.FromResult("EMPTY");
	}

	public override async Task<object?> VisitMember_assign_expr([NotNull] SBProcLangParser.Member_assign_exprContext context)
	{
		var lhs = context.GetChild(0).GetText();
		var rhs = context.GetChild(2).GetText();

		var value = await QueryAgent.EvaluateExpressionAsync(rhs);

		var parts = lhs.Split(new[] { '.', '[' }, StringSplitOptions.RemoveEmptyEntries);

		var obj = (object?)WideVariable.GetValue(parts[0][1..]);

		for (var i = 1; i < parts.Length; i++)
		{
			var part = parts[i];
			if (i == parts.Length - 1)
			{
				if (part.EndsWith("]"))
				{
					part = part[..^1];
					var index = int.Parse(part);

					((Array?)obj)?.SetValue(value, index);
					continue;
				}

				var type = obj?.GetType();

				var prop = type?.GetProperty(part);

				if (prop != null)
				{
					prop.SetValue(obj, value);
					continue;
				}

				var field = type?.GetField(part);

				if (field != null)
				{
					field.SetValue(obj, value);
					continue;
				}

				throw new Exception("No such property or field: " + part);
			}

			if (part.EndsWith("]"))
			{
				part = part[..^1];
				var index = int.Parse(part);

				obj = ((Array?)obj)?.GetValue(index);
			}
			else
			{
				var type = obj?.GetType();

				var prop = type?.GetProperty(part);

				if (prop != null)
				{
					obj = prop.GetValue(obj);
					continue;
				}
				var field = type?.GetField(part);

				if (field != null)
				{
					obj = field.GetValue(obj);
					continue;
				}

				throw new Exception("No such property or field: " + part);
			}
		}

		return "MEMBER_ASSIGN";
	}

	public override async Task<object?> VisitFactor([NotNull] SBProcLangParser.FactorContext context)
	{
		if (context.Number() != null)
			return double.Parse(context.Number().GetText());

		if (context.WideID() != null)
		{
			var name = context.GetText()[1..];
			return WideVariable.GetValue(name);
		}

		if (context.BooleanLiteral() != null)
			return bool.Parse(context.BooleanLiteral().GetText());

		if (context.WordTypeLiteral() != null)
			return context.WordTypeLiteral().GetText();

		if (context.WordLiteral() != null)
			return context.WordLiteral().GetText();

		if (context.CharLiteral() != null)
			return context.CharLiteral().GetText()[1];

		if (context.StringLiteral() != null)
			return context.StringLiteral().GetText().Trim('"');

		if (context.RegexLiteral() != null)
			return new Regex(context.RegexLiteral().GetText().Trim('`'));

		if (context.arrayInitializer() != null)
			return await Visit(context.arrayInitializer());

		if (context.hashInitializer() != null)
			return await Visit(context.hashInitializer());

		if (context.listMarker() != null)
			return await Visit(context.listMarker());

		if (context.methodCall() != null)
			return await VisitMethodCall(context.methodCall());

		if (context.NullLiteral() != null)
			return null;

		if (context.expr() != null)
			return await Visit(context.expr());

		throw new SBProcLangVisitorException("Invalid factor: " + context.GetText());
	}


	public override async Task<object?> VisitTerm([NotNull] SBProcLangParser.TermContext context)
	{
		if (context.factor().Length > 0)
		{
			var value = await VisitFactor(context.factor(0)) as dynamic;

			for (var i = 1; i < context.factor().Length; i++)
			{
				var op = context.GetChild(i * 2 - 1).GetText();
				var next = await VisitFactor(context.factor(i)) as dynamic;

				value = op switch
				{
					"*" => value * next,
					"/" => value / next,
					"%" => value % next,
					"??" => value ?? next,
					_ => throw new SBProcLangVisitorException("Invalid operator"),
				};
			}

			return value;
		}
		if (context.WideID() != null)
		{
			var varName = context.WideID().Symbol.Text[1..];
			var op = context.op.Text;

			var value = WideVariable.GetValue(varName);

			switch (op)
			{
				case "++":
					value++;
					break;
				case "--":
					value--;
					break;
				default:
					throw new SBProcLangVisitorException("Invalid operator");
			}

			WideVariable.Variables[varName] = value;

			return value;
		}

		if (context.inputCall() != null)
			return await VisitInputCall(context.inputCall());

		if (context.procCall() != null)
			return await VisitProcCall(context.procCall());

		throw new SBProcLangVisitorException("Invalid term");
	}

	public override async Task<object?> VisitExpr([NotNull] SBProcLangParser.ExprContext context)
	{
		if (context.expr().Length == 3)
		{
			var condition = await VisitExpr(context.expr(0));
			var trueValue = await VisitExpr(context.expr(1));
			var falseValue = await VisitExpr(context.expr(2));

			return (bool)(condition ?? false) ? trueValue : falseValue;
		}
		if (context.term().Length > 0)
		{
			var value = await VisitTerm(context.term(0)) as dynamic;

			for (var i = 1; i < context.term().Length; i++)
			{
				var op = context.GetChild(i * 2 - 1).GetText();
				var next = await VisitTerm(context.term(i)) as dynamic;

				value = op switch
				{
					"+" => value + next,
					"-" => value - next,
					"==" => value == next,
					"!=" => value != next,
					"<" => value < next,
					">" => value > next,
					"<=" => value <= next,
					">=" => value >= next,
					"&&" => value && next,
					"||" => value || next,
					"&" => value & next,
					"|" => value | next,
					"<<" => value << next,
					">>" => value >> next,
					_ => throw new SBProcLangVisitorException("Invalid operator")
				};
			}

			return value;
		}

		if (context.op != null && context.expr() != null)
		{
			var value = await VisitExpr(context.expr(0)) as dynamic;

			value = context.op.Text switch
			{
				"-" => -value,
				"!" => !value,
				_ => throw new SBProcLangVisitorException("Invalid operator")
			};
			return value;
		}

		throw new SBProcLangVisitorException("Invalid expression");

	}
}

internal class SBProcLangVisitorException : Exception
{
	public SBProcLangVisitorException(string message = "") : base(message) { }
}

