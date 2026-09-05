import fs from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";
import { Presentation, PresentationFile } from "@oai/artifact-tool";

const workspaceDir = "/Users/hannguyen/Desktop/ZOMBIE_HOUSE_3D";
const skillDir = "/Users/hannguyen/.codex/plugins/cache/openai-primary-runtime/presentations/26.903.11726/skills/presentations";
const tmpDir = path.join(workspaceDir, ".codex-build", "zombie-house-deck");
const finalPptx = path.join(workspaceDir, "presentation", "zombie-house-3d-presentation.pptx");
const runtimePython = "/Users/hannguyen/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/bin/python3";
const { resolvePresentationFont, finalizePresentation } = await import(pathToFileURL(
  path.join(skillDir, "container_tools", "artifact_tool_utils.mjs"),
).href);

await fs.mkdir(tmpDir, { recursive: true });
await fs.mkdir(path.dirname(finalPptx), { recursive: true });
const font = resolvePresentationFont();

const W = 1280;
const H = 720;
const colors = {
  ink: "#0B1F33",
  muted: "#466070",
  teal: "#1E6473",
  green: "#9ACB5A",
  cream: "#F7F4EE",
  paper: "#FFFDF9",
  amber: "#F2B35C",
  line: "#C9D6D8",
  placeholder: "#E8EFF1",
};
const asset = (...parts) => path.join(workspaceDir, "Assets", ...parts);
const images = {
  menu: await fs.readFile(asset("Textures", "MainMenuBG.jpg")),
  peashooter: await fs.readFile(asset("UI", "PlantPortraits", "PeaShooter.png")),
  sunflower: await fs.readFile(asset("UI", "PlantPortraits", "Sunflower.png")),
  zombie: await fs.readFile(asset("ThirdParty", "CartoonZombie", "zombie_tex.png")),
  minimap: await fs.readFile(asset("Minimap", "location_maker.png")),
  barbarian: await fs.readFile(asset("Data", "Characters", "Portraits", "Barbarian_Portrait.png")),
};

const presentation = Presentation.create({ slideSize: { width: W, height: H } });

function addText(slide, text, left, top, width, height, style = {}) {
  const shape = slide.shapes.add({
    geometry: "textbox",
    position: { left, top, width, height },
    fill: "none",
    line: { fill: "none", width: 0 },
  });
  shape.text = text;
  shape.text.style = {
    typeface: font,
    fontSize: 22,
    color: colors.ink,
    autoFit: "shrinkText",
    ...style,
  };
  return shape;
}

function addRule(slide, top, color = colors.green) {
  slide.shapes.add({
    geometry: "rect",
    position: { left: 72, top, width: 1140, height: 5 },
    fill: color,
    line: { fill: "none", width: 0 },
  });
}

function addHeader(slide, title, slideNumber) {
  slide.background.fill = colors.paper;
  addText(slide, title, 72, 46, 1000, 54, { fontSize: 38, bold: true, color: colors.ink });
  addRule(slide, 116);
  addText(slide, `CS427  |  Zombie House 3D  |  ${slideNumber}`, 72, 682, 650, 20, { fontSize: 13, color: colors.muted });
}

function addPlaceholder(slide, { left, top, width, height, title, instruction }) {
  const frame = slide.shapes.add({
    geometry: "rect",
    position: { left, top, width, height },
    fill: colors.placeholder,
    line: { style: "solid", fill: colors.teal, width: 2 },
  });
  frame.text = `${title}\n\n${instruction}`;
  frame.text.style = {
    typeface: font,
    fontSize: 21,
    color: colors.teal,
    bold: true,
    alignment: "center",
    autoFit: "shrinkText",
  };
  return frame;
}

function addBulletList(slide, items, left, top, width, height, size = 23) {
  const text = items.map((item) => `• ${item}`).join("\n\n");
  return addText(slide, text, left, top, width, height, { fontSize: size, color: colors.ink });
}

function addAssetImage(slide, imageBytes, contentType, alt, left, top, width, height, fit = "contain") {
  return slide.images.add({
    blob: imageBytes,
    contentType,
    alt,
    fit,
    geometry: "roundRect",
    borderRadius: "rounded-xl",
    position: { left, top, width, height },
  });
}

