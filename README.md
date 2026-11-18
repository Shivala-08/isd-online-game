# Whack-a-Mole 3D

A short single-player 3D arcade game made in Unity. Moles pop out of a 3x3 grid of holes
and you hit as many as you can before the 30-second clock runs out. Bombs hide in the grid
too: hit one and you lose two points.

The project also contains an in-game Q-learning bot with a live Q-table view, a bot that
imitates recorded human play, and a Cardboard VR mode that lets you hit moles by looking at
them. The mole and the bomb are low-poly models made in Blender.

![Gameplay](Screenshots/scene_20261118_153229.png)

## What is in the game

**Playing**

- A 3x3 grid of holes on a wooden-bordered board, with a leaning "WHACK-A-MOLE" sign.
- Moles rise from a random free hole every 1.0 to 1.5 seconds, stay for a moment, then sink
  back. A hit is worth +1; a bomb is worth -2 and the score never drops below zero.
- A 30-second round. The HUD shows the score, the time left and the best score, and the
  timer turns red and pulses in the last five seconds.
- The game gets harder as the round goes on: moles stay up for less time near the end.
- Game over shows the final score and the best score, with a Restart button. The best score
  is saved between sessions.

**Feel**

- Moles stretch as they pop out and squash flat when they are hit.
- Hits throw sparks and a floating "+1"; bombs explode with a camera shake and a "-2";
  a new best score gets a confetti shower.
- Dust puffs when a mole starts to rise, four sound effects (pop, hit, bomb, cheer), and
  physics: the hit mole is thrown off the board instead of sinking politely.
- The hammer is a real Rigidbody: `Space` lifts and drops it, and whatever is under the head
  is hit.

**Controls**

| Input | Action |
| --- | --- |
| Left click / tap | Hit the mole under the pointer |
| `Space` | Swing the physics hammer |
| `W A S D` | Move the hammer |
| `C` | Cycle camera: angled, top-down, close |
| `P` / `Esc` | Pause and resume |
| `R` | Restart the round |
| HUD buttons | Camera, day/night lighting, mole colour, pause |

**Scene controls**

- **Day / night button** switches the sun colour and intensity, the ambient light, the fog,
  the skybox exposure and turns the point and spot lamps on only at night.
- **Colour button** repaints every mole through a palette of four body materials.

**Learning bots** (see `docs/DESIGN.md`)

- Q-learning bot: trains inside the game on a 9-hole observation and 10 actions, with a
  Q-table heat map, episode count and epsilon on screen.
- Imitation bot: records the player's decisions, trains a small model and then plays by
  copying them.

**VR**

- Cardboard stereo view with head tracking and gaze selection (a centre reticle that fills
  over half a second). Testable in the Editor with mouse look.

## Requirements

| Item | Version |
| --- | --- |
| Unity Editor | 6000.6.9f1 |
| Render pipeline | Universal Render Pipeline 17.6.0 |
| UI | uGUI 2.6.0 + TextMeshPro |
| Input | Input System 1.20.1 |
| Blender (only to rebuild the models) | 5.2 LTS |

## How to run

1. Open the project folder with Unity Hub using editor **6000.6.9f1**.
2. Open `Assets/Scenes/Game.unity`.
3. Press **Play**.

If the scene, materials or prefabs are ever missing, rebuild them in one click:
**Whack-a-Mole → Build All**. The menu rebuilds the materials, the prefabs, the whole
scene and the HUD, so the game is playable again immediately.

## Project layout

```
Assets/
  Scenes/      Game.unity
  Scripts/
    Core/      GameManager, MoleSpawner
    Gameplay/  Mole, Bomb, HoleOccupant, Hammer, HitRaycaster, HitEffects,
               CameraCycler, CameraShaker
    UI/        HUDController, UIController, LightPanel, MaterialSwapper, ScorePopup
    AI/        MoleEnv, QLearningAgent, QTableView, DemoRecorder, ImitationBot
    VR/        GazePointer
    Editor/    SceneBuilder, ScreenshotTool
  Prefabs/     Mole, Bomb, Hole, HitBurst, DustPuff, Explosion, Confetti, Hammer, ScorePopup
  Prefabs/UI/  StartCard, PauseCard, GameOverCard
  Materials/   URP Lit materials for every surface and effect
  Models/      Mole.fbx, Bomb.fbx, Characters.blend
  Audio/       SFX_Pop, SFX_Hit, SFX_Bomb, SFX_Cheer
  Settings/    URP assets, renderer and volume profile
docs/DESIGN.md          game design, gameplay rules, AI and VR notes, test plan
tools/build_models.py   builds and exports the Blender models
tools/make_sfx.py       synthesises the four sound effects
Screenshots/            report evidence
```

## Rebuilding the models and sounds

```
/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/build_models.py
python3 tools/make_sfx.py
```

The model script rewrites `Assets/Models/*.fbx` plus four preview renders (solid and
wireframe for each model); run **Whack-a-Mole → Build All** afterwards to put the new meshes
into the prefabs. If a model file is missing, the builder falls back to primitive shapes, so
the project still builds.

## Screenshots

| File | Shows |
| --- | --- |
| `scene_20261118_153229.png` | Day play, hit sparks and the floating "+1" |
| `scene_20261118_153049.png` | Night lighting with the lamps on |
| `scene_20261118_155536.png` | The Blender mole in game |
| `scene_20261118_155554.png` | The Blender bomb in game |
| `model_mole.png`, `model_mole_wireframe.png` | Mole model and its wireframe |
| `model_bomb.png`, `model_bomb_wireframe.png` | Bomb model and its wireframe |
| `hud_normal.png`, `hud_danger.png`, `hud_gameover.png` | HUD states |

## Notes

- **Graphics:** everything uses URP shaders only. A material's colour is set on
  `_BaseColor`; `_Color` belongs to the built-in pipeline and does nothing here.
- **Input:** the project uses the new Input System only, so pointer and keyboard state are
  read through `Mouse.current`, `Touchscreen.current` and `Keyboard.current`.
- **Sounds:** the four effects are generated by `tools/make_sfx.py`, so no audio package is
  needed.
- **Editor background:** on macOS the Editor may pause the Play-mode loop while its window
  is not frontmost. Focus the Unity window before starting a round.
