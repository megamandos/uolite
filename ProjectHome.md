Intended primarily for programming bots, this is a .NET DLL. Written in VB.NET, although you can use just about any language to work with it; such as C#, VB.NET, J#, C++, etc... since it is a COM visible DLL.

With UOLite you don't need the Ultima Online Client running, wasting resources, only your application that uses the UOLite DLL. When running with the test application I rarely see it consume over 12MB of RAM, and 1% CPU. The entire client is there, its up to you how to use it.

Ever wanted a bot that would cross-heal you? What about a bot that could follow you around and carry your dungeon loot and recall out, or hide at the first sign of danger? What about a bot that works as a mobile bank, storing and retrieving items as you desire?

These are all very easily implemented using UOLite. No messing around with poorly implemented LUA scripting, no having to research and update client memory offsets as client updates come out.

Is there something that UOLite doesn't offer? Well, UOLite allows you to send your own custom packets to the server and handle packets received, releasing your ability to implement new ideas from the constraints of what it already supports! Expand on its functionality as you wish! Its open source, so if you find a bug feel free to fix it instead of waiting for the next version to come out and HOPING that its fixed.