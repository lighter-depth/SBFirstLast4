﻿namespace SBFirstLast4.Simulator;

/// <summary>
/// 命令の種類を表す列挙型です。
/// </summary>
public enum OrderType
{
	None,
	Action,
	Change,
	EnablerOption,
	NumOption,
	PlayerNumOption,
	PlayerStringOption,
	ModeOption,
	Show,
	AI,
	Reset,
	Exit,
	Help,
	Add,
	Remove,
	Search,
	Error
}
/// <summary>
/// プレイヤーを指定する列挙型です。
/// </summary>
public enum PlayerSelector
{
	None,
	Player1,
	Player2
}
/// <summary>
/// オプションの種類を表す列挙型です。
/// </summary>
public enum Options
{
	None,
	SetMaxHP,
	SetHP,
	SetATK,
	SetDEF,
	SetCurrentWord,
	InfiniteSeed,
	InfiniteCure,
	AbilChange,
	Strict,
	Infer,
	CustomAbil,
	CPUDelay,
	SetAbilCount,
	SetMaxCureCount,
	SetMaxFoodCount,
	SetSeedDmg,
	SetMaxSeedTurn,
	SetCritDmgMultiplier,
	SetInsBufQty,
	SetMode,
	SetLuck
}

public class Order
{
	public OrderType Type { get; set; }

	public PlayerSelector Selector { get; set; }

	public Options Option { get; set; }

	public bool Enabler { get; set; }

	public string Body { get; set; } = string.Empty;

	public string? TypeParam { get; set; }

	public double[] Param { get; set; } = Array.Empty<double>();

	public string? ErrorMessage { get; internal set; }

	public static Order Empty => _empty;
	private static readonly Order _empty = new();

	static readonly Order DefaultError = new(OrderType.Error) { ErrorMessage = "なにかがおかしいよ" };
	static readonly Order NoParameterError = new(OrderType.Error) { ErrorMessage = "パラメーターが指定されていません" };
	static readonly Dictionary<string, Options> OptionDic = new()
	{
		["setmaxhp"] = Options.SetMaxHP,
		["smh"] = Options.SetMaxHP,
		["sethp"] = Options.SetHP,
		["sh"] = Options.SetHP,
		["infiniteseed"] = Options.InfiniteSeed,
		["is"] = Options.InfiniteSeed,
		["infinitecure"] = Options.InfiniteCure,
		["ic"] = Options.InfiniteCure,
		["abilchange"] = Options.AbilChange,
		["ac"] = Options.AbilChange,
		["strict"] = Options.Strict,
		["s"] = Options.Strict,
		["infer"] = Options.Infer,
		["i"] = Options.Infer,
		["customabil"] = Options.CustomAbil,
		["ca"] = Options.CustomAbil,
		["cpudelay"] = Options.CPUDelay,
		["delay"] = Options.CPUDelay,
		["cd"] = Options.CPUDelay,
		["setabilcount"] = Options.SetAbilCount,
		["sac"] = Options.SetAbilCount,
		["setmaxcurecount"] = Options.SetMaxCureCount,
		["smcc"] = Options.SetMaxCureCount,
		["smc"] = Options.SetMaxCureCount,
		["setmaxfoodcount"] = Options.SetMaxFoodCount,
		["smfc"] = Options.SetMaxFoodCount,
		["smf"] = Options.SetMaxFoodCount,
		["setseeddmg"] = Options.SetSeedDmg,
		["ssd"] = Options.SetSeedDmg,
		["setmaxseedturn"] = Options.SetMaxSeedTurn,
		["smst"] = Options.SetMaxSeedTurn,
		["sms"] = Options.SetMaxSeedTurn,
		["setcritdmgmultiplier"] = Options.SetCritDmgMultiplier,
		["scdm"] = Options.SetCritDmgMultiplier,
		["scd"] = Options.SetCritDmgMultiplier,
		["setinfbufqty"] = Options.SetInsBufQty,
		["sibq"] = Options.SetInsBufQty,
		["sib"] = Options.SetInsBufQty,
		["setmode"] = Options.SetMode,
		["sm"] = Options.SetMode,
		["setluck"] = Options.SetLuck,
		["sl"] = Options.SetLuck
	};
	public Order(OrderType type = OrderType.None, string body = "", PlayerSelector selector = PlayerSelector.None, params double[] param)
	=> (Type, Body, Selector, Param) = (type, body, selector, param);
	public Order(Options option, string body) => (Type, Option, Body) = (OrderType.ModeOption, option, body);
	public Order(Options option, bool enabler) => (Type, Option, Enabler) = (OrderType.EnablerOption, option, enabler);
	public Order(Options option, PlayerSelector selector, params double[] param) => (Type, Option, Selector, Param) = (OrderType.PlayerNumOption, option, selector, param);
	public Order(Options option, string body, PlayerSelector selector) => (Type, Option, Body, Selector) = (OrderType.PlayerStringOption, option, body, selector);
	public Order(Options option, params double[] param) => (Type, Option, Param) = (OrderType.NumOption, option, param);
	public Order(string wordName) => (Type, Body) = (OrderType.Action, wordName);
	public Order(string wordName, string typeName) => (Type, Body, TypeParam) = (OrderType.Action, wordName, typeName);
	
	public static Order Parse(string[] value, Battle parent)
	{
		var key = value.At(0)?.ToLower();
		if (key is "change" or "ch") return ParseChangeOrder(value, parent);
		if (key is "show" or "sh")
		{
			if (value.Length < 2) return NoParameterError;
			return new(OrderType.Show, value[1].ToLower());
		}
		if (key is "reset" or "rs") return new(OrderType.Reset);
		if (key is "exit" or "ex") return new(OrderType.Exit);
		if (key is "help") return new(OrderType.Help);
		if (key is "__add")
		{
			if (value.Length < 2) return NoParameterError;
			return new(OrderType.Add, value[1]);
		}
		if (key is "__remove")
		{
			if (value.Length < 2) return NoParameterError;
			return new(OrderType.Remove, value[1]);
		}
		if (key is "__search") return new(OrderType.Search);
		if (key is "action" or "ac")
		{
			if (value.Length < 2) return NoParameterError;
			return ParseActionOrder(value[1..]);
		}
		if (!string.IsNullOrWhiteSpace(key)) return ParseActionOrder(value);
		return new();
	}
	private static Order ParseActionOrder(string[] value)
	{
		if (value.Length > 1) return new(value[0], value[1]);
		return new(value[0]);
	}
	private static Order ParseChangeOrder(string[] value, Battle parent)
	{
		if (value.Length is not (2 or 3)) return NoParameterError;
		if (value.Length is 2)
		{
			var body = value[1];
			var selector = GetSelector(parent);
			return new(OrderType.Change, body, selector);
		}
		if (value.Length is 3)
		{
			var body = value[2];
			var selector = GetSelector(parent, value[1]);
			return new(OrderType.Change, body, selector);
		}
		return DefaultError;
	}
	private static PlayerSelector GetSelector(Battle parent, string name = "")
	{
		var nameLow = name.ToLower();
		var player1Name = parent.Player1.Name;
		var player2Name = parent.Player2.Name;
		return nameLow is "p1" or "player1" || nameLow == player1Name ? PlayerSelector.Player1
			: nameLow is "p2" or "player2" || nameLow == player2Name ? PlayerSelector.Player2
			: nameLow is "" ? (parent.CurrentPlayer == parent.Player1 ? PlayerSelector.Player1
			: PlayerSelector.Player2)
			: PlayerSelector.None;
	}
}
