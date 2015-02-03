#define _CRT_SECURE_NO_WARNINGS

#include "UnityPluginInterface.h"
#include "SpoutHelpers.h"
#include "pthread.h"

#include "SpoutSDK.h"

Spout spout;

using namespace std;


typedef void (*SpoutStartPtr)(char * senderName,ID3D11ShaderResourceView * resourceView ,int width,int height);
typedef void (*SpoutStopPtr)(char * senderName);
SpoutStartPtr UnitySharingStarted;
SpoutStopPtr UnitySharingStopped;


extern "C"  void EXPORT_API SetSpoutHandlers( SpoutStartPtr sharingStartedHandler, SpoutStopPtr sharingStoppedHandler )
{
        UnitySharingStarted = sharingStartedHandler;
		UnitySharingStopped = sharingStoppedHandler;
}


// ************** SENDING ******************* //

ID3D11Texture2D * sendingTexture; //todo : find a way to be able to share more than one texture

extern "C" int EXPORT_API shareDX11(char * senderName, ID3D11Texture2D * texturePointer)
{
	
	/*
	AllocConsole();
    freopen("CONIN$",  "r", stdin);
    freopen("CONOUT$", "w", stdout);
    freopen("CONOUT$", "w", stderr);
	*/

	// Get the description of the passed texture
	D3D11_TEXTURE2D_DESC td;
	texturePointer->GetDesc(&td);
	td.BindFlags |=  D3D11_BIND_RENDER_TARGET;
	td.MiscFlags =  D3D11_RESOURCE_MISC_SHARED;
	td.Format = DXGI_FORMAT_R8G8B8A8_UNORM; //force format

	// Create a new shared texture with the same properties
	g_D3D11Device->CreateTexture2D(&td, NULL, &sendingTexture);
	HANDLE sharedHandle = getSharedHandleForTexture(sendingTexture);
	createSenderFromSharedHandle(senderName, sharedHandle,td);

	
	
	//Loopback test
	/*
	ID3D11ShaderResourceView * resourceView;
	ID3D11Resource * tempResource11;
	g_D3D11Device->OpenSharedResource(sharedHandle, __uuidof(ID3D11Resource), (void**)(&tempResource11));
	g_D3D11Device->CreateShaderResourceView(tempResource11,NULL, &resourceView);
	*/
	//UnitySharingStarted(0,resourceView,td.Width,td.Height);

	return 2;
}

extern "C" void EXPORT_API updateTexture(char* senderName, ID3D11Texture2D * texturePointer)
{
	
	if(g_pImmediateContext == NULL) UnityLog("Immediate context is null");
	if((ID3D11Resource *)texturePointer == NULL) UnityLog("Resource is null");

	//UnityLog("Update in plugin");
	D3D11_TEXTURE2D_DESC td;
	texturePointer->GetDesc(&td);

	// SPOUT
	// Check the texture against the global size used when the sender was created
	// and update the sender info if it has changed
	if(bInitialized) {
		if(td.Width != g_width || td.Height != g_height) {
			g_width			= td.Width;
			g_height		= td.Height;
			g_info.width	= (unsigned __int32)g_width;
			g_info.height	= (unsigned __int32)g_height;
			// This assumes that the sharehandle in the info structure remains 
			// the same as the texture this could be checked as well
			spout.UpdateSender(senderName,g_width,g_height,g_shareHandle,g_info.format);
		}
	}

	g_pImmediateContext->CopyResource(sendingTexture,texturePointer);
	g_pImmediateContext->Flush();
}


extern "C" void EXPORT_API stopSharing(char * senderName)
{
	spout.CloseSender(senderName);
}


// *************** RECEIVING ************************ //

typedef void (*SpoutSenderUpdatePtr)(int numSenders);
SpoutSenderUpdatePtr UnitySenderUpdate;
typedef void (*SpoutSenderStartedPtr)(char * senderName);
SpoutSenderStartedPtr UnitySenderStarted;
typedef void (*SpoutSenderStoppedPtr)(char * senderName);
SpoutSenderStoppedPtr UnitySenderStopped;


extern "C" int EXPORT_API getNumSenders()
{
	spoutSenders senders;
	return senders.GetSenderCount();
}


