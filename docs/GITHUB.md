# GitHub listing settings

The repository metadata lives in GitHub, not in the code. This file records what is set so
it can be re-applied if the repo is ever recreated or renamed.

## Description

> Open source 3D whack-a-mole arcade game built in Unity 6 with URP and C#, plus Q-learning
> and imitation-learning bots and a Google Cardboard VR mode.

## Topics

`unity` `unity3d` `unity-6` `game-development` `csharp` `urp` `universal-render-pipeline`
`arcade-game` `whack-a-mole` `3d-game` `reinforcement-learning` `q-learning`
`imitation-learning` `game-ai` `vr` `google-cardboard` `blender` `low-poly`
`particle-effects` `mobile-game`

Apply them with:

```bash
gh repo edit Shivala-08/isd-online-game \
  --description "Open source 3D whack-a-mole arcade game built in Unity 6 with URP and C#, plus Q-learning and imitation-learning bots and a Google Cardboard VR mode." \
  --add-topic unity --add-topic unity3d --add-topic unity-6 --add-topic game-development \
  --add-topic csharp --add-topic urp --add-topic universal-render-pipeline \
  --add-topic arcade-game --add-topic whack-a-mole --add-topic 3d-game \
  --add-topic reinforcement-learning --add-topic q-learning --add-topic imitation-learning \
  --add-topic game-ai --add-topic vr --add-topic google-cardboard --add-topic blender \
  --add-topic low-poly --add-topic particle-effects --add-topic mobile-game
```

## Social preview

`docs/social-preview.png` is the 1280x640 social preview image. GitHub has no API for it, so
upload it by hand: **Settings → Social preview → Edit → Upload an image**.

## Search and discovery notes

- GitHub indexes the repository name, the description, the topic list and the README text,
  so the same keywords (Unity, URP, C#, whack-a-mole, Q-learning, Cardboard VR, Blender) are
  used in all four places.
- The README leads with a keyword-rich H1 and summary paragraph, has a table of contents,
  one `##` heading per concept, and descriptive `alt` text on every screenshot.
- `.gitattributes` marks bundled packages as `linguist-vendored`, so the language bar on the
  repo page reports this project's own code rather than third-party DLLs and Unity packages.
