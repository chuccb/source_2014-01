#pragma once

class CX2ViewerCamera : public CX2ViewerObject
{
	public:
		enum CAMERA_MODE
		{
			CM_NORMAL = 0,
			CM_NAVIGATION,
		};

		enum LOCAR_DIR
		{
			LOCAL_X = 0,
			LOCAL_Y,
			LOCAL_Z
		};

	public:
		CX2ViewerCamera();
		virtual ~CX2ViewerCamera(void);

		virtual	HRESULT OnFrameMove( double fTime, float fElapsedTime );
		virtual	HRESULT	OnFrameRender();
		virtual	HRESULT	OnResetDevice();

		D3DXVECTOR3	GetEye()	{ return m_sCamera.m_Eye;}
		D3DXVECTOR3	GetLookVec(){ return m_sCamera.m_LookAt;}
		D3DXVECTOR3	GetUpVec()	{ return m_sCamera.m_UpVec;}

		void		SetCameraMode( CAMERA_MODE cameraMode );
		CAMERA_MODE	GetCameraMode(){ return m_CameraMode; }

		void		CameraReset(){ Init(); }

	private:
		void		Init();
		void		CameraMode_Normal();
		void		CameraMode_Function();

		void		SetTracking( float Time = 1.0f );
		void		MoveLocal( float dist, LOCAR_DIR localDir = LOCAL_X );
		void		RotateLocal( LOCAR_DIR localDir, float angle );
		void		SetView( float fElapsedTime = 0.0f );


	private:
		
		//LPDIRECT3DDEVICE9			m_pd3dDevice;
		CKTDIDevice*				m_pMouse;
		CKTDIDevice*				m_pKeyboard;

		CKTDGCamera::CameraData		m_sCamera;
		D3DXMATRIX					m_matWorld;
		D3DXMATRIX					m_matProjection;
		D3DXVECTOR3					m_vView;		/// 카메라가 향하는 단위방향벡터
		D3DXVECTOR3					m_vCross;		/// 카마레의 측면벡터 cross( view, up )

		// MMO STyle
		D3DXVECTOR3					m_vAngle;
		float						m_fDist;
		float						m_fElapsedTime;

		long						m_nMouseNowPosX, m_nMousePrePosX;
		long						m_nMouseNowPosY, m_nMousePrePosY;
		long						m_nWheel;

		D3DXVECTOR3					m_TrackAt;
		float						m_fTrackTime;
		bool						m_bIsTrack;

		CAMERA_MODE					m_CameraMode;
};
