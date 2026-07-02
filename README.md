[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/funding)
[![Latest Release](https://img.shields.io/github/v/release/hmlendea/backgammon-by-horatiu)](https://github.com/hmlendea/backgammon-by-horatiu/releases/latest)
[![Build Status](https://github.com/hmlendea/backgammon-by-horatiu/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/backgammon-by-horatiu/actions/workflows/dotnet.yml)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://gnu.org/licenses/gpl-3.0)

# Backgammon, by Horațiu!

Open-source cross-platform reimplementation of one of the oldest backgammon games for Windows, built with MonoGame and .NET 10.

![Preview screenshot](preview.png)

## Features

- **Standard backgammon rules** — 24-point board, 15 pieces per player, bar and bearing-off areas
- **Single-player vs AI** — play as white (Player 1) against a computer-controlled brown opponent
- **Animated piece movement** — smooth sprite-based animation for every move
- **Drag-and-drop interaction** — click to pick up a piece, then click a valid destination to place it
- **Legal-move highlighting** — valid target columns are highlighted after selecting a piece
- **Dice rolling** — roll dice when no moves are available by clicking the centre dice area
- **Undo** — revert the last move at any time
- **Reset** — start a fresh game from the opening position
- **Context-sensitive cursor** — cursor changes to reflect the current interaction state (pointer, pick up, grab, open hand, dice)
- **Splash screen** — animated logo on launch, dismissible by any input
- **Fullscreen and windowed modes** — configurable at start-up

## Gameplay

You play as **white** (moving from high-numbered to low-numbered columns). The AI plays as **brown** (moving in the opposite direction).

Each turn:
1. Dice are rolled automatically at the start of your turn.
2. Click a white piece to select it. Legal destinations are highlighted.
3. Click a highlighted column to move the piece there.
4. Repeat until all dice values are used (or no moves remain).
5. If you have no legal moves you can click the dice to pass.

Pieces sent to the **bar** must re-enter from the opponent's home board before any other moves are made. Once all 15 of your pieces have moved past column 0 you can begin **bearing off**.

The first player to bear off all 15 pieces wins.

### AI

The computer opponent uses a **minimax search** with a transposition table (memoisation) to evaluate full move sequences. The position evaluator accounts for:

- **Pip count** — distance remaining to bear off
- **Point control** — bonuses for owning anchors, home-board points, and outer points
- **Primes** — bonus for consecutive owned points (4-, 5-, and 6-primes)
- **Blot threats** — penalties for exposed single pieces within direct or combined dice reach of the opponent
- **Bar pieces** — penalties for pieces on the bar and bonuses for putting the opponent on the bar

All weights and thresholds are adjusted automatically based on the current **game phase**:
| Phase | Condition |
|-------|-----------|
| Racing | AI leads by more than 15 pips |
| Blocking | Pip difference between −15 and +40 |
| Back game | AI trails by more than 40 pips |

## Installation

### Flatpak

[![Get it from FlatHub](https://raw.githubusercontent.com/hmlendea/readme-assets/master/badges/stores/flathub.png)](https://flathub.org/apps/details/io.github.hmlendea.BackgammonByHoratiu)

### Prebuilt releases

Download the latest packaged build from the [GitHub releases page](https://github.com/hmlendea/backgammon-by-horatiu/releases/latest).

## Running From Source

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- MonoGame content build tools (`dotnet-mgcb`) — required to rebuild game assets
- TrueType core fonts — required for font rendering on Linux (`fonts-freefont-ttf` or equivalent)

All NuGet dependencies (MonoGame, NuciXNA) are restored automatically by `dotnet restore`.

On Ubuntu the CI workflow installs the missing tools with:

```bash
dotnet tool install --global dotnet-mgcb
sudo apt-get install -y fonts-freefont-ttf
```

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run --project BackgammonByHoratiu
```

### Test

```bash
dotnet test
```

### Release

The repository includes `release.sh`, which delegates to the upstream deployment script used by the project maintainer.

```bash
bash ./release.sh 1.0.0
```

This script downloads and executes an external release helper from `https://raw.githubusercontent.com/hmlendea/deployment-scripts/master/release/dotnet/10.0.sh`.

**Note:** Always review any external script before piping it into `bash`.

## Project Structure

```
BackgammonByHoratiu/
├── Entities/              # Pure data models (Table, Player, Piece, GameSnapshot, Dice)
├── GameLogic/
│   ├── GameManagers/      # Rules engine (GameManager) and AI coordinator (AiGameManager)
│   └── AI/
│       ├── BackgammonAi.cs          # Move planner and execution queue
│       ├── BoardLayout.cs           # Board constants (columns, home-board ranges)
│       ├── Evaluation/              # Position scoring (PositionEvaluator, AiWeights, ThreatCalculator)
│       └── Search/                  # Minimax search with transposition table (MoveSearcher)
├── Gui/
│   ├── Screens/           # SplashScreen and GameplayScreen
│   └── Controls/          # GuiGameBoard, GuiButton, FramerateCounter
├── Settings/              # Constants and configuration (GameDefines, GraphicsSettings, AudioSettings)
├── Program.cs             # Entry point
└── GameWindow.cs          # MonoGame window and screen-manager setup

BackgammonByHoratiu.UnitTests/
├── Entities/              # Unit tests for Table, Player, Piece
└── GameLogic/             # Unit tests for game managers, AI evaluator, and search
```

### Dependencies

| Package | Purpose |
|---------|---------|
| `MonoGame.Framework.DesktopGL` | Cross-platform 2D rendering, input, and audio |
| `MonoGame.Content.Builder.Task` | Asset pipeline (fonts, textures, sprites) |
| `NuciXNA.Graphics` | Sprite and animation abstraction |
| `NuciXNA.Gui` | Screen and control framework |
| `NuciXNA.Input` | Keyboard and mouse input abstraction |
| `NuciXNA.Primitives` | Geometric types (`Point2D`, `Size2D`, …) |
| `NuciXNA.DataAccess` | Content loading utilities |
| `NuciDAL` | Data-access base library |

## Contributing

Contributions are welcome.

Please:

- Keep changes cross-platform (Windows, Linux, macOS via MonoGame DesktopGL)
- Keep pull requests focused and consistent with the existing code style
- Update documentation when behaviour changes
- Add or update unit tests for any new or modified logic

## Links

- [Latest release](https://github.com/hmlendea/backgammon-by-horatiu/releases/latest)
- [FlatHub release](https://flathub.org/apps/details/io.github.hmlendea.BackgammonByHoratiu)
- [FlatHub repository](https://github.com/flathub/io.github.hmlendea.BackgammonByHoratiu)

## License

Licensed under the GNU General Public License v3.0 or later.
See [LICENSE](./LICENSE) for details.