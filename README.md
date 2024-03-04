# Whack-A-Mole Games Service

The Games Service is responsible for storing game state information. This means then whenever a new game is created in the backend, this service is responsible for doing so. It will from then on control and maintain the state of the game and pass changes on using events sent out with Azure Web PubSub messages. A game generally contains a unique ID, a list of players and a status. Status can only be changed by authorized identities.

## New games

Newly created games are always created so they get the 'New' status. This is to be able to create some kind of lobby where players can already enter the upcoming game, while the previous game is still actively being played.

## Current game

A game in new status can be promoted to 'Current' Current is one of two states under which a game is considered to be the 'Active' game. There can only be one 'Active' game at a time. So a game can never be promoted to an 'Active' state if a different game is already in that state. This 'Current' state is a state that allows for creating a new lobby game, the game is not yet started.

## Started game

'Current' games can be started. The game state changes to the 'Started' status and from then on, times and scores are being tracked for that game. 'Started' is the second state under which a game is considered to be 'Active'. A game changed to 'Started' will cause an additional real-time message being sent to players and will make the game actually start on all client devices.