// 1. Cover
{
  const slide = presentation.slides.add();
  slide.background.fill = colors.ink;
  slide.images.add({
    blob: images.menu,
    contentType: "image/jpeg",
    alt: "Zombie House 3D main menu artwork",
    fit: "cover",
    position: { left: 0, top: 0, width: W, height: H },
  });
  slide.shapes.add({
    geometry: "rect",
    position: { left: 0, top: 0, width: W, height: H },
    fill: "#071C33/75",
    line: { fill: "none", width: 0 },
  });
  addText(slide, "ZOMBIE HOUSE 3D", 78, 118, 720, 74, { fontSize: 54, bold: true, color: "#FFFFFF" });
  addText(slide, "A third-person 3D defense game", 82, 206, 610, 42, { fontSize: 28, color: "#E5F4E8" });
  addRule(slide, 278, colors.green);
  addText(slide, "CS427 - 3D Visualization and Game Development\nUniversity of Science, VNUHCM", 82, 312, 650, 80, { fontSize: 22, color: "#FFFFFF" });
  addText(slide, "Huynh Tran Uy  |  Tran My An  |  Nguyen Hoang Gia Han  |  Pham Bao Kha", 82, 613, 1000, 30, { fontSize: 18, color: "#FFFFFF" });
  slide.speakerNotes.textFrame.setText("Source: project asset Assets/Textures/MainMenuBG.jpg. Introduce the team and the central premise: defend the house against zombie waves.");
}

// 2. Game concept
{
  const slide = presentation.slides.add();
  addHeader(slide, "Game Concept", 2);
  addText(slide, "Protect the house while zombie waves advance.", 74, 155, 500, 80, { fontSize: 32, bold: true, color: colors.teal });
  addBulletList(slide, [
    "Move through a low-poly 3D map and fight nearby enemies.",
    "Spend sun resources to place plant defenders.",
    "Clear every wave before the house health reaches zero.",
  ], 76, 275, 470, 260, 22);
  addPlaceholder(slide, { left: 625, top: 160, width: 540, height: 360, title: "GAMEPLAY SCREENSHOT", instruction: "Insert a wide capture showing the player, house, and an active lane." });
  addText(slide, "Suggested capture: MapDay after the player spawns near the defended house.", 625, 545, 525, 46, { fontSize: 16, color: colors.muted });
  addAssetImage(slide, images.barbarian, "image/png", "Project character portrait", 432, 544, 96, 96);
  slide.speakerNotes.textFrame.setText("Explain the round objective. Screenshot placeholder: capture the active MapDay scene with player, house, and enemies in view. Character portrait source: project asset Assets/Data/Characters/Portraits/Barbarian_Portrait.png.");
}

// 3. Core gameplay loop
{
  const slide = presentation.slides.add();
  addHeader(slide, "Core Gameplay Loop", 3);
  addText(slide, "A round repeats one clear decision cycle.", 74, 150, 850, 45, { fontSize: 30, bold: true, color: colors.teal });
  const steps = [
    ["1", "Collect sun", "Fund the defense"],
    ["2", "Place plants", "Cover key lanes"],
    ["3", "Fight zombies", "Support the defenses"],
    ["4", "Protect the house", "Win after all waves"],
  ];
  steps.forEach(([n, title, detail], i) => {
    const left = 84 + i * 286;
    slide.shapes.add({ geometry: "ellipse", position: { left, top: 260, width: 64, height: 64 }, fill: colors.green, line: { fill: "none", width: 0 } });
    addText(slide, n, left, 271, 64, 38, { fontSize: 25, bold: true, color: colors.ink, alignment: "center" });
    addText(slide, title, left - 10, 347, 188, 40, { fontSize: 23, bold: true, color: colors.ink, alignment: "center" });
    addText(slide, detail, left - 10, 394, 188, 40, { fontSize: 17, color: colors.muted, alignment: "center" });
    if (i < 3) slide.shapes.add({ geometry: "rightArrow", position: { left: left + 185, top: 278, width: 58, height: 28 }, fill: colors.teal, line: { fill: "none", width: 0 } });
  });
  addText(slide, "Loss condition: house health reaches zero.", 75, 532, 500, 34, { fontSize: 21, color: "#A03D31", bold: true });
  addText(slide, "Win condition: every configured zombie wave is cleared.", 75, 582, 580, 34, { fontSize: 21, color: colors.teal, bold: true });
  slide.speakerNotes.textFrame.setText("Describe the core loop from resource collection through plant placement and combat. The final slide distinguishes the two round outcomes.");
}

