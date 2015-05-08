#define _CRT_SECURE_NO_WARNINGS

#include "UnityPluginInterface.h"
//#include "pthread.h"

#include "Spout.h"
/*#include "spoutDirectX.h"
#include "spoutGLDXinterop.h"
#include "spoutSenderNames.h"
*/
using namespace std;

HWND hWnd;

spoutSenderNames * sender;
spoutGLDXinterop * interop;
spoutDirectX * sdx;

	
//DX11
//DXGI_FORMAT texFormat = DXGI_FORMAT_R8G8B8A8_UNORM;

vector<ID3D11Texture2D *> activeTextures;
vector<HANDLE> activeHandles;
vector<string> activeNames;
int numActiveSenders;

//DX9
D3DFORMAT d9TexFormat =  D3DFMT_A8R8G8B8;

vector<SpoutSender *> spoutSendersD9;
vector<IDirect3DTexture9 *> activeTexturesD9;
vector<IDirect3DSurface9 *>activeSurfacesD9;
vector<HANDLE> activeHandlesD9;
vector<string> activeNamesD9;
int numActiveSendersD9;

extern "C" void EXPORT_API initDebugConsole()
{
	AllocConsole();
    freopen("CONIN$",  "r", stdin);
    freopen("CONOUT$", "w", stdout);
    freopen("CONOUT$", "w", stderr);
}


// ************** UTILS ********************* //
/*
HANDLE getSharedHandleForTexture(ID3D11Texture2D * texToShare)
{
	HANDLE sharedHandle;

	IDXGIResource* pOtherResource(NULL);
	texToShare->QueryInterface( __uuidof(IDXGIResource), (void**)&pOtherResource );
	pOtherResource->GetSharedHandle(&sharedHandle);
	pOtherResource->Release();

	delete pOtherResource;

	return sharedHandle;
}
*/


BOOL CALLBACK EnumProc(HWND hwnd, LPARAM lParam)
	{
		DWORD windowID;
		GetWindowThreadProcessId(hwnd, &windowID);

		if (windowID == lParam)
		{
			printf("Found HWND !\n");
			hWnd = hwnd;

			return false;
		}

		return true;
	}



//OpenGL for DX9-GL-DX9 conversion
bool bOpenGL;
HGLRC m_hRC;
HDC m_hdc;
HGLRC m_hSharedRC;
HWND m_hwnd;

// OpenGL setup function - tests for current context first

// Spout OpenGL initialization function
bool InitOpenGL()
{
	char windowtitle[512];

	// We only need an OpenGL context with no window
	m_hwnd = GetForegroundWindow(); // Any window will do - we don't render to it
	if(!m_hwnd) { printf("InitOpenGL error 1\n"); MessageBoxA(NULL, "Error 1\n", "InitOpenGL", MB_OK); return false; }
	m_hdc = GetDC(m_hwnd);
	if(!m_hdc) { printf("InitOpenGL error 2\n"); MessageBoxA(NULL, "Error 2\n", "InitOpenGL", MB_OK); return false; }
	GetWindowTextA(m_hwnd, windowtitle, 256); // debug

	PIXELFORMATDESCRIPTOR pfd;
	ZeroMemory( &pfd, sizeof( pfd ) );
	pfd.nSize = sizeof( pfd );
	pfd.nVersion = 1;
	pfd.dwFlags = PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER;
	pfd.iPixelType = PFD_TYPE_RGBA;
	pfd.cColorBits = 32;
	pfd.cDepthBits = 16;
	pfd.iLayerType = PFD_MAIN_PLANE;

	int iFormat = ChoosePixelFormat(m_hdc, &pfd);
	if(!iFormat) { printf("InitOpenGL error 3\n"); MessageBoxA(NULL, "Error 3\n", "InitOpenGL", MB_OK); return false; }

	if(!SetPixelFormat(m_hdc, iFormat, &pfd)) { printf("InitOpenGL error 4\n"); MessageBoxA(NULL, "Error 4\n", "InitOpenGL", MB_OK); return false; }

	m_hRC = wglCreateContext(m_hdc);
	if(!m_hRC) { printf("InitOpenGL error 5\n"); MessageBoxA(NULL, "Error 5\n", "InitOpenGL", MB_OK); return false; }

	wglMakeCurrent(m_hdc, m_hRC);
	if(wglGetCurrentContext() == NULL) { printf("InitOpenGL error 6\n"); MessageBoxA(NULL, "Error 6\n", "InitOpenGL", MB_OK); return false; }

	// spoutreceiver.SetDX9(true); // DirectX 11 is the default
	
	// Set up a shared context
	if(!m_hSharedRC) m_hSharedRC = wglCreateContext(m_hdc);
	if(!m_hSharedRC) { printf("InitOpenGL shared context not created\n"); }
	if(!wglShareLists(m_hSharedRC, m_hRC)) { printf("wglShareLists failed\n"); }

	// Drop through to return true
	// printf("InitOpenGL : hwnd = %x (%s), hdc = %x, context = %x\n", m_hwnd, windowtitle, m_hdc, m_hRC);

	// int nCurAvailMemoryInKB = 0;
	// glGetIntegerv(0x9049, &nCurAvailMemoryInKB);
	// printf("Memory available [%i]\n", nCurAvailMemoryInKB);

	return true;

}


