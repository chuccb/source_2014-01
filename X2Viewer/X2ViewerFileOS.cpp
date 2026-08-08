#include "dxstdafx.h"
#include ".\x2viewerfileos.h"

CX2ViewerFileOS::CX2ViewerFileOS(void)
{
	m_hWnd	= g_pKTDXApp->GetHWND();
}

CX2ViewerFileOS::~CX2ViewerFileOS(void)
{
}

CX2ViewerFileOS::FILE_STATE CX2ViewerFileOS::FileOpen( WCHAR* wsFilter )
{
	WCHAR wstrFile[256]			= L"";
	WCHAR wstrFileTitle[256]	= L"";
	WCHAR wstrDir[256]			= L"";

	ZeroMemory( &m_OFN, sizeof( OPENFILENAME));
	m_wsPULLFileName.clear();
	m_wsTITLEFileName.clear();

	m_OFN.lStructSize		= sizeof(OPENFILENAME);
	m_OFN.hwndOwner			= m_hWnd;
	m_OFN.lpstrFilter		= wsFilter;//L"X/Lua(*.x|*.lua)\0*.lua;*.x\0";//L"lua(*.lua)\0*.lua\0X-file(*.x)\0*.x\0";
	m_OFN.lpstrFile			= wstrFile;
	m_OFN.nMaxFile			= 256;
	m_OFN.lpstrTitle		= L"::: 파일열기 :::";
	m_OFN.lpstrFileTitle	= wstrFileTitle;
	m_OFN.nMaxFileTitle		= 256;
	//GetCurrentDirectory( MAX_PATH, wstrDir);
	//m_OFN.lpstrInitialDir	= wstrDir;
	m_OFN.Flags				= OFN_PATHMUSTEXIST;

	if( GetOpenFileNameW(&m_OFN) != 0)
	{
		m_wsPULLFileName	= wstrFile;
		m_wsTITLEFileName	= wstrFileTitle;

		if( wstrFile[lstrlenW(wstrFile)-1] == L'X' || wstrFile[lstrlenW(wstrFile)-1] == L'x' 
            || wstrFile[lstrlenW(wstrFile)-1] == L'Y' || wstrFile[lstrlenW(wstrFile)-1] == L'y' )
			return FS_XFILE;
	}

	return FS_NONE;
}

CX2ViewerFileOS::FILE_STATE CX2ViewerFileOS::FileSave( WCHAR* wsFilter )
{
	WCHAR wstrFile[256]	= L"";
	WCHAR wstrDir[256]	= L"";
	ZeroMemory( &m_OFN, sizeof( OPENFILENAME));
	m_wsPULLFileName.clear();

	m_OFN.lStructSize		= sizeof(OPENFILENAME);
	m_OFN.hwndOwner			= m_hWnd;
	m_OFN.lpstrFilter		= wsFilter;//L"lua(*.lua)\0*.lua\0";
	m_OFN.lpstrFile			= wstrFile;
	m_OFN.nMaxFile			= 256;
	m_OFN.lpstrTitle		= L"::: 파일저장 :::";
	m_OFN.lpstrFileTitle	= NULL;
	//GetCurrentDirectory( MAX_PATH, wstrDir);
	//m_OFN.lpstrInitialDir	= wstrDir;

	if( GetSaveFileName( &m_OFN))
	{
		m_wsPULLFileName	= wstrFile;

		//if( wcscmp( &wstrFile[lstrlenW(wstrFile) - 3], L"Lin") == 0)
		//	return FSAVE_LIN;

		//if( wcscmp( &wstrFile[lstrlenW(wstrFile) - 3], L"lua") == 0)
		//	return FSAVE_LUA;

		//if( wcscmp( &wstrFile[lstrlenW(wstrFile) - 3], L"map") == 0)
		//	return FSAVE_MAP;
	}

	return FS_NONE;
}

std::wstring CX2ViewerFileOS::GetPullFileName()
{
	return m_wsPULLFileName;
}
std::wstring CX2ViewerFileOS::GetTitleFileName()
{
	return m_wsTITLEFileName;
}