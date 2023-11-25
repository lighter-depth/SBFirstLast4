#pragma warning disable CS8618

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace SBFirstLast4.Simulator;

public partial class Battle
{
	public Player Player1 { get; init; } = new(AbilityManager.Default);
	public Player Player2 { get; init; } = new(AbilityManager.Default);
	public Player PreActor { get; set; } = new(AbilityManager.Default);
	public Player PreReceiver { get; set; } = new(AbilityManager.Default);
	public bool IsPlayer1sTurn { get; private set; } = true;

	public bool WasPlayer1sTurn => !IsPlayer1sTurn;
	public int TurnNum { get; private set; } = 1;
	public List<string> UsedWords { get; init; } = new();
	public required Func<Task<Order>> In { get; init; }
	public required Func<List<AnnotatedString>, Task> Out { get; init; }
	public List<AnnotatedString> Buffer { get; private set; } = new();
	public required Action<CancellationTokenSource> OnReset { get; init; }

	Dictionary<OrderType, Action<Order, CancellationTokenSource>> OrderFunctions = new();
	public Player CurrentPlayer => IsPlayer1sTurn ? Player1 : Player2;
	public Player OtherPlayer => IsPlayer1sTurn ? Player2 : Player1;
	public OrderType CurrentOrderType { get; private set; } = OrderType.None;

	public bool IsStrict { get; set; } = true;
	public bool IsInferable { get; set; } = true;
	public bool IsCustomAbilUsable { get; set; } = true;

	public bool IsBeforeInit { get; private set; } = true;

	public char NextChar => OtherPlayer.CurrentWord.End;
	static readonly Action<Order, CancellationTokenSource> emptyDelegate = (o, c) => { };
	CancellationTokenSource cts = new();
	public static Battle Empty => new(new(AbilityManager.Default), new(AbilityManager.Default))
	{
		In = async () => await Task.Run(() => new Order()),
		Out = async a => await Task.CompletedTask,
		OnReset = c => { }
	};

	[SetsRequiredMembers]
	public Battle(Player p1, Player p2) => (Player1, Player2) = (p1, p2);

	[SetsRequiredMembers]
	public Battle() : this(new(AbilityManager.Default), new(AbilityManager.Default)) { }

	public void Dispose()
	{
		if(!cts.IsCancellationRequested) OnReset(cts);
	}

	public async Task Run()
	{
		Initialize();
		IsBeforeInit = false;
		await Out(Buffer);
		cts = new CancellationTokenSource();
		while (!cts.IsCancellationRequested)
		{
			Buffer.Clear();

			// 入力処理、CPU かどうかを判定
			var order = await In();
			CurrentOrderType = order.Type;
			if (order.Type is OrderType.None)
			{
				await Out(Buffer);
				continue;
			}

			// 辞書 OrderFunctions からコマンド名に合致するハンドラーを取り出す
			if (OrderFunctions.TryGetValue(order.Type, out var func))
				func(order, cts);
			else
				OnDefault(order, cts);

			// 出力処理
			await Out(Buffer);

		}
	}
	private void Initialize()
	{
		IsPlayer1sTurn = InitIsP1sTurn();
		Buffer.Add($"{CurrentPlayer.Name} のターンです", Notice.General);
		Buffer.Add($"{Player1.Name}: {Player1.HP}/{Player.MaxHP},     {Player2.Name}: {Player2.HP}/{Player.MaxHP}", Notice.LogInfo);
		OrderFunctions = new()
		{
			[OrderType.Error] = OnErrorOrdered,
			[OrderType.Change] = OnChangeOrdered
		};
	}
	/// <summary>
	/// 先攻・後攻の設定を行います。
	/// </summary>
	/// <returns><see cref="Player1"/>が先攻するかどうかを表すフラグ</returns>
	private bool InitIsP1sTurn()
	{
		var randomFlag = SBUtils.Random.Next(2) == 0;
		var p1TPA = Player1.Proceeding;
		var p2TPA = Player2.Proceeding;
		if (p1TPA == p2TPA) return randomFlag;
		if (p1TPA == Proceeds.True || p2TPA == Proceeds.False) return true;
		if (p1TPA == Proceeds.False || p2TPA == Proceeds.True) return false;
		return randomFlag;
	}

	/// <summary>
	/// デフォルトで実行されるハンドラーです。単語の種別に応じて<see cref="System.Diagnostics.Contracts.Contract"/>を生成し処理します。
	/// </summary>
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
		var c = Contract.Create(ct, CurrentPlayer, OtherPlayer, word, this, new ContractArgs(PreActor, PreReceiver) { IsInferSuccessed = isInferSuccessed });
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

		Buffer.AddMany(c.Message);

		// プレイヤーが死んだかどうかの判定、ターンの交代。
		if (c.DeadFlag)
		{
			IsPlayer1sTurn = !IsPlayer1sTurn;
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
			Buffer.Add($"入力 {order.Body} に対応するとくせいが見つかりませんでした。", Notice.Warn);
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
		if ((word = SBDictionary.TypedWords.Find(word => word.Name == name)) != default)
			return true;

		if (SBDictionary.IsLite || SBDictionary.NoTypeWords.Contains(name))
		{
			word = new(name, WordType.Empty, WordType.Empty);
			return true;
		}
		word = new();
		return false;
	}

	public void ToggleTurn()
	{
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
}