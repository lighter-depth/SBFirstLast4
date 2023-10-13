using System.Diagnostics.CodeAnalysis;

namespace SBFirstLast4.Simulator;

/// <summary>
/// アクションの種類を管理するフラグです。
/// </summary>
public enum ContractType
{
	None,
	Attack,
	Buf,
	Heal,
	Seed
}
/// <summary>
/// １ターンの間に起こるアクションを管理するスーパークラスです。
/// </summary>
public abstract class Contract
{
	#region properties
	/// <summary>
	/// アクションを行う側のプレイヤー
	/// </summary>
	public required Player Actor { get; init; }
	/// <summary>
	/// アクションを受ける側のプレイヤー
	/// </summary>
	public required Player Receiver { get; init; }
	/// <summary>
	/// アクションに使用される単語
	/// </summary>
	public required Word Word { get; init; }
	/// <summary>
	/// 呼び出し元の<see cref="Battle"/>クラスの情報
	/// </summary>
	public required Battle Parent { get; init; }
	/// <summary>
	/// アクションを補助する情報
	/// </summary>
	public ContractArgs Args { get; set; } = ContractArgs.Empty;
	/// <summary>
	/// アクションの種類
	/// </summary>
	public abstract ContractType Type { get; }
	/// <summary>
	/// アクションの進行状況
	/// </summary>
	public AbilityType State { get; protected set; } = AbilityType.None;
	/// <summary>
	/// プレイヤーが死んだかどうかを表すフラグ
	/// </summary>
	public bool DeadFlag { get; protected set; } = false;
	/// <summary>
	/// アクションが実行されたかどうかを表すフラグ
	/// </summary>
	public bool IsBodyExecuted { get; protected set; } = true;
	/// <summary>
	/// <see cref="Battle.Out"/>に渡すアノテーション付き文字列のリスト
	/// </summary>
	public List<AnnotatedString> Message { get; protected set; } = new();
	/// <summary>
	/// アクションを実行するハンドラーの情報を保持するリスト
	/// </summary>
	protected List<Action> Contents { get; set; } = new();
	#endregion

	#region constructors
	[SetsRequiredMembers]
	public Contract(Player actor, Player receiver, Word word, Battle parent) : this(actor, receiver, word, parent, ContractArgs.Empty) { }
	[SetsRequiredMembers]
	public Contract(Player actor, Player receiver, Word word, Battle parent, ContractArgs args)
	{
		Actor = actor;
		Receiver = receiver;
		Word = word;
		Parent = parent;
		Args = args;
		Contents = new()
		{
			OnContractBegin,
			OnActionBegin,
			OnActionExecuted,
			OnReceive,
			OnActionEnd,
			OnContractEnd
		};
	}
	[SetsRequiredMembers] public Contract() : this(new(AbilityManager.Default), new(AbilityManager.Default), new(), Battle.Empty) { }
	#endregion

