# System audit — baseline ecf7e32

ตรวจ source ใน godot/Core, Scripts, Scenes, Tests, Tools และ native/ รวมถึง World, Network, WinForms input/UI, renderer/asset/cache, animation, timers/disposal, diagnostics และ build scripts. Native เป็น compatibility implementation ที่ต้องเก็บไว้; รอบนี้ไม่แก้กฎหรือ renderer เดิม

| ระดับ | จุดอ้างอิง baseline | ปัญหาและแนวแก้ |
|---|---|---|
| Critical | Scripts/FarmScene.cs:38–41,65–70 | `--showcase` แก้ loaded farm และ autosave ลงโปรไฟล์จริง แยกทุก diagnostic flag ให้ใช้ disposable test profile |
| High | Core/LocalFarmSession.cs:33–46 | Execute/auto-save ทำ synchronous file I/O บน game loop แยก snapshot serialization กับ bounded single-writer async persistence |
| High | Scripts/FarmScene.cs:218–234 | import คัดลอก farm และ identity แยกขั้น เกิด partial import แล้ว old scene อาจเซฟทับไฟล์ที่เพิ่งนำเข้า ใช้ transaction/backups/rollback และ suspend session ระหว่าง import |
| High | native/World.cs:180–185 | legacy Load ไม่ตรวจ Effects:null, nullable string fields, energy/moisture ranges และ invalid IDs ทั้งหมด เพิ่ม structural guard ที่ขอบเขต save/network โดยไม่แก้สูตรใน World |
| High | Scripts/FarmScene.cs:12, Core/LocalFarmSession.cs:8–9, Scripts/FarmHud.cs:73–105 | mutable Farm/Farmer ถูก UI/controller ใช้โดยตรง ย้ายคำสั่งเข้าชั้น Application และให้ presentation อ่าน immutable value views |
| High | Scripts/FarmScene.cs:235–312 | async smoke ไม่มี cancellation/lifetime guard; reload/exit ระหว่าง await เสี่ยงใช้ node ที่ free แล้ว แยก Diagnostics พร้อม scene lifetime token และทดสอบ teardown |
| Medium | Scripts/FarmHud.cs:66–67 | anonymous signal subscription ไม่มี explicit unsubscribe/initialize guard เสี่ยงซ้ำเมื่อ controller reuse เพิ่ม typed handlers และ lifecycle detach |
| Medium | Core/LegacyRoomAdapter.cs:8–23 | ยังเป็น transport wrapper ไม่มี coordination สำหรับ timeout/reconnect/snapshot validation/ack-lost retry เพิ่ม NetworkService ที่ไม่ tick remote world |
| Medium | Scripts/FarmScene.cs:33–75,165–237 | composition, rendering setup, commands, persistence, config, logging, input และ diagnostics รวมใน controller เดียว แยก renderer/input/config/diagnostics และใช้ services |
| Medium | Scripts/AssetCatalog.cs:8–35 | textures/region definitions/frames load โดยไม่ validate manifest; FarmerFrames สร้างใหม่เมื่อเรียกซ้ำ; MakeTileSet ซ้ำ resource ที่มีแล้ว แคชและตรวจ resources ครั้งเดียวก่อนรัน |
| Medium | Scripts/WeatherLighting.cs:19–25, Scripts/EffectPool.cs:16–20 | temporary Image/ImageTexture ownership ไม่ชัด, lamp ถูกเพิ่มใน parent แทน effect owner ให้ dispose generated resources และจัด node ownership |
| Medium | native/Network.cs:34–45,97–108 | save/server timer/connection tasks มี legacy lifecycle constraints และ synchronous persistence ใต้ lock; Godot future adapter ไม่ย้าย host จนกว่าจะทดสอบเต็ม เก็บเป็น residual native risk |
| Medium | native/Network.cs:63–95 | authenticated malformed request เช่น Seed:null ไม่ถูกตรวจทุก field อาจทำให้ handle task fault; รักษา protocol และเสริม outbound validation ใน Godot service ไม่อ้างว่า native server hardened ครบ |
| Low | Scripts/FarmScene.cs:25,173–177 | frame monitor ฝัง scene และวัดเพียงช่วงสั้น ใช้ bounded diagnostics ที่เลือก sample ได้และแยก workload ก่อน/หลัง |
| Low | Scripts/FarmHud.cs:110,125–132 | persistence/browser validationอยู่ใน UI แยก config/save/application commands และ audio-link service; ไม่เปลี่ยน UI/art |
| Low | Core/*.csproj, Scripts/*.cs | ไม่มี architectural regression guard เพิ่มตรวจ source dependency และ forbidden presentation mutation |

## สิ่งที่มีอยู่และควรรักษา

World.cs ใช้ BCL ไม่มี Godot/WinForms/Graphics และเป็นสูตรหลักชุดเดียว; Network.cs มี server-authoritative/range/owner/cap4/idempotency512; native FramePulse จำกัด queued callback และ unsubscribe, HUD caches และ sprite atlas ลดงานวาด; Godot ใช้ TileMapLayer/nearest/integer viewport, bounded particle sprites และ authoritative local simulation

## แผนแก้ทีละ commit

1. audit/baseline และแยก diagnostic profile/lifetime (เสี่ยงต่ำ)
2. SaveService + invariants + background writer + failure/rollback tests
3. Application command/read-model/events และ presentation routing ทีละ consumer
4. NetworkService/recovery tests โดยไม่เปิด Godot online host
5. resource lifecycle/structured log/soak/performance regression และเอกสารส่งมอบ

การวัด baseline ต้องใช้ rendererจริง1600×900, storm/24ripe crops, cap60, warmup60/sample600 ต่อquality และไม่เปิด user save. build35core/91rendered regression เป็นฐานอ้างอิง ผล raw เก็บใน reports/architecture-before.json
