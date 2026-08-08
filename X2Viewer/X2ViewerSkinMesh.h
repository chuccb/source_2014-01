#pragma once

class CX2ViewerSkinMesh : public CX2ViewerObject
{
	public:
		enum SKINMESH_OPEN_TYPE
		{
			SOT_NONE = 0,
			SOT_SKINMESH,
			SOT_MESH,
			SOT_NOT_ADDMESH,
		};        

	public:
		CX2ViewerSkinMesh(void);
		virtual ~CX2ViewerSkinMesh(void);

		virtual HRESULT OnFrameMove( double fTime, float fElapsedTime );
		virtual HRESULT OnFrameRender();
		virtual HRESULT OnResetDevice();
		virtual HRESULT OnLostDevice();

        virtual bool MsgProc( HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam );

	public:
		bool				SetAnimXSkinMesh( std::wstring fileName );
		SKINMESH_OPEN_TYPE	InsertSkinMesh( std::wstring& fileName, std::wstring& dir );
		bool				AddWeapon( const WCHAR* pFullFileName, const WCHAR* attachFrameName, D3DXVECTOR3 rot );
        bool                AddAccessory( const WCHAR* pFullFileName, const WCHAR* attachFrameName, D3DXVECTOR3 rot );
		bool				DelModelXSkinMesh( std::wstring fileName );
		

		void	Reset();
		void	ChangeAnim( std::wstring animName );
		void	ChangeAnim( int index );

		void	SetScale( D3DXVECTOR3& vScale ){ m_vScale = vScale; }
		void	SetScale( float fX, float fY, float fZ );
		void	SetScaleX( float fX ){ m_vScale.x = fX; }
		void	SetScaleY( float fY ){ m_vScale.y = fY; }
		void	SetScaleZ( float fZ ){ m_vScale.z = fZ; }

        void    SetTransX( float fX)    { m_TransAccessory.x = fX; }
        void    SetTransY( float fY)    { m_TransAccessory.y = fY; }
        void    SetTransZ( float fZ)    { m_TransAccessory.z = fZ; }
        void    SetAccRotX(float fX)    { m_RotAccessory.x = fX; }
        void    SetAccRotY(float fY)    { m_RotAccessory.y = fY; }
        void    SetAccRotZ(float fZ)    { m_RotAccessory.z = fZ; }
        void    SetAccScaleX(float fX)  { m_ScaleAccessory.x = fX; }
        void    SetAccScaleY(float fY)  { m_ScaleAccessory.y = fY; }
        void    SetAccScaleZ(float fZ)  { m_ScaleAccessory.z = fZ; }

        void    SetRotX(float fX)       {m_RotWeapon.x = fX; }
        void    SetRotY(float fY)       {m_RotWeapon.y = fY; }
        void    SetRotZ(float fZ)       {m_RotWeapon.z = fZ; }

		void	SetLightPos( D3DXVECTOR3& vLightPos ){ m_RenderParam.lightPos = vLightPos; }
		void	SetLightPos( float fX, float fY, float fZ );
		void	SetLightPosX( float fX ){ m_RenderParam.lightPos.x = fX; }
		void	SetLightPosY( float fY ){ m_RenderParam.lightPos.y = fY; }
		void	SetLightPosZ( float fZ ){ m_RenderParam.lightPos.z = fZ; }

		void	SetWireFrameMode( bool bWireFrame ){ m_bWireframeMode = bWireFrame; }

		D3DXVECTOR3&	GetScale(){ return m_vScale; }
		D3DXVECTOR3&	GetLightPos(){ return m_RenderParam.lightPos; }
		void	SetLightOnOff( bool bIsLight ){ m_bIsLight = bIsLight; }

        bool	GetFrameNameList( std::vector<CKTDXDeviceXSkinMesh::MultiAnimFrame *>& vecFrameNameList );
		bool	GetAnimNameList( std::vector<std::wstring>& vecAnimNameList );