pthread_t receiveThread;
bool doReceive;
int lastSendersCount = 0;
void * receiveThreadLoop(void * data)
{
	
	AllocConsole();
    freopen("CONIN$",  "r", stdin);
    freopen("CONOUT$", "w", stdout);
    freopen("CONOUT$", "w", stderr);
	
	UnityLog("receive Thread Loop start !\n");
	printf("Unity Thread loop start !\n");

	char senderNames[32][256];

	while(doReceive)
	{
		int numSenders = getNumSenders();
		if(numSenders != lastSendersCount)
		{
			printf("Num Senders changed : %i\n",numSenders);
			UnitySenderUpdate(numSenders);

			char newNames[32][256];
			int i,j;
			bool found;
			
			UnityLog("## Sender Update \n\n");
			printf("\n\n################ SENDER UPDATE ###############\n\n");
			printf("> Old senders : ");

			for(i=0;i<lastSendersCount;i++)
			{
				printf("%s | ",senderNames[i]);
			}
			printf("\n");
			printf("> New senders : ");
			for(i=0;i<numSenders;i++)
			{
				spout.getSenderNameForIndex(i,newNames[i]);

				printf("%s | ",newNames[i]);
			}

			printf("\n");

			//NEW SENDERS DETECTION
			printf("\n** Detecting new senders **\n");
			for(i=0;i<numSenders;i++)
			{
				printf("Check for : %s  >>> ",newNames[i]);
				found = false;
				for(j = 0;j<lastSendersCount;j++)
				{
					printf(" | %s ",senderNames[j]);
					if(!found && strcmp(newNames[i],senderNames[j]) == 0) 
					{
							found = true;
							printf("(found !) ");
					}
				}

				printf("\nFound ? %i\n",found);
				if(!found) UnitySenderStarted(newNames[i]);
			}
			
			//SENDER STOP DETECTION
			printf("\n** Detecting leaving senders **\n");
			for(int i=0;i<lastSendersCount;i++)
			{
				found = false;
				printf("Check for : %s  >>> ",senderNames[i]);
				for(j = 0;j<numSenders;j++)
				{
					printf(" | %s  ",newNames[j]);
					if(!found && strcmp(senderNames[i],newNames[j]) == 0) 
					{
							found = true;
							printf("(found !) ");
					}
				}

				printf("\nFound ? %i\n",found);
				if(!found) UnitySenderStopped(senderNames[i]);
			}
			

			memcpy(senderNames,newNames,sizeof(newNames));
		}

		

		lastSendersCount = numSenders;
		
		Sleep(50);
	}

	UnityLog("Receive Stop !");
	return 0;
}

extern "C" void EXPORT_API stopReceiving()
{
	doReceive = false;
}

extern "C" bool EXPORT_API startReceiving(SpoutSenderUpdatePtr senderUpdateHandler,SpoutSenderStartedPtr senderStartedHandler,SpoutSenderStoppedPtr senderStoppedHandler) 
{
	//UnityLog("Start Receiving");
	UnitySenderUpdate = senderUpdateHandler;
	UnitySenderStarted = senderStartedHandler;
	UnitySenderStopped = senderStoppedHandler;

	if(doReceive)
	{
		pthread_join(receiveThread, NULL);
		doReceive = false;
	}

	doReceive = true;
	int ret = pthread_create(&receiveThread,NULL, receiveThreadLoop,NULL);

	lastSendersCount = 0;

	return true;//ret == 0; //success
}


// Receive a spout texture from a sender
extern "C" bool EXPORT_API receiveTexture(char * senderName)
{
	
	//UnityLog("Receive Texture !");
	/*
	AllocConsole();
    freopen("CONIN$",  "r", stdin);
    freopen("CONOUT$", "w", stdout);
    freopen("CONOUT$", "w", stderr);
	*/

	DWORD wFormat = 0;
	HANDLE hShareHandle = NULL;

	spoutSenders senders;	
	unsigned int w = 0;
	unsigned int h = 0;

	bool result = false;
	//senders.GetFirstSenderName(0,senderName);
	
	printf("Sender name ? %s\n",senderName);

	//result = spout.ReceiveActiveDXTexture(senderName,w,h,hShareHandle,wFormat);
	result = spout.ReceiveDXTexture(senderName,w,h,hShareHandle,wFormat);

	printf("Get sender name (%s) info result : %i\n",senderName, result);

	if(!result)
	{
		printf("No sender info, stopping here\n");
		return false;
	}

	printf("There is a sender : %s ! Texture width / height /handle : %i %i %p  format = %i\n",senderName,w,h,hShareHandle,wFormat);
	
	ID3D11Resource * tempResource11;
	ID3D11ShaderResourceView * rView;

	HRESULT openResult = g_D3D11Device->OpenSharedResource(hShareHandle, __uuidof(ID3D11Resource), (void**)(&tempResource11));
	g_D3D11Device->CreateShaderResourceView(tempResource11,NULL, &rView);

	UnitySharingStarted(senderName,rView,w,h);

	return true;
}





