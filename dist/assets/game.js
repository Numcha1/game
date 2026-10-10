"use strict";
const CROPS = Object.freeze({
  carrot: { name: "แครอต", icon: "🥕", cost: 8, seconds: 20, sell: 15, xp: 10 },
  tomato: { name: "มะเขือเทศ", icon: "🍅", cost: 15, seconds: 35, sell: 28, xp: 15 },
  corn: { name: "ข้าวโพด", icon: "🌽", cost: 25, seconds: 60, sell: 48, xp: 22 },
  strawberry: { name: "สตรอว์เบอร์รี", icon: "🍓", cost: 35, seconds: 90, sell: 70, xp: 30 }
});
const STORAGE_KEY = "cozy-farm-state-v1";
const MUSIC_KEY = "cozy-farm-music-v1";
function freshState() {
  return { version: 1, coins: 100, xp: 0, harvested: 0, unlocked: 6, selected: "carrot", freeSeeds: 0, plots: Array(12).fill(null), inventory: { carrot: 0, tomato: 0, corn: 0, strawberry: 0 } };
}
function validInt(value, max = 100000000) { return Number.isSafeInteger(value) && value >= 0 && value <= max; }
function validateState(value) {
  if (!value || value.version !== 1 || !validInt(value.coins) || !validInt(value.xp) || !validInt(value.harvested) || !validInt(value.freeSeeds, 3) || !validInt(value.unlocked, 12) || value.unlocked < 6 || !Object.hasOwn(CROPS, value.selected) || !Array.isArray(value.plots) || value.plots.length !== 12 || !value.inventory) return null;
  if (!Object.keys(CROPS).every(key => validInt(value.inventory[key]))) return null;
  const plots = [];
  for (let index = 0; index < 12; index++) {
    const plot = value.plots[index];
    if (plot === null) { plots.push(null); continue; }
    if (index >= value.unlocked || !plot || !Object.hasOwn(CROPS, plot.crop) || (plot.wateredAt !== null && (!Number.isSafeInteger(plot.wateredAt) || plot.wateredAt <= 0 || plot.wateredAt > Date.now() + 60000))) return null;
    plots.push({ crop: plot.crop, wateredAt: plot.wateredAt });
  }
  return { version: 1, coins: value.coins, xp: value.xp, harvested: value.harvested, unlocked: value.unlocked, selected: value.selected, freeSeeds: value.freeSeeds, plots, inventory: Object.fromEntries(Object.keys(CROPS).map(key => [key, value.inventory[key]])) };
}
function growth(plot, now = Date.now()) {
  if (!plot || plot.wateredAt === null) return 0;
  return Math.min(1, Math.max(0, (now - plot.wateredAt) / (CROPS[plot.crop].seconds * 1000)));
}
function saleTotal(state) { return Object.keys(CROPS).reduce((sum, key) => sum + state.inventory[key] * CROPS[key].sell, 0); }
function unlockCost(state) { return 40 + Math.max(0, state.unlocked - 6) * 20; }
function actOnPlot(state, index, now = Date.now()) {
  if (!Number.isInteger(index) || index < 0 || index >= 12) return "ไม่พบแปลงนี้";
  if (index >= state.unlocked) {
    if (index !== state.unlocked) return "ขยายสวนจากแปลงที่ล็อกแปลงแรกก่อนนะ";
    const price = unlockCost(state);
    if (state.coins < price) return `ขยายแปลงต้องใช้ ${price} เหรียญ เก็บเกี่ยวแล้วขายผลผลิตก่อนนะ`;
    state.coins -= price; state.unlocked++;
    return "ขยายสวนแล้ว! เลือกเมล็ดแล้วปลูกได้เลย";
  }
  const plot = state.plots[index];
  if (!plot) {
    const crop = CROPS[state.selected];
    if (state.selected === "carrot" && state.freeSeeds > 0) state.freeSeeds--;
    else if (state.coins < crop.cost) return "เหรียญไม่พอ เลือกเมล็ดที่ถูกลง หรือรับเมล็ดแครอตฟรีได้";
    else state.coins -= crop.cost;
    state.plots[index] = { crop: state.selected, wateredAt: null };
    return `ปลูก${crop.name}แล้ว แตะแปลงอีกครั้งเพื่อรดน้ำ`;
  }
  const crop = CROPS[plot.crop];
  if (plot.wateredAt === null) { plot.wateredAt = now; return `รดน้ำแล้ว! ${crop.name}จะโตใน ${crop.seconds} วินาที`; }
  if (growth(plot, now) < 1) return `กำลังเติบโต อีก ${Math.ceil((1 - growth(plot, now)) * crop.seconds)} วินาที มาเปิดเพลงรอกัน`;
  const previousLevel = Math.floor(state.xp / 50) + 1;
  state.inventory[plot.crop]++; state.xp += crop.xp; state.harvested++; state.plots[index] = null;
  const newLevel = Math.floor(state.xp / 50) + 1;
  return `เก็บ${crop.name}ลงตะกร้าแล้ว +${crop.xp} XP${newLevel > previousLevel ? ` • เลื่อนเป็นระดับ ${newLevel}!` : ""}`;
}
function parseYouTube(raw) {
  let url;
  try { url = new URL(raw.trim()); } catch { throw new Error("ใส่ลิงก์ YouTube ที่สมบูรณ์ เช่น https://youtu.be/รหัสวิดีโอ"); }
  const hosts = ["youtube.com", "www.youtube.com", "m.youtube.com", "music.youtube.com", "youtu.be", "www.youtu.be"];
  if (!hosts.includes(url.hostname.toLowerCase()) || url.protocol !== "https:" || url.username || url.password || url.port) throw new Error("รองรับเฉพาะลิงก์ https:// จาก YouTube หรือ youtu.be เท่านั้น");
  const parts = url.pathname.split("/").filter(Boolean);
  let video = null, list = url.searchParams.get("list");
  if (url.hostname.endsWith("youtu.be")) { if (parts.length === 1) video = parts[0]; }
  else if (parts.length === 1 && parts[0] === "watch") video = url.searchParams.get("v");
  else if (parts.length === 2 && ["shorts", "embed", "live"].includes(parts[0])) video = parts[1];
  else if (parts.length === 1 && parts[0] === "playlist" && list) { /* playlist only */ }
  else throw new Error("รองรับลิงก์ watch, youtu.be, shorts, embed และ playlist");
  if (video !== null && !/^[A-Za-z0-9_-]{11}$/.test(video)) throw new Error("รหัสวิดีโอไม่ถูกต้อง ตรวจสอบลิงก์แล้วลองอีกครั้ง");
  if (list !== null && !/^[A-Za-z0-9_-]{10,128}$/.test(list)) throw new Error("รหัสเพลย์ลิสต์ไม่ถูกต้อง");
  if (!video && !list) throw new Error("ลิงก์นี้ไม่มีรหัสวิดีโอหรือเพลย์ลิสต์");
  const embed = new URL(video ? `https://www.youtube.com/embed/${video}` : "https://www.youtube.com/embed/videoseries");
  embed.searchParams.set("controls", "1");
  if (list) embed.searchParams.set("list", list);
  const canonical = new URL(video ? "https://www.youtube.com/watch" : "https://www.youtube.com/playlist");
  if (video) canonical.searchParams.set("v", video);
  if (list) canonical.searchParams.set("list", list);
  return { embed: embed.href, canonical: canonical.href };
}
if (typeof module !== "undefined" && module.exports) module.exports = { CROPS, freshState, validateState, growth, saleTotal, unlockCost, actOnPlot, parseYouTube };
if (typeof document !== "undefined") {
  const $ = id => document.getElementById(id);
  let state = freshState(), toastTimer, storageWorks = true;
  try { const raw = localStorage.getItem(STORAGE_KEY); if (raw) { const validated = validateState(JSON.parse(raw)); if (validated) state = validated; else $("saveStatus").textContent = "เริ่มสวนใหม่: ข้อมูลเดิมไม่สมบูรณ์"; } } catch { storageWorks = false; }
  function save() { try { localStorage.setItem(STORAGE_KEY, JSON.stringify(state)); storageWorks = true; $("saveStatus").textContent = "บันทึกอัตโนมัติ"; } catch { storageWorks = false; $("saveStatus").textContent = "บันทึกไม่ได้ • เล่นได้ในหน้านี้"; } }
  if (!storageWorks) $("saveStatus").textContent = "บันทึกไม่ได้ • เล่นได้ในหน้านี้";
  function notify(message) { clearTimeout(toastTimer); $("toast").textContent = message; $("toast").classList.add("visible"); toastTimer = setTimeout(() => $("toast").classList.remove("visible"), 4400); }
  const plotButtons = Array.from({ length: 12 }, (_, index) => {
    const button = document.createElement("button"); button.type = "button"; button.className = "plot";
    button.addEventListener("click", () => { notify(actOnPlot(state, index)); save(); render(); });
    $("plots").append(button); return button;
  });
  const seedButtons = {};
  for (const [key, crop] of Object.entries(CROPS)) {
    const button = document.createElement("button"); button.type = "button"; button.className = "seed";
    button.innerHTML = `<span class="seed-icon" aria-hidden="true">${crop.icon}</span><span class="seed-name">${crop.name}<span class="seed-desc">${crop.seconds} วินาที · ขาย ${crop.sell} เหรียญ</span></span><span class="seed-price">◉ ${crop.cost}</span>`;
    button.addEventListener("click", () => { state.selected = key; save(); render(); notify(`เลือกเมล็ด${crop.name}แล้ว แตะแปลงว่างเพื่อปลูก`); });
    $("seeds").append(button); seedButtons[key] = button;
  }
  function render() {
    const now = Date.now();
    $("coins").textContent = state.coins.toLocaleString("th-TH");
    $("level").textContent = String(Math.floor(state.xp / 50) + 1).padStart(2, "0");
    $("xpBar").style.width = `${state.xp % 50 * 2}%`; $("xpLabel").textContent = `${state.xp % 50} / 50 XP`;
    $("plotCount").textContent = `${state.unlocked} / 12 แปลง`;
    Object.keys(seedButtons).forEach(key => { seedButtons[key].classList.toggle("selected", state.selected === key); seedButtons[key].setAttribute("aria-pressed", String(state.selected === key)); });
    $("selectedNote").textContent = `เลือกอยู่: ${CROPS[state.selected].name}${state.freeSeeds ? ` • แครอตฟรีเหลือ ${state.freeSeeds} เมล็ด` : " • แตะแปลงว่างเพื่อปลูก"}`;
    for (let index = 0; index < 12; index++) {
      const button = plotButtons[index], plot = state.plots[index]; let html, label, className;
      if (index >= state.unlocked) {
        const next = index === state.unlocked;
        className = "plot locked"; label = next ? `แปลง ${index + 1} ขยายสวน ${unlockCost(state)} เหรียญ` : `แปลง ${index + 1} ยังล็อก ขยายแปลงก่อนหน้าก่อน`;
        html = `<span class="plot-art" aria-hidden="true">♧</span><span class="plot-label">${next ? "ขยายสวน" : "ยังล็อกอยู่"}</span><span class="plot-meta">${next ? `◉ ${unlockCost(state)}` : "ขยายตามลำดับ"}</span>`;
      } else if (!plot) {
        className = "plot empty"; label = `แปลง ${index + 1} ว่าง ปลูก${CROPS[state.selected].name}`;
        html = '<span class="plot-art" aria-hidden="true">＋</span><span class="plot-label">ปลูกเมล็ด</span><span class="plot-meta">แปลงว่าง</span>';
      } else {
        const crop = CROPS[plot.crop], progress = growth(plot, now), thirsty = plot.wateredAt === null, ripe = progress === 1;
        className = `plot${ripe ? " ripe" : ""}`;
        const status = thirsty ? "💧 แตะเพื่อรดน้ำ" : ripe ? "พร้อมเก็บเกี่ยว" : `อีก ${Math.ceil((1 - progress) * crop.seconds)} วินาที`;
        label = `แปลง ${index + 1} ${crop.name} ${status}`;
        html = `<span class="plot-art" aria-hidden="true">${ripe ? crop.icon : '<span class="sprout"></span>'}</span><span class="plot-label">${crop.name}</span><span class="plot-meta">${status}</span>${!thirsty ? `<span class="plot-progress" aria-hidden="true"><span style="width:${progress * 100}%"></span></span>` : ""}`;
      }
      button.className = className; button.setAttribute("aria-label", label);
      if (button.innerHTML !== html) button.innerHTML = html;
    }
    const count = Object.values(state.inventory).reduce((a, b) => a + b, 0), total = saleTotal(state);
    $("inventoryCount").textContent = count;
    $("inventoryItems").textContent = count ? Object.entries(state.inventory).filter(([, n]) => n).map(([key, n]) => `${CROPS[key].icon} ${n}`).join("   ·   ") : "ยังไม่มีผลผลิต รอเก็บต้นแรกกันนะ";
    $("sellBtn").disabled = !count; $("sellBtn").textContent = count ? `ขายทั้งหมด +${total} ◉` : "ขายผลผลิต";
    $("giftBtn").hidden = state.coins >= CROPS.carrot.cost || state.freeSeeds > 0;
    const ripeCount = state.plots.filter(plot => plot && growth(plot, now) === 1).length;
    const dryCount = state.plots.filter(plot => plot && plot.wateredAt === null).length;
    $("tipTitle").textContent = ripeCount ? `เก็บเกี่ยวได้แล้ว ${ripeCount} แปลง!` : dryCount ? "ต้นเล็ก ๆ กำลังรอน้ำ" : state.harvested ? "สวนเติบโต เราก็เติบโต" : "เริ่มต้นจากเมล็ดเล็ก ๆ";
    $("tipText").textContent = ripeCount ? "แตะผักที่โตแล้วเพื่อเก็บลงตะกร้า แล้วขายเป็นเหรียญได้เลย" : dryCount ? "แตะแปลงที่เพิ่งปลูกเพื่อรดน้ำ เวลาการเติบโตจะเริ่มนับทันที" : "เลือกเมล็ด แล้วแตะแปลงว่างเพื่อปลูก ผักที่รดน้ำแล้วโตต่อแม้ปิดหน้าเว็บ";
    $("achievement").textContent = state.harvested >= 10 ? `✦ ชาวสวนมืออาชีพ • เก็บเกี่ยว ${state.harvested} ต้นแล้ว` : state.harvested ? `✦ ตราชาวสวนต้นแรก • อีก ${10 - state.harvested} ต้นถึงชาวสวนมืออาชีพ` : "○ เก็บเกี่ยวครั้งแรก แล้วรับตราชาวสวน";
  }
  $("sellBtn").addEventListener("click", () => { const total = saleTotal(state); if (!total) return; state.coins += total; Object.keys(CROPS).forEach(key => state.inventory[key] = 0); save(); render(); notify(`ขายผลผลิตแล้ว ได้รับ ${total} เหรียญ!`); });
  $("giftBtn").addEventListener("click", () => { if (state.coins >= 8 || state.freeSeeds > 0) return; state.freeSeeds = 3; state.selected = "carrot"; save(); render(); notify("รับเมล็ดแครอตฟรี 3 เมล็ดแล้ว ปลูกต่อได้เลย!"); });
  $("helpBtn").addEventListener("click", () => $("helpDialog").showModal());
  $("resetBtn").addEventListener("click", () => $("resetDialog").showModal());
  $("cancelReset").addEventListener("click", () => $("resetDialog").close());
  $("confirmReset").addEventListener("click", () => { state = freshState(); save(); render(); $("resetDialog").close(); notify("สวนใหม่พร้อมแล้ว เริ่มปลูกความสุขกัน!"); });
  const emptyPlayer = $("musicPlayer").innerHTML;
  try { $("musicUrl").value = localStorage.getItem(MUSIC_KEY) || ""; } catch { /* Game remains playable without storage. */ }
  $("musicForm").addEventListener("submit", event => {
    event.preventDefault(); let parsed;
    try { parsed = parseYouTube($("musicUrl").value); } catch (error) { $("musicError").textContent = error.message; return; }
    $("musicError").textContent = "";
    const iframe = document.createElement("iframe"); iframe.src = parsed.embed; iframe.title = "เพลงประจำสวนจาก YouTube"; iframe.width = "300"; iframe.height = "210"; iframe.referrerPolicy = "strict-origin-when-cross-origin"; iframe.allow = "encrypted-media; fullscreen; picture-in-picture"; iframe.allowFullscreen = true;
    $("musicPlayer").replaceChildren(iframe); $("youtubeLink").href = parsed.canonical; $("youtubeLink").hidden = false; $("stopMusic").hidden = false;
    try { localStorage.setItem(MUSIC_KEY, parsed.canonical); } catch { /* Optional preference. */ }
    notify("เปิดตัวเล่นแล้ว กดเล่นใน YouTube เพื่อเริ่มฟังเพลง");
  });
  $("stopMusic").addEventListener("click", () => { $("musicPlayer").innerHTML = emptyPlayer; $("musicUrl").value = ""; $("musicError").textContent = ""; $("youtubeLink").hidden = true; $("youtubeLink").removeAttribute("href"); $("stopMusic").hidden = true; try { localStorage.removeItem(MUSIC_KEY); } catch { /* Optional preference. */ } notify("หยุดและล้างเพลงแล้ว"); });
  render(); setInterval(render, 1000);
  document.addEventListener("visibilitychange", () => { if (!document.hidden) render(); });
}