bool StartOpenGL()
{
	HGLRC hrc = NULL;

	// Check to see if a context has already been created
	if(bOpenGL && m_hdc && m_hRC) {
		// Switch back to the primary context to check it
		if(!wglMakeCurrent(m_hdc, m_hRC)) {
			// Not current so start again
			wglMakeCurrent(NULL, NULL);
			wglDeleteContext(m_hSharedRC);
			wglDeleteContext(m_hRC);
			m_hdc = NULL;
			m_hRC = NULL;
			m_hSharedRC = NULL;
			// restart opengl
			bOpenGL = InitOpenGL();
		}
	}
	else {
		bOpenGL = InitOpenGL();
	}

	return bOpenGL;
}






// INIT //

extern "C" bool EXPORT_API init()
{

	//Temp
	/*
	AllocConsole();
    freopen("CONIN$",  "r", stdin);
    freopen("CONOUT$", "w", stdout);
    freopen("CONOUT$", "w", stderr);

	printf("Spout Init, deviceType = %i\n",g_DeviceType);
	*/

	DWORD processID = GetCurrentProcessId();
	EnumWindows(EnumProc, processID);

	if(hWnd == NULL)
	{
		printf("SpoutNative :: HWND NULL\n");
		return false;
	}


	
	sender		= new spoutSenderNames;
	interop		= new spoutGLDXinterop;
	sdx			= new spoutDirectX;

	if(g_DeviceType == 1) //DX9
	{

		
		/*
		m_hwnd = NULL;
		m_hdc = NULL;
		m_hRC = NULL; // rendering context
		m_hSharedRC = NULL; // shared context
		*/

		bOpenGL = StartOpenGL();
		printf("openGL init %i\n",bOpenGL);
	}

	numActiveSenders = 0;

	return true;
} 

int getIndexForSenderName(char * senderName)
{
	
	if(g_DeviceType == 2) //DX11
	{
		//printf("Get Index For Name %s  (%i active Senders) :\n",senderName,numActiveSenders);
		for(int i=0;i<numActiveSenders;i++)
		{
			//printf("\t> %s\n",activeNames[i].c_str());
			if(strcmp(senderName,activeNames[i].c_str()) == 0) return i;
		}
	}else if(g_DeviceType == 1) //DX9
	{
		for(int i=0;i<numActiveSendersD9;i++)
		{
			if(strcmp(senderName,activeNamesD9[i].c_str()) == 0) return i;
		}
	}
	//printf("\t....Not found\n");
	return -1;
}


//declaration
//extern "C" bool EXPORT_API updateSender(char * senderName, void * texturePointer);

