RankHub Leaderboard

RankHub Leaderboard is a plug-and-play leaderboard SDK for Unity games. It allows you to submit player scores, retrieve rankings, and display leaderboards with minimal setup — no backend required.

The SDK is backend-agnostic and can be connected to any compatible API. An example integration is included for quick setup and testing.

> ⚠️ This asset is subject to the Unity Asset Store End User License Agreement (EULA). External backend services are optional and not required to use the SDK.



Features

Ready-to-use UI — Prefab with leaderboard, name editing, and score simulation
Automatic player identification — Unique `player_id` stored in PlayerPrefs
Live leaderboard — Paginated display with current player highlighting
Name editing — Players can change their display name
Score simulation — Built-in +10 button for testing
Persistent data — Player name and best score saved locally
Multi-platform — Works on all Unity-supported platforms



Quick Start

1. Add the Manager

Drag the `RankHubManager` prefab into your first scene, OR
Add the `RankHubManager` component to any GameObject



2. Configure API

In the Inspector, set:

API Key — Identifies your leaderboard
Base URL — Your backend endpoint

> You can use your own backend or any compatible API. An example endpoint can be used for testing.



3. Add the UI

Drag the `RankHubCanvas` prefab into your scene
Assign references in the Inspector:

  Name HUD
  Score HUD
  Player Name Input
  Leaderboard Container
  Score Entry Prefab
  Buttons (Submit, Refresh, Next, Prev, etc.)



4. Run the Demo

Open `RankHub_Demo`
Test score submission, name editing, and leaderboard navigation



How It Works

Player Flow

1. First launch — Player gets a default name
2. Add Score — Local score increases
3. Submit — Sends score to backend
4. Edit Name — Updates display name



Backend Integration

`player_id` is generated and persisted automatically
Scores update only if higher than previous best
Clear feedback messages for each result



Compatibility

This SDK works with any leaderboard backend that supports score submission and retrieval.



Documentation

For full documentation, visit:
https://leaderboard-game.vercel.app/docs_api.html



Contributing

Report issues
Suggest features
Submit pull requests



Contact

Email: [Guardabarrancoestudioapp@gmail.com](mailto:Guardabarrancoestudioapp@gmail.com)
Website: https://leaderboard-game.vercel.app



Made with ❤️ for indie game developers
