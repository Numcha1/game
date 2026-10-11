# Playable six-type furniture loop

Buy furniture/wood from Rowan, carry it in the bag, preview/place/move supported pieces in the farmhouse and use F. Tables/shelves display actual inventory items; boxes/chests store and organize them; chairs sit/stand; workbenches craft real furniture from wood. Original bed/chest and farming rules remain intact. Saves v1/v2 load; new furniture uses an explicit v3 envelope with a preserved original backup.

[Player guide, audit, architecture and save migration](docs/FURNITURE_SYSTEM.md) · [Actual tests, screenshots, changed files and performance](docs/FURNITURE_VALIDATION.md). Core 470, first real box loop 14 and six-type Godot gate 60 pass; full regression/build passes. Furniture co-op, wardrobes, timed production and a new executable export are outside this first catalog. Open `Run-Seasons-Together.cmd`.

---

The following records describe earlier milestones; current furniture status is above.

# Item foundation and backpack compatibility slice

The 25-item catalog now drives existing seed-shop offers, icon references and validated item transactions. The backpack combines actual produce/seeds with filters, sorting and a 24-cell display; the farmhouse chest supports quick deposit/withdraw of a whole crop stack. Original save IDs, prices, farming and spoilage remain intact.

[Audit, actual tests, screenshots and pending milestones](docs/ITEM_SYSTEM_MILESTONES.md). M1 is verified; full M2 remains open for persisted stack splitting, drag/drop, capacity upgrades and configurable equipment. Furniture placement/crafting/production are later milestones and are not enabled by the fixed bed/chest metadata.

---
# World-wide scale and composition (tested presentation milestone)

All seven existing playable regions now share camera/actor scale 1.0 and prepared NPC pixels. Interiors use original edited assets, cached canvases, silhouette depth and coordinated lighting; farm props, shadows, paths and decoration clusters have been composed explicitly. [Implementation, changed files and remaining art work](docs/WORLD_COMPOSITION.md) | [Actual regression, performance and before/after gallery](docs/WORLD_VISUAL_VALIDATION.md) | [Scale contract](docs/WORLD_SCALE.md).

Build/Core/native/rendered regression passed, including 190 composition checks. Exact bed/door/floor proportions and organic terrain transitions remain art tasks; one pre-existing ObjectDB exit warning remains unresolved. Open `Run-Seasons-Together.cmd` for the existing launcher. No new Windows export or Internet multiplayer certification is claimed.

---
# Earlier indoor scale and compact HUD (superseded scale profile)

Enclosed rooms now use 1.25x camera zoom and 1.05x visual character scale, with a compact top bar and one contextual bottom prompt. The farm/village outdoor profile and gameplay rules remain unchanged. [Audit, design tokens and before/after captures](docs/INDOOR_PRESENTATION.md) | [Executed validation and limits](docs/INDOOR_VALIDATION.md).

---

# Living village: NPC Milestones 1-2

From the farm, walk to the village sign on the right near `(23, 8)` and press **F**. Press **F** near Mira/Rowan or a registered door. Mira sells the original eight crop seeds; bought seeds appear in the bag and are consumed before cash when planting. Rowan installs the existing greenhouse through the original game rules. Both walk between workplaces, square and visitable homes; NPCs, seeds and daily conversations persist.

[Audit and architecture](docs/NPC_SYSTEM.md) | [Executed tests, performance and open resource warning](docs/NPC_MEASUREMENTS.md) | [Changed files](docs/NPC_CHANGED_FILES.md). Run using the existing `run.ps1`. Only two villagers are playable; gifts, reward-bearing quests, new house upgrades and NPC co-op are later milestones. One unidentified native ObjectDB instance is still reported at diagnostic exit; this slice is not leak-free certified.

---

# Playable farmhouse (building Milestones 1-2)