// 4. Player interaction
{
  const slide = presentation.slides.add();
  addHeader(slide, "Player Interaction", 4);
  addPlaceholder(slide, { left: 72, top: 155, width: 660, height: 430, title: "GAMEPLAY SCREENSHOT", instruction: "Insert a third-person capture showing movement, camera angle, and melee range." });
  addText(slide, "Direct player control", 795, 173, 360, 46, { fontSize: 30, bold: true, color: colors.teal });
  addBulletList(slide, [
    "Third-person movement and camera control.",
    "Melee attacks for nearby zombie threats.",
    "Plant selection, placement, and removal.",
  ], 796, 250, 360, 250, 21);
  addText(slide, "Suggested capture: player attacking beside a plantable square.", 796, 538, 350, 46, { fontSize: 16, color: colors.muted });
  slide.speakerNotes.textFrame.setText("Show that the player actively participates rather than watching an automated defense. Screenshot placeholder: capture movement or a melee attack near a plantable square.");
}

// 5. Plants and economy
{
  const slide = presentation.slides.add();
  addHeader(slide, "Plants and Sun Economy", 5);
  addText(slide, "Plants provide the strategic defense layer.", 74, 150, 600, 46, { fontSize: 30, bold: true, color: colors.teal });
  addBulletList(slide, [
    "Collect sun as the resource for planting.",
    "Place defenders on valid plantable squares.",
    "Use Peashooter projectile attacks to pressure zombies.",
  ], 76, 232, 520, 250, 21);
  addAssetImage(slide, images.sunflower, "image/png", "Sunflower plant portrait", 92, 526, 96, 96);
  addAssetImage(slide, images.peashooter, "image/png", "Peashooter plant portrait", 210, 526, 96, 96);
  addPlaceholder(slide, { left: 666, top: 155, width: 500, height: 420, title: "GAMEPLAY SCREENSHOT", instruction: "Insert a capture of a planted Peashooter firing at approaching zombies." });
  slide.speakerNotes.textFrame.setText("Talk through the resource decision: sun enables placement, and plant positions determine lane coverage. Project asset portraits shown at lower left. Screenshot placeholder: show a Peashooter attacking zombies.");
}

// 6. Enemy waves
{
  const slide = presentation.slides.add();
  addHeader(slide, "Zombie Waves and Base Defense", 6);
  addPlaceholder(slide, { left: 72, top: 155, width: 650, height: 425, title: "GAMEPLAY SCREENSHOT", instruction: "Insert a capture with a zombie wave following a route toward the house." });
  addText(slide, "Enemy pressure", 790, 166, 350, 48, { fontSize: 30, bold: true, color: colors.teal });
  addBulletList(slide, [
    "Zombies follow routes toward the house.",
    "Health and melee attacks define each encounter.",
    "Spider and zombie variants extend the enemy set.",
    "Waves end when all spawned enemies are cleared.",
  ], 790, 242, 355, 280, 20);
  addAssetImage(slide, images.zombie, "image/png", "Cartoon Zombie project texture", 984, 535, 120, 80);
  slide.speakerNotes.textFrame.setText("Focus on the defensive tension: enemies approach the house, while the player and plants must remove them before base health reaches zero. Screenshot placeholder: a wave and the house on the same frame. Project zombie texture appears as a supporting asset.");
}

// 7. UI, minimap and audio
{
  const slide = presentation.slides.add();
  addHeader(slide, "Feedback: UI, Minimap, and Audio", 7);
  addText(slide, "The game communicates the state of each round through visible and audible feedback.", 74, 150, 1050, 48, { fontSize: 27, bold: true, color: colors.teal });
  addPlaceholder(slide, { left: 72, top: 235, width: 540, height: 345, title: "UI / MINIMAP SCREENSHOT", instruction: "Insert a capture that shows HUD, minimap, and a zombie marker." });
  addAssetImage(slide, images.minimap, "image/png", "Project minimap marker", 545, 510, 58, 58);
  addBulletList(slide, [
    "Minimap follows the player and identifies enemies.",
    "Win and lose panels close the round clearly.",
    "Separate music supports menus and gameplay maps.",
    "Sound effects reinforce UI, combat, plants, zombies, and results.",
  ], 694, 240, 460, 290, 20);
  slide.speakerNotes.textFrame.setText("Use this slide to tie together orientation and feedback. Screenshot placeholder: make a capture with minimap and HUD visible. Minimap marker source: project asset Assets/Minimap/location_maker.png.");
}

