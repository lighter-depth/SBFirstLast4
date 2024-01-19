using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Buffer = System.Collections.Generic.List<(string Content, string Type)>;


namespace SBFirstLast4.Dynamic;

internal class SBProcLangVisitor : SBProcLangBaseVisitor<Task<object?>>
{
	private readonly Buffer _outputBuffer;
	private readonly Action<string> _setTranslated;
	private readonly Func<Task> _update;

	internal SBProcLangVisitor(Buffer outputBuffer, Action<string> setTranslated, Func<Task> update) 
		=> (_outputBuffer, _setTranslated, _update) = (outputBuffer, setTranslated, update);

	public override async Task<object?> VisitScript([NotNull] SBProcLangParser.ScriptContext context)
	{
		foreach (var statement in context.statement())
			await Visit(statement);

		return "SCRIPT";
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

	public override async Task<object?> VisitWhile_stat([NotNull] SBProcLangParser.While_statContext context)
	{
		var condition = await Visit(context.expr());

		while (condition is bool b && b)
		{
			try
			{
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
				await Visit(context.stat_block());
			}

			condition = await Visit(context.expr());
		}

		return "WHILE";
	}

	public override async Task<object?> VisitReturn_stat([NotNull] SBProcLangParser.Return_statContext context)
	{
		var value = await Visit(context.expr());
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
		return "WideAssignment";
	}

	public override async Task<object?> VisitInternalAssignment([NotNull] SBProcLangParser.InternalAssignmentContext context)
	{
		await QueryAgent.RunStatementAsync(context.GetText(), _outputBuffer, _setTranslated);
		await _update();
		return "InternalAssignment";
	}

	public override async Task<object?> VisitVariableDeletion([NotNull] SBProcLangParser.VariableDeletionContext context)
	{
		await QueryAgent.RunStatementAsync(context.GetText(), _outputBuffer, _setTranslated);
		await _update();
		return "VariableDeletion";
	}

	public override async Task<object?> VisitPrint([NotNull] SBProcLangParser.PrintContext context)
	{
		await QueryAgent.RunStatementAsync(context.GetText(), _outputBuffer, _setTranslated);
		await _update();
		return "Print";
	}

	public override async Task<object?> VisitExpr_stat([NotNull] SBProcLangParser.Expr_statContext context)
	{
		await QueryAgent.RunStatementAsync(context.GetText(), _outputBuffer, _setTranslated);
		await Task.CompletedTask;
		return "STAT_EXPR";
	}

	public override async Task<object?> VisitExpr([NotNull] SBProcLangParser.ExprContext context)
	{
		await Task.CompletedTask;
		return await QueryAgent.EvaluateExpression(context.GetText());
	}
}

