
# Fantasy Console War: Emoji Battle Royale

**Fantasy Console War** is a real-time, text-based multiplayer battle arena where players control emoji warriors in a fight for survival. This project demonstrates a high-performance hybrid network architecture designed to handle both critical state management and low-latency real-time updates.

## 🎮 Game Overview

Players enter a 50x50 arena, choosing their unique emoji skins. As the game progresses, a deadly "Ring" (Safe Zone) shrinks, forcing players into close-quarters combat . The last warrior standing wins the round.

## 📡 Technical Architecture

### Hybrid Networking Model

The system leverages two distinct protocols to optimize the gaming experience:


**TCP (Transmission Control Protocol):** Manages reliable communication including lobi synchronization, player "Ready" status, chat messages, and game-over rankings.


**UDP (User Datagram Protocol):** Handles high-frequency, time-sensitive data such as player coordinates, projectile movement, and real-time safe-zone boundaries to ensure minimal latency.



### Core Systems


**Authoritative Server:** The server maintains the "source of truth" for all physics, collision detection, and health management at a target rate of 60 FPS.


**JSON Serialization:** Data is exchanged using a polymorphic packet system built on `System.Text.Json`, allowing for flexible and extensible network messages .


**Physics & Collision:** A dedicated physics engine calculates projectile trajectories and handles overlaps between solid entities and world boundaries .


**Ability System:** Includes four distinct projectile types (Physical, Electric, Fire, Ice), each with unique cooldowns and status effects like burning or freezing .



## 💻 Setup and Installation

### Prerequisites

* .NET 8.0 SDK or higher.
* A terminal/console supporting UTF-8 (to render emojis correctly).

### Building the Project

Navigate to the root directory and build the solution:

```bash
dotnet build

```

### Running the Application

1. **Start the Server:**
```bash
dotnet run --project FantasyWar_Server

```


The server will output its local IP address and listen on ports **5000 (TCP)** and **5001 (UDP)**.


2. **Start the Client(s):**
```bash
dotnet run --project FantasyWar_Client

```


Enter the server's IP address when prompted (use `127.0.0.1` for local testing).



## ⌨️ Controls

**Movement:** `W`, `A`, `S`, `D` .


**Attack:** `Spacebar` (Shoots in the direction of last movement) .


**Select Ability:** `Num1` to `Num4` (Switch between Physical, Electric, Fire, and Ice projectiles) .


**Chat:** Press `T` to open chat, type your message, and press `Enter` to send .


**Lobby:** Press `N` to change your name, use `Left/Right Arrows` to change your skin, and press `R` to toggle ready status .



## 📁 Project Structure


* `FantasyWar_Server/`: Contains game logic, lobi management, and the packet processing engine .


* `FantasyWar_Client/`: Includes the camera system, rendering engine, and input handlers .


* `FantasyWar_Engine/`: Shared library containing the network wrappers, physics system, and packet definitions .



## 🤖 AI Tool Usage

AI assistance (Gemini/Claude) was utilized during development for:

* Refining the **Asynchronous Socket** logic for the `TcpConnection` class.
* Designing the double-buffered **Render System** to prevent flickering in the console .


* Implementing thread-safe packet queuing using `ConcurrentQueue`.


## Video Presentation

https://youtu.be/G2k949aGSHM?si=1ViPkZwj1fI82ktk
