using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace SBFirstLast4.Simulator;

public partial class Battle
{
	public Player Player1 { get; private set; } = new(Ability.Default);
	public Player Player2 { get; private set; } = new(Ability.Default);
	public Player PreActor { get; set; } = new(Ability.Default);
	public Player PreReceiver { get; set; } = new(Ability.Default);
	public bool IsPlayer1sTurn { get; private set; } = true;

	public bool WasPlayer1sTurn => !IsPlayer1sTurn;
	public int TurnNum { get; private set; }
	public List<string> UsedWords { get; private set; } = new();
	public required Func<Task<Order>> In { get; init; } = () => Task.FromResult(Order.Empty);
	public required Func<IEnumerable<AnnotatedString>, Task> Out { get; init; } = _ => Task.CompletedTask;
	public List<AnnotatedString> Buffer { get; private set; } = new();
	public required Action<CancellationTokenSource> OnReset { get; init; } = _ => { };

	private Dictionary<OrderType, Action<Order, CancellationTokenSource>> OrderFunctions = new();
	public Player CurrentPlayer => IsPlayer1sTurn ? Player1 : Player2;
	public Player OtherPlayer => IsPlayer1sTurn ? Player2 : Player1;
	public OrderType CurrentOrderType { get; private set; } = OrderType.None;

	public bool IsStrict { get; set; } = true;
	public bool IsInferable { get; set; } = true;
	public bool IsCustomAbilUsable { get; set; } = true;

	public bool IsBeforeInit { get; private set; } = true;

	public char NextChar => OtherPlayer.CurrentWord.End;

	public Dictionary<int, BattleData> DHistory => _dHistory.Where(kv => !kv.Value.IsEdited).ToDictionary(kv => kv.Key, kv => kv.Value);

	public void SetDHistoryElement(BattleData value, int index)
	{
		_dHistory[index] = value;
		foreach(var kv in _dHistory)
			if(kv.Key > index)
				kv.Value.IsEdited = true;
	}

	private Dictionary<int, BattleData> _dHistory = new();

	private CancellationTokenSource Cancellation = new();
	public static Battle Default => new(new(Ability.Default), new(Ability.Default));

	[SetsRequiredMembers]
	public Battle(Player p1, Player p2) => (Player1, Player2) = (p1, p2);

	[SetsRequiredMembers]
	public Battle() : this(new(Ability.Default), new(Ability.Default)) { }

	public void Dispose()
	{
		if(!Cancellation.IsCancellationRequested) OnReset(Cancellation);
	}

	public async Task Run()
	{
		Initialize();
		IsBeforeInit = false;
		await Out(Buffer);
		Cancellation = new CancellationTokenSource();
		while (!Cancellation.IsCancellationRequested)
		{
			Buffer.Clear();

			var order = await In();
			CurrentOrderType = order.Type;
			if (order.Type is OrderType.None)
			{
				await Out(Buffer);
				continue;
			}

			if (OrderFunctions.TryGetValue(order.Type, out var func))
				func(order, Cancellation);
			else
				OnDefault(order, Cancellation);

			await Out(Buffer);

		}
	}
	private void Initialize()
	{
		IsPlayer1sTurn = InitIsPlayer1sTurn();
		Buffer.Add($"{CurrentPlayer.Name} のターンです", Notice.General);
		Buffer.Add($"{Player1.Name}: {Player1.HP}/{Player.MaxHP},     {Player2.Name}: {Player2.HP}/{Player.MaxHP}", Notice.LogInfo);
		OrderFunctions = new()
		{
			[OrderType.Error] = OnErrorOrdered,
			[OrderType.Change] = OnChangeOrdered
		};
		_dHistory[TurnNum] =(BattleData)this with { IsPlayer1sTurn = !IsPlayer1sTurn };
		TurnNum++;
	}

	private bool InitIsPlayer1sTurn()
	{
		var randomFlag = Utils.Random.Next(2) == 0;
		var player1Proceeds = Player1.Proceeds;
		var player2Proceeds = Player2.Proceeds;
		if (player1Proceeds == player2Proceeds) return randomFlag;
		if (player1Proceeds == Proceeds.True || player2Proceeds == Proceeds.False) return true;
		if (player1Proceeds == Proceeds.False || player2Proceeds == Proceeds.True) return false;
		return randomFlag;
	}

