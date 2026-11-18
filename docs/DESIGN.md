# Whack-a-Mole 3D — Game Design and Technical Overview

A short single-player 3D arcade game built in Unity. Moles pop out of a 3x3 grid of holes
and the player hits as many as possible before the clock runs out. On top of the arcade
loop the project demonstrates material and light control, particle physics, two small
learning bots (Q-learning and imitation learning) and a Cardboard VR mode with gaze
selection.

- Engine: Unity 6000.6.9f1, Universal Render Pipeline 17.6.0
- Language: C#
- UI: uGUI with TextMeshPro
- Models: authored in Blender 5.2 and exported as FBX
- Input: Unity Input System (mouse, touch, keyboard)

---

## 1. Overview

| Item | Value |
| --- | --- |
| Genre | Single-player arcade / reflex |
| Round length | 30 seconds |
| Board | 3x3 holes on a 12x12 ground plane |
| Scoring | Mole +1, bomb -2 (score never drops below 0) |
| Difficulty | The stay time shrinks as the round progresses |
| Persistence | High score in `PlayerPrefs` |
| Platforms | Windows/macOS Editor build, Android (Cardboard VR) |

## 2. Goals and non-goals

**Goals**

- A complete loop: menu, play, pause, game over, restart, with no console errors.
- Every feature demonstrable in under five minutes.
- Simple low-poly visuals that hold 60 FPS in the Editor and 30 FPS or more on a
  mid-range phone.
- Code the reader can follow: one responsibility per script, comments in plain language,
  values exposed with `[SerializeField]` instead of hard-coded lookups.

**Non-goals**

- Multiplayer, online leaderboards, accounts.
- Photorealistic art.
- Production-grade machine learning; the bots are teaching demonstrations.

## 3. Features

### Core gameplay

- A 3x3 grid of layered holes (dirt mound, light rim, dark opening) created by a nested loop.
- One mole at a time rises from a random free hole, stays, then sinks back if it is not hit.
- A hit adds 1 point and removes the mole; a click on the ground scores nothing.
- A 30-second countdown starts on Play; the HUD shows score, time left and high score.
- Game over shows the final score and the best score, with a Restart button.
- Bombs spawn with a fixed probability; hitting one costs 2 points and never goes below 0.
- Difficulty ramps up: the stay time lerps from 1.4 s to 0.6 s across the round, and the
  spawn gap is a random 1.0 to 1.5 s.

### Polish and presentation

- Squash and stretch on a mole as it leaves the hole and when it is hit.
- Particle bursts on a hit, dust when something rises, an explosion for a bomb, confetti
  for a new record.
- Floating "+1" / "-2" score labels that face the camera and fade out.
- Camera shake on a bomb.
- Physics: a Rigidbody hammer that drops under gravity, and a hit mole that is thrown away
  with a Rigidbody instead of sinking politely.
- Four synthesised sound effects: pop, hit, bomb, cheer.

### Controls and camera

- Mouse click or touch to hit; `Space` swings the physics hammer; `WASD` moves it.
- `C` cycles three camera views (angled, top-down, close) with a smooth blend.
- `P` or `Esc` pauses, `R` restarts.
- HUD buttons for camera, day/night lighting, mole colour and pause.

### Scene control (Week 1 topics)

- Materials: a URP Lit material set, plus an in-game button that repaints every mole
  through a small palette (brown, blue, pink, green).
- Lights: one directional light with shadows plus a point lamp and a spot lamp. A single
  button switches day and night (sun colour, intensity, ambient, fog and skybox exposure,
  and the two lamps turn on only at night).
- Prefabs for mole, bomb, hole, hammer, effects and the three menu cards.

### Learning demonstrations (Week 3 and 4 topics)

- A tabular Q-learning bot that trains inside the game, with a Q-table heat map, episode
  counter, average reward and epsilon on screen.
- A record-and-imitate bot: the player's (observation, action) pairs are logged, a simple
  model is trained on them, and the bot plays by copying the prediction.
