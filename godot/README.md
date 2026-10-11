# Seasons Together · Godot single-player vertical slice

ฉากเล่นได้จริงใน Godot 4.7.2 .NET/C# สร้างแยกจาก native release เดิม ใช้กติกาเดิมผ่าน Core project ไม่ใช้ WinForms renderer

เปิด `project.godot` ด้วย Godot Mono แล้ว Build และ F6/F5 หรือดับเบิลคลิก `Run-Seasons-Together.cmd` ในเครื่องนี้ launcher ค้นหา Godot ที่ติดตั้งใน Downloads ได้ หากอยู่ที่อื่นกำหนด environment `GODOT_MONO` หรือใช้ `powershell -File run.ps1 -GodotPath <path-to-Godot-Mono.exe>` ต้องมี .NET SDK8 โปรเจกต์pin8.0.423/latestPatch

เดิน WASD/ลูกศร, เครื่องมือ1–7, Tab เปลี่ยนเมล็ด, คลิกแปลงใกล้ตัว, E ขายใกล้ลังด้านขวา, Space นอนหน้าบ้าน, Esc pause, B/I กระเป๋า, O ร้านค้า, F1 วิธีเล่น, F3 ตั้งค่า/นำเข้าเซฟ/YouTube. YouTube เปิด HTTPS link ในเบราว์เซอร์เช่น native เกมเดิม

ฉากแรกเริ่มฟาร์มว่างเพื่อเล่นตามกติกาจริง Screenshot ใน reports ใช้ fixture 24 ripe crops เพื่อทดสอบภาพและประสิทธิภาพ ไม่ใช่ค่าเริ่มต้นของผู้เล่น

เซฟแยก `%LOCALAPPDATA%/SuanPleng/godot-slice/farm.json` ปุ่มนำเข้าเซฟเดิมคัดลอกหลังยืนยัน โดยเก็บสำรองฟาร์ม slice และไม่เขียนทับเซฟ native คุณภาพต่ำ/กลาง/สูงบันทึกแยกในgraphics.json

Renderer ใช้ Compatibility, logical viewport800×450, nearest filtering, camera zoom1และตำแหน่งปัดเป็นพิกเซล, integer scaling. หน้าต่าง1600×900ขยาย2เท่า หน้าต่างที่หารไม่ลงตัวมีขอบletterboxเพื่อรักษาพิกเซล ไม่มีการยืดภาพเบลอ

```powershell
.\build.ps1
dotnet run --project Tests -- reports/core-tests.json
& $env:GODOT_MONO --headless --path . -- --smoke
& $env:GODOT_MONO --path . --resolution 1600x900 -- --smoke
```

Smoke mode ใช้ test profile UUID ในreports ไม่ใช้ฟาร์มผู้เล่น ตรวจเดินผ่าน input, กล้อง, mouse picking, ปลูก/รด/เก็บ/ขาย, modal, ฤดู, quality, resize และจับภาพจากviewportจริง Render sample600framesต่อqualityรวมประมาณ30วินาที

อ่าน [การวิเคราะห์และ compatibility](ANALYSIS.md), [สถานะ artwork](ART.md), [ผลตรวจและข้อจำกัด](TESTING.md). ภาพเกมจริงอยู่ใน `reports/farm-day.png`, `farm-storm.png`, `shop.png`.

งานที่ยังไม่ครบ: Godot online host/join และ remote players; growth-stage sprites ที่วาดเฉพาะทุกชนิด, tool/action animationเต็มชุด, UI/weather iconsบางส่วน, shoreline/ripple polish และsoundpack. ต้นฉบับภาพใหม่และpromptบันทึกในArtSource และ ART.md ไม่ใช้assetจากเกมอ้างอิง

ยังไม่ส่ง Godot Windows standalone export ในรอบนี้ ไม่พบ Godot Mono export templates ติดตั้งในเครื่อง โปรเจกต์และ launcher รันทดสอบได้ด้วย editor ที่มีอยู่ `SuanPleng.exe` v0.3 ของ native เดิมเก็บไว้เหมือนเดิม ไม่มีการนำ editor executable มาตั้งชื่อเป็นgame export

## Modular architecture refactor

Branch `godot/modular-architecture` แยก Domain / Application / Infrastructure / Presentation โดยยังใช้กฎใน World.cs ชุดเดิม เพิ่ม asynchronous SaveService, readonly read model, command validation, network recovery seam, asset caching และ scene cancellation

ผลตรวจใน checkout จริง: Build 0 warning/error, Core 82 ข้อ, scene headless 16 / rendered 91 ข้อ, lifecycle 60 ข้อทั้ง headless และ rendered, Native Release build และ self-test 35 ข้อ ผ่านทั้งหมด ผล rendered ใกล้ 60 FPS ทั้งสามระดับคุณภาพ

อ่าน [Audit](docs/AUDIT.md), [Dependency diagram และ responsibility matrix](docs/ARCHITECTURE.md), [รายงานทุก Phase และความเสี่ยงที่เหลือ](docs/PHASES.md), [Performance ก่อน/หลัง](docs/PERFORMANCE.md), [รายการไฟล์ที่เปลี่ยน](docs/CHANGED_FILES.md). หลักฐาน raw อยู่ใน reports/architecture-before.json, architecture-after.json, final-core-tests.json, lifecycle-*.json และ preservation.json

ทดสอบ lifecycle เพิ่มด้วย `& $env:GODOT_MONO --headless --path . -- --lifecycle` หรือรันแบบมีภาพโดยเอา --headless ออก ทุก diagnostics ใช้โปรไฟล์ทดสอบ UUID เท่านั้น NetworkService ยังไม่เปิด multiplayer UI ใน Godot; Native Game และโปรโตคอลเดิมคงไว้