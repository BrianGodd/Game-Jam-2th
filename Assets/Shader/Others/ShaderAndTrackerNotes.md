# Gaussian Shader And Tracker Interactive Display

## 1. RenderGaussianSplatsOldComputer.shader 
(原HW1的Kyaru效果, at SparkJS)

### 製作概念
這個版本的目標是把 Gaussian Splatting 模型做成「老電腦 / 舊顯示器」質感，而不是單純改顏色。
效果主要拆成幾個部分：

- 以 `scanline` 製造橫向掃描線的明暗起伏
- 以 `phosphor tint` 與 `crtShadowTint` 讓整體偏向舊螢光顯示器色調
- 以 `static noise` 增加畫面的不穩定感
- 以 `row jitter` 讓部分水平列產生輕微抖動
- 以整體透明度與亮度控制，方便依場景做融合

這支 shader 是建立在 Gaussian splat 原本的渲染流程上，先保留 splat 的形狀與 alpha，再疊加舊螢幕的色彩與節奏感。

### 預期呈現效果
- 模型表面會有掃描線感
- 顏色會略帶綠色或螢光顯示器色偏
- 畫面有些微雜訊與抖動
- 整體視覺會比較像舊式 CRT 或老電腦顯示器


## 2. RenderGaussianSplatsCRT.shader
(原HW2的櫻花樹效果，at TD)

### 製作概念
這個版本的方向比 Old Computer 更接近「CRT 顯示器本體」的視覺語言，除了顏色之外，也模擬螢幕幾何與顯示結構。
效果主體包含：

- `crtWarp`：模擬螢幕弧面與邊緣變形
- `row jitter` 與 `frame jitter`：模擬顯示訊號不穩定
- `scanline`：橫向掃描線
- `grille`：垂直柵格感
- `vignette`：邊緣暗角
- `flicker`：整體輕微閃爍
- `phosphor tint`：螢光顯示器偏色
- `monitor mask`：讓模型像是被顯示在一個螢幕區域內

這支 shader 的重點不是把模型做舊，而是把模型包裝成「被一台 CRT 顯示器播放」的感覺。

### 預期呈現效果
- 模型畫面會有更明顯的顯示器感
- 邊緣會帶有弧面與暗角
- 掃描線與柵格感比 Old Computer 更明顯
- 整體更接近監視器、終端機或復古影像輸出效果


## 3. TrackerManager 與裸眼 3D 技術

### 製作概念
`TrackerManager` 的核心目標，是讓 Unity 相機依照觀察者頭部位置即時改變視角，讓螢幕像是一個「真正存在於空間中的窗口」。
這是裸眼 3D / 視差視窗（window into the scene）常見的做法。

整體流程如下：

1. Python 或外部追蹤系統透過 UDP 傳送觀察者頭部座標。
2. `TrackerManager` 接收座標後，將追蹤空間轉成螢幕空間或世界空間。
3. 為了減少抖動，系統提供 `EMA` 與 `SmoothDamp` 平滑處理。
4. 再加上一點速度預測，降低延遲感。
5. 最後根據螢幕實際尺寸、螢幕中心、相機眼點位置，重建一個 `off-axis projection matrix`。

### 技術重點
- 使用 UDP 接收即時 head tracking 資料
- 使用 `trackerToScreen` 做追蹤空間到螢幕空間的座標轉換
- 使用 `screenCenter`、`screenWidth`、`screenHeight` 定義真實螢幕平面
- 使用 `BuildOffAxisAligned(...)` 建立偏軸投影矩陣
- 相機不是只移動位置，而是重新計算投影，讓畫面透視與觀察者位置對應

### 預期呈現效果
- 使用者左右移動頭部時，畫面透視會跟著改變
- 螢幕會像一個可向內看進去的立體窗口
- 不需要佩戴頭盔，也能產生明顯的空間深度感
- 如果追蹤與螢幕校正準確，模型會有接近裸眼 3D 的視差效果


## 4. 總結

這個專案目前有兩條主要視覺方向(調整參數後)：

- `RenderGaussianSplatsOldComputer.shader`
  用於Kyaru角色模型
  帶出**全息投影**，螢光螢幕、訊號雜訊感

- `RenderGaussianSplatsCRT.shader`
  用於背景櫻花樹場景
  完整 **CRT 顯示器視覺**，包括弧面、柵格、閃爍與暗角，增加玩家往螢幕內部窺視的氛圍感

而 `TrackerManager` 則負責讓整個場景的觀看方式**更接近真實空間中的視窗**，透過 head tracking 與 **off-axis projection** 建立 **裸眼 3D** 的觀看體驗。
