#pragma once

//Light Position 은 사용되지 않는다.

class CX2ViewerParam
{
	public:
		CX2ViewerParam(void);
		~CX2ViewerParam(void);

		void	GetRenderParam( CKTDGXRenderer::RenderParam* renderParam, IMPACT_DATA* impactData,
								std::vector<TEX_STAGE_DATA>& vecTexStageData );

		void	SetParamDlg( CDXUTDialog* pDlg );
		void	GetParamDlg( CDXUTDialog* pDlg );

		void	Reset();

		void	SetEffect();

	private:
		std::map<std::wstring, CKTDGXRenderer::RENDER_TYPE>			m_mapRenderType;
		std::map<std::wstring, CKTDGXRenderer::CARTOON_TEX_TYPE>	m_mapCartoonTexType;
		std::map<std::wstring, D3DCULL>		m_mapD3DCull;
		std::map<std::wstring, D3DBLEND>	m_mapD3DBlend;
		std::map<std::wstring, bool>		m_mapTrueFalse;

		CKTDGXRenderer::RenderParam*		m_pRenderParam;
		IMPACT_DATA							m_ImpactData;
		std::vector<TEX_STAGE_DATA>			m_vecTexStageData;
};
