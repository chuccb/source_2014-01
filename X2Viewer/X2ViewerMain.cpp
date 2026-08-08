#include "dxstdafx.h"
#include ".\x2viewermain.h"

CX2ViewerMain::CX2ViewerMain(void)
{
	CX2ViewerCamera*	pCamera		= new CX2ViewerCamera();
	CX2ViewerGrid*		pGrid		= new CX2ViewerGrid();
	CX2ViewerSkinMesh*	pSkinMesh	= new CX2ViewerSkinMesh();
	CX2ViewerMesh*		pMesh		= new CX2ViewerMesh();
	CX2ViewerWorldMesh*	pWorldMesh	= new CX2ViewerWorldMesh();
	CX2ViewerUI*		pUI			= new CX2ViewerUI( this );
	CX2ViewerParticle*	pParticle	= new CX2ViewerParticle( pSkinMesh );
	
	m_vecObject.push_back( pCamera );
	m_vecObject.push_back( pGrid );
	m_vecObject.push_back( pWorldMesh );
	m_vecObject.push_back( pSkinMesh );
	m_vecObject.push_back( pMesh );
	m_vecObject.push_back( pUI );
	m_vecObject.push_back( pParticle );

//	m_pWeaponMatrix = NULL;
	m_SelectedAnimIndex = -1;

	m_AnimFileName = L"";
	m_AnimFileDir = L"";

	ReadAdditionalResourceFolder();

	g_pKTDXApp->SkipFrame();
}

CX2ViewerMain::~CX2ViewerMain(void)
{
	for( int i = 0; i < (int)m_vecObject.size(); ++i )
	{
		SAFE_DELETE( m_vecObject[i] );
	}
	m_vecObject.clear();
}

HRESULT CX2ViewerMain::OnFrameMove( double fTime, float fElapsedTime )
{
	for( int i = 0; i < (int)m_vecObject.size(); ++i )
	{
		m_vecObject[i]->OnFrameMove( fTime, fElapsedTime );
	}

	D3DVIEWPORT9 Viewport;
	g_pKTDXApp->GetDevice()->GetViewport( &Viewport );

	return S_OK;
}

HRESULT CX2ViewerMain::OnFrameRender()
{
	for( int i = 0; i < (int)m_vecObject.size(); ++i )
	{
		m_vecObject[i]->OnFrameRender();
	}

	return S_OK;
}

bool CX2ViewerMain::MsgProc( HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam )
{
	for( int i = 0; i < (int)m_vecObject.size(); ++i )
	{
		if( m_vecObject[i]->MsgProc( hWnd, uMsg, wParam, lParam ) == true )
			return true;
	}

	return false;
}

HRESULT CX2ViewerMain::OnResetDevice()
{
	for( int i = 0; i < (int)m_vecObject.size(); ++i )
	{
		m_vecObject[i]->OnResetDevice();
	}

	return S_OK;
}
HRESULT CX2ViewerMain::OnLostDevice()
{
	for( int i = 0; i < (int)m_vecObject.size(); ++i )
	{
		m_vecObject[i]->OnLostDevice();
	}

	return S_OK;
}

CX2ViewerObject*  CX2ViewerMain::GetObject( CX2ViewerObject::OBJECT_STYLE objectStyle )
{

	for( int i = 0; i < (int)m_vecObject.size(); ++i )
	{
		if( m_vecObject[i]->GetObjectStyle() == objectStyle )
			return m_vecObject[i];
	}

	return NULL;
}

void CX2ViewerMain::ReadAdditionalResourceFolder()
{
	KLuaManager	luaManager(g_pKTDXApp->GetLuaBinder()->GetLuaState(), 0, true);
	KGCMassFileManager::CMassFile::MASSFILE_MEMBERFILEINFO_POINTER Info;
	Info = g_pKTDXApp->GetDeviceManager()->GetMassFileManager()->LoadDataFile( L"AdditionalResourceDir.lua" );
	if( Info == NULL )
	{		
		return;
	}

	if( luaManager.DoMemory( Info->pRealData, Info->size ) == false )
	{		
		return;
	}

	if( luaManager.BeginTable( "ADDITIONAL_DIR" ) == true )
	{
		int tableIndex = 1;
		while( luaManager.BeginTable( tableIndex ) )
		{
			string dirName;

			bool bIncludeSubFolder;
			LUA_GET_VALUE( luaManager, 1, dirName, "" );
			LUA_GET_VALUE( luaManager, 2, bIncludeSubFolder, false );
		
			AddAdditionalResourceFolder( dirName, bIncludeSubFolder );
			
			tableIndex++;
			luaManager.EndTable();
		}
		luaManager.EndTable();
	}

}

void CX2ViewerMain::AddAdditionalResourceFolder( string folder, bool bIncludeSub )
{
	if( folder.empty() )
		return;
	
	g_pKTDXApp->GetDeviceManager()->GetMassFileManager()->AddDataDirectory( folder );

	HANDLE				hSearch;
	WIN32_FIND_DATAA	fd;
	char				szSearchPath[256];

	strcpy(szSearchPath, folder.c_str());
	strcat(szSearchPath, "\\*.*");

	hSearch = FindFirstFileA(szSearchPath, &fd);

	if(hSearch == INVALID_HANDLE_VALUE)
		return;

	if( bIncludeSub )
	{
		do
		{
			if(strcmp(fd.cFileName, ".") && strcmp(fd.cFileName, ".."))
			{
				if(fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
				{
					char	szNewSearchDir[256];
					string	strDir;

					strcpy(szNewSearchDir, folder.c_str());
					strcat(szNewSearchDir, "\\");
					strcat(szNewSearchDir, fd.cFileName);

					//#ifndef _SERVICE_
					if( strcmp( fd.cFileName, ".svn" ) != 0 &&
						strcmp( fd.cFileName, "Branches" ) != 0)
						//#endif _SERVICE_
					{
						strDir = szNewSearchDir;
						strDir += "\\";
						AddAdditionalResourceFolder( strDir, bIncludeSub );
					}
				}
			}

		}while(FindNextFileA(hSearch, &fd));

	}
	

	FindClose(hSearch);

	return;

}