- Optional stretch: a Unity ML-Agents agent with a PPO trainer configuration.

### Cardboard VR

- Split-screen stereo view with head tracking from the phone sensors.
- Gaze selection: a centre raycast with a 0.5 s dwell timer and a filling reticle.
- Mouse look in the Editor for quick testing, phone test in a Cardboard viewer.

### Models

- Mole and bomb modelled in Blender as low-poly meshes with simple materials.
- Origin at the bottom centre, scale applied, exported as FBX with Apply Transform.
- If the FBX files are missing, the editor builder falls back to primitive shapes.

## 4. Scene specification

- **Ground:** 12 x 12 plane with a grass material, surrounded by a wooden rail.
- **Holes:** nine hole prefabs with x and z each in {-3, 0, 3}, parented under `Holes`.
- **Mole / bomb prefabs:** imported Blender meshes under a `Visual` child (so squash and
  stretch never touches the collider), a collider on the root and the gameplay script.
- **Camera:** angled view at (0, 6.5, 6) with a 50 degree pitch and 45 degree field of view.
  Two more views are stored as transforms and blended to by the cycler.
- **Lights:** directional sun with shadows, one point lamp and one spot lamp over the board.
- **Sign:** a wooden "WHACK-A-MOLE" board leaning towards the camera.
- **UI:** HUD canvas with score/time/high score, a control strip, and the start, pause and
  game over cards as prefabs.

## 5. Scripts

| Script | Responsibility |
| --- | --- |
| `Core/GameManager` | State machine (Menu, Playing, Paused, GameOver), score, timer, restart, high score |
| `Core/MoleSpawner` | Picks a free hole and spawns a mole or a bomb on a random gap |
| `Gameplay/HoleOccupant` | Shared rise / stay / fall life cycle, squash and stretch, knock-away physics |
| `Gameplay/Mole` | `+1` scoring and the mole's own effects |
| `Gameplay/Bomb` | `-2` penalty and the explosion |
| `Gameplay/Hammer` | Pointer following, `WASD` nudge, physics drop on `Space` |
| `Gameplay/HitRaycaster` | Pointer ray hits a hittable object once |
| `Gameplay/HitEffects` | Owns particle prefabs, sounds and score labels |
| `Gameplay/CameraShaker` | Temporary camera offset that undoes itself every frame |
| `Gameplay/CameraCycler` | Blends between saved camera views on `C` |
| `UI/HUDController` | Score, time, high score, the last-five-seconds warning |
| `UI/UIController` | Shows and hides the menu cards, pause and restart keys |
| `UI/LightPanel` | Day and night lighting |
| `UI/MaterialSwapper` | Cycles the mole body material |
| `UI/ScorePopup` | Floating score label that faces the camera |
| `AI/MoleEnv`, `AI/QLearningAgent`, `AI/QTableView` | Q-learning environment, learner and heat map |
| `AI/DemoRecorder`, `AI/ImitationBot` | Recording the player and copying the recording |
| `VR/GazePointer` | Centre-screen gaze with a dwell timer |
| `Editor/SceneBuilder` | Menu item that rebuilds materials, prefabs, the scene and the HUD |

## 6. Gameplay logic

- **Spawning:** a timer runs down; at zero the spawner picks a random hole that has no
  occupant, instantiates a mole (or a bomb with probability `bombChance`), and resets the
  timer to a random gap.
- **Difficulty:** `progress = 1 - timeLeft / roundTime`, then `stay = Lerp(1.4, 0.6, progress)`.
- **Life cycle:** Rising (0.25 s) -> Up (`stay`) -> Falling (0.25 s) -> destroyed. A hit only
  counts during Rising (once the body has cleared the ground) and Up, and only once.
- **Hit detection:** a ray from the camera through the pointer, or from the screen centre in
  VR. Hitting a mole scores and plays the effects; hitting a bomb explodes it.
- **Game over:** at zero time the field is cleared, the best score is compared and saved,
  and the game over card is shown. Restart resets all counters and resumes time.