	#region methods
	/// <summary>
	/// コントラクトを実行します。
	/// </summary>
	public void Execute()
	{
		var usedCheck = OnWordUsedCheck();
		var inferCheck = OnWordInferCheck();
		if (!usedCheck || !inferCheck)
		{
			IsBodyExecuted = false;
			return;
		}
		foreach (var action in Contents)
		{
			action();
		}
	}
	/// <summary>
	/// 単語が使用済みかどうかをチェックします。
	/// </summary>
	/// <returns>単語が使用可能かどうかを表すフラグ</returns>
	public virtual bool OnWordUsedCheck()
	{
		State = AbilityType.WordUsedChecked;
		if (Parent.IsStrict && Args.IsInferSuccessed)
		{
			var strictFlag = Word.IsSuitable(Receiver.CurrentWord);
			Args.IsWordNotUsed = !Parent.UsedWords.Contains(Word.Name);
			if (strictFlag > 0)
			{
				Message.Add("開始文字がマッチしていません。", Notice.Warn);
				return false;
			}
			if (strictFlag < 0)
			{
				Message.Add("「ん」で終わっています", Notice.Warn);
				return false;
			}
			if (Actor.Ability.Type.HasFlag(AbilityType.WordUsedChecked))
			{
				Actor.Ability.Execute(this);
			}
			if (!Args.IsWordNotUsed)
			{
				Message.Add("すでに使われた単語です", Notice.Warn);
				return false;
			}
		}
		return true;
	}
	/// <summary>
	/// 単語がタイプ推論可能かどうかをチェックします。
	/// </summary>
	/// <returns>単語が使用可能かどうかを表すフラグ</returns>
	public virtual bool OnWordInferCheck()
	{
		State = AbilityType.WordInferChecked;
		if (Actor.Ability.Type.HasFlag(State))
		{
			Actor.Ability.Execute(this);
			return true;
		}
		if (Parent.IsInferable)
		{
			if (!Args.IsInferSuccessed)
			{
				Message.Add("辞書にない単語です。", Notice.Warn);
				return false;
			}
		}
		return true;
	}
	/// <summary>
	/// コントラクト開始時の処理を実行します。
	/// </summary>
	public virtual void OnContractBegin()
	{
		State = AbilityType.ContractBegin;
		Actor.CurrentWord = Word;
		if (Actor.Ability.Type.HasFlag(State))
			Actor.Ability.Execute(this);
	}
	/// <summary>
	/// アクション開始時の処理を実行します。
	/// </summary>
	public virtual void OnActionBegin()
	{
		State = AbilityType.ActionBegin;
		if (Actor.Ability.Type.HasFlag(State))
			Actor.Ability.Execute(this);
	}
	/// <summary>
	/// アクション実行時の処理を実行します。
	/// </summary>
	public virtual void OnActionExecuted()
	{
		State = AbilityType.ActionExecuted;
		if (Actor.Ability.Type.HasFlag(State))
			Actor.Ability.Execute(this);
	}
	/// <summary>
	/// アクション終了時の処理を実行します。
	/// </summary>
	public virtual void OnActionEnd()
	{
		State = AbilityType.ActionEnd;
		if (Actor.Ability.Type.HasFlag(State))
			Actor.Ability.Execute(this);
	}
	/// <summary>
	/// アクションを受け取ったときの処理を実行します。
	/// </summary>
	public virtual void OnReceive()
	{
		State = AbilityType.Received;
		if (Receiver.Ability.Type.HasFlag(State))
			Receiver.Ability.Execute(this);
	}
	/// <summary>
	/// コントラクト終了時の処理を実行します。
	/// </summary>
	public virtual void OnContractEnd()
	{
		State = AbilityType.ContractEnd;
		if (Actor.Ability.Type.HasFlag(State))
			Actor.Ability.Execute(this);
		if (Receiver.State.HasFlag(PlayerState.Seed))
		{
			Receiver.TakeSeedDmg(Actor);
			Message.Add(Notice.HPUpdated, Parent.Player1.Clone().HP, Parent.Player2.Clone().HP);
			Message.Add($"{Actor.Name} はやどりぎで体力を奪った！", Notice.SeedDmg);
		}
		if (Receiver.State.HasFlag(PlayerState.Poison))
		{
			Receiver.TakePoisonDmg();
			Message.Add(Notice.HPUpdated, Parent.Player1.Clone().HP, Parent.Player2.Clone().HP);
			Message.Add($"{Receiver.Name} は毒のダメージをうけた！", Notice.PoisonDmg);
		}
		if (Receiver.HP <= 0)
		{
			Receiver.HP = 0;
			Message.Add($"{Actor.Name} は {Receiver.Name} を倒した！", Notice.DeathInfo);
			DeadFlag = true;
			return;
		}
		DeadFlag = false;
		if (!Word.Name.IsWild()) Parent.UsedWords.Add(Word.Name);
	}
	/// <summary>
	/// コントラクトを生成するファクトリ メソッドです。
	/// </summary>
	/// <exception cref="ArgumentException"></exception>
	public static Contract Create(ContractType c, Player actor, Player receiver, Word word, Battle parent, ContractArgs args)
	{
		return c switch
		{
			ContractType.Attack => new AttackContract(actor, receiver, word, parent, args),
			ContractType.Buf => new BufContract(actor, receiver, word, parent, args),
			ContractType.Heal => new HealContract(actor, receiver, word, parent, args),
			ContractType.Seed => new SeedContract(actor, receiver, word, parent, args),
			_ => throw new ArgumentException($"ContractType \"{c}\" is not implemented.")
		};
	}
	#endregion
}
/// <summary>
/// <see cref="Contract"/>の補助的な情報を管理するクラスです。
/// </summary>
public class ContractArgs
{
	/// <summary>
	/// タイプ推論が成功したかどうかを表すフラグ
	/// </summary>
	public bool IsInferSuccessed { get; set; }
	/// <summary>
	/// 単語がまだ使われていないかを表すフラグ
	/// </summary>
	public bool IsWordNotUsed { get; set; }
	/// <summary>
	/// １ターン前の<see cref="Contract.Actor"/>の情報
	/// </summary>
	public Player PreActor { get; set; }
	/// <summary>
	/// １ターン前の<see cref="Contract.Receiver"/>の情報
	/// </summary>
	public Player PreReceiver { get; set; }
	public ContractArgs(Player pa, Player pr)
	{
		IsInferSuccessed = false;
		IsWordNotUsed = false;
		PreActor = pa;
		PreReceiver = pr;
	}
	public static ContractArgs Empty => _empty;
	private static readonly ContractArgs _empty = new(new(AbilityManager.Default), new(AbilityManager.Default));
}
/// <summary>
/// 攻撃のコントラクトを管理するクラスです。
/// </summary>
internal class AttackContract : Contract
{
	public override ContractType Type => ContractType.Attack;
	/// <summary>
	/// 基礎ダメージの値
	/// </summary>
	public int BaseDmg { get; internal set; }
	/// <summary>
	/// タイプ相性によるダメージ倍率の値
	/// </summary>
	public double PropDmg { get; internal set; } = 1;
	/// <summary>
	/// バフによるダメージ倍率の値
	/// </summary>
	public double MtpDmg { get; internal set; } = 1;
	/// <summary>
	/// とくせいによるダメージ倍率の値
	/// </summary>
	public double AmpDmg { get; internal set; } = 1;
	/// <summary>
	/// はんしょくによるダメージ倍率の値
	/// </summary>
	public int BrdDmg { get; internal set; } = 1;
	/// <summary>
	/// 攻撃が急所に当たるかどうかを表すフラグ
	/// </summary>
	public bool CritFlag { get; internal set; } = false;
	/// <summary>
	/// どく状態を付与したかを表すフラグ
	/// </summary>
	public bool PoisonFlag { get; internal set; } = false;

