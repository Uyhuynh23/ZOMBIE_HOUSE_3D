# WebGL multiplayer test and Vercel deployment

## 1. Unity prerequisites

- Open the project with Unity `6000.3.16f1`.
- Confirm the project is linked to Unity Cloud in **Edit → Project Settings → Services**.
- In the Unity Dashboard for cloud project `a11639a8-8cc0-4901-8cef-afdf9535c444`, enable Multiplayer/Lobby and Relay for the development environment.
- In **File → Build Profiles**, select **Web**. The enabled scenes must start with `MainMenu`, followed by `Map_Day`, `Map_Cloudy`, `Map_Night`, and `Map_Tutorial`.

## 2. Create the WebGL build

1. Select **File → Build Profiles → Web → Switch Platform**.
2. For a first test, enable **Development Build** so the browser console contains useful `[NET]` logs.
3. Select **Build** and choose `<project>/Builds/WebGL`.
4. Do not open `index.html` directly. WebAssembly must be served over HTTP(S).

For deployment, disable **Development Build**, keep **Compression Format = Brotli** and **Decompression Fallback = Off**, then build to `<project>/Builds/WebGL-Vercel`.

The current project uses Relay over secure WebSockets for online WebGL. Solo intentionally stays local and does not start a UDP host in the browser.

## 3. Test locally

From PowerShell in the project root:

```powershell
python -m http.server 8765 --directory Builds/WebGL
```

Open `http://localhost:8765` in two independent browser profiles. Chrome and Edge are the simplest pair. Two normal tabs of the same browser profile may share Unity anonymous-auth storage, so they are not a reliable two-player test. Port `8080` is already occupied by MiniTool ShadowMaker on the machine used for this verification.

Host browser:

1. Open **Play Game**.
2. Select a character and Round 1.
3. Click **Host**.
4. Wait for a six-character room code and send it to the guest.
5. After the roster reads `2/2`, click **Start**.

Guest browser:

1. Open **Play Game**.
2. Select a character.
3. Enter the host room code and click **Join**.
4. Wait for `2/2`; only the host can start.

Expected result:

- Both browsers load the same map and reach `Intro`, then `Playing`.
- Each browser controls only its own avatar and follows it with one gameplay camera/minimap.
- Enemy, plant, Sun, HP, wave, and Win/Lose state agree on both clients.
- Press `F8` for the network overlay and `F9` to write a diagnostic snapshot to the browser console.

## 4. Deploy the existing build to Vercel

Current production deployment:

- Project: `zombie-house-3d-webgl`
- URL: <https://zombie-house-3d-webgl.vercel.app>

Login once (this requires completing Vercel authentication yourself):

```powershell
vercel login
```

Deploy a preview:

```powershell
vercel link --cwd Builds/WebGL-Vercel --yes --project zombie-house-3d-webgl --scope michaelpham2005s-projects
vercel deploy --cwd Builds/WebGL-Vercel --yes --local-config Deploy/vercel.json
```

Deploy production:

```powershell
vercel deploy --cwd Builds/WebGL-Vercel --prod --yes --local-config Deploy/vercel.json
```

The config supplies long-lived cache headers for hashed Unity build assets and correct `Content-Type`/`Content-Encoding` headers for Brotli or gzip builds.

Vercel Hobby currently limits CLI static uploads to 100 MB total. If the generated WebGL directory exceeds that limit, use a Pro project, reduce/stream assets, or deploy the build to a game-oriented static host such as itch.io instead.

After a release build has been verified and deployed, delete the older build directory only after confirming its absolute path is inside this project. Keep `Builds/WebGL-Vercel` as the current deployable artifact.

## 5. Two-device Relay test

- Use the same HTTPS Vercel URL on both laptops, preferably on different networks for the final Relay check.
- Use different browser profiles/devices so anonymous authentication identities differ.
- Record the room code, both browser console logs, role/client IDs, RTT from `F8`, and the result of guest-disconnect and host-disconnect tests.
- A physical two-laptop pass is required before marking the Phase 3 release gate complete.