// ************** SENDING ******************* //
extern "C" bool EXPORT_API createSender(char * senderName, void * texturePointer, int texFormatIndex = 0)
{
	printf("SpoutNative :: create Sender %s\n",senderName);
	
	int checkSenderIndex = getIndexForSenderName(senderName);
	if(checkSenderIndex != -1)
	{
		printf("SpoutNative :: sender alreay exists\n");
		return false;
	}

	printf("Check TexturePointer : %i\n",texturePointer);

	if(texturePointer == nullptr) 
	{
		printf("## Texture Pointer null, create Sender fail");
		return false;
	}
	 
	HANDLE sharedSendingHandle = NULL;

	bool texResult = false;
	bool updateResult = false;
	bool senderResult = false;
	string sName=  string(senderName);

	if(g_DeviceType == 2) //DX11
	{
		D3D11_TEXTURE2D_DESC desc;
		((ID3D11Texture2D *)texturePointer)->GetDesc(&desc);	
		ID3D11Texture2D * sendingTexture; 

		/*
		HRESULT res = d3d11->CreateTexture2D(&desc, NULL,&sendingTexture);
		printf("CreateTexture2D MANUAL TEST : [0x%x]\n", res);
		*/

		DXGI_FORMAT texFormat = DXGI_FORMAT_R32G32B32A32_FLOAT;//DXGI_FORMAT_B8G8R8A8_UNORM;
		switch(texFormatIndex)
		{
		case 0:
			texFormat = DXGI_FORMAT_B8G8R8A8_UNORM;
		case 1:
			texFormat = DXGI_FORMAT_R32G32B32A32_FLOAT;
			break;
		case 2:
			texFormat = DXGI_FORMAT_R16G16B16A16_FLOAT;
			break;
		}

		texResult = sdx->CreateSharedDX11Texture(g_D3D11Device,desc.Width,desc.Height,texFormat,&sendingTexture,sharedSendingHandle);
		printf(">> Create shared Texture with SDX : %i\n",texResult);
	
		if(!texResult)
		{
			printf("# SharedDX11Texture creation failed, stop here.\n");
			return 0;
		}


		senderResult = sender->CreateSender(senderName,desc.Width,desc.Height,sharedSendingHandle,texFormat);
		printf(">> Create sender DX11 with sender names : %i\n",senderResult);	
	
		g_pImmediateContext->CopyResource(sendingTexture,(ID3D11Texture2D *)texturePointer);
		g_pImmediateContext->Flush();

		updateResult = sender->UpdateSender(senderName,desc.Width,desc.Height,sharedSendingHandle);

		activeTextures.push_back(sendingTexture);
		activeNames.push_back(sName);
		activeHandles.push_back(sharedSendingHandle);
		numActiveSenders++;

	}else if(g_DeviceType == 1) //DX9
	{
		printf("We are here\n");
		
		D3DSURFACE_DESC desc;
		IDirect3DTexture9 * srcTex = (IDirect3DTexture9*)texturePointer;
		srcTex->GetLevelDesc(0,&desc);

		printf("Desc width/height %i %i\n",desc.Width,desc.Height);


		
		SpoutSender * spoutSender = new SpoutSender();
		spoutSender->SetDX9();

		senderResult = spoutSender->CreateSender(senderName,desc.Width,desc.Height,desc.Format); 
		printf(">> Create sender DX9 with sender names : %i\n",senderResult);	

		//activeTexturesD9.push_back(srcTex);
		IDirect3DSurface9 * surf = NULL;
		activeNamesD9.push_back(sName);
		activeSurfacesD9.push_back(surf);
		spoutSendersD9.push_back(spoutSender); 
		numActiveSendersD9++;

		//if(!bOpenGL) bOpenGL = StartOpenGL();  
	}

	 
	int senderIndex = getIndexForSenderName(senderName);
	printf("Index search test > %i\n",senderIndex);

	return senderResult;
}