	#region constructors
	[SetsRequiredMembers]
	public AttackContract(Player actor, Player receiver, Word word, Battle parent, ContractArgs args) : base(actor, receiver, word, parent, args)
	{
		Contents = new()
		{
			OnContractBegin,
			OnBaseCalc,
			OnPropCalc,
			OnAmpCalc,
			OnBrdCalc,
			OnCritCalc,
			OnMtpCalc,
			OnActionBegin,
			OnActionExecuted,
			OnViolenceUsed,
			OnReceive,
			OnActionEnd,
			OnContractEnd
		};
	}
	[SetsRequiredMembers]
	public AttackContract() : base() { }
	#endregion
	/// <summary>
	/// 基礎ダメージを計算します。
	/// </summary>
	public void OnBaseCalc()
	{
		State = AbilityType.BaseDecided;
		BaseDmg = Word.Type1 == WordType.Empty ? 7 : 10;
		if (Actor.Ability.Type.HasFlag(State))
			Actor.Ability.Execute(this);
	}
	/// <summary>
	/// タイプ相性によるダメージ倍率を計算します。
	/// </summary>
	public void OnPropCalc()
	{
		State = AbilityType.PropCalced;
		PropDmg = Word.CalcEffectiveDmg(Receiver.CurrentWord);
		if (Actor.Ability.Type.HasFlag(State))
			Actor.Ability.Execute(this);
		if (Receiver.Ability.Type.HasFlag(State))
			Receiver.Ability.Execute(this);
	}
	/// <summary>
	/// とくせいによるダメージ倍率を計算します。
	/// </summary>
	public void OnAmpCalc()
	{
		State = AbilityType.AmpDecided;
		if (Actor.Ability.Type.HasFlag(State))
			Actor.Ability.Execute(this);
	}
	/// <summary>
	/// はんしょくによるダメージ倍率を計算します。
	/// </summary>
	public void OnBrdCalc()
	{
		State = AbilityType.BrdDecided;
		if (Actor.Ability.Type.HasFlag(State))
			Actor.Ability.Execute(this);
	}
	/// <summary>
	/// 急所によるダメージを計算します。
	/// </summary>
	public void OnCritCalc()
	{
		State = AbilityType.CritDecided;
		if (Word.IsCritable)
			CritFlag = SBUtils.Random.Next(5) == 0;
		if (Actor.Ability.Type.HasFlag(State))
			Actor.Ability.Execute(this);
	}
	/// <summary>
	/// バフによるダメージ倍率を計算します。
	/// </summary>
	public void OnMtpCalc()
	{
		State = AbilityType.MtpCalced;
		if (!Actor.Ability.Type.HasFlag(State) && !Receiver.Ability.Type.HasFlag(State))
		{
			MtpDmg = CritFlag ? Math.Max(Actor.ATK / Receiver.DEF, 1) : Actor.ATK / Receiver.DEF;
			return;
		}
		if (Actor.Ability.Type.HasFlag(State))
			Actor.Ability.Execute(this);
		if (Receiver.Ability.Type.HasFlag(State))
			Receiver.Ability.Execute(this);
	}
	/// <summary>
	/// ダメージの計算、およびアクション開始時の処理を実行します。
	/// </summary>
	public override void OnActionBegin()
	{
		State = AbilityType.ActionBegin;

		// かりうむ式のダメージ計算。
		// 攻撃1.5倍で4倍弱点をつくと最低乱数で50ダメージ出る。
		// 4倍弱点 × 急所 でダメージを与えると 51 - 58 ダメージ出る。
		var critDmg = CritFlag ? Player.CritDmg : 1;
		var randomFlag = !(Actor.CurrentWord.Type1 == WordType.Empty || Receiver.CurrentWord.Type1 == WordType.Empty);
		var random = randomFlag ? 0.85 + SBUtils.Random.Next(15) * 0.01 : 1;
		var damage = (int)(critDmg * (int)(AmpDmg * BrdDmg * (int)(BaseDmg * PropDmg * MtpDmg * random)));
		Receiver.HP -= damage;
		if (Receiver.Ability.Type.HasFlag(State))
			Receiver.Ability.Execute(this);
		Message.Add(Notice.HPUpdated, Parent.Player1.Clone().HP, Parent.Player2.Clone().HP);
	}
	/// <summary>
	/// アクションの結果を判定します。
	/// </summary>
	public override void OnActionExecuted()
	{
		State = AbilityType.ActionExecuted;
		var (text, notice) = PropDmg switch
		{
			0 => ("こうかがないようだ...", Notice.NoDmgProp),
			>= 2 => ("こうかはばつぐんだ！", Notice.EffectiveProp),
			> 0 and < 1 => ("こうかはいまひとつのようだ...", Notice.NonEffectiveProp),
			1 => ("ふつうのダメージだ", Notice.MidDmgProp),
			_ => (string.Empty, Notice.None)
		};
		Message.Add(text, notice);
		if (CritFlag)
		{
			Message.Add("急所に当たった！", Notice.CritInfo);
		}
		if (Actor.Ability.Type.HasFlag(State))
			Actor.Ability.Execute(this);
	}
	/// <summary>
	/// 暴力タイプ使用時の処理を実行します。
	/// </summary>
	public void OnViolenceUsed()
	{
		State = AbilityType.ViolenceUsed;
		if (!Word.IsViolence) return;
		if (Actor.Ability.Type.HasFlag(State))
		{
			Actor.Ability.Execute(this);
			return;
		}
		if (Actor.TryChangeATK(-2, Word))
		{
			Message.Add($"{Actor.Name} の攻撃ががくっと下がった！(現在 {Actor.ATK,0:0.0#}倍)", Notice.Debuf);
			return;
		}
		Message.Add($"{Actor.Name} の攻撃はもう下がらない！", Notice.Caution);
	}
}
/// <summary>
/// バフのコントラクトを管理するクラスです。
/// </summary>
internal class BufContract : Contract
{
	public override ContractType Type => ContractType.Buf;
	#region constructors
	[SetsRequiredMembers]
	public BufContract(Player actor, Player receiver, Word word, Battle parent, ContractArgs args) : base(actor, receiver, word, parent, args) { }
	[SetsRequiredMembers]
	public BufContract() : base() { }
	#endregion
	public override void OnActionBegin()
	{
		State = AbilityType.ActionBegin;
		if (Actor.Ability.Type.HasFlag(State))
			Actor.Ability.Execute(this);
	}
}
/// <summary>
/// 回復のコントラクトを管理するクラスです。
/// </summary>
internal class HealContract : Contract
{
	public override ContractType Type => ContractType.Heal;
	/// <summary>
	/// 医療タイプによる回復であるかどうかを表すフラグ
	/// </summary>
	public bool IsCure { get; set; } = false;
	/// <summary>
	/// 回復が成功するかどうかを表すフラグ
	/// </summary>
	public bool CanHeal { get; set; } = false;
	#region constructors
	[SetsRequiredMembers]
	public HealContract(Player actor, Player receiver, Word word, Battle parent, ContractArgs args) : base(actor, receiver, word, parent, args)
	{
		Contents = new()
		{
			OnContractBegin,
			OnHealAmtCalc,
			OnDetermineCanHeal,
			OnActionBegin,
			OnActionExecuted,
			OnReceive,
			OnActionEnd,
			OnContractEnd
		};
	}
	[SetsRequiredMembers]
	public HealContract() : base() { }
	#endregion
	/// <summary>
	/// 回復量を計算します。
	/// </summary>
	public void OnHealAmtCalc()
	{
		State = AbilityType.HealAmtCalc;
		if (Actor.Ability.Type.HasFlag(State))
		{
			Actor.Ability.Execute(this);
			return;
		}
		IsCure = !Word.Contains(WordType.Food);
	}
	/// <summary>
	/// 回復可能かどうかを決定します。
	/// </summary>
	public void OnDetermineCanHeal()
	{
		State = AbilityType.DetermineCanHeal;
		if (Actor.Ability.Type.HasFlag(State))
		{
			Actor.Ability.Execute(this);
			return;
		}
		if (IsCure)
		{
			CanHeal = Actor.CureCountRemain > 0;
			return;
		}
		CanHeal = Actor.FoodCountRemain > 0;
	}
	public override void OnActionBegin()
	{
		State = AbilityType.ActionBegin;
		if (Actor.Ability.Type.HasFlag(State))
		{
			Actor.Ability.Execute(this);
		}
		if (!CanHeal)
		{
			if (IsCure)
			{
				Message.Add($"{Actor.Name} はもう回復できない！", Notice.Caution);
				return;
			}
			Message.Add($"{Actor.Name} はもう食べられない！", Notice.Caution);
			return;
		}
		Actor.Heal(IsCure);
	}
	public override void OnActionExecuted()
	{
		State = AbilityType.ActionExecuted;
		if (Actor.Ability.Type.HasFlag(State))
		{
			Actor.Ability.Execute(this);
		}
		if (IsCure && CanHeal)
		{
			if (Actor.State.HasFlag(PlayerState.Poison))
			{
				Message.Add($"{Actor.Name} の毒がなおった！", Notice.PoisonHeal, 0);
				Actor.DePoison();
			}
			Message.Add(Notice.HPUpdated, Parent.Player1.Clone().HP, Parent.Player2.Clone().HP);
			Message.Add($"{Actor.Name} の体力が回復した", Notice.Heal);
			return;
		}
		if (CanHeal)
		{
			Message.Add(Notice.HPUpdated, Parent.Player1.Clone().HP, Parent.Player2.Clone().HP);
			Message.Add($"{Actor.Name} の体力が回復した", Notice.Heal);
		}
	}
}
/// <summary>
/// やどりぎのコントラクトを管理するクラスです。
/// </summary>
internal class SeedContract : Contract
{
	public override ContractType Type => ContractType.Seed;
	/// <summary>
	/// やどりぎ状態を付与したかどうかを表すフラグ
	/// </summary>
	public bool SeedFlag { get; internal set; } = false;
	#region constructors
	[SetsRequiredMembers]
	public SeedContract(Player actor, Player receiver, Word word, Battle parent, ContractArgs args) : base(actor, receiver, word, parent, args) { }
	[SetsRequiredMembers]
	public SeedContract() : base() { }
	#endregion
}
