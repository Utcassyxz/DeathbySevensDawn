# 新闻插图

四张原创插图由内置 imagegen 工具生成，作为连续新闻页的配图。没有使用参考游戏截图、人物或美术文件。图内不生成文字；标题、新闻和旁白由 StoryText.json / StoryTextEnglish.json 显示。实际文件为本目录 Circus.png、Sushi.png、Warning.png、Supernova.png，运行时路径 News/同名文件（无扩展名）。

## 采用的提示描述

共同要求：original hand-drawn ink illustration for a grim domestic horror game's television news; muted olive gray, nicotine cream and rust palette; rough newsprint texture, restrained shadows, dry mundane unease; landscape 16:9 composition; no text, captions, logos, UI, existing game characters or copied assets.

- Circus.png：a shabby traveling circus arriving on a city street; striped circus tent, service truck and locked animal crates; the slightly neglected everyday news photograph becomes an illustrated still; no visible monster.
- Sushi.png：an elderly sushi chef using ceremonial scissors to cut the opening ribbon outside a modest restaurant; boxed lunches and a small storefront; an awkward local-news ceremony, mundane and faintly bleak.
- Warning.png：an apartment noticeboard and boarded door in a worn corridor; a small triangle folded from kitchen foil pinned beside the notice; a public-safety news still, low-key unease rather than a supernatural reveal.
- Supernova.png：an observatory telescope above ordinary apartment rooftops, aimed at a bright supernova in a dark sky; a detached astronomical news illustration; no creature or god silhouette.

## 资源规则

字幕按语言目录读取，不烘焙进图片。替换图片时保留文件名和 .meta。每页独立加载 Texture2D；已读后的无聊节目界面不显示新闻插图。
