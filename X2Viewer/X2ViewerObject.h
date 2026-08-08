#pragma once

class CX2ViewerObject
{
	public:
		enum OBJECT_STYLE
		{
			OS_NONE		= 0,
			OS_GRID,
			OS_CAMERA,
			OS_SKIN_MESH,
			OS_MESH,
			OS_UI,
			OS_WORLD_MESH,

			OS_PARTICLE,
			OS_PARTICLE_EDITOR,
		};

	public:
		CX2ViewerObject(void);
		virtual ~CX2ViewerObject(void);

		virtual HRESULT OnFrameMove( double fTime, float fElapsedTime ){return S_OK;}
		virtual HRESULT OnFrameRender(){return S_OK;}

		virtual bool MsgProc( HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam ){return false;}

		virtual HRESULT OnResetDevice(){return S_OK;}
		virtual HRESULT OnLostDevice(){return S_OK;}

		OBJECT_STYLE	GetObjectStyle()							{ return m_ObjectStyle; }
		void			SetObjectStyle( OBJECT_STYLE objectStyle )	{ m_ObjectStyle = objectStyle; }              
        
	private:
		OBJECT_STYLE	m_ObjectStyle;
};