		bool	SetPlayOnOff();
		void	SetMotionOnOff( bool bIsMotion );
		bool	GetMotionOnOff(){ return m_bIsMotion; }
		void	SetPlaySpeed( float fPlaySpeed );
		void	GetAnimTime( float& fNowTime, float& fMaxTime );
		void	SetAnimTime( float fTime );
		void	SetPlayType( WCHAR* wszPlayType );
		void	SetBounding( bool bIsBounding ){ m_bIsBounding = bIsBounding; }
		bool	GetBounding(){ return m_bIsBounding; }
		void	SetShowAttackBox( bool bShowAttackBox ) { m_bShowAttackBox = bShowAttackBox; }
		bool	GetShowAttackBox() { return m_bShowAttackBox; }

		CKTDGXRenderer::RenderParam*	GetRenderParam(){ return &m_RenderParam; }
		IMPACT_DATA*					GetImpactData(){ return &m_ImpactData; }
		std::vector<TEX_STAGE_DATA>*	GetTexStageData(){ return &m_vecTexStageData; }
		CKTDGXSkinAnim::XSKIN_ANIM_PLAYTYPE	GetPlayType() { return m_AnimPlaytype; }

		CKTDGXSkinAnim*	GetXSkinAnim() { return m_pXSkinAnim; }
		CKTDGXSkinAnim* GetWeaponXSkinAnim() { return m_pXSkinWeapon; }
		
        void SetAttachPoint(WCHAR *szName);
        void SelectionChange( DWORD dwX,  DWORD dwY);
        BOOL BIntersectMeshContainer
            (
            CKTDXDeviceXSkinMesh::MultiAnimMC *pmcMesh,

            DWORD dwX, 
            DWORD dwY,
            D3DVIEWPORT9 *pViewport,
            D3DXMATRIX *pmatProjection,
            D3DXMATRIX *pmatView,
            float *pfDistMin,

            //SMeshContainer **ppmcHit,

            DWORD *pdwFaceHit,
            DWORD *pdwVertHit
            );

	private:
		CKTDGXSkinAnim*									m_pXSkinAnim;

		CKTDGXSkinAnim*									m_pXSkinWeapon;  
        
        CKTDGXRenderer*				                    m_pRendererAccessory;        
        CKTDXDeviceXMesh*                               m_pXMeshAccessory;
        

		CKTDXDeviceXET*									m_pXETWeapon;
		std::map<std::wstring, CKTDXDeviceXSkinMesh*>	m_mapSkinMesh;
		CKTDXDeviceXET*									m_pXET;

		CKTDXDeviceXMesh*								m_pXMeshLight;
		CKTDGMatrix*									m_pMatrixLight;

		CKTDXDeviceXMesh*								m_pXMeshSphere;
		CKTDGMatrix*									m_pMatrixSphere;

		D3DXVECTOR3			m_vScale;

		bool				m_bWireframeMode;
		bool				m_bIsLight;
		bool				m_bIsAnimPlay;
		bool				m_bIsMotion;
		bool				m_bIsBounding;
		bool				m_bShowAttackBox;

		CKTDGXSkinAnim::XSKIN_ANIM_PLAYTYPE	m_AnimPlaytype;

		CKTDGXRenderer::RenderParam	m_RenderParam;
		IMPACT_DATA					m_ImpactData;
		std::vector<TEX_STAGE_DATA>	m_vecTexStageData;
		float						m_ImpactNowAnimTime;

		D3DXMATRIX*			m_pMatrix;
		D3DXVECTOR3			m_RotWeapon;

        D3DXMATRIX*			m_pMatrixAccessory;
        D3DXVECTOR3			m_TransAccessory;
        D3DXVECTOR3			m_RotAccessory;
        D3DXVECTOR3			m_ScaleAccessory;

        bool                m_bAttachPoint;
        CKTDGXRenderer*		m_pRendererPoint;
        CKTDXDeviceXMesh*	m_pXMeshPoint;
        D3DXMATRIX*			m_pMatrixPoint;

		//LPD3DXMESH	m_pSMesh;
		//CKTDGMatrix* m_pSMatrix;
};
