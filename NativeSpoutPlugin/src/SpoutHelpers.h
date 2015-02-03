#include <Windows.h>

// SPOUT
// #include "spoutInterop.h"

// Other spout files might be needed due to dependency of spoutInterop
// but the functions for DirectX initialization etc. will not be used.
// A single sender or receiver is assumed. Support for multiple senders / receivers not yet established.

// Spout shared texture info structure used for communication
// between a sender and receiver


#include "SpoutDirectX.h"

// GLOBAL VARIABLES :: need to be changed so multiple senders / receivers can co exist
bool					bInitialized = false;
char					g_SenderName[256];	// Spout sender and shared memory name
HANDLE					g_shareHandle;		// Global texture share handle
unsigned int			g_width;			// Global texture width
unsigned int			g_height;			// Global texture height
SharedTextureInfo		g_info;				// Global texture info structre
spoutInterop			spout;				// Spout setup functions

// ############# SPOUT FUNCTIONS #################
extern "C"  int EXPORT_API initSpout()
{
	UnityLog("initSpout !");
	// Not needed
	return 1;
}


extern "C" void EXPORT_API SpoutCleanup()
{
	UnityLog("clean Spout !");

	spout.CloseSender(g_SenderName);

}

HANDLE getSharedHandleForTexture(ID3D11Texture2D * texToShare)
{
	HANDLE sharedHandle;

	IDXGIResource* pOtherResource(NULL);
	texToShare->QueryInterface( __uuidof(IDXGIResource), (void**)&pOtherResource );
	pOtherResource->GetSharedHandle(&sharedHandle);
	pOtherResource->Release();

	return sharedHandle;
}

extern "C" void createSenderFromSharedHandle(char * senderName, HANDLE sharedHandle, D3D11_TEXTURE2D_DESC td)
{
	// 1) Set global variables to check for texture changes
		g_shareHandle	= sharedHandle;
		g_width			= td.Width;
		g_height		= td.Height;

		// 2) Set up the texture info structure
		g_info.shareHandle		= (unsigned __int32)sharedHandle; // used
		g_info.width			= (unsigned __int32)g_width; // used
		g_info.height			= (unsigned __int32)g_height; // used
		g_info.format			= (DWORD)td.Format; // can be used for DX11 - but needs work
		g_info.usage			= (DWORD)td.Usage; // unused

		// 3) Create a sender name
		//strcpy_s(g_SenderName, 256, senderName);	// Sender name 

		// 4) Create a Spout sender
		spout.CreateSender(senderName,g_info.width,g_info.height,g_shareHandle,g_info.format);

		bInitialized = true; // flag that a sender has been created for update
}


//DX9 Compat

HRESULT CreateDX9SharedTexture(UINT width, UINT height, D3DFORMAT format, DWORD usage, PDIRECT3DTEXTURE9 * out_pD3D9Texture, HANDLE * out_SharedTextureHandle) {
	HRESULT hr = S_OK;

	//create Direct3D instance if necessary
	if ( g_pDirect3D9Ex == NULL ) {
		hr = Direct3DCreate9Ex( D3D_SDK_VERSION,  /*_Out_*/ &g_pDirect3D9Ex );
		if ( hr != S_OK ) {
			return hr;
		}
	}

	//create device if necessary
	if ( g_pDeviceD3D9ex == NULL ) {

		//seems to work if no window !
		//g_hWnd_D3D9Ex = NULL;


		// Do we support hardware vertex processing? if so, use it. 
		// If not, downgrade to software.
		D3DCAPS9 d3dCaps;
		hr = g_pDirect3D9Ex->GetDeviceCaps( D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, &d3dCaps );
		if ( hr != S_OK ) {
			// TO DO: Respond to failure of GetDeviceCaps
			return hr;
		}

		DWORD dwBehaviorFlags = 0;
		if ( d3dCaps.VertexProcessingCaps != 0 ) {
			dwBehaviorFlags |= D3DCREATE_HARDWARE_VERTEXPROCESSING;

			//usage = usage XOR D3DUSAGE_SOFTWAREPROCESSING;
		}
		else {
			dwBehaviorFlags |= D3DCREATE_SOFTWARE_VERTEXPROCESSING;

			usage = usage | D3DUSAGE_SOFTWAREPROCESSING;
		}

		usage = usage | D3DUSAGE_NONSECURE;
			
		D3DDISPLAYMODE displayMode;

		hr = g_pDirect3D9Ex->GetAdapterDisplayMode( D3DADAPTER_DEFAULT, &displayMode);

		if ( hr != S_OK ) {
			return hr;
		}

		//D3DPRESENT_PARAMETERS * presentParameters
		D3DPRESENT_PARAMETERS presentParameters = {0};
		ZeroMemory( &presentParameters, sizeof(presentParameters) );
		presentParameters.Windowed = true;
		presentParameters.hDeviceWindow = NULL; //g_hWnd; //NULL; //g_hWnd_D3D9Ex;
		presentParameters.SwapEffect = D3DSWAPEFFECT_DISCARD;
		presentParameters.BackBufferWidth = 64;
		presentParameters.BackBufferHeight = 64;
		presentParameters.BackBufferFormat = displayMode.Format; //D3DFMT_A8R8G8B8; //format
		presentParameters.EnableAutoDepthStencil = FALSE;
		presentParameters.AutoDepthStencilFormat = D3DFMT_D24S8;
		presentParameters.BackBufferCount = 1;
		//present_parameters.Flags = 0;
		//present_parameters.PresentationInterval   = D3DPRESENT_INTERVAL_IMMEDIATE;


		hr = g_pDirect3D9Ex->CreateDeviceEx(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, NULL, dwBehaviorFlags, &presentParameters, NULL, &g_pDeviceD3D9ex);
		//hr = g_pDirect3D9Ex->CreateDeviceEx(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, g_hWnd_D3D9Ex, dwBehaviorFlags, &presentParameters, NULL, &g_pDeviceD3D9ex);
			
		if ( hr != S_OK ) {
			return hr;
		}
	}

	//create texture
	hr = g_pDeviceD3D9ex->CreateTexture(width, height, 1, usage, format, D3DPOOL_DEFAULT, out_pD3D9Texture, out_SharedTextureHandle);

	return hr;
}