	public void OnDefault(Order order, CancellationTokenSource cts)
	{
		Word word;

		// 単語をタイプ推論し、生成する。

		// タイプ推論が成功したかどうかを表すフラグ。
		bool isInferSuccessed;

		// タイプ推論が有効な場合。推論できない場合は無属性。
		if (IsInferable && order.TypeParam is null)
		{
			var name = order.Body.Replace('ヴ', 'ゔ');
			isInferSuccessed = TryInferWordTypes(name, out var wordtemp);
			word = wordtemp;
		}
		// タイプ推論が無効な場合。手動で入力されたタイプを参照し、単語に変換する。
		// 単語の書式が不正な場合には、isInferSuccessed を false に設定する。
		else
		{
			var type1 = order.TypeParam?.Length > 0 ? order.TypeParam?[0].CharToType() : WordType.Empty;
			var type2 = order.TypeParam?.Length > 0 ? order.TypeParam?.Length > 1 ? order.TypeParam?[1].CharToType() : WordType.Empty : WordType.Empty;
			word = new Word(order.Body, type1 ?? WordType.Empty, type2 ?? WordType.Empty);
			isInferSuccessed = word.Name.IsWild() || KanaRegex().IsMatch(word.Name);
		}

		// ContractType の決定。
		var ct = word.IsBuf(CurrentPlayer.Ability) ? ContractType.Buf
			   : word.IsHeal ? ContractType.Heal
			   : word.IsSeed(CurrentPlayer.Ability) ? ContractType.Seed
			   : ContractType.Attack;
		var c = Contract.Create(ct, CurrentPlayer, OtherPlayer, word, this, new(PreActor, PreReceiver) { IsInferSuccessed = isInferSuccessed });
		c.Execute();

		// Contract の情報をバッファーに追加する。
		Buffer.Add(ct switch
		{
			ContractType.Attack => $"{CurrentPlayer.Name} は単語 {word} で攻撃した！",
			ContractType.Buf => $"{CurrentPlayer.Name} は単語 {word} で能力を高めた！",
			ContractType.Heal => $"{CurrentPlayer.Name} は単語 {word} を使った！",
			ContractType.Seed => $"{CurrentPlayer.Name} は単語 {word} でやどりぎを植えた！",
			_ => throw new ArgumentException($"ContractType \"{ct}\" has not been implemented.")
		}, Notice.LogActionInfo);

		Buffer.AddRange(c.Message);

		// プレイヤーが死んだかどうかの判定、ターンの交代。
		if (c.DeadFlag)
		{
			IsPlayer1sTurn = !IsPlayer1sTurn;
			SetDHistoryElement((BattleData)this, TurnNum);
			Out(Buffer);
			OnReset(cts);
		}
		if (c.IsBodyExecuted && !cts.IsCancellationRequested) ToggleTurn();
	}
	public void OnErrorOrdered(Order order, CancellationTokenSource cts)
	{
		Buffer.Add(order?.ErrorMessage ?? "入力が不正です", Notice.Warn);
	}


	public void OnChangeOrdered(Order order, CancellationTokenSource cts)
	{
		var player = PlayerSelectorToPlayerOrDefault(order.Selector);
		if (player is null)
		{
			Buffer.Add("条件に一致するプレイヤーが見つかりませんでした。", Notice.Warn);
			return;
		}
		var nextAbil = AbilityManager.Create(order.Body, IsCustomAbilUsable);
		if (nextAbil is null)
		{
			Buffer.Add($"そんなとくせいはない！", Notice.Warn);
			return;
		}
		if (nextAbil.ToString() == player.Ability.ToString())
		{
			Buffer.Add("既にそのとくせいになっている！", Notice.Warn);
			return;
		}
		if (player.TryChangeAbility(nextAbil))
		{
			Buffer.Add($"{player.Name} はとくせいを {nextAbil.ToString()} に変更しました", Notice.SystemInfo);
			return;
		}
		Buffer.Add($"{player.Name} はもう特性を変えられない！", Notice.Caution);
	}


	public static bool TryInferWordTypes(string name, out Word word)
	{
		if (name.IsWild())
		{
			word = new(name, WordType.Empty, WordType.Empty);
			return true;
		}
		if ((word = Words.TypedWords.Find(word => word.Name == name)) != default)
			return true;

		if (Words.IsLite || Words.NoTypeWords.Contains(name))
		{
			word = new(name, WordType.Empty, WordType.Empty);
			return true;
		}
		word = new();
		return false;
	}

	public void ToggleTurn()
	{
		SetDHistoryElement((BattleData)this, TurnNum);
		PreActor = CurrentPlayer.Clone();
		PreReceiver = OtherPlayer.Clone();
		IsPlayer1sTurn = !IsPlayer1sTurn;
		TurnNum++;
		if (TurnNum > 1)
		{
			Buffer.Add($"{CurrentPlayer.Name} のターンです", Notice.General);
			Buffer.Add($"{Player1.Name}: {Player1.HP}/{Player.MaxHP},     {Player2.Name}: {Player2.HP}/{Player.MaxHP}", Notice.LogInfo);
		}
	}
	internal Player? PlayerSelectorToPlayerOrDefault(PlayerSelector selector)
	{
		return selector switch
		{
			PlayerSelector.Player1 => Player1,
			PlayerSelector.Player2 => Player2,
			_ => null
		};
	}

	[GeneratedRegex("^[ぁ-ゔゟヴー]*$")]
	private static partial Regex KanaRegex();

	public void AlterTo(BattleData? d)
	{
		if (d is null)
			return;

		Player1 = d.Player1;
		Player2 = d.Player2;
		PreActor = d.PreActor;
		PreReceiver = d.PreReceiver;
		IsPlayer1sTurn = d.IsPlayer1sTurn;
		TurnNum = d.TurnNum;
		UsedWords = d.UsedWords.Select(x => x).ToList();
	}

	public void AlterBack()
	{
		if (!DHistory.TryGetValue(Math.Max(0, TurnNum - 2), out var d))
			return;

		AlterTo(d with { IsPlayer1sTurn = !d.IsPlayer1sTurn, TurnNum = d.TurnNum + 1});
	}

	public void AlterForth()
	{
		if (!DHistory.TryGetValue(TurnNum, out var d))
			return;

		AlterTo(d with { IsPlayer1sTurn = !d.IsPlayer1sTurn, TurnNum = d.TurnNum + 1});
	}

	public void AlterLatest()
	{
		var d = DHistory.OrderBy(kv => kv.Key).LastOrDefault();

		if (d.IsDefault())
			return;

		AlterTo(d.Value with { IsPlayer1sTurn = !d.Value.IsPlayer1sTurn, TurnNum = d.Value.TurnNum + 1 });
	}
}