#  SimpleTide - a riptide Steam template
A simple and lightweight kick-start to any unity riptide project that aims to use steam and a server-client player-hosted architecture. This template provides a set of features that are quite common in many multiplayer games, such as lobbies, matchmaking and replicated variables. The steam wrapper is already implented to remove the need for players to do port forwarding.

# Features
- Two types of matchmaking through steam out of the box
- Network Creation and Destruction for joining and existing players
- Object, prefab and user identification
- Unidirectional and biderectional synchronised variables

# Supported matchmaking types
Out of the box, there are two available matchmaking types
- Unlisted matchmaking
	> Players press the 'matchmake' button and get matchmade together

- Private room creation
	> One user can create a room, and then invite friends with the 'invite' button

This project can easily be extended to add features like public lobbies (matchmaking + friends) and searchable ID lobbies / lobby lists.

# Installation & Prerequisites
The prerequisites for using this project are :
1. Having a computer that can do virtualisation
2. Having two steam accounts
3. Having a virtual machine of any operating system that can sync the build folder with the host machine

** or **

Having two computers

Once these have been met, follow these steps to install the template
1. Clone the repository `git clone https://github.com/AlooTheAloo/RiptideSteamTemplate`
2. Create a shared folder with your host machine on the 'build' folder
3. Log in to **both** of your steam accounts on host machine and VM (steam must be open as a daemon in the background on both machines) 
4. Build the game on the host machine, open it on both the host and the VM.
5. Press the `matchmake` button once the game launches, you should see the same lobby name on both machines


## How to use


Every object that should be able to be instantiated and tracked by the server should be placed in the **Assets/Resources/NetworkPrefabs** folder or one of its subfolders. They must also have the **NetworkObject** component attached to them.

Every **network object** contains a prefab ID, object ID and owner ID.

Most of this template's features can be accessed through static methods from the `SimpleTide` class.   
Notable ones include 
- `isServer()` Returns true if the current game instance is acting as a server
- `isMine(NetworkObject no)` Returns true if 'no' is owned by the client 
- `networkCreate(NetworkObject objectPrefab, int owner)`  As a client, tells every client to spawn an object prefab. If no owner is specified, the current client is used as an owner. 
- `networkCreate(ushort targetClient, NetworkObject netObj, int owner)` As a server, tells a specific client to spawn an already instantiated Network Object or a prefab. If an owner is not specified, it will use the current client if it's a prefab or the current owner if it's already instantiated.

There are also some useful static variables including 
- `Server` Returns the server
- `Client` Returns the client

<hr>

**Any feedback concerning fixes or new features is appreciated**
##### Provided with the <a href="https://github.com/anak10thn/WTFPL" target="_top">WTFPL licence</a>
