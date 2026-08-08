#pragma once

#define D3DFVF_GRID_VERTEX (D3DFVF_XYZ | D3DFVF_DIFFUSE)
#define LINE_NUM	46

class CX2ViewerGrid : public CX2ViewerObject
{
	public:
		struct GRID_VERTEX
		{
			D3DXVECTOR3	pos;
			DWORD		color;
		};

	public:
		CX2ViewerGrid(void);
		virtual ~CX2ViewerGrid(void);

		virtual HRESULT OnFrameMove( double fTime, float fElapsedTime );
		virtual HRESULT OnFrameRender();
		virtual HRESULT OnResetDevice();
		virtual HRESULT OnLostDevice();

				void	SetOnOff( bool bOnOff ){ m_bOnOff = bOnOff; }

	private:
		bool	Init();

	private:
		
		//LPDIRECT3DDEVICE9			m_pd3dDevice;
		LPDIRECT3DVERTEXBUFFER9		m_pGridVB;
		CKTDGMatrix*				m_pMatrix;

		CKTDGFontManager::CKTDGFont*	m_pFont;

		CKTDIDevice*	m_pKTDIMouse;
		bool			m_bOnOff;
};