Approach the home door and press **F**. Walk inside, use **F** at bed/chest/exit, or Space near the bed. Deposit/withdraw chosen quantities; room position, chest and lights persist. [Audit, architecture, save migration and remaining scope](docs/BUILDINGS.md), [executed tests and measured performance](docs/BUILDINGS_MEASUREMENTS.md). Saves v1 still load; using buildings writes an atomic Godot v2 envelope and retains the original v1 file when present. Native remains separate. Furniture placement/upgrades/shop interiors and building co-op are not yet implemented.

---

# GDD 1.1 development baseline (2026-10-11)

[GDD](docs/GDD_V1_1.md) is the current design specification: peaceful farming, single-player A-F before co-op G. [Current implementation and open acceptance](docs/GDD_IMPLEMENTATION_STATUS.md). Press E near the shipping crate or use the bag to inspect daily revenue and confirm selling. Old saves and rules remain intact. The records below describe prior milestones.

---

# Premium systems development (2026-10-11)

Current branch: `feature/premium-game-systems`. [Milestone 1 implementation/audit](docs/SYSTEMS_MILESTONE1.md), [feature status and roadmap](docs/SYSTEMS_ROADMAP.md), and [actual performance comparison](docs/SYSTEMS_MEASUREMENTS.md) are the current records. Earlier sections below retain historical results from the integrated presentation work.

Milestone 1 is TESTED: strict save/undo validation, tracked pending persistence and 2/4-process loopback protocol tests. Core 234, Native 35, startup 60, recovery 10, rendered gameplay 101, premium 175 and lifecycle 350 passed in their documented scopes. No feature is marked RELEASE READY. The playable Godot scene remains single-player while room/remote-actor binding is developed; the user selected local multiple-instance testing before Internet acceptance.

Open `Run-Seasons-Together.cmd` and choose **เข้าฟาร์ม**. From the repository root, run `dotnet run --project godot/Tests -- godot/reports/systems-m1-core.json` for Core including real peer processes, or `.\godot\verify.ps1 -Rendered -ExtendedLifecycle` for builds/rendered regressions. All tests use isolated profiles. Unsupported/incomplete future save data is refused while retaining original bytes; complete original v1/BOM/pre-animation saves remain readable.

---

# Premium presentation verification (2026-10-11)

Current branch: `feature/premium-polish`. Read [implementation and remaining acceptance](docs/PREMIUM_POLISH.md), [prioritized audit](docs/PREMIUM_AUDIT.md) and [measured evidence](docs/PREMIUM_MEASUREMENTS.md). Open `Run-Seasons-Together.cmd`, then choose **เข้าฟาร์ม**. The Godot launcher requires the installed .NET engine/SDK; it is not a standalone export.

Bundled Thai typography, integer canvas scaling, original four-stage crops/tool icons, shared item cards, title/pause/loading menus, tutorial overlay and action audio are implemented. Native rules, Save v1 and the existing network protocol remain unchanged. The older sections below are historical evidence; current counts and limitations are in the premium report.

---

# Production core verification (2026-10-11)

Current branch: `feature/production-core`. Read [progress](docs/PRODUCTION_PROGRESS.md), [focused audit](docs/PRODUCTION_AUDIT.md), [feature matrix](FEATURE_MATRIX.md) and [quality gates](QUALITY_GATES.md) for current evidence. The older vertical-slice numbers below remain historical.

Open `Run-Seasons-Together.cmd` in this Godot directory to play on the current configured machine. It requires Godot .NET and the .NET SDK; it is not a standalone exported game. For editing, import `project.godot` in Godot .NET and build before F5.

From the repository root:

```powershell
.\godot\verify.ps1 -Rendered -ExtendedLifecycle
# Faster logic/lifecycle run without GPU metrics:
.\godot\verify.ps1
```

Pass `-GodotPath <absolute-path-to-Mono-console.exe>` if discovery fails. Diagnostics use isolated GUID profiles. Startup fault injection deliberately logs errors; exit code, fresh reports and check counts determine success. No test overwrites the real Native/Godot player profile. Reports contain no raw player credentials; fixture/profile directories are excluded from Git.

Core CI runs only the engine-independent regression suite. Godot rendering, export, Internet co-op and real-player acceptance remain separate gates.

---

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