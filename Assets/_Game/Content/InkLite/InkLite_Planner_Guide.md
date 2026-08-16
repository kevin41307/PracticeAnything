# InkLite 企劃使用說明

這份文件給企劃撰寫聊天劇本使用。InkLite 是專案內的輕量版聊天腳本格式，用純文字描述「NPC 傳訊息、玩家選項、分支、結束」。

## 劇本檔案位置

目前範例劇本在：

```txt
Assets/_Game/Content/InkLite/TestInkLite.inklite.txt
```

你可以直接編輯這個檔案測試。儲存後回到 Unity，打開：

```txt
Assets/_Game/Content/Scenes/TestInkLite.unity
```

按 Play 即可驗收聊天流程。

## 基本概念

劇本由多個「段落」組成。每個段落用 `@段落名稱` 開頭。

玩家選擇某個選項後，劇情會跳到指定段落繼續播放。

最重要的入口段落一定要叫：

```txt
@start
```

## 最小範例

```txt
@start
npc: text "你到了嗎？"
choice:
  - "到了" => "我到了" -> arrived
  - "還沒" => "我還沒到" -> not_arrived

@arrived
npc: text "好，我現在過去找你。"
end

@not_arrived
npc: text "那你快一點，我在門口等。"
end
```

這段劇本的流程是：

```txt
NPC：你到了嗎？
玩家選擇：到了 / 還沒
如果選「到了」：跳到 @arrived
如果選「還沒」：跳到 @not_arrived
播放完後結束
```

## 支援語法

### 段落

```txt
@start
@arrived
@bad_end
```

段落名稱建議只用英文、數字、底線 `_` 或減號 `-`。

好的命名：

```txt
@start
@trust_route
@ending_good
@ending_bad
```

不建議：

```txt
@好結局
@第一段 對話
```

### NPC 文字訊息

```txt
npc: text "你好，我是 NPC。"
```

NPC 訊息會自動先顯示 `...`，等待 1 秒後才顯示正式訊息。

### NPC 圖片訊息

```txt
npc: image "photo_01"
```

目前圖片會先顯示成佔位框：

```txt
[圖片]
photo_01
```

`photo_01` 是圖片 ID。之後如果接正式圖片資源，企劃仍然只需要填圖片 ID。

### 玩家訊息

通常不需要手寫玩家訊息，因為玩家選項會自動送出回覆。

如果真的需要劇本主動插入一則玩家訊息，可以寫：

```txt
player: text "我知道了。"
```

### 玩家選項

```txt
choice:
  - "按鈕文字" => "玩家送出的訊息" -> 下一段落
```

範例：

```txt
choice:
  - "答應幫忙" => "好，我幫你。" -> help
  - "拒絕" => "抱歉，我現在沒空。" -> reject
```

畫面上玩家會看到：

```txt
答應幫忙
拒絕
```

如果玩家點「答應幫忙」，聊天室會送出：

```txt
好，我幫你。
```

然後劇情跳到：

```txt
@help
```

### 選項數量限制

每個 `choice:` 只能有 1 到 3 個選項。

正確：

```txt
choice:
  - "好" -> yes

choice:
  - "好" -> yes
  - "不好" -> no

choice:
  - "好" -> yes
  - "不好" -> no
  - "再想想" -> think
```

錯誤：

```txt
choice:
  - "A" -> a
  - "B" -> b
  - "C" -> c
  - "D" -> d
```

### 省略玩家送出訊息

如果按鈕文字和玩家送出的訊息一樣，可以省略 `=>`。

```txt
choice:
  - "了解" -> ending
```

等同於：

```txt
choice:
  - "了解" => "了解" -> ending
```

### 直接跳轉

```txt
goto ending
```

用途是讓不同分支回到同一段共用劇情。

範例：

```txt
@route_a
npc: text "你選了 A。"
goto common_end

@route_b
npc: text "你選了 B。"
goto common_end

@common_end
npc: text "不管你選哪個，最後都會看到這句。"
end
```

### 結束

```txt
end
```

走到 `end` 後，聊天會停止，底部會顯示「模擬結束」。

## 完整範例

```txt
@start
npc: text "你今天有空嗎？"
npc: image "calendar_screenshot"
choice:
  - "有空" => "有，我今天有空。" -> free
  - "沒空" => "我今天沒空。" -> busy
  - "先說什麼事" => "你先說是什麼事。" -> ask

@free
npc: text "太好了，我想請你幫我看一份資料。"
goto ending

@busy
npc: text "沒關係，那我晚點再問你。"
goto ending

@ask
npc: text "是關於明天活動的流程。"
choice:
  - "可以" => "可以，我幫你看。" -> free
  - "不行" => "抱歉，這我沒辦法。" -> busy

@ending
npc: text "謝謝你回覆。"
end
```

## 建議撰寫流程

1. 先寫 `@start`
2. 先把主線 NPC 訊息寫完
3. 遇到玩家需要選擇時加入 `choice:`
4. 為每個選項建立對應段落
5. 如果多條分支會回到同一段，用 `goto`
6. 每條最終路線都要走到 `end`
7. 回 Unity 按 Play 測試

## 常見錯誤

### 忘記 `@start`

錯誤：

```txt
@intro
npc: text "你好"
```

正確：

```txt
@start
npc: text "你好"
```

### 選項跳到不存在的段落

錯誤：

```txt
@start
choice:
  - "好" -> yes
```

但下面沒有：

```txt
@yes
```

正確：

```txt
@start
choice:
  - "好" -> yes

@yes
npc: text "你選了好。"
end
```

### 訊息忘記加雙引號

錯誤：

```txt
npc: text 你好
```

正確：

```txt
npc: text "你好"
```

### choice 下面沒有選項

錯誤：

```txt
choice:

@next
npc: text "下一段"
```

正確：

```txt
choice:
  - "繼續" -> next
```

### 選項超過 3 個

目前不支援 4 個以上選項。請拆成兩層選項，或改成 3 個以內。

## 注意事項

- 所有文字內容都要放在英文雙引號 `""` 裡。
- 段落名稱要保持一致，大小寫雖然目前不敏感，但建議統一小寫。
- `choice:` 後面的選項要用 `-` 開頭。
- 每條路線最好都能走到 `end`。
- 圖片目前用 ID 佔位，不會真的顯示圖片檔。
- 如果 Unity Console 顯示錯誤，通常是劇本格式或跳轉段落名稱寫錯。

## 驗收檢查表

- 進入聊天室後，NPC 是否從第一句開始播放
- 每則 NPC 訊息前是否有 `...`
- `...` 是否約 1 秒後消失並顯示訊息
- 玩家選項是否正確顯示 1-3 個
- 點選選項後，玩家回覆文字是否正確
- 選項是否跳到正確分支
- 圖片訊息是否顯示圖片佔位框與圖片 ID
- 劇本結束時是否顯示「模擬結束」
