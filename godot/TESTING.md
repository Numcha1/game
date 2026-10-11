# ผลตรวจ vertical slice

ตรวจบน Windows 10 build19045, Intel Core i5-12400F, NVIDIA RTX3060, driver610.62, Godot4.7.2Mono Compatibility/OpenGL3.3, .NET8 SDK8.0.423. C# Build ผ่าน0warning/0error; editor headless import ผ่าน; rendered run ไม่มี missing resource/shader/runtime error ในstderr

| ส่วน | ผล |
|---|---|
| Core/save/legacy local protocol | 35 checks ผ่าน |
| Godot headless scene | 16 checks ผ่าน |
| Godot rendered scene/input/resize | 91 checks ผ่าน |
| 960×640, 1280×800, 1920×1080 windows | logical800×450คงเดิม, inverse camera24plotsทุกsizeผ่าน |
| Native implementation และ EXE เดิม | SHA256ตรงก่อนทำงาน ไม่มีการแก้ native/dist |

หลักฐาน JSON ในreports: core-tests, godot-smoke-headless, godot-smoke-windows. Screenshot farm-day/farm-storm/shop สร้างจาก Godot viewportจริง (fixtureเพื่อQA, ไม่ใช่ภาพmockupหรือเว็บ) source artwork ในArtSourceและAssetsใช้จริงในscene

## Frame time และ memory

`Stopwatch` วัดwall-clockระหว่าง `_Process` ไม่ใช้เพียง `delta` หรือค่าคำนวณrendercapacity. Cap60FPS, ทดสอบภาพ stormnightกับ24ripe cropsและplayer1คน, warmup60framesแล้วsample600frames(~10sec)ต่อquality, หน้าต่าง1600×900, logicalviewport800×450 integer2×. ใช้pointlightsเมื่อquality>0, rain24/64/120sprites และpool96effects

| Quality | Mean FPS | Mean ms | p95 ms | p99 ms | Private memory หลังsample |
|---|---:|---:|---:|---:|---:|
| Low | 60.003 | 16.666 | 16.787 | 16.870 | 189.0 MiB |
| Medium | 60.003 | 16.666 | 16.791 | 16.860 | 189.8 MiB |
| High | 60.002 | 16.666 | 16.779 | 16.835 | 190.0 MiB |

ผลรวมรวมช่วงจับภาพและresizeมีFPS59.716 จึงไม่อ้างว่าทุกframe60FPSตรงเป๊ะ Managed heapหลังสามsamples~3.39MiB; privateหลังcapture/resize~208.2MiB ไม่มีการสรุปmemory leakจากsampleสั้น แม้ใช้fixedspritepool, sharedPNGtextures, boundedmeasurementbufferและcacheHUDstringsแล้ว simulationเดิมยังสร้างenumerators/listsตามเดิม

Baseline native v0.3 ใน `../native/PERFORMANCE.md` เป็นoffscreenCPUrender/mixedUI-loop คนละrendererและความละเอียด/จำนวนผู้เล่นกับGodotนี้ จึงใช้เพื่ออธิบายการเปลี่ยนระบบเท่านั้น ไม่เอามาคำนวณเปอร์เซ็นต์เร็วขึ้น Godotมีengine/driver/resourcecachecostสูงกว่า native จำเป็นต้องทดสอบเครื่องขั้นต่ำจริงต่อก่อนการreleaseเต็ม

## Coverage และขอบเขต

Core testsตรวจราคา/แรง/น้ำ/fertility/rotation/season/growth/disease/storm/harvest/spoilage/sleep/pause, saveversion1และolderv1withoutEffects, invalidsave, authoritative4clients/four-playercap/badbearer/reconnect/commandidempotency. ใช้linkedWorld.cs/Network.csเดิม ไม่มีWinFormsdependency

Rendered smokeตรวจเดินด้วย Godot physical input, camerafollow, click pickingผ่านinversecanvas, actionplant-water-harvest-sell, modalblock, fourseasons, quality, resizeและcoordinateinverse. Headlessไม่วัดGPUperformanceและไม่ถ่ายscreenshot

**ยังไม่ได้ตรวจ/ยังไม่มี:** Godot online sceneกับเพื่อนต่างบ้าน, long-runningsoak, targetlow-endGPU, packagedstandaloneGodotexport, everycropeverytoolanimationartreview และYouTubeplaybackintegrationในengine (ฟีเจอร์ลิงก์เปิดbrowserเหมือนnativeมีแล้ว). Cloudflarehost/joinยังอยู่nativeครบ ไม่มีการdeployserverใหม่

Hashที่รักษา: World.cs `f21875fe49b274ae598a466968e4b64c593675c365838323459d292f17154444`; Network.cs `e3617a1038b262c31773e44bd03ea3a463117f95d7183a628143879253c96d8b`; nativeSuanPleng.exe `4cc9a21581e51ac0e7961ed11edb3441a55be8b02157fcf0ea60bed890c6c24c`.

## งานถัดไปหลังตรวจรับฉากนี้

เติมpixelgrowth/action/HUDartที่ยังชั่วคราวและปรับshoreline → สำรวจขั้นต่ำhardware/soak → ค่อยต่อLegacyRoomAdapterกับsceneออนไลน์/remoteplayers, syncmovement/retry/disconnect/latejoin และทดสอบinternetจริง → ติดตั้งmatchingMonoexporttemplatesและexportWindows. ไม่รวมmultiplayerfullmigrationไว้ในcommitชุดเดียวกับverticalslice
