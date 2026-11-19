#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Japanese localization batch 10: harem route, d02."""
from __future__ import annotations

import csv
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OVERLAY = ROOT / "tools" / "ja_translations.json"

TRANS = {
    "er_dual_choice_d02_c1_1_label": "二人の間に立つ",
    "er_dual_choice_d02_c1_1_desc": "リディアと男の間に割って入る。",
    "er_dual_choice_d02_c1_2_label": "何を売るのか聞く",
    "er_dual_choice_d02_c1_2_desc": "話を商売に戻す。",
    "er_dual_choice_d02_c1_3_label": "ドアを開けて出て行かせる",
    "er_dual_choice_d02_c1_3_desc": "路地の光を入れる。",
    "er_dual_d02_b001_01": "リディアは両手を空にして入ってきた。鞄の紐は手首に二巻きしてある。",
    "er_dual_d02_b001_02": "見た？ 手ぶらよ。",
    "er_dual_d02_b001_03": "出る前に鞄を逆さにして三回振ったんだから。",
    "er_dual_d02_b001_04": "座れ。",
    "er_dual_d02_b001_05": "灯りのこと、最後まで聞いてきたわ。",
    "er_dual_d02_b001_06": "配給所は住宅管理に回して、住宅管理は街道に回して、街道は修理工の周さんに聞けって。",
    "er_dual_d02_b001_07": "三か所、三つの顔、みんな同じ言葉。管轄外ですって。",
    "er_dual_d02_b001_08": "上では、灯り一つのために走り回ったことなんてなかった。",
    "er_dual_d02_b001_09": "上で灯りが切れたら、翌日には誰かが替えに来た。",
    "er_dual_d02_b001_10": "周さんは電線はあるけど、電球はないって。",
    "er_dual_d02_b001_11": "ここに一箱ある。",
    "er_dual_d02_b001_12": "あんた——",
    "er_dual_d02_b001_13": "先に言ってよ。",
    "er_dual_d02_b001_14": "今聞いた。",
    "er_dual_d02_b001_15": "いくら？",
    "er_dual_d02_b001_16": "一個三クレジット。箱に六個。悪いのは選り分けて出せ。",
    "er_dual_d02_b001_17": "選り分けるの？",
    "er_dual_d02_b001_18": "悪いのを付けたら、周さんがまた登らないといけない。",
    "er_dual_d02_b001_19": "じゃあ、選り分け終わるまで待つわ。",
    "er_dual_d02_b001_20": "彼女は腰かけを半尺ほど手前へ引き、座って、両手を膝に置いた。",
    "er_dual_d02_b001_21": "今日は来て、言いたかったの——",
    "er_dual_d02_b001_22": "ドアが押し開けられ、男が二人、前後して入ってきた。前の男が帽子を取った。",
    "er_dual_d02_b001_23": "店主。",
    "er_dual_d02_b001_24": "買い取りか？",
    "er_dual_d02_b001_25": "買い取る。",
    "er_dual_d02_b001_26": "じゃあ、あの女の物も買い取るのか？",
    "er_dual_d02_b001_27": "彼の目はリディアの手首に落ちたまま、離れなかった。",
    "er_dual_d02_b001_28": "売る物なんてないわ。",
    "er_dual_d02_b001_29": "手首のそれだ。",
    "er_dual_d02_b001_30": "これは売らない。",
    "er_dual_d02_b001_31": "売らなくてもいい。",
    "er_dual_d02_b001_32": "じゃあ、別の勘定をしよう。",
    "er_dual_d02_b001_33": "見慣れない顔がこの通りを歩くなら、月に二十。",
    "er_dual_d02_b001_34": "二十クレジット。",
    "er_dual_d02_b001_35": "二十だ。",
    "er_dual_d02_b001_36": "二十クレジットが上で何を買えるか、分かってる？",
    "er_dual_d02_b001_37": "知らない。",
    "er_dual_d02_b001_38": "なら、説明してみろ。",
    "er_dual_d02_b001_39": "彼は半歩進み、カウンターの縁に手を置いた。ブレスレットまで指一本分。",
    "er_dual_d02_b002_01": "店主がカウンターの後ろから回り込み、二人の顔の間に立った。",
    "er_dual_d02_b002_02": "手を離せ。",
    "er_dual_d02_b002_03": "お前は誰だ。",
    "er_dual_d02_b002_04": "店主だ。",
    "er_dual_d02_b002_05": "ちょうどいい。この店もまとめて勘定に入れてやる。",
    "er_dual_d02_b002_06": "いくらだ。",
    "er_dual_d02_b002_07": "五十。",
    "er_dual_d02_b002_08": "この店は一か月で五十も入らない。",
    "er_dual_d02_b002_09": "なら品物で払え。",
    "er_dual_d02_b002_10": "いい。",
    "er_dual_d02_b002_11": "……いい？",
    "er_dual_d02_b002_12": "カウンターの中に電球六個、歯が三本欠けた櫛が一本、釘が半缶。",
    "er_dual_d02_b002_13": "運べ。",
    "er_dual_d02_b002_14": "俺を物乞い扱いか。",
    "er_dual_d02_b002_15": "話をしてる。",
    "er_dual_d02_b002_16": "金ならここにない。品物なら量って渡す。",
    "er_dual_d02_b002_17": "本当に秤があるのね。",
    "er_dual_d02_b002_18": "黙れ。",
    "er_dual_d02_b003_01": "何を売る？",
    "er_dual_d02_b003_02": "俺は物を売らない。",
    "er_dual_d02_b003_03": "なら立つ場所を間違えてる。",
    "er_dual_d02_b003_04": "この店は商売しかしない。",
    "er_dual_d02_b003_05": "……俺に商売の話をするのか。",
    "er_dual_d02_b003_06": "この人は規則の話をしてるのよ。",
    "er_dual_d02_b003_07": "規則にいくらの値打ちがある。",
    "er_dual_d02_b003_08": "彼は帽子をカウンターに放り、続けて手を伸ばした。",
    "er_dual_d02_b004_01": "店主は回り込んでドアを目いっぱい開け、路地の光を入れた。",
    "er_dual_d02_b004_02": "外で話せ。",
    "er_dual_d02_b004_03": "中に入って座るのもだめか。",
    "er_dual_d02_b004_04": "お前とは商売しない。",
    "er_dual_d02_b004_05": "この通りで商売しないと言う奴は少ない。",
    "er_dual_d02_b004_06": "今、一人増えた。",
    "er_dual_d02_b004_07": "彼はドアを押し戻した。",
    "er_dual_d02_b004_08": "なら、無理にでも買うまでだ。",
    "er_dual_d02_b005_01": "彼の手が下り、ブレスレットを付けた手首を掴んだ。",
    "er_dual_d02_b005_02": "離して。",
    "er_dual_d02_b005_03": "ブレスレットは預かる。金を取りに戻れ。",
    "er_dual_d02_b005_04": "ペンの先が男の手の甲に突き刺さり、彼女はそのまま一歩前へ出た。",
    "er_dual_d02_b005_05": "男は手を引っ込め、ドアのそばまで下がり、帽子を握り潰した。",
    "er_dual_d02_b005_06": "この——",
    "er_dual_d02_b005_07": "三つ数える。",
    "er_dual_d02_b005_08": "一。",
    "er_dual_d02_b005_09": "この通りは短い。",
    "er_dual_d02_b005_10": "端まで行っても、戻ってこなきゃならねえ。",
    "er_dual_d02_b005_11": "ドアが乱暴に閉められ、ガラスが二度揺れて止まった。",
    "er_dual_d02_b006_01": "……ペンが曲がった。",
    "er_dual_d02_b006_02": "けっこう深く刺したわよね。",
    "er_dual_d02_b006_03": "手の甲が真っ黒だった。インクが漏れたのかしら。",
    "er_dual_d02_b006_04": "血だ。",
    "er_dual_d02_b006_05": "……あ。",
    "er_dual_d02_b006_06": "彼女はペンをカウンターに置き、指先をズボンの縫い目で二度拭った。",
    "er_dual_d02_b006_07": "さっきは全然怖くなかった。",
    "er_dual_d02_b006_08": "今になって怖くなってきた。",
    "er_dual_d02_b006_09": "怖いなら、少し座れ。",
    "er_dual_d02_b006_10": "あの人たち、私のブレスレットを知ってた。",
    "er_dual_d02_b006_11": "この前、それを着けてこの通りを端から端まで歩いたの。",
    "er_dual_d02_b006_12": "路地の入口で立ち止まって、しばらく人だかりも見てたのに。",
    "er_dual_d02_b006_13": "この通りに人だかりなんてない。",
    "er_dual_d02_b006_14": "……じゃあ、記憶違いね。",
    "er_dual_d02_b006_15": "あのとき、別のことを考えてたの。",
    "er_dual_d02_b006_16": "二十はお前を狙ったんじゃない。",
    "er_dual_d02_b006_17": "じゃあ誰を？",
    "er_dual_d02_b006_18": "この通りだ。新入りは絞りやすい。",
    "er_dual_d02_b006_19": "じゃあ、手ぶらで来たのも間違いだったのね。",
    "er_dual_d02_b006_20": "来たじゃないか。",
    "er_dual_d02_b006_21": "……それ、褒められてるみたいに聞こえる。",
    "er_dual_d02_b006_22": "事実だ。",
    "er_dual_d02_b006_23": "電球は買うわよ。六個全部。悪いのも全部。",
    "er_dual_d02_b006_24": "悪いのは使えない。",
    "er_dual_d02_b006_25": "周さんに持っていくの。私がどれだけ厄介か、見せてやる。",
    "er_dual_d02_b006_26": "彼女は札を一枚ずつカウンターに数え、数え終わると押し出した。遠くへは押さなかった。",
    "er_dual_d02_b006_27": "外まで送ってくれる？",
    "er_dual_d02_b006_28": "路地の入口まででいい。",
    "er_dual_d02_b006_29": "ああ。",
    "er_dual_d02_b006_30": "彼はカウンターの物を引き出しに収め、錠を二度押した。",
    "er_dual_d02_b006_31": "路地の向こうで誰かが壁にもたれていた。二人が出てくるのを見ると、向きを変えて去った。",
    "er_dual_d02_b006_32": "あの人……",
    "er_dual_d02_b006_33": "見た。",
    "er_dual_d02_b006_34": "ここまででいい。",
    "er_dual_d02_b006_35": "道は分かるから。",
    "er_dual_d02_b006_36": "彼女は十数歩歩いて一度振り返り、それから角を曲がった。",
}


def main() -> int:
    zh = {}
    with open(ROOT / "Localization" / "zh.csv", encoding="utf-8", newline="") as f:
        for row in csv.reader(f):
            if len(row) >= 2:
                zh[row[0]] = row[1]
    expected = {k for k in zh if k.startswith("er_dual_d02_") or k.startswith("er_dual_choice_d02_")}
    unknown = sorted(set(TRANS) - expected)
    missing = sorted(expected - set(TRANS))
    if unknown:
        raise SystemExit("Unknown keys: " + ", ".join(unknown[:10]))
    if missing:
        raise SystemExit(f"Missing {len(missing)} d02 keys: " + ", ".join(missing[:10]))
    overlay = json.loads(OVERLAY.read_text(encoding="utf-8")) if OVERLAY.exists() else {}
    overlay.update(TRANS)
    OVERLAY.write_text(json.dumps(overlay, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Updated {OVERLAY} (+{len(TRANS)} keys, total {len(overlay)})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