## 7. AI design

**Environment.** Observation: nine hole values (0 empty, 1 mole, -1 bomb) plus normalised
time left. Actions: ten discrete choices (hit hole 0 to 8, or wait). Rewards: +1 for a mole,
-1 for a bomb, -0.05 for an empty hole, -0.001 per step so the bot prefers acting early.

**Q-learning.** The state key is built from the nine hole values and stored in a dictionary
of `Q[state, action]`. Update rule: `Q += alpha * (reward + gamma * maxQ(next) - Q)` with
alpha 0.1, gamma 0.95 and epsilon decaying from 1.0 to 0.05. Training runs many simulated
steps per frame while a speed slider is raised, then the Watch button lets the trained bot
play the real scene. The UI shows the Q-table as a heat map so the learning is visible.

**Imitation learning.** Every human hit records the (observation, action) pair. A simple
classifier is trained on the log and the bot picks the predicted action at each decision
step; the report compares the bot's score with the human's.

**ML-Agents (optional).** An `Agent` subclass with `CollectObservations`,
`OnActionReceived` and `Heuristic`, a `BehaviorParameters` component and a PPO trainer YAML
(learning rate, batch size, buffer size, epochs, epsilon, beta, lambda, hidden units,
layers). Training uses `mlagents-learn`, then the exported model becomes the bot's brain.

## 8. VR design

- Google Cardboard XR Plugin, enabled through XR Plug-in Management for Android.
- A VR camera rig provides head tracking; the stereo split comes from the XR settings.
- Gaze selection: a centre raycast with a 0.5 s dwell time and a reticle that fills; a tap
  or the Cardboard button can act as a trigger.
- The same gaze code runs in the Editor with mouse look, which is how it was tested.

## 9. Blender pipeline

1. Model the mole (UV-sphere body, ears, eyes with pupils, nose) and the bomb (shell, band,
   fuse, spark), low poly, with simple materials.
2. Set the origin at the bottom centre and apply the scale.
3. Export as FBX with Apply Transform, axes configured for Unity.
4. Copy the files into `Assets/Models` and rebuild the prefabs from
   **Whack-a-Mole → Build All**; the colliders are sized to the imported meshes.
5. Screenshots of the model, its wireframe and the model in Unity are in `Screenshots`.

## 10. Build and delivery

- Editor build: open `Assets/Scenes/Game.unity` and press Play.
- Android: switch the platform, set the package name and the minimum API level required by
  the Cardboard plugin, then build the APK.
- Submission: repository, APK or recorded demo, screenshots, report, README.

## 11. Test plan

| ID | Test | Expected result |
| --- | --- | --- |
| T1 | Open the project on a clean clone | Compiles with no errors |
| T2 | Play a round | Timer counts 30 to 0, moles spawn in random holes |
| T3 | Hit a mole | +1 score, particle burst, floating label, mole thrown away |
| T4 | Click empty ground | No score change |
| T5 | Hit a bomb | Explosion, camera shake, score drops but never below 0 |
| T6 | Timer reaches 0 | Game over card with the correct score |
| T7 | Beat the high score and restart the Editor | High score persists |
| T8 | Press `C` | The camera blends to the next view |
| T9 | Toggle night and the mole colour | Scene updates immediately |
| T10 | Train the Q-learning bot | Average reward rises, the heat map fills |
| T11 | Record, train and watch the imitation bot | The bot scores above zero and copies the human |
| T12 | VR mode in the Editor | Gaze fills the reticle and hits a mole |
| T13 | VR mode on a phone | Tilting rotates the view, gaze hits work |
| T14 | Remove the model files and rebuild | The prefabs fall back to primitives without errors |

## 12. Notes

- The deprecated GVR SDK is replaced by the Google Cardboard XR Plugin; the concepts (head
  pose, reticle pointer, trigger) are the same.
- ML-Agents and PPO are optional: they need a matching Python version and PyTorch.
- Package versions are pinned in the README so the project opens the same way on any machine.
