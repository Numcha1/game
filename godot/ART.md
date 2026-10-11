# ภาพต้นฉบับและสถานะ art pipeline

ใช้ built-in image_gen ตาม [imagegen skill](C:/Users/NUM/.codex/skills/.system/imagegen/SKILL.md) สร้างภาพเฉพาะโครงการนี้ ไม่ใช้พิกเซล/ตัวละคร/แผนที่จากภาพอ้างอิง และไม่ใช้ CLI/API key ต้นฉบับ environment และ character PNG เก็บใน `ArtSource/` รวมไว้เพื่อทำ atlas ใหม่ซ้ำได้ ไม่ถูก import/export เป็นเกมresource

PNG runtime: `Assets/props.png`, `characters.png`, `terrain.png`, `pond.png`, `shadow.png`, `glow.png`, `stake.png`; regions ใน JSON และ `FarmTiles.tres`. PNG import ใช้lossless/no mipmaps และ nearest canvasfilter. Build-time tooling `Tools/ArtBake` ใช้ System.Drawing เฉพาะ crop/pack/nearest resizingและสร้าง terrain32px ไม่อยู่ใน game loop

Environment sheet ต้องปรับ extraction boundaries หลังตรวจจริง เพราะภาพที่สร้างไม่ได้วางequal256cellsตรงตามprompt การตัดเป็นgridอัตโนมัติเดิมมีเพื่อนบ้านติดมา ได้แก้ด้วย curated boundaries. ตรวจatlasและภาพจากgame viewportอีกครั้งหลังแก้

ภาพใหม่ที่ใช้งาน: บ้าน/ร้านที่มีroof shingles, ผนังไม้/หน้าต่าง/ดอกไม้, ต้นไม้สี่ฤดูใบไม่สมมาตร, well/barrel/crate/sign/flowers/rocks, พืช8ชนิด และตัวละครเดิน4ทิศ×4frames สีsage/honey/cream/copper, แสงจากซ้ายบน กำหนดspritefoot anchorเดียวกันและdepthตามY

**ส่วนที่ยังเป็นงานชั่วคราวและต้องเปลี่ยนก่อนประกาศ art เสร็จ:** ต้นอ่อน generic spriteและyoung cropsที่ตัดจากupper foliageของภาพโตเต็มวัย (ยังไม่ใช่growth sheetเฉพาะ4stages), tool iconsบางช่องใช้ภาพเดิมซ้ำ, idle/tool gestureยังไม่ใช่animationที่วาดทุกframe, terrain/path/fence/pondเป็นภาพpixelที่สร้างด้วยtoolingและยังต้องartist polish โดยเฉพาะpond shorelineและrain ripple, HUDเป็นnativeControlธีมcream woodยังไม่มีornateUI atlasเต็มชุด. Character sheetจากimage generationมีรายละเอียดการก้าวบางframeซ้ำ ต้องreviewและเติมin-betweenโดยartist ไม่เรียกทั้งชุดว่าfinalpremium artwork

Windows ใช้ SystemFont/Tahoma จากเครื่อง ไม่แจกไฟล์fontที่มีลิขสิทธิ์ fallbackNoto Sans Thai/sans-serif เป็นfontที่ติดตั้งเอง ถ้ารันแพลตฟอร์มอื่นต้องมีfontไทย

## Prompt set ที่ใช้กับ built-in tool

Environment: original production PNG sprite atlas for top-down 2D cozy farming RPG; 1536×1024, six columns/four rows, transparent, detailed crisp pixel clusters, 32px tile direction, upper-left light, restrained umber outlines, sage/honey/cream/copper palette. Row1 farmhouse sage roof, seed shop terracotta roof, spring/summer/autumn/winter oak; row2 crate/well/barrel/sign/flowers/rocks; row3 carrot/turnip/potato/tomato/corn/strawberry; row4 pumpkin/cabbage/watering can/hoe/seed bag/farmer. Isolated cells withpadding, no text/grids/overlap, no copyrighted designs.

Character: original precise pixelart sprite sheet, four columns/four rows transparent; same brown-haired straw-hat farmer cream shirt green overalls russet boots; rows south/west/east/north; columns leftfoot/neutral/rightfoot/neutral; same scale/feetbaseline, approximately32×48native pixels enlarged, upper-left light, no text/grids/floor/background.

Background extraction edit: remove colored backdrop from environment sheet only; preserve1536×1024 canvas and24objects' existinglocations/designs; keepobjects, leaves/trunks/crop root soil, remove smoothbetween-objectbackdrop, realtransparentPNG. Actual output dimensions/alpha inspected andimage reprocessed intoatlas throughcuratedcrop tool.

These prompts are normalized production specifications; exact original prompt text is preserved in `ArtSource/prompts.json`.
