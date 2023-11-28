namespace SBFirstLast4.Simulator;

/// <summary>
/// アノテーションの種類を表します。
/// </summary>
public enum Notice
{
	/// <summary>
	/// 情報を持たないアノテーションです。
	/// </summary>
	None,
	/// <summary>
	/// コマンド入力の不正を表します。
	/// </summary>
	Warn,
	/// <summary>
	/// アクションの失敗を表します。
	/// </summary>
	Caution,
	/// <summary>
	/// 汎用的なアノテーションです。
	/// </summary>
	General,
	/// <summary>
	/// HP値の更新を表すアノテーションです。
	/// </summary>
	HPUpdated,
	/// <summary>
	/// タイプ相性に関する情報を示します。
	/// </summary>
	PropInfo,
	/// <summary>
	/// 効果抜群のタイプ相性を表します。
	/// </summary>
	EffectiveProp,
	/// <summary>
	/// ふつうのタイプ相性を表します。
	/// </summary>
	MidDmgProp,
	/// <summary>
	/// いまひとつのタイプ相性を表します。
	/// </summary>
	NonEffectiveProp,
	/// <summary>
	/// ダメージ無しのタイプ相性を表します。
	/// </summary>
	NoDmgProp,
	/// <summary>
	/// 急所に関する情報を示します。
	/// </summary>
	CritInfo,
	/// <summary>
	/// 攻撃力や防御力のバフに関する情報を示します。
	/// </summary>
	Buf,
	/// <summary>
	/// 攻撃力や防御力のデバフに関する情報を示します。
	/// </summary>
	Debuf,
	/// <summary>
	/// 回復に関する情報を示します。
	/// </summary>
	Heal,
	/// <summary>
	/// 毒状態の治療に関する情報を示します。
	/// </summary>
	PoisonHeal,
	/// <summary>
	/// 毒状態の付与に関する情報を示します。
	/// </summary>
	Poison,
	/// <summary>
	/// 毒ダメージを受ける処理に関する情報を示します。
	/// </summary>
	PoisonDmg,
	/// <summary>
	/// やどりぎ状態の付与に関する情報を示します。
	/// </summary>
	Seed,
	/// <summary>
	/// やどりぎダメージを受ける処理に関する情報を示します。
	/// </summary>
	SeedDmg,
	/// <summary>
	/// かくめい・たいふういっかの処理に関する情報を示します。
	/// </summary>
	RevInfo,
	/// <summary>
	/// 特殊なとくせいの発動に関する情報を示します。
	/// </summary>
	InvokeBufInfo,
	/// <summary>
	/// 特殊な防御とくせいの発動に関する情報を示します。
	/// </summary>
	Barrier,
	/// <summary>
	/// ログに表示するプレイヤーの情報を示します。
	/// </summary>
	LogInfo,
	/// <summary>
	/// ログに表示するアクションの情報を示します。
	/// </summary>
	LogActionInfo,
	/// <summary>
	/// ゲーム システムに関する情報を示します。
	/// </summary>
	SystemInfo,
	/// <summary>
	/// ゲーム オプションに関する情報を示します。
	/// </summary>
	SettingInfo,
	/// <summary>
	/// 状態異常に関する情報など、補助的な情報を示します。
	/// </summary>
	AuxInfo,
	/// <summary>
	/// プレイヤーの死亡に関する情報を示します。
	/// </summary>
	DeathInfo
}
/// <summary>
/// アノテーション付き文字列を表すクラスです。
/// </summary>
public class AnnotatedString
{
	/// <summary>
	/// アノテーションを受ける文字列
	/// </summary>
	public string Text { get; set; } = string.Empty;
	/// <summary>
	/// アノテーションの種類
	/// </summary>
	public Notice Notice { get; set; } = Notice.None;
	public int[] Params { get; set; } = Array.Empty<int>();
	/// <summary>
	/// アノテーションの種類がログにのみ作用するかどうかの判定
	/// </summary>
	public bool IsLog => Notice is Notice.LogInfo or Notice.LogActionInfo;
	/// <summary>
	/// 文字列が本体をもつかどうかの判定
	/// </summary>
	public bool IsInvisible => Notice is Notice.HPUpdated;
	public AnnotatedString(string text, Notice notice) => (Text, Notice) = (text, notice);
	public override string ToString()
	{
		return Text + " " + Notice;
	}
	public static implicit operator AnnotatedString((string text, Notice notice) t) => new(t.text, t.notice);
}