// 8. Maps and technical stack
{
  const slide = presentation.slides.add();
  addHeader(slide, "Maps and Technical Stack", 8);
  addText(slide, "Map variants", 76, 151, 400, 42, { fontSize: 30, bold: true, color: colors.teal });
  addBulletList(slide, [
    "Day, cloudy, and night scenes present the same defense concept with different moods.",
    "A tutorial map will introduce movement, planting, combat, removal, and objectives.",
  ], 76, 220, 480, 210, 21);
  addPlaceholder(slide, { left: 76, top: 465, width: 490, height: 118, title: "MAP SCREENSHOT", instruction: "Insert a wide capture of one final map variant." });
  addText(slide, "Technology", 705, 151, 420, 42, { fontSize: 30, bold: true, color: colors.teal });
  addBulletList(slide, [
    "Unity 6.3 LTS with Universal Render Pipeline.",
    "C# gameplay systems for input, waves, health, economy, UI, and audio.",
    "Git and GitHub branches for team integration.",
  ], 705, 220, 430, 230, 21);
  slide.speakerNotes.textFrame.setText("Summarize how the team uses Unity 6.3 LTS, URP, C#, and GitHub. Screenshot placeholder: pick the strongest final map scene.");
}

// 9. Team and next work
{
  const slide = presentation.slides.add();
  addHeader(slide, "Team and Final Integration", 9);
  addText(slide, "Team", 76, 152, 300, 42, { fontSize: 30, bold: true, color: colors.teal });
  addText(slide,
    "Huynh Tran Uy\nCore gameplay and zombie systems\n\nTran My An\nMaps, menu, character selection, and win/lose UI\n\nNguyen Hoang Gia Han\nAudio, integration support, QA, and report\n\nPham Bao Kha\nMinimap, organization, and plantable squares",
    76, 220, 490, 350, { fontSize: 19, color: colors.ink });
  addText(slide, "Final integration", 686, 152, 400, 42, { fontSize: 30, bold: true, color: colors.teal });
  addBulletList(slide, [
    "Bring plantable squares, minimap, waves, house, and result UI into the main maps.",
    "Complete the tutorial scene and test the full route from Main Menu to game result.",
    "Balance waves, plant costs, enemy health, and audio levels through playtests.",
  ], 686, 224, 460, 270, 20);
  addText(slide, "Thank you", 688, 555, 420, 58, { fontSize: 42, bold: true, color: colors.green });
  slide.speakerNotes.textFrame.setText("Close by assigning the team roles and emphasizing the remaining integration and testing work before the deadline.");
}

const candidatePath = path.join(workspaceDir, ".codex-finalizer", "zombie-house-3d-candidate.pptx");
await (await PresentationFile.exportPptx(presentation)).save(candidatePath);

const requirements = {
  requiredNativeTableOwnerSlides: [],
  requiredNativeChartOwnerSlides: [],
};
const result = await finalizePresentation({
  ...requirements,
  workspaceDir,
  candidatePath,
  finalPath: finalPptx,
  pythonExecutable: runtimePython,
  integrityValidatorPath: path.join(skillDir, "container_tools", "inspect_presentation_package_integrity.py"),
  layoutValidatorPath: path.join(skillDir, "container_tools", "inspect_presentation_layout_geometry.py"),
  layoutArgs: ["--expected-slide-size-emu", "12192000,6858000", "--validate-bullet-geometry", "--validate-heading-fit"],
  fontPolicy: { basis: "design", families: [font] },
  verifyArtifactToolImport: true,
  receiptPath: path.join(workspaceDir, ".codex-finalizer", "zombie-house-3d-presentation.validation.json"),
});
console.log(JSON.stringify({ finalPptx, font, result }, null, 2));