extern "C" bool EXPORT_API updateSender(char* senderName, void * texturePointer)
{
	int senderIndex = getIndexForSenderName(senderName);
	//printf("Update sender : %s, sender index is %i\n",senderName,senderIndex);

	if(senderIndex == -1)
	{
		printf("Sender is not known, creating one.\n");
		createSender(senderName,texturePointer);
		return false;
	}

	

	
	bool result = false;
	if(g_DeviceType == 2)//DX11
	{
		if(activeTextures[senderIndex] == nullptr)
		{
			printf("activeTextures[%i] is null (badly created ?)\n",senderIndex);
			return false;
		}

		HANDLE targetHandle = activeHandles[senderIndex];

		ID3D11Texture2D * targetTex = activeTextures[senderIndex];
	
		g_pImmediateContext->CopyResource(targetTex,(ID3D11Texture2D*)texturePointer);
		g_pImmediateContext->Flush();
	
		D3D11_TEXTURE2D_DESC td;
		((ID3D11Texture2D *)texturePointer)->GetDesc(&td);
		//printf("update texFormat %i %i\n",texFormat,td.Format);


		result = sender->UpdateSender(senderName,td.Width,td.Height,targetHandle);
	//printf("updateSender result : %i\n",result);
	

	}else if(g_DeviceType == 1) //DX9
	{
		
		if(!bOpenGL) 
		{
			printf("[updateSender] bOpenGL false, openGL not init\n");
			
			return false;
		}

		// Activate the shared context for draw
		if(!wglMakeCurrent(m_hdc, m_hSharedRC)) {
			bOpenGL = false;
			printf("############################### Draw - no context - hdc = %x, ctx = %x\n", m_hdc, m_hSharedRC);
			// It will start again if the start button is toggled
			return false;
		}


		//HANDLE targetHandle = activeHandlesD9[senderIndex];
		//IDirect3DTexture9 * targetTex = activeTexturesD9[senderIndex];
	
		D3DSURFACE_DESC desc;
		SpoutSender * spoutSender  = spoutSendersD9[senderIndex];
		IDirect3DTexture9 * srcTex = (IDirect3DTexture9*)texturePointer;
		srcTex->GetLevelDesc(0,&desc);

		IDirect3DSurface9 * targetSurf = NULL;//activeSurfacesD9[senderIndex];
		IDirect3DSurface9 * srcSurf = NULL;

		D3DLOCKED_RECT d3dlr;
		 

		HRESULT hr = g_D3D9Device->CreateOffscreenPlainSurface(desc.Width, desc.Height, desc.Format, D3DPOOL_SYSTEMMEM, &targetSurf, NULL);
		
		//printf("Create offscreen plain surface :%i\n",hr);

		if(SUCCEEDED(hr)) {
			// Get Texture Surface
			hr = srcTex->GetSurfaceLevel(0, &srcSurf);
			if(SUCCEEDED(hr)) {
				//printf("Get renderTarget data\n");
				// Copy Surface to Surface
				hr = g_D3D9Device->GetRenderTargetData(srcSurf, targetSurf);	
				if(SUCCEEDED(hr)) {
					//printf("Lock rect\n");
					// Lock the source surface using some flags for optimization
					hr = targetSurf->LockRect(&d3dlr, NULL, D3DLOCK_NO_DIRTY_UPDATE | D3DLOCK_READONLY);

					
					if(SUCCEEDED(hr)) {
						//printf("Rect is locked, drawing\n");
						D3DSURFACE_DESC targetDesc;
						targetSurf->GetDesc(&targetDesc);


						//printf("Desc format / src format :%i / %i\n",desc.Format,targetDesc);
						//printf("Desc width / height %i /%i :: %i / %i\n",desc.Width,desc.Height,targetDesc.Width,targetDesc.Height);

						if(targetDesc.Width != desc.Width || targetDesc.Height != desc.Height) {

						// Update the sender	
							printf("Update sender size changed !\n");
							spoutSender->UpdateSender(senderName, desc.Width, desc.Height);
					
						}

						result = spoutSender->SendImage((unsigned char *)d3dlr.pBits, desc.Width, desc.Height, GL_BGRA_EXT,true,false);
						//printf("sender updateImage : %i\n",result); 
						//}
						srcSurf->UnlockRect();
					}
				}
			}
		}
		 
		
		if(targetSurf) targetSurf->Release();
		if(srcSurf) srcSurf->Release();
		targetSurf = NULL;
		srcSurf = NULL;
		 
	}

	
	return result;
}




extern "C" void EXPORT_API closeSender(char * senderName)
{
	int senderIndex = getIndexForSenderName(senderName);

	printf("Close Sender : %s\n",senderName);
	//sender->CloseSender(senderName);
	
	if(senderIndex != -1)
	{
		if(g_DeviceType == 2) //DX11
		{
			sender->ReleaseSenderName(senderName);

			activeNames.erase(activeNames.begin()+senderIndex);
			activeHandles.erase(activeHandles.begin()+senderIndex);
			activeTextures.erase(activeTextures.begin()+senderIndex);
			numActiveSenders--;
		}else if(g_DeviceType == 1) //DX9
		{
			spoutSendersD9[senderIndex]->ReleaseSender();
			spoutSendersD9.erase(spoutSendersD9.begin()+senderIndex);
			activeNamesD9.erase(activeNamesD9.begin()+senderIndex);
			activeSurfacesD9.erase(activeSurfacesD9.begin()+senderIndex);
			//activeHandlesD9.erase(activeHandlesD9.begin()+senderIndex);
			//activeTexturesD9.erase(activeTexturesD9.begin()+senderIndex);
			numActiveSendersD9--;

		}
	}

	printf("There are now %i senders remaining\n",numActiveSenders,activeNames.size());
	
}


// *************** RECEIVING ************************ //

typedef void (*SpoutSenderUpdatePtr)(int numSenders);
SpoutSenderUpdatePtr UnitySenderUpdate;
typedef void (*SpoutSenderStartedPtr)(char * senderName,ID3D11ShaderResourceView * resourceView ,int width,int height );
SpoutSenderStartedPtr UnitySenderStarted;
typedef void (*SpoutSenderStoppedPtr)(char * senderName);
SpoutSenderStoppedPtr UnitySenderStopped;


extern "C" int EXPORT_API getNumSenders()
{
	return sender->GetSenderCount();
}



