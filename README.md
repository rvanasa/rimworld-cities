# RimCities (work in progress)
## A procedural city generator mod for RimWorld.

### [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=1775170117) | [Previous Versions](https://github.com/rvanasa/rimworld-cities/releases)

---

![](About/Preview.png)

---

#### Patch v0.3.14 (October 8, 2019)
- Added "Crashlanded (Ghost City)" scenario. 
- Increased time between "Defend City" quest attack waves.
- Fixed localization for "Disable city events" menu option. 

#### Patch v0.3.13 (October 4, 2019)
- Continued deprecation of Keanu/Legends easter egg. 

#### Patch v0.3.12 (July 4, 2019)
- Fixed a potential edge case with city quest home selection logic. 
- Updated Japanese translation (@Proxyer).

#### Patch v0.3.11 (July 3, 2019)
- Beds can now be claimed in abandoned cities.
- Mod and scenario settings are now translatable.
- Turrets no longer generate in ghost cities.

#### Patch v0.3.10 (July 2, 2019)
- Added "Ghost" cities designed for zombie survival scenarios, which are essentially friendly/hostile cities without any citizens. This is accessible via the "Start in city" scenario option.

#### Patch v0.3.9 (July 2, 2019)
- Added a setting to enable/disable city events such as hostile reinforcement raids and food airdrops.
- Improved city quest starting logic.
- Improved compatibility with Wick Time (standalone John Wick event mod).

#### Patch v0.3.8 (July 1, 2019)
- Fixed a minor quest system persistence bug.

#### Patch v0.3.7 (July 1, 2019)
- Improved "Combined Assault" quest completion logic.

#### Patch v0.3.6 (July 1, 2019)
- Fixed save corruption due to secret incident removal.

#### Patch v0.3.5 (June 30, 2019)
- Removed secret incident.

#### Patch v0.3.4 (June 30, 2019)
- Updated Medieval Times compatibility.

#### Patch v0.3.3 (June 30, 2019)
- Significantly increased rarity of secret incident.
- Added Combat Enhanced compatibility to secret incident.

#### Patch v0.3.2 (June 29, 2019)
- Increased friendly raid strength in "Combined Assault" quest type.
- Rebalanced "Heavy Siege" quest type (citizens no longer join your faction).
- Fixed a persistence-related bug in "Assassination" quest type.

#### Patch v0.3.1 (June 27, 2019)
- Added compatibility for Quest Tab (collaboration with @Merthykins).
- Improved city quest expiration time logic.
- Updated Japanese translation (@Proxyer is awesome).

#### Patch v0.3.0 (June 27, 2019)
- New quest type: Sabotage (deactivate a city's defenses for an allied invasion force)
- New quest type: Combined Assault (attack a settlement with your ally)
- New quest type: Assassination (track down and creatively assassinate a specific target)
- New quest type: Heavy Siege (defend a city from waves of enemies)
- New quest type: Prison Break (help prisoners escape a city)
- New very rare secret incident type :)
- Abandoned city resources are no longer automatically claimed for more control over player wealth.
- Building materials have been rebalanced for all city types.
- When using Faction Control, city world generation works better on existing saves.
- When using Medieval Times, fewer rooms will contain production-related stations.
- The "Area revealed" message no longer shows up repeatedly when exploring city maps.
- A greater number of cities will generate per world (by default).
- Abandoned cities are no longer affected by the "Limit city map size" mod setting.
- Hostile cities no longer claim ownership of items (i.e. items can now be un-forbidden).
- Friendly cities now receive occasional food deliveries via drop pod.
- Map generation now works better with other terrain generation mods.

#### Patch v0.2.10 (June 24, 2019)
- Fixed Medieval Times compatibility.

#### Patch v0.2.9 (June 23, 2019)
- Updated Japanese translation (@Proxyer).

#### Patch v0.2.8 (June 23, 2019)
- Abandoned city buildings are now automatically claimed by the player.

