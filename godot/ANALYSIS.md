# ตรวจโปรเจกต์และขอบเขตการย้าย

ตรวจ `native/`, `dist/`, renderer, assets และ game loop ก่อนลงมือ ไม่พบ Godot project เดิม เครื่องนี้มี Godot `4.7.2.stable.mono.official.ed1daf0bf`, .NET SDK 8.0.423 และ 10.0.401 โครงการนี้เลือก SDK 8 และ net8.0 ให้ตรงกับ simulation เดิม ใช้ Godot SDK 4.7.2 จากแพ็กเกจที่มากับ editor

`native/` เป็น WinForms/GDI+ มี FramePulse สำหรับภาพประมาณ 60 Hz และ RoomServer timer สำหรับ simulation แยกกัน `FarmArt.cs` สร้างภาพในหน่วยความจำ `PremiumScene.cs` จัดกล้อง สภาพอากาศ depth และ picking ส่วน `GameForm.cs`, `GameUi.cs`, `HudCache.cs`, `Scene.cs` ผูกกับ System.Drawing/WinForms จึงไม่ย้าย renderer เหล่านี้เข้า Godot

`World.cs` ไม่มี dependency ของ UI ใช้ซ้ำผ่าน linked Compile ใน `Core/SeasonsTogether.Core.csproj` ไม่คัดลอกไปแก้อีกชุด `Network.cs` ใช้ BCL HTTP/TCP และสามารถ compile ใน Core ได้เช่นกัน สองไฟล์เดิมไม่ถูกแก้ `dist/` เป็นเกมเว็บคนละกติกา (เติบโตเป็นวินาที, localStorage, 6–12 แปลง) จึงเก็บไว้แต่ไม่ใช้เป็น simulation หรือแปลงเซฟเว็บเป็น native save โดยไม่มี specification

## กติกาที่ต้องรักษา

- 24 แปลงที่ logical coordinates `(8+i%6,6+i/6)`, เดิน 4 tiles/วินาที, bounds และบริเวณห้ามเดินใช้ `Farm.Walkable` เดิม
- ฤดูละ 14 วัน สี่ฤดู crop 8 ชนิด ต้นทุน ระยะโต ฤดูที่ปลูกได้ และ harvest window มาจาก `Farm.Crops` เดิม
- action range 2.4 tiles, ใช้แรง 5 ต่อการทำงานที่สำเร็จ, น้ำ +45 สูงสุด100, ปุ๋ย +30 ราคา10, ไม้ค้ำราคา8, ยารักษาราคา10
- ปลูกซ้ำชนิดเดิมเสีย fertility15, growth ตามน้ำและfertility, disease/พายุ/คลื่นร้อน/การเปลี่ยนฤดู/late harvest ใช้ `NextDay()` เดิม
- yield ตามคุณภาพ 1–3 ชิ้น, ราคาขายรายวันเดิม, ใกล้ลัง `(20,8)` ระยะ2.5, กระเป๋าเสีย25% ข้ามคืน
- เวลาขยับ1.5นาที/วินาที (06:00–21:00 = 600วินาที), sleep ใกล้ `(4,4)`, ทุกคนที่activeต้องนอน, owner pause/greenhouse, rescue ใช้กติกาเดิม

## Save contract

JSON version1, fields `Day, Minute, Coins, Weather, Greenhouse, Harvests, Owner, Paused, Plots, Players, Journal, Effects, EffectSequence` รวม property computed เดิม การ deserialize ใช้ `Farm.Load` เดิมซึ่งตรวจschemaและรีเซ็ตpresence/sleep/pause ไม่มีการแก้ชื่อ property, crop keys หรือ farmer IDs สร้างด้วย SHA256 ของ UUID session เช่นเดิม

เซฟ Windows เดิม `%LOCALAPPDATA%/SuanPleng/farm.json` และ `player.txt` ไม่ถูกเขียนโดยฉากใหม่นี้ ฉากใช้ `SuanPleng/godot-slice/` และมี confirmation นำเข้าแบบคัดลอก ตรวจเซฟก่อนคัดลอก และสำรอง slice เป็น `.before-import-<timestamp>` รวม `.bak` ตาม save method เดิม การนำเข้าจากเกม native ที่ยังเปิดอยู่ควรรอให้เซฟเสร็จก่อน ไม่มีการ merge ฟาร์มสองเกม

## Multiplayer contract และสิ่งที่ยังไม่ย้าย

RoomServer เป็น authority, POST `/game`, bearer secret48hex, Request มี session UUID, action, command UUID, index, seed, x/y; Reply ส่ง farm snapshot และmessage ใช้ sync polling, server validates movement/range/cap4players/owner, command cache512เพื่อretryไม่ทำงานซ้ำ Cloudflare temporary tunnel เป็น transport ของ native เดิม

Core compile `Network.cs` เดิมและมี `LegacyRoomAdapter` เป็น seam สำหรับ scene ออนไลน์ภายหลัง ทดสอบ local authoritative server/client และidempotencyได้ **แต่ Godot scene นี้ไม่เปิด host/join, tunnel, remote farmer rendering หรือ sleep voting UI** ไม่สรุปว่า multiplayer Godot พร้อมแล้ว native release เดิมยังใช้งานออนไลน์ได้ตามเดิม

## โครงสร้างฉากใหม่

`Farm.tscn` ใช้ TileMapLayer Ground/Details และ TileSet 32×32, Node2D Actors Y-sort, FarmerController + AnimatedSprite2D + Camera2D, CanvasModulate, PointLight2D, pooled Sprite2D effects และ CanvasLayer HUD. `DayTransition.tscn` ใช้ AnimationPlayer; shaders Water/Wind เป็นเอฟเฟกต์เบา ไม่มี WinForms/GDI+ game loop ใน Godot

`LocalFarmSession` รับผิดชอบ simulation/action/save, `AssetCatalog` cache imported PNG/AtlasTexture, `FarmScene` ต่อข้อมูลเข้าฉาก, `WeatherLighting` ต่อแสงและฝน, `EffectPool` prealloc96sprites, `FarmHud` controls ภาษาไทยกับ modal input ownership Tests แยกเป็น console project และ engine smoke mode

ลำดับงานรอบนี้: ตรวจและแยกcore → สร้างscene/resources → เพิ่ม PNG art pipeline → ทำ single-player slice → ตรวจภาพจริง/แก้ atlasและpicking → วัดrender loopสามquality → commit โดยหยุดก่อนย้ายเกมออนไลน์เต็มรูปแบบตามคำขอ