int lastSendersCount = 0;

char (*senderNames)[256];
char (*newNames)[256];

unsigned int w;
unsigned int h;
HANDLE sHandle;

extern "C" void EXPORT_API checkReceivers()
{

	if(sender == nullptr) return; 
	

	int numSenders = sender->GetSenderCount();
	
	printf("Num senders :%i\n",numSenders);

	if(numSenders != lastSendersCount)
	{
		printf("Num Senders changed : %i\n",numSenders);
			
		UnitySenderUpdate(numSenders);

		int i,j;
		bool found;
		
		printf("Old Sender List :\n");
		for(i=0;i<lastSendersCount;i++)
		{
			printf("\t> %s\n",senderNames[i]);
		}
			
		printf("\nUpdated Sender List :\n");
		for(i=0;i<numSenders;i++)
		{
			sender->GetSenderNameInfo(i,newNames[i],256,w,h,sHandle);
			printf("\t> %s\n",newNames[i]);
		}

		//NEW SENDERS DETECTION
		printf("\nNew Sender Detection, checking against previous sender list :\n");
		for(i=0;i<numSenders;i++)
		{
			printf("\t> %s .... ",newNames[i]);

			found = false;
			for(j = 0;j<lastSendersCount;j++)
			{
				if(!found && strcmp(newNames[i],senderNames[j]) == 0) 
				{
						found = true;
						printf("found in previous list.\n");
						break;
				}
			}

			

			if(!found) 
			{
				printf("not found [New]\n");

				sender->GetSenderNameInfo(i,newNames[i],256,w,h,sHandle);

				if(g_DeviceType == 2) //DX11
				{
					ID3D11Resource * tempResource11;
					ID3D11ShaderResourceView * rView;

					HRESULT openResult = g_D3D11Device->OpenSharedResource(sHandle, __uuidof(ID3D11Resource), (void**)(&tempResource11));
					g_D3D11Device->CreateShaderResourceView(tempResource11,NULL, &rView);

					printf("\t => Send Started Event with name : %s\n",newNames[i]);
					UnitySenderStarted(newNames[i],rView,w,h);
				}else if(g_DeviceType == 1) //DX9
				{
					printf("DX9 handle to come\n");
				}
			}
		}
			
		//SENDER STOP DETECTION
		for(int i=0;i<lastSendersCount;i++)
		{
			found = false;
			for(j = 0;j<numSenders;j++)
			{
				if(!found && strcmp(senderNames[i],newNames[j]) == 0) 
				{
						found = true;
				}
			}


			if(!found) 
			{
				printf("Send Stopped Event with name : %s\n",senderNames[i]);
				UnitySenderStopped(senderNames[i]);
			}
		}

		for(int i=0;i<numSenders;i++)
		{
			memcpy(senderNames[i],newNames[i],sizeof(newNames[i]));
		}
	}

	lastSendersCount = numSenders;
}


extern "C" bool EXPORT_API startReceiving(SpoutSenderUpdatePtr senderUpdateHandler,SpoutSenderStartedPtr senderStartedHandler,SpoutSenderStoppedPtr senderStoppedHandler) 
{
	printf("SpoutNative :: Start Receiving\n");

	//UnityLog("Start Receiving");
	UnitySenderUpdate = senderUpdateHandler;
	UnitySenderStarted = senderStartedHandler;
	UnitySenderStopped = senderStoppedHandler;
	
	lastSendersCount = 0;

	senderNames = new char[32][256];
	newNames = new char[32][256];

	return true;//ret == 0; //success
}

bool isCleaned = false;

extern "C" void EXPORT_API clean()
{

	printf("*** clean, already cleaned ? %i ***\n",isCleaned);
	
	for(int i=0;i<numActiveSenders;i++)
	{
		closeSender((char *)activeNames[i].c_str());
	}
	for(int i=0;i<numActiveSendersD9;i++)
	{
		closeSender((char *)activeNames[i].c_str());
	}

	delete[] senderNames; 
	delete[] newNames;
	
	FreeConsole();

	
	if(g_DeviceType == 1)
	{
		
		if(m_hRC && wglMakeCurrent(m_hdc, m_hRC)) {
			wglMakeCurrent(NULL, NULL);
			wglDeleteContext(m_hSharedRC);
			wglDeleteContext(m_hRC);
		}
		
		bOpenGL = false;
		
		m_hwnd = NULL;
		m_hdc = NULL; 
		m_hRC = NULL;
		m_hSharedRC = NULL;
		
	}

	
}


