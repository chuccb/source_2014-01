#pragma once

class CX2ViewerFileOS
{
	public:
		enum FILE_STATE
		{
			FS_NONE,
			FS_XFILE,
		};

	public:
		CX2ViewerFileOS(void);
		~CX2ViewerFileOS(void);

		FILE_STATE		FileOpen( WCHAR* wsFilter );
		FILE_STATE		FileSave( WCHAR* wsFilter );

		std::wstring	GetPullFileName();
		std::wstring	GetTitleFileName();

	private:
		OPENFILENAME	m_OFN;
		HWND			m_hWnd;
		std::wstring	m_wsPULLFileName, m_wsTITLEFileName;
};
