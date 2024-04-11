﻿namespace SBFirstLast4.Versioning;

internal static class VersionHistory
{
	internal static Version Latest => Versions[0].Version;

	internal static readonly (Version Version, string[] Changes)[] Versions =
	[
		(new(13, 2, 0), ["新機能「ワイルドカード」をページ「しりとり単語検索」「しりバト単語検索」「グループ検索」に追加", "手動クエリにマクロ「WILDCARD」を追加"]),
		(new(13, 1, 3), ["辞書のキャッシュが正常にクリアされない不具合の修正"]),
		(new(13, 1, 2), ["ポストコールの改善"]),
		(new(13, 1, 1), ["「当サイトについて」ページにおいて、一部フォントが正常に表示されない不具合の修正"]),
		(new(13, 1, 0), ["「当サイトについて」ページの追加", "ページ「グループ検索」「革命シミュレーター」のベータ版指定を解除"]),
		(new(13, 0, 2), ["ポストコールにおいて、一部の型のコンストラクターを正常に呼び出せない不具合の修正"]),
		(new(13, 0, 1), ["operatorポストコール オブジェクトに演算子を追加"]),
		(new(13, 0, 0), ["新機能「革命シミュレーター」を追加"]),
		(new(12, 1, 1), ["パフォーマンスの改善"]),
		(new(12, 1, 0), ["プロシージャに文法「once-next文」「repeat文」「with文」を追加", "パフォーマンスの改善"]),
		(new(12, 0, 14), ["パフォーマンスの改善"]),
		(new(12, 0, 13), ["パフォーマンスの改善"]),
		(new(12, 0, 11), ["パフォーマンスの改善"]),
		(new(12, 0, 10), ["「簡易シミュレーター」ページにおいて、ダメージ計算の方式を変更", "キャッシュの改善", "パフォーマンスの改善"]),
		(new(12, 0, 9), ["「最高打点検索」ページにおいて、ダメージ計算の方式を変更"]),
		(new(12, 0, 8), ["パフォーマンスの改善"]),
		(new(12, 0, 5), ["UIの改善"]),
		(new(12, 0, 2), ["「安全単語検索」の検索結果が最新の単語を反映していない不具合の修正"]),
		(new(12, 0, 1), ["「ベータ版の機能」オプションにロギングを反映"]),
		(new(12, 0, 0), ["新機能「グループ検索」を追加", "「ベータ版の機能」オプションを追加"]),
		(new(11, 5, 2), ["operatorポストコール オブジェクトに演算子を追加", "パフォーマンスの改善"]),
		(new(11, 5, 1), ["パフォーマンスの改善"]),
		(new(11, 5, 0), ["ポストコールに関数「assert(bool)」「halt()」「halt(string)」を追加", "operatorポストコール オブジェクトに演算子を追加"]),
		(new(11, 4, 1), ["castポストコール オブジェクトにオペランドを追加", "operatorポストコール オブジェクトに演算子を追加", "ポストコールの改善", "パフォーマンスの改善"]),
		(new(11, 4, 0), ["手動クエリに新機能「await演算子」を追加"]),
		(new(11, 3, 0), ["手動クエリに新機能「enum文」を追加", "procof演算子の改善"]),
		(new(11, 2, 1), ["一部の拡張メソッドをポストコールで呼び出せない不具合の修正"]),
		(new(11, 2, 0), ["手動クエリに新機能「procof演算子」を追加", "プロシージャに文法「flush文」を追加", "ポストコールの型解決が正常に機能しない不具合の修正"]),
		(new(11, 1, 0), ["手動クエリに新機能「operatorポストコール オブジェクト」を追加"]),
		(new(11, 0, 1), ["ポストコールにおいて、一部の環境から想定されるメソッドを呼び出せない不具合の修正"]),
		(new(11, 0, 0), ["手動クエリに新機能「ポストコール」を追加", "辞書読み込みの高速化", "パフォーマンス改善"]),
		(new(10, 5, 2), ["MultiWord構造体について、ダメージ計算用のメソッドを正常に呼び出せない不具合の修正"]),
		(new(10, 5, 1), ["Word構造体およびMultiWord構造体について、相互にキャスト用コンストラクターを追加"]),
		(new(10, 5, 0), ["手動クエリに新機能「MultiWordリテラル」を追加"]),
		(new(10, 4, 0), ["手動クエリに新機能「生Wordリテラル」を追加"]),
		(new(10, 3, 1), ["[quiet]属性が正常に適用されない不具合の修正"]),
		(new(10, 3, 0), ["手動クエリに新機能「[quiet]属性」を追加", "プロシージャに文法「raise文」「whenany文」「whenever文」を追加", "モジュール初期化子中において、文脈の復帰が正常に行われない不具合の修正"]),
		(new(10, 2, 0), ["手動クエリに新機能「モジュール初期化子」「静的モジュール初期化子」を追加", "プロシージャに文法「swotch文」「throw文」「try-catch-finally文」「retry文」の追加", "手動クエリの改善", "パフォーマンスの改善"]),
		(new(10, 1, 1), ["「手動クエリ」ページから遷移する際に予期しないポップアップが表示される不具合の修正"]),
		(new(10, 1, 0), ["手動クエリに新機能「名前付きプロシージャ」「プロシージャ呼び出し」を追加", "プロシージャに文法「メンバー代入」「do-while文」「do-until文」「until文」「foreach文」「空ステートメント」を追加", "プロシージャに関数「clear()」「delay(int)」を追加", "プロシージャの改善", "手動クエリの改善"]),
		(new(10, 0, 0), ["手動クエリに新機能「プロシージャ」を追加", "パフォーマンスの改善"]),
		(new(9, 11, 1), ["パフォーマンスの改善"]),
		(new(9, 11, 0), ["手動クエリに新機能「ハッシュ リテラル」を追加"]),
		(new(9, 10, 1), ["レコード型の改善", "辞書読み込みのパフォーマンス改善"]),
		(new(9, 10, 0), ["手動クエリに新機能「レコード型」を追加"]),
		(new(9, 9, 0), ["手動クエリに新機能「インクリメント」「デクリメント」を追加", "手動クエリに新機能「複合代入」を追加", "「簡易シミュレーター」ページにおいて、エフェクトが正常に表示されない不具合の修正"]),
		(new(9, 8, 2), ["マクロ同士の名前衝突が発生した際に、正常にマクロが展開されない不具合の修正"]),
		(new(9, 8, 1), ["Ephemeralマクロを正常に定義できない不具合の修正", "Ephemeralマクロが正常に展開されない不具合の修正"]),
		(new(9, 8, 0), ["手動クエリに新機能「Ephemeralマクロ」を追加"]),
		(new(9, 7, 0), ["手動クエリに新機能「複文実行」を追加", "手動クエリの改善"]),
		(new(9, 6, 0), ["手動クエリに新機能「広域変数」を追加", "手動クエリに新機能「delete文」を追加"]),
		(new(9, 5, 1), ["ドキュメントへのリンクをロギングに対応"]),
		(new(9, 5, 0), ["「手動クエリ」ページにドキュメントへのリンクを追加"]),
		(new(9, 4, 2), ["手動クエリからAttackInfo構造体を使用可能に"]),
		(new(9, 4, 1), ["組み込みモジュールをリポジトリから分離"]),
		(new(9, 4, 0), ["「最高打点検索」ページに「攻撃サイド/防御サイド」スイッチを追加", "パフォーマンスの改善"]),
		(new(9, 3, 0), ["「最高打点検索」ページに「除外タイプ」フォームを追加", "「最高打点検索」ページに「さらに表示」「表示数を減らす」ボタンを追加", "UIの改善"]),
		(new(9, 2, 0), ["「簡易シミュレーター」ページに「戻る」「進む」「最新」ボタンを追加", "とくせい「神」のテキストの修正", "パフォーマンスの改善"]),
		(new(9, 1, 2), ["「安全単語検索」の検索結果が正確でない不具合の修正", "パフォーマンスの改善"]),
		(new(9, 1, 1), ["とくせい「リワインド」使用時に正しくターンが巻き戻らない不具合の修正", "とくせい「リワインド」使用時の効果音のボリュームが大きすぎる問題の修正"]),
		(new(9, 1, 0), ["とくせい「リワインド」の追加", "UIの改善", "パフォーマンスの改善"]),
		(new(9, 0, 3), ["UIの修正"]),
		(new(9, 0, 2), ["タイプ付き単語のタイプが誤って表示される不具合の修正"]),
		(new(9, 0, 1), ["「簡易シミュレーター」中のメッセージ表示の改善"]),
		(new(9, 0, 0), ["機能「簡易シミュレーター」のオーバーホール", "オプション「ライトなタイプ判定」の追加"]),
		(new(8, 6, 3), ["デプロイの修正"]),
		(new(8, 6, 2), ["パフォーマンスの改善"]),
		(new(8, 6, 1), ["「サウンドルーム」にロギングを反映"]),
		(new(8, 6, 0), ["新機能「サウンドルーム」追加"]),
		(new(8, 4, 7), ["トップページにおいて、ハッシュを正常に読み込めない不具合の修正"]),
		(new(8, 4, 6), ["トップページにおいて、エラーが発生した場合の通知機構を追加"]),
		(new(8, 4, 5), ["「手動クエリ」機能において、モジュールを正しく削除できない不具合の修正"]),
		(new(8, 4, 4), ["パフォーマンスの改善"]),
		(new(8, 4, 2), ["「手動クエリ」機能において、モジュールを正しく読み込めない不具合の修正", "「手動クエリ」機能において、モジュールを正しく削除できない不具合の修正"]),
		(new(8, 4, 1), ["UIの修正"]),
		(new(8, 4, 0), ["手動クエリに新機能「実行時モジュール」を追加", "「モジュールデータの消去」オプションの追加"]),
		(new(8, 3, 0), ["「検索結果のソート」オプションの追加", "ロギングの拡張"]),
		(new(8, 2, 3), ["辞書のキャッシュが正常に削除されない不具合の修正"]),
		(new(8, 2, 2), ["パフォーマンスの改善"]),
		(new(8, 2, 1), ["不正なログインを防止する機能の追加"]),
		(new(8, 2, 0), ["ユーザー名による認証の追加", "「設定」ページの追加", "ロギングの追加", "手動クエリに新機能「組み込みモジュール」を追加", "UIの改善"]),
		(new(8, 1, 0), ["パスワード認証の削除", "手動クエリに新機能「Deduce リテラル」を追加", "手動クエリに新機能「マクロ」を追加"]),
		(new(8, 0, 0), ["新機能「手動クエリ」を追加"]),
		(new(7, 0, 11), ["パフォーマンスの改善"]),
		(new(7, 0, 10), ["「安全単語検索」機能において、フィルタを設定せずに単語を検索できない不具合の修正"]),
		(new(7, 0, 0), ["新機能「安全単語検索」を追加", "セキュリティの改善"]),
		(new(6, 0, 2), ["セキュリティの改善"]),
		(new(6, 0, 1), ["UIの修正"]),
		(new(6, 0, 0), ["新機能「簡易シミュレーター」を追加", "シミュレーター関連の機能の移植", "セキュリティの改善"]),
		(new(5, 1, 0), ["パスワード認証の追加"]),
		(new(5, 0, 0), ["新機能「ダメージ計算」を追加", "UIの改善"]),
		(new(4, 0, 1), ["UIの改善"]),
		(new(4, 0, 0), ["新機能「最高打点検索」を追加", "「タイプ判定」ページの改善", "重複した単語がタイプ付き辞書に含まれる不具合の修正"]),
		(new(3, 0, 8), ["ワイルドカード機能の再追加"]),
		(new(3, 0, 7), ["「しりとり単語検索」からワイルドカード機能を削除", "「しりバト単語検索」からワイルドカード機能を削除"]),
		(new(3, 0, 6), ["「しりバト単語検索」において、後方一致検索ができない不具合の修正"]),
		(new(3, 0, 5), ["「タイプ判定」機能のパフォーマンス改善"]),
		(new(3, 0, 4), ["UIの修正"]),
		(new(3, 0, 3), ["UIの修正"]),
		(new(3, 0, 2), ["UIの修正"]),
		(new(3, 0, 1), ["UIの修正"]),
		(new(3, 0, 0), ["新機能「しりバト単語検索」を追加", "新機能「タイプ判定」追加", "タイプ付きリスト作成機能の追加", "ライト版辞書の追加"]),
		(new(2, 0, 5), ["フッターの改善"]),
		(new(2, 0, 4), ["リスト保存ページに説明文を追加"]),
		(new(2, 0, 3), ["UIの修正"]),
		(new(2, 0, 2), ["UIの修正"]),
		(new(2, 0, 1), ["UIの修正"]),
		(new(2, 0, 0), ["新機能「リスト保存」を追加"]),
		(new(1, 1, 1), ["「単語の長さ」オプションが正しく反映されない不具合の修正"]),
		(new(1, 1, 0), ["「単語の長さ」検索オプションの追加"]),
		(new(1, 0, 4), ["デプロイの修正"]),
		(new(1, 0, 3), ["デプロイのルート修正"]),
		(new(1, 0, 2), ["パフォーマンスの改善", "ページが正しくリロードされない不具合の修正"]),
		(new(1, 0, 1), ["ボタンが正しく表示されない不具合の修正"]),
		(new(1, 0, 0), ["サイトの公開", "新機能「しりとり単語検索」を追加"]),
		(new(0, 1, 6), ["単語辞書の追加"]),
		(new(0, 1, 5), ["トップページの編集"]),
		(new(0, 1, 4), ["ロールのマージ"]),
		(new(0, 1, 3), ["ロールのテスト"]),
		(new(0, 1, 2), ["ロールの追加"]),
		(new(0, 1, 1), ["プロジェクトファイルの追加"]),
		(new(0, 1, 0), ["リポジトリの作成"])
	];
}