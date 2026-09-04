# Audio drop folder

The game loads these clips at runtime. Keep the names exactly as listed; Unity can import `.ogg`, `.wav`, or `.mp3` files.

```
Assets/Resources/Audio/Music/MainMenuMusic.ogg
Assets/Resources/Audio/Music/GameplayMusic.ogg
Assets/Resources/Audio/SFX/UiClick.ogg
Assets/Resources/Audio/SFX/PlayerAttack.ogg
Assets/Resources/Audio/SFX/PeashooterShot.ogg
Assets/Resources/Audio/SFX/ProjectileHit.ogg
Assets/Resources/Audio/SFX/ZombieAttack.ogg
Assets/Resources/Audio/SFX/ZombieDeath.ogg
Assets/Resources/Audio/SFX/SunCollect.ogg
Assets/Resources/Audio/SFX/PlantPlaced.ogg
Assets/Resources/Audio/SFX/PlantRemoved.ogg
Assets/Resources/Audio/SFX/HouseHit.ogg
Assets/Resources/Audio/SFX/Win.ogg
Assets/Resources/Audio/SFX/Lose.ogg
```

`AudioManager` is created automatically and is preserved while scenes change. It plays `MainMenuMusic` only in `MainMenu` and `GameplayMusic` in every map scene. Missing clips are optional: the game remains playable and reports each missing name only once in the Console.

The extension does not matter to the loader. For example, `MainMenuMusic.mp3` and `GameplayMusic.mp3` also work.