#### Patch v0.2.7 (June 23, 2019)
- Added compatibility for [Faction Control](https://steamcommunity.com/sharedfiles/filedetails/?id=1509102551) and [Medieval Times](https://steamcommunity.com/sharedfiles/filedetails/?id=732569232).
- Rebalanced abandoned city wall materials.

#### Patch v0.2.6 (June 23, 2019)
- Added more wildlife to abandoned cities.
- Added a "Crashlanded (Friendly City)" scenario.
- Decreased detection range of hostile inhabitants.
- Using the "Standing" scenario arrival option in an abandoned city will no longer delete your pawns and items.

#### Patch v0.2.5 (June 23, 2019)
- While under attack, hostile cities will occasionally call in reinforcements based on your storyteller settings.
- Significantly improved both friendly and hostile citizen AI (eating, sleeping, responding to attacks). A few more upgrades are coming soon.
- Spiced up city map generation with town center spaces and more room / building variants.
- City stockpile ownership works properly now (theft penalties no longer apply to natural resources and player-owned items).
- Increased the number of items scattered in abandoned cities.
- Fixed several bugs pointed out by the community.

#### Patch v0.2.4 (June 22, 2019)
- Added "Crashlanded (Abandoned City)" scenario.
- Fixed several minor bugs.

#### Patch v0.2.3 (June 21, 2019)
- Fixed several mod incompatibility edge cases.
- Improved AI in friendly cities.
- Stealing from friendly cities affects faction relations.

#### Patch v0.2.2 (June 21, 2019)
- Improved compatibility with [Combat Extended](https://steamcommunity.com/workshop/filedetails/?id=1631756268).

#### Patch v0.2.1 (June 20, 2019)
- Cities now generate in previous save files.
- You can start a new game in a friendly, hostile, or abandoned city using the "Start in a city" scenario option.
- Both abandoned and conquered cities can now be claimed and converted into player colonies.
- Friendly cities can now be visited without invoking hostilities.
- More settings are  available for city generation (check the mod options page).
- Added partial Japanese language support (@Proxyer).

---

### [Announcement/Discusssion Thread](https://www.reddit.com/r/RimWorld/comments/c2odfh/10_rimcities_beta_release_procedural_city_map/)

---

RimCities is an attempt to create an interesting and unique end-game challenge for RimWorld players. This mod adds randomly generated cities to the world map, which are similar to other settlements but are far more difficult to attack. Pirate-controlled cities provide even more of a challenge by occasionally bombarding you with mortar shells.

It is possible to visit and explore friendly cities with in-person trading and custom quest objectives. Rival factions will attack one another's cities from time to time, giving you opportunities to choose sides and establish military alliances.

You may also come across abandoned cities, which provide great scavenging opportunities for you and other nearby raiding parties.

RimCities is compatible with existing save files and works with Combat Extended, Real Ruins, Zombieland, Faction Control, RimQuest, Quest Tab, Medieval Times, and many other mods. If you're encountering issues, try disabling other mods and/or tweaking the load order.

If you'd like to start your colony in a city, click "Scenario Editor" and check the box next to "Edit mode". From there, click "Add part" and add the "Start in city (RimCities)" option. You can then customize the scenario to your liking.

Work in progress! Let me know if you have any ideas or suggestions for the mod.

---

## Features:

#### Visit cities on the world map.
![](Docs/World1.png)

#### Explore large, intricate settlement layouts.
![](Docs/Map1.png)

#### Scavenge for supplies and avoid defenses.
![](Docs/Map2.png)

#### Reveal rooms as you progress through the map.
![](Docs/Map3.png)

#### Claim conquered and abandoned cities for your faction.
![](Docs/Map4.png)

---

### [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=1775170117) | [Previous Versions](https://github.com/rvanasa/rimworld-cities/releases)

If you have any ideas or suggestions, please feel free to add an issue or submit a pull request.
