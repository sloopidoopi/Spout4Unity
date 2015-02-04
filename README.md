# Spout4Unity
Spout2 support for Unity3D

This plugin allows Unity to receive and share textures from and to external softwares like Resolume, Adobe AIR, After Effects...
It has been updated to fully supports the Spout2SDK available here : https://github.com/leadedge/Spout2

This is a new repository based on the original <a href="https://github.com/benkuper/Unity-Plugins">SpoutPlugin</a> from Benjamin Kuperberg.

The components now work in play&edit mode. You can enable/disable the spout components.

Known issues: 
If you enable/disable multiple Unity senders at the same time or in a short interval you can break the connection to the texture in an external Unity receiver. 
If you change the Unity scene with the 'editor enabled' option it is possible to loose the connections to the external senders and receivers. (The plugin disable and enable itself on a scene change)
