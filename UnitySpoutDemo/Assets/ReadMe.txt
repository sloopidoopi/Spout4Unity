Before you can start with Spout4Unity you have to check if you installed the Spout2 infrastructure (get from https://github.com/leadedge/Spout2/releases)
This Spout4Unity release is tested with  https://github.com/leadedge/Spout2/releases/download/v2.002-beta/SpoutSetup_V2.002-beta.zip

With the installation you get a couple of demo apps you can use for testing. Go to the Spout installation folder.In the DEMO folder you can start the SpoutReceiver.exe and the SpoutSender.exe

Spout4Unity comes with a couple of demo scenes (Assets/Spout/Scenes). For testing sending&receiving start the 'Spout2 Sender.and.Receiver' scene.


General Setup:
Add a Spout Component to your Scene. Normally you have to enter the play mode to see the texture sharing but you can enable the 'Is Enabled in Editor' option. 

Receiving a Spout texture in Unity:

Add the SpoutReceiver component to a Gameobject that has a Mesh Renderer and a Material attached. (The Material should not be used by other SpoutReceivers!) 
If you have only one external Spout texture source you can use under 'Select sender' the 'Any' option, so the first registered Spout texture is displayed.
For more complex set-ups with multiple texture shares you should explicit set a name which texture you want to display. (Select sender 'Other(specify)' and enter the name of your Spout texture share name) 
Depending on the name you use in your external SpoutSender app  you have to change the 'Sender name' property of your SpoutReceiver component in Unity.(Or use 'Any' for 'Select sender' option)


Sending a Spout texture from Unity:

Setup a camera in your Unity scene: Select a RenderTexture in the 'Target Texture' property to render into.
Add the SpoutSender component to your Camera:
Use a sharing name for your texture. (Has to be unique on your system so Spout can provide this texture to other clients under this name)
Select a RenderTexture. (You have to select the RenderTexture that your Camera uses for rendering!) 
(RenderTexture settings: Color Format: ARGB32, Depth Buffer: No depth buffer)
Depending on Unity's current DirectX mode and the graphics card of your system you have to change the Texture Format of your Spout Senders.
DX9(Normal Unity rendering mode): Should work with all modes 
DX11: DXGI_FORMAT_R8G8B8A8_UNORM





