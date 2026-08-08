#include "dxstdafx.h"
#include ".\x2viewerskinmesh.h"

CX2ViewerSkinMesh::CX2ViewerSkinMesh(void)
{
	CX2ViewerObject::SetObjectStyle( CX2ViewerObject::OS_SKIN_MESH );

	m_pXSkinAnim		= NULL;
	m_pXSkinWeapon		= NULL;    
	m_pXET				= NULL;
	m_pXETWeapon		= NULL;
	m_pXMeshLight		= NULL;
	m_bWireframeMode	= false;
	m_bIsLight			= true;
	m_bIsAnimPlay		= true;
	m_bIsMotion			= false;
	m_ImpactNowAnimTime	= 0.0f;
	m_bIsBounding		= false;
	m_bShowAttackBox	= false;

	m_AnimPlaytype = CKTDGXSkinAnim::XAP_LOOP;

	//Light 위치표기 메쉬
	m_pXMeshLight		= g_pKTDXApp->GetDeviceManager()->OpenXMesh( L"Sun.X" );
	m_pMatrixLight		= new CKTDGMatrix( g_pKTDXApp->GetDevice() );
	m_pMatrixLight->Scale( 0.5f, 0.5f, 0.5f );

	//충돌박스 메쉬
	m_pXMeshSphere		= g_pKTDXApp->GetDeviceManager()->OpenXMesh( L"Bounding_Sphere.X" );
	m_pMatrixSphere		= new CKTDGMatrix( g_pKTDXApp->GetDevice() );

	TEX_STAGE_DATA texStageData;
	m_vecTexStageData.push_back( texStageData );
	m_vecTexStageData.push_back( texStageData );
	m_vecTexStageData.push_back( texStageData );

	m_pMatrix = NULL;
	m_RotWeapon = D3DXVECTOR3( 0, 0, 0 );

    m_pRendererAccessory	= new CKTDGXRenderer( g_pKTDXApp->GetDevice() );
    
    m_pXMeshAccessory	= NULL;
    m_pMatrixAccessory  = NULL;
    m_TransAccessory    = D3DXVECTOR3( 0, 0, 0 );
    m_RotAccessory      = D3DXVECTOR3( 0, 0, 0 );
    m_ScaleAccessory    = D3DXVECTOR3( 100, 100, 100 );
	//D3DXCreateCylinder( g_pKTDXApp->GetDevice(), 100.0f, 100.0f, 200.0f, 10, 10, &m_pSMesh, NULL );
	//m_pSMatrix = new CKTDGMatrix( g_pKTDXApp->GetDevice() );


#ifdef X2VIEWER
    m_bAttachPoint      = false;
    m_pRendererPoint	= new CKTDGXRenderer( g_pKTDXApp->GetDevice() );
    m_pXMeshPoint		= g_pKTDXApp->GetDeviceManager()->OpenXMesh( L"Bounding_Sphere.X" );
    m_pMatrixPoint		= NULL;
#endif
}

CX2ViewerSkinMesh::~CX2ViewerSkinMesh(void)
{
	//SAFE_RELEASE( m_pSMesh );
	//SAFE_DELETE( m_pSMatrix );

	g_pKTDXApp->GetDeviceManager()->CloseDevice( m_pXMeshLight->GetDeviceID() );
    SAFE_DELETE( m_pMatrixLight );

	g_pKTDXApp->GetDeviceManager()->CloseDevice( m_pXMeshSphere->GetDeviceID() );
	SAFE_DELETE( m_pMatrixSphere );


#ifdef X2VIEWER
    SAFE_DELETE( m_pRendererPoint );
    g_pKTDXApp->GetDeviceManager()->CloseDevice( m_pXMeshPoint->GetDeviceID() );
#endif
    SAFE_DELETE( m_pRendererAccessory );

	Reset();
}

HRESULT CX2ViewerSkinMesh::OnFrameMove( double fTime, float fElapsedTime )
{
	if( m_pXSkinAnim == NULL || m_pXSkinAnim->GetAnimXSkinMesh() == NULL )
		return S_OK;

	CKTDGXRenderer::RenderParam* pRenderParam = m_pXSkinAnim->GetRenderParam();
	*pRenderParam = m_RenderParam;

	D3DXVECTOR3 position = m_pXSkinAnim->GetMatrix().GetPos();
	//모션 이동
	position.x += m_pXSkinAnim->GetMotionOffset().x/3.0f;
	position.z += m_pXSkinAnim->GetMotionOffset().z/3.0f;

	m_pXSkinAnim->GetMatrix().Scale( m_vScale );
	m_pXSkinAnim->GetMatrix().Move( position);
	m_pXSkinAnim->OnFrameMove( fTime, fElapsedTime );

	if( m_pXSkinAnim->GetNowAnimationTime() == 0.0f)
	{
		m_pXSkinAnim->GetMatrix().Move( D3DXVECTOR3( 0.0f,0.0f,0.0f));
	}

	//Impact Anim setting
	if( m_ImpactData.fAnimTime != 0.0f )
	{
		if( m_ImpactData.fAnimTime <= m_ImpactNowAnimTime )
		{
			m_ImpactNowAnimTime = 0.0f;
			pRenderParam->fLightFlowImpact = m_ImpactData.fMin;
		}
		else
		{
			m_ImpactNowAnimTime += fElapsedTime;
		}
		pRenderParam->fLightFlowImpact = m_ImpactData.fMin + ((m_ImpactData.fMax - m_ImpactData.fMin)*(m_ImpactNowAnimTime/m_ImpactData.fAnimTime));
	}
	else
	{
		m_ImpactNowAnimTime = 0.0f;
	}

	//Texture Stage 0~2 Setting
	for( int i = 0; i < (int)m_vecTexStageData.size(); ++i )
	{
		if( m_vecTexStageData[i].fAnimTime != 0.0f )
		{
			if( m_vecTexStageData[i].fAnimTime <= m_vecTexStageData[i].fNowAnimTime )
			{
				m_vecTexStageData[i].fNowAnimTime = 0.0f;
				switch( i )
				{
				case 0: pRenderParam->texOffsetStage0 = m_vecTexStageData[i].vMin; break;
				case 1: pRenderParam->texOffsetStage1 = m_vecTexStageData[i].vMin; break;
				case 2: pRenderParam->texOffsetStage2 = m_vecTexStageData[i].vMin; break;
				}
			}
			else
			{
				m_vecTexStageData[i].fNowAnimTime += fElapsedTime;
			}
			switch( i )
			{
			case 0:
				{
					pRenderParam->texOffsetStage0.x = m_vecTexStageData[i].vMin.x + ((m_vecTexStageData[i].vMax.x - m_vecTexStageData[i].vMin.x)*(m_vecTexStageData[i].fNowAnimTime/m_vecTexStageData[i].fAnimTime));
					pRenderParam->texOffsetStage0.y = m_vecTexStageData[i].vMin.y + ((m_vecTexStageData[i].vMax.y - m_vecTexStageData[i].vMin.y)*(m_vecTexStageData[i].fNowAnimTime/m_vecTexStageData[i].fAnimTime));
				}
				break;
			case 1:
				{
					pRenderParam->texOffsetStage1.x = m_vecTexStageData[i].vMin.x + ((m_vecTexStageData[i].vMax.x - m_vecTexStageData[i].vMin.x)*(m_vecTexStageData[i].fNowAnimTime/m_vecTexStageData[i].fAnimTime));
					pRenderParam->texOffsetStage1.y = m_vecTexStageData[i].vMin.y + ((m_vecTexStageData[i].vMax.y - m_vecTexStageData[i].vMin.y)*(m_vecTexStageData[i].fNowAnimTime/m_vecTexStageData[i].fAnimTime));
				}
				break;
			case 2:
				{
					pRenderParam->texOffsetStage2.x = m_vecTexStageData[i].vMin.x + ((m_vecTexStageData[i].vMax.x - m_vecTexStageData[i].vMin.x)*(m_vecTexStageData[i].fNowAnimTime/m_vecTexStageData[i].fAnimTime));
					pRenderParam->texOffsetStage2.y = m_vecTexStageData[i].vMin.y + ((m_vecTexStageData[i].vMax.y - m_vecTexStageData[i].vMin.y)*(m_vecTexStageData[i].fNowAnimTime/m_vecTexStageData[i].fAnimTime));
				}
				break;
			}
		}
		else
		{
			m_vecTexStageData[i].fNowAnimTime = 0.0f;
		}
	}

	if( m_bIsLight == true )
	{
        m_pMatrixLight->Move( m_RenderParam.lightPos );
	}

	if ( m_pXSkinWeapon != NULL )
	{
		CKTDGXRenderer::RenderParam* pEqipRenderParam = m_pXSkinWeapon->GetRenderParam();
		//pEqipRenderParam->worldMatrix = *m_pMatrix;
		if( pRenderParam != NULL )
		{				
			*pEqipRenderParam = m_RenderParam;
		}

		D3DXMATRIX matDX = *m_pMatrix;
		D3DXMATRIX matRot;

		D3DXMatrixRotationYawPitchRoll( &matRot, m_RotWeapon.y, m_RotWeapon.x, m_RotWeapon.z );

		matDX = matRot * matDX;

		m_pXSkinWeapon->SetDXMatrix( matDX );
		m_pXSkinWeapon->OnFrameMove( fTime, fElapsedTime );
		//m_pXSkinWeapon->GetMatrix().RotateRel( m_RotWeapon );
	}

    //if ( m_pXMeshAccessory != NULL )
    //{
    //    CKTDGXRenderer::RenderParam* pRenderParam = m_pRendererAccessory->GetRenderParam();
    //    *pRenderParam = m_RenderParam;
    //    pRenderParam->bAlphaBlend = true;
    //    //pRenderParam->renderType = CKTDGXRenderer::RT_CARTOON_BLACK_EDGE;
    //    //pRenderParam->fOutLineWide	= 1.5f;
    //    //INIT_VECTOR3( m_RenderParam.lightPos, 500, 500, 500 );
    //    D3DXMATRIX localMove, matRot, matScale;
    //    D3DXMATRIX matTrans1, matTrans2, matTrans3;

    //    D3DXVECTOR3 rotDegree = D3DXVECTOR3((D3DX_PI / 180.0f) * m_RotAccessory.x, (D3DX_PI / 180.0f) * m_RotAccessory.y, (D3DX_PI / 180.0f) * m_RotAccessory.z);
    //    D3DXVECTOR3 scaleDegree = D3DXVECTOR3(m_ScaleAccessory.x / 100.0f, m_ScaleAccessory.y / 100.0f, m_ScaleAccessory.z / 100.0f);

    //    D3DXMatrixScaling(&matScale, scaleDegree.x, scaleDegree.y, scaleDegree.z);
    //    D3DXMatrixRotationYawPitchRoll( &matRot, rotDegree.y, rotDegree.x, rotDegree.z );
    //    D3DXMatrixTranslation( &localMove, m_TransAccessory.x, m_TransAccessory.y, m_TransAccessory.z );

    //    D3DXMatrixMultiply( &matTrans1, &localMove, m_pMatrixAccessory); 
    //    D3DXMatrixMultiply( &matTrans2, &matRot, &matTrans1); 
    //    D3DXMatrixMultiply( &matTrans3, &matScale, &matTrans2);

    //    pRenderParam->worldMatrix = matTrans3; 
    //}	

    //if(m_bAttachPoint)
    //{
    //    D3DXMATRIX  matScale, wMat;
    //    CKTDGXRenderer::RenderParam* pRenderParam = m_pRendererPoint->GetRenderParam();

    //    D3DXMatrixScaling(&matScale, 3.0f, 3.0f, 3.0f);
    //    D3DXMatrixMultiply(&wMat, &matScale, m_pMatrixPoint);
    //    pRenderParam->worldMatrix = wMat;
    //}

	return S_OK;
}

HRESULT CX2ViewerSkinMesh::OnFrameRender()
{
	if( m_pXSkinAnim == NULL || m_pXSkinAnim->GetAnimXSkinMesh() == NULL )
		return S_OK;

	//-- Wireframe Mode
	if( m_bWireframeMode )
		CKTDGStateManager::SetRenderState( D3DRS_FILLMODE, D3DFILL_WIREFRAME );
	else
		CKTDGStateManager::SetRenderState( D3DRS_FILLMODE, D3DFILL_SOLID );

	m_pXSkinAnim->OnFrameRender();  


	if ( m_pXSkinWeapon != NULL )
		m_pXSkinWeapon->OnFrameRender();

    if( m_pXMeshAccessory != NULL)
    {
		CKTDGXRenderer::RenderParam renderParam;
		renderParam = m_RenderParam;
		renderParam.bAlphaBlend = true;
		D3DXMATRIX localMove, matRot, matScale;
		D3DXMATRIX matTrans1, matTrans2, matTrans3;

		D3DXVECTOR3 rotDegree = D3DXVECTOR3((D3DX_PI / 180.0f) * m_RotAccessory.x, (D3DX_PI / 180.0f) * m_RotAccessory.y, (D3DX_PI / 180.0f) * m_RotAccessory.z);
		D3DXVECTOR3 scaleDegree = D3DXVECTOR3(m_ScaleAccessory.x / 100.0f, m_ScaleAccessory.y / 100.0f, m_ScaleAccessory.z / 100.0f);

		D3DXMatrixScaling(&matScale, scaleDegree.x, scaleDegree.y, scaleDegree.z);
		D3DXMatrixRotationYawPitchRoll( &matRot, rotDegree.y, rotDegree.x, rotDegree.z );
		D3DXMatrixTranslation( &localMove, m_TransAccessory.x, m_TransAccessory.y, m_TransAccessory.z );

		D3DXMatrixMultiply( &matTrans1, &localMove, m_pMatrixAccessory); 
		D3DXMatrixMultiply( &matTrans2, &matRot, &matTrans1); 
		D3DXMatrixMultiply( &matTrans3, &matScale, &matTrans2);

        m_pRendererAccessory->OnFrameRender( renderParam, matTrans3, *m_pXMeshAccessory );
    }

	if( m_bIsBounding == true )
	{
		CKTDGStateManager::SetRenderState( D3DRS_FILLMODE, D3DFILL_WIREFRAME );

		//const CKTDXCollision::CollisionDataList& collisionDataList = m_pXSkinAnim->GetCollisionDataList();
		//CKTDXCollision::CollisionData* pCollisionData = NULL;

		//int nMax = (int)collisionDataList.size();
		//for( int i = 0; i < nMax; ++i )
		//{
		//	pCollisionData = collisionDataList[i];//m_pXSkinAnim->GetCollisionDataList()[i];
		//	if( pCollisionData->m_CollisionType == CKTDXCollision::CT_SPHERE )
		//	{
		//		m_pMatrixSphere->Move( pCollisionData->GetPointStart() );
		//		float fScale = pCollisionData->GetScaleRadius();
		//		m_pMatrixSphere->Scale( fScale, fScale, fScale );
		//		m_pMatrixSphere->UpdateWorldMatrix();
		//		m_pXMeshSphere->Render();
		//	}
		//}

		BOOST_TEST_FOREACH( CKTDXCollision::CollisionData*, pCollisionData, m_pXSkinAnim->GetCollisionDataList() )
		{
			if( pCollisionData->m_CollisionType == CKTDXCollision::CT_SPHERE )
			{
				m_pMatrixSphere->Move( pCollisionData->GetPointStart() );
				float fScale = pCollisionData->GetScaleRadius();
				m_pMatrixSphere->Scale( fScale, fScale, fScale );
				m_pMatrixSphere->UpdateWorldMatrix();
				m_pXMeshSphere->Render();
			}
		}
	}

	if( m_bShowAttackBox == true )
	{
		CKTDGStateManager::SetRenderState( D3DRS_FILLMODE, D3DFILL_WIREFRAME );
		
		//CKTDXCollision::CollisionData* pCollisionData = NULL;
		//int nMax = (int)m_pXSkinAnim->m_AttackDataList.size();
		//for( int i = 0; i < nMax; ++i )
		BOOST_TEST_FOREACH( CKTDXCollision::CollisionData*, pCollisionData, m_pXSkinAnim->GetAttackDataList() )
		{			
			//pCollisionData = m_pXSkinAnim->m_AttackDataList[i];
			if( pCollisionData->m_CollisionType == CKTDXCollision::CT_SPHERE )
			{
				m_pMatrixSphere->Move( pCollisionData->GetPointStart() );
				float fScale = pCollisionData->GetScaleRadius();
				m_pMatrixSphere->Scale( fScale, fScale, fScale );
				m_pMatrixSphere->UpdateWorldMatrix();
				m_pXMeshSphere->Render();
			}
			else if( pCollisionData->m_CollisionType == CKTDXCollision::CT_LINE )
			{
				D3DXVECTOR3 vPos = pCollisionData->GetPointStart();
				D3DXVECTOR3 vPosDelta = pCollisionData->GetPointEnd() - pCollisionData->GetPointStart();
				vPosDelta /= 29.f;
				for( int j=0; j<30; j++ )
				{
					m_pMatrixSphere->Move( vPos );
					float fScale = pCollisionData->GetScaleRadius();
					m_pMatrixSphere->Scale( 1, 1, 1 );
					m_pMatrixSphere->UpdateWorldMatrix();
					m_pXMeshSphere->Render();
					vPos += vPosDelta;
				}			
				//g_pKTDXApp->SetWorldTransform( pCollisionData->pCombineMatrix );
				//m_pSMesh->DrawSubset( 0 );
			}
		}

		if( NULL != m_pXSkinWeapon )
		{
			//CKTDXCollision::CollisionData* pCollisionData = NULL;
			//int nMax = (int)m_pXSkinWeapon->m_AttackDataList.size();
			//for( int i = 0; i < nMax; ++i )
			BOOST_TEST_FOREACH( CKTDXCollision::CollisionData*, pCollisionData, m_pXSkinWeapon->GetAttackDataList() )
			{
				//pCollisionData = m_pXSkinWeapon->m_AttackDataList[i];
				if( pCollisionData->m_CollisionType == CKTDXCollision::CT_SPHERE )
				{
					m_pMatrixSphere->Move( pCollisionData->GetPointStart() );
					float fScale = pCollisionData->GetScaleRadius();
					m_pMatrixSphere->Scale( fScale, fScale, fScale );
					m_pMatrixSphere->UpdateWorldMatrix();
					m_pXMeshSphere->Render();
				}
				else if( pCollisionData->m_CollisionType == CKTDXCollision::CT_LINE )
				{
					D3DXVECTOR3 vPos = pCollisionData->GetPointStart();
					D3DXVECTOR3 vPosDelta = pCollisionData->GetPointEnd() - pCollisionData->GetPointStart();
					vPosDelta /= 29.f;
					for( int j=0; j<30; j++ )
					{
						m_pMatrixSphere->Move( vPos );
						float fScale = pCollisionData->GetScaleRadius();
						m_pMatrixSphere->Scale( 1, 1, 1 );
						m_pMatrixSphere->UpdateWorldMatrix();
						m_pXMeshSphere->Render();
						vPos += vPosDelta;
					}			
					//g_pKTDXApp->SetWorldTransform( pCollisionData->pCombineMatrix );
					//m_pSMesh->DrawSubset( 0 );
				}
			}
		}


	}

    if(m_bAttachPoint)
    {
		D3DXMATRIX  matScale, wMat;
		CKTDGXRenderer::RenderParam renderParam;
		renderParam.color = D3DXCOLOR(1.0f, 0.0f, 0.0f, 1.0f);

		D3DXMatrixScaling(&matScale, 3.0f, 3.0f, 3.0f);
		D3DXMatrixMultiply(&wMat, &matScale, m_pMatrixPoint);

        m_pRendererPoint->OnFrameRender( renderParam, wMat, *m_pXMeshPoint );
    }
    

	//Light Rendering
	if( m_bIsLight == true )
	{
		CKTDGStateManager::SetRenderState( D3DRS_FILLMODE, D3DFILL_SOLID );
		m_pMatrixLight->UpdateWorldMatrix( CKTDGMatrix::BT_ALL );

		CKTDGStateManager::PushRenderState( D3DRS_ALPHABLENDENABLE,	true );
		m_pXMeshLight->Render();
		CKTDGStateManager::PopRenderState( D3DRS_ALPHABLENDENABLE );
	}

	return S_OK;
}

HRESULT CX2ViewerSkinMesh::OnResetDevice()
{
	if( m_pXSkinAnim == NULL || m_pXSkinAnim->GetAnimXSkinMesh() == NULL )
		return S_OK;

	m_pXSkinAnim->OnResetDevice();
    m_pRendererAccessory->OnResetDevice();
    m_pRendererPoint->OnResetDevice();

	return S_OK;
}

HRESULT CX2ViewerSkinMesh::OnLostDevice()
{
	if( m_pXSkinAnim == NULL || m_pXSkinAnim->GetAnimXSkinMesh() == NULL )
		return S_OK;

	m_pXSkinAnim->OnLostDevice();

    
	return S_OK;
}


bool CX2ViewerSkinMesh::SetAnimXSkinMesh( std::wstring fileName )
{
	//하나의 SkinMesh에 하나의 애니메이션 파일이 들어가므로 만약 삭제가
	//되어있지 않은 상황이 올경우를 대비해 삭제후 할당한다.
	//if( m_pXSkinAnim != NULL )
	//{
	//	SAFE_DELETE( m_pXSkinAnim );
	//	ERRORMSG( L"애니메이션 셋팅 상황이 잘못되었음.! 이런" );
	//}

	//m_pXSkinAnim = new CKTDGXSkinAnim( g_pKTDXApp->GetDevice() );
	m_pXSkinAnim = CKTDGXSkinAnim::CreateSkinAnim();

	CKTDXDeviceXSkinMesh* pMotion = NULL;
	pMotion = g_pKTDXApp->GetDeviceManager()->OpenXSkinMesh( fileName );

	if( pMotion != NULL )
	{
		m_pXSkinAnim->SetAnimXSkinMesh( pMotion );

		m_mapSkinMesh.insert( std::make_pair( fileName, pMotion ) );

		return true;
	}

	return false;
}

CX2ViewerSkinMesh::SKINMESH_OPEN_TYPE CX2ViewerSkinMesh::InsertSkinMesh( std::wstring& fileName, std::wstring& dir )
{
	CKTDXDeviceXSkinMesh* pMotion = NULL;
	pMotion = g_pKTDXApp->GetDeviceManager()->OpenXSkinMesh( fileName );

	if( pMotion == NULL )
	{
		std::string strDir;
		ConvertWCHARToChar( strDir, dir.c_str() );
		g_pKTDXApp->GetDeviceManager()->GetMassFileManager()->AddDataDirectory( strDir );

		pMotion = g_pKTDXApp->GetDeviceManager()->OpenXSkinMesh( fileName );

		if( pMotion == NULL )
		{
			WARNINGMSG( L"Skin Mesh 파일 열기 실패~ 이런." );
			return SOT_NONE;
		}
	}

	if( pMotion->GetIsOnlyModelData() == false )
	{
		Reset();

		//m_pXSkinAnim = new CKTDGXSkinAnim( g_pKTDXApp->GetDevice() );
		m_pXSkinAnim = CKTDGXSkinAnim::CreateSkinAnim();

		//
		wstring xetName = fileName + L"ET";

		m_pXET = g_pKTDXApp->GetDeviceManager()->OpenXET( xetName );

		m_pXSkinAnim->SetAnimXSkinMesh( pMotion, m_pXET );
		
		// 모션 xet파일 있으면 불러들이게끔.. 변경 06 12 14 태욱

		m_pXSkinAnim->AddAnimXSkinMesh( pMotion );

		m_bIsMotion = true;

		m_mapSkinMesh.insert( std::make_pair( fileName, pMotion ) );

		m_pXSkinAnim->ChangeAnim( 0 );
		m_pXSkinAnim->Play( m_AnimPlaytype );

		return SOT_SKINMESH;
	}
	else
	{
		if( pMotion != NULL && m_pXSkinAnim != NULL )
		{
			if( m_pXSkinAnim->GetAnimXSkinMesh()->GetFrameNum() == pMotion->GetFrameNum() )
			{
				CKTDXDeviceXET* pModelXET = NULL;

				wstring xetName = fileName + L"ET";

				pModelXET = g_pKTDXApp->GetDeviceManager()->OpenXET( xetName );

				m_pXSkinAnim->AddModelXSkinMesh( pMotion, pModelXET, NULL, pModelXET );

				m_mapSkinMesh.insert( std::make_pair( fileName, pMotion ) );

				return SOT_MESH;
			}

            //g_pKTDXApp->GetDeviceManager()->CloseCachedXSkinMeshes();
			g_pKTDXApp->GetDeviceManager()->CloseDevice( pMotion->GetDeviceID() );

			if( MessageBox( g_pKTDXApp->GetHWND(), L"넣을수 없는 몸뚱이 메쉬만 랜더링 할까욤..?", L"Info", MB_OKCANCEL ) 
				== IDOK )
				return SOT_NOT_ADDMESH;
		}
		else
		{
            //g_pKTDXApp->GetDeviceManager()->CloseCachedXSkinMeshes();
			g_pKTDXApp->GetDeviceManager()->CloseDevice( pMotion->GetDeviceID() );
			return SOT_NOT_ADDMESH;
		}
	}

	return SOT_NONE;
}

bool CX2ViewerSkinMesh::AddWeapon( const WCHAR* pFullFileName, const WCHAR* attachFrameName, D3DXVECTOR3 rot )
{	
	WCHAR drive[10] = L"";
	WCHAR dir[256]   = L"";
	WCHAR fname[256] = L"";
	WCHAR ext[10]    = L"";

	WCHAR fileName[256] = L"";
	WCHAR PullDir[256] = L"";
	_wsplitpath( pFullFileName, drive, dir, fname, ext);

	wcscat( fileName, fname);
	wcscat( fileName, ext);

	wcscat( PullDir, drive );
	wcscat( PullDir, dir );



	CKTDXDeviceXSkinMesh* pMotion = NULL;
	pMotion = g_pKTDXApp->GetDeviceManager()->OpenXSkinMesh( fileName );

	if( pMotion == NULL )
	{
		std::string strDir;
		ConvertWCHARToChar( strDir, PullDir );
		g_pKTDXApp->GetDeviceManager()->GetMassFileManager()->AddDataDirectory( strDir );

		pMotion = g_pKTDXApp->GetDeviceManager()->OpenXSkinMesh( fileName );

		if( pMotion == NULL )
		{
			WARNINGMSG( L"Skin Mesh 파일 열기 실패~ 이런." );
			return false;
		}
	}

	if ( m_pXSkinAnim == NULL )
	{
		WARNINGMSG( L"애니메이션 파일이 로드 되지 않았어요." );
		SAFE_CLOSE( pMotion );
		return false;
	}

	CKTDXDeviceXSkinMesh::MultiAnimFrame* pFrame = NULL;
	pFrame		= m_pXSkinAnim->GetCloneFrame( attachFrameName );

	if ( pFrame == NULL )
	{
		WARNINGMSG( L"본 이름이 정확하지 않아요!" );
		SAFE_CLOSE( pMotion );
		return false;
	}
	
	//SAFE_DELETE( m_pXSkinWeapon );
	SAFE_CLOSE( m_pXETWeapon ); 
	m_pMatrix = NULL;


	//m_pXSkinWeapon = new CKTDGXSkinAnim( g_pKTDXApp->GetDevice() );
	m_pXSkinWeapon = CKTDGXSkinAnim::CreateSkinAnim();

	wstring fileNameWstring = fileName;
	//
	wstring xetName = fileNameWstring + L"ET";

	m_pXETWeapon = g_pKTDXApp->GetDeviceManager()->OpenXET( xetName );

	m_pXSkinWeapon->SetAnimXSkinMesh( pMotion, m_pXET  );

	// 모션 xet파일 있으면 불러들이게끔.. 변경 06 12 14 태욱

	m_pXSkinWeapon->AddAnimXSkinMesh( pMotion );

	m_bIsMotion = true;

	m_mapSkinMesh.insert( std::make_pair( fileName, pMotion ) );

	m_pXSkinWeapon->ChangeAnim( 0 );
	m_pXSkinWeapon->Play( CKTDGXSkinAnim::XAP_LOOP );

	m_pMatrix = &(pFrame->combineMatrix);

	m_pXSkinWeapon->UseDXMatrix( true );

	m_RotWeapon = rot;

	return true;
}

bool CX2ViewerSkinMesh::AddAccessory( const WCHAR* pFullFileName, const WCHAR* attachFrameName, D3DXVECTOR3 trans )
{	
    WCHAR drive[10] = L"";
    WCHAR dir[256]   = L"";
    WCHAR fname[256] = L"";
    WCHAR ext[10]    = L"";

    WCHAR fileName[256] = L"";
    WCHAR PullDir[256] = L"";
    _wsplitpath( pFullFileName, drive, dir, fname, ext);

    wcscat( fileName, fname);
    wcscat( fileName, ext);

    wcscat( PullDir, drive );
    wcscat( PullDir, dir );


    if(m_pXMeshAccessory != NULL)
    {
        SAFE_CLOSE( m_pXMeshAccessory );        
    }

    m_pRendererAccessory->OnResetDevice();

    m_pXMeshAccessory  = g_pKTDXApp->GetDeviceManager()->OpenXMesh( fileName );

    if( m_pXMeshAccessory == NULL )
    {
        std::string strDir;
        ConvertWCHARToChar( strDir, PullDir );
        g_pKTDXApp->GetDeviceManager()->GetMassFileManager()->AddDataDirectory( strDir );

        m_pXMeshAccessory = g_pKTDXApp->GetDeviceManager()->OpenXMesh( fileName );

        if( m_pXMeshAccessory == NULL )
        {
            WARNINGMSG( L"Mesh 파일 열기 실패~ 이런." );
            return false;
        }
    }
    
    if ( m_pXSkinAnim == NULL )
    {
        WARNINGMSG( L"애니메이션 파일이 로드 되지 않았어요." );
        SAFE_CLOSE( m_pXMeshAccessory );
        return false;
    }

    CKTDXDeviceXSkinMesh::MultiAnimFrame* pFrame = NULL;
    pFrame		= m_pXSkinAnim->GetCloneFrame( attachFrameName );
    
    if ( pFrame == NULL )
    {
        WARNINGMSG( L"본 이름이 정확하지 않아요!" );
        SAFE_CLOSE( m_pXMeshAccessory );
        return false;
    }    

    m_bIsMotion = true;

    //m_pMatrix = &(pFrame->combineMatrix);
    m_pMatrixAccessory = &(pFrame->combineMatrix); 
    

    m_TransAccessory = trans;

    return true;
}

bool CX2ViewerSkinMesh::DelModelXSkinMesh( std::wstring fileName )
{
	std::map<std::wstring, CKTDXDeviceXSkinMesh*>::iterator itr;
	itr = m_mapSkinMesh.find( fileName );

	if( itr != m_mapSkinMesh.end() )
	{
		m_pXSkinAnim->RemoveModelXSkinMesh( itr->second );
		g_pKTDXApp->GetDeviceManager()->CloseDevice( itr->first );

		m_mapSkinMesh.erase( itr );

		return true;
	}

	return false;
}

void CX2ViewerSkinMesh::Reset()
{
	if( m_bIsMotion == true && m_pXSkinAnim != NULL )
	{
		m_pXSkinAnim->RemoveModelXSkinMesh( m_pXSkinAnim->GetAnimXSkinMesh() );
	}

	std::map<std::wstring, CKTDXDeviceXSkinMesh*>::iterator itr;
	for( itr = m_mapSkinMesh.begin(); itr != m_mapSkinMesh.end(); ++itr )
	{
		m_pXSkinAnim->RemoveModelXSkinMesh( itr->second );
		g_pKTDXApp->GetDeviceManager()->CloseDevice( itr->first );
	}
	m_mapSkinMesh.clear();

	//SAFE_DELETE( m_pXSkinAnim );
	//SAFE_DELETE( m_pXSkinWeapon );
    SAFE_CLOSE( m_pXMeshAccessory );
    
	SAFE_CLOSE( m_pXET );
	SAFE_CLOSE( m_pXETWeapon ); 

	INIT_VECTOR3( m_vScale,	1.0f, 1.0f, 1.0f );
	INIT_VECTOR3( m_RenderParam.lightPos, 500, 500, 500 );

	m_pMatrix = NULL;
}

void CX2ViewerSkinMesh::ChangeAnim( std::wstring animName )
{
	if( m_pXSkinAnim == NULL || m_pXSkinAnim->GetAnimXSkinMesh() == NULL )
		return;

	m_pXSkinAnim->ChangeAnim( animName.c_str(), false );
	m_pXSkinAnim->Play( m_AnimPlaytype );
}

void CX2ViewerSkinMesh::ChangeAnim( int index )
{
	if( m_pXSkinAnim == NULL || m_pXSkinAnim->GetAnimXSkinMesh() == NULL )
		return;

	m_pXSkinAnim->ChangeAnim( index, false );
	m_pXSkinAnim->Play( m_AnimPlaytype );
}

void CX2ViewerSkinMesh::SetPlayType( WCHAR* wszPlayType )
{
	if( wcscmp( wszPlayType, L"ONE" ) == 0 )
	{
		m_AnimPlaytype = CKTDGXSkinAnim::XAP_ONE_WAIT;
	}
	else if( wcscmp( wszPlayType, L"LOOP" ) == 0 )
	{
		m_AnimPlaytype = CKTDGXSkinAnim::XAP_LOOP;
	}

	m_pXSkinAnim->Play( m_AnimPlaytype );
}

void CX2ViewerSkinMesh::SetScale( float fX, float fY, float fZ )
{
	D3DXVECTOR3 vScale;
	vScale.x = fX;
	vScale.y = fY;
	vScale.z = fZ;

	SetScale( vScale );
}

void CX2ViewerSkinMesh::SetLightPos( float fX, float fY, float fZ )
{
	D3DXVECTOR3 vLightPos;
	vLightPos.x = fX;
	vLightPos.y = fY;
	vLightPos.z = fZ;

	SetLightPos( vLightPos );
}

bool CX2ViewerSkinMesh::GetFrameNameList( std::vector<CKTDXDeviceXSkinMesh::MultiAnimFrame *>& vecFrameNameList )
{
    if( m_pXSkinAnim == NULL || m_pXSkinAnim->GetAnimXSkinMesh() == NULL )
    {
        return false;
    }   
    
    LPCSTR			szName;
    std::wstring	wstrName;
    WCHAR			wszName[128] = L"";
    string          frameName;

    CKTDXDeviceXSkinMesh *pSM = m_pXSkinAnim->GetAnimXSkinMesh();   


    for( UINT i = 0; i < pSM->GetFrameNum(); ++i )
    {
        CKTDXDeviceXSkinMesh::MultiAnimFrame *pFM = pSM->GetFrame(i);
        szName = pFM->Name;

        MultiByteToWideChar( CP_ACP, 0, szName, -1, wszName, MAX_PATH);
        
        wstrName = wszName;

        if( wstrName.compare(L"Scene_Root") != 0 && wstrName.compare(L"") != 0)
            vecFrameNameList.push_back( pFM );
    }

    return true;
}

bool CX2ViewerSkinMesh::GetAnimNameList( std::vector<std::wstring>& vecAnimNameList )
{
	if( m_pXSkinAnim == NULL || m_pXSkinAnim->GetAnimXSkinMesh() == NULL )
	{
		return false;
	}

	LPD3DXANIMATIONCONTROLLER	pAC;
	LPD3DXANIMATIONSET			pAnimSet;
	UINT			aniNum;
	LPCSTR			szName;
	std::wstring	wstrName;
	WCHAR			wszName[128] = L"";

	pAC		= m_pXSkinAnim->GetAnimXSkinMesh()->GetCloneAC();
	aniNum	= pAC->GetNumAnimationSets();

	for( UINT i = 0; i < aniNum; ++i )
	{
		pAC->GetAnimationSet( i, &pAnimSet );
		szName = pAnimSet->GetName();

		MultiByteToWideChar( CP_ACP, 0, szName, -1, wszName, MAX_PATH);

		wstrName = wszName;
		vecAnimNameList.push_back( wstrName );

		pAnimSet->Release();
	}

	SAFE_RELEASE( pAC);

	return true;
}

bool CX2ViewerSkinMesh::SetPlayOnOff()
{
	m_bIsAnimPlay = !m_bIsAnimPlay;

	if( m_pXSkinAnim == NULL || m_pXSkinAnim->GetAnimXSkinMesh() == NULL )
	{
		return false;
	}

	if( m_bIsAnimPlay == true )
	{
		m_pXSkinAnim->Play( m_AnimPlaytype );
	}
	else
	{
		m_pXSkinAnim->Wait();
	}

	return m_bIsAnimPlay;
}

void CX2ViewerSkinMesh::SetMotionOnOff( bool bIsMotion )
{
	if( m_pXSkinAnim == NULL || m_pXSkinAnim->GetAnimXSkinMesh() == NULL )
	{
		return;
	}

	if( m_bIsMotion == false && bIsMotion == true )
	{
		m_pXSkinAnim->AddAnimXSkinMesh( m_pXSkinAnim->GetAnimXSkinMesh() );
		m_bIsMotion = true;
	}
	else if( m_bIsMotion == true && bIsMotion == false )
	{
		m_pXSkinAnim->RemoveModelXSkinMesh( m_pXSkinAnim->GetAnimXSkinMesh() );
		m_bIsMotion = false;
	}
}

void CX2ViewerSkinMesh::SetPlaySpeed( float fPlaySpeed )
{
	if( m_pXSkinAnim == NULL || m_pXSkinAnim->GetAnimXSkinMesh() == NULL )
	{
		return;
	}

	m_pXSkinAnim->SetPlaySpeed( fPlaySpeed );
}

void CX2ViewerSkinMesh::GetAnimTime( float& fNowTime, float& fMaxTime )
{
	if( m_pXSkinAnim == NULL || m_pXSkinAnim->GetAnimXSkinMesh() == NULL )
	{
		fNowTime = fMaxTime = -1.0f;
		return;
	}

	fNowTime = m_pXSkinAnim->GetNowAnimationTime();
	fMaxTime = m_pXSkinAnim->GetMaxAnimationTime();
}

void CX2ViewerSkinMesh::SetAnimTime( float fTime )
{
	if( m_pXSkinAnim == NULL || m_pXSkinAnim->GetAnimXSkinMesh() == NULL )
	{
		return;
	}

	m_pXSkinAnim->SetAnimationTime( fTime );
}

bool CX2ViewerSkinMesh::MsgProc( HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam )
{
#if 0 
    switch(uMsg)
    {
    case WM_LBUTTONDOWN:        
        SelectionChange(LOWORD(lParam), HIWORD(lParam));
        break;
    }
#endif
    return false;
}

//void CX2ViewerSkinMesh::SelectionChange( DWORD dwX,  DWORD dwY)
//{
//    D3DVIEWPORT9 Viewport;
//    D3DXMATRIX matProjection, matView, matWorld;
//
//    float fDistMin= 0.0f;
//    DWORD dwFaceHit= 0;
//    DWORD dwVertHit= 0;
//
//    g_pKTDXApp->GetDevice()->GetViewport(&Viewport);
//    g_pKTDXApp->GetDevice()->GetTransform(D3DTS_VIEW, &matView);
//    g_pKTDXApp->GetDevice()->GetTransform(D3DTS_PROJECTION, &matProjection);
//
//    
//    CKTDXDeviceXSkinMesh *pRoot = m_pXSkinAnim->GetAnimXSkinMesh();
//
//    for( UINT i = 0; i < pRoot->GetFrameNum(); ++i )
//    {
//        CKTDXDeviceXSkinMesh::MultiAnimFrame *pdeCur = pRoot->GetFrame(i);
//
//
//        if (pdeCur->pMeshContainer != NULL && 
//            BIntersectMeshContainer((CKTDXDeviceXSkinMesh::MultiAnimMC *)pdeCur->pMeshContainer, dwX, dwY, &Viewport, &matProjection, &matView, &fDistMin, &dwFaceHit, &dwVertHit))
//        {
//            LPCSTR			szName;
//            std::wstring	wstrName;
//            WCHAR			wszName[128] = L"";
//
//            szName = pdeCur->Name;
//
//            MultiByteToWideChar( CP_ACP, 0, szName, -1, wszName, MAX_PATH);
//
//            wstrName = wszName;
//
//            if( wstrName.compare(L"Scene_Root") != 0 && wstrName.compare(L"") != 0)
//            {                   
//                MessageBox(NULL, wstrName.c_str(), L"Name", MB_OK);
//            }
//        }
//    }
//}




//BOOL CX2ViewerSkinMesh::BIntersectMeshContainer
//(
// CKTDXDeviceXSkinMesh::MultiAnimMC *pmcMesh,
//
// DWORD dwX, 
// DWORD dwY,
// D3DVIEWPORT9 *pViewport,
// D3DXMATRIX *pmatProjection,
// D3DXMATRIX *pmatView,
// float *pfDistMin,
//
// //SMeshContainer **ppmcHit,
//
// DWORD *pdwFaceHit,
// DWORD *pdwVertHit
// )
//{
//    // 충돌검사하자
//    HRESULT hr = S_OK;
//    D3DXVECTOR3 vProjected;
//    D3DXVECTOR3 vRayPos;
//    D3DXVECTOR3 vRayDirection;
//    float fDist= 0.0f;
//    BOOL bHit= FALSE;
//    BOOL bFound = FALSE;
//    float fRayLength= 0.0f;
//    DWORD dwFace= 0;
//    float fU= 0.0f, fV= 0.0f;
//    DWORD iVertexIndex= 0;
//    PBYTE pbIndices= NULL;
//    LPD3DXBASEMESH pMeshCur= NULL;
//    PBYTE       pbVerticesSrc= NULL;
//    PBYTE       pbVerticesDest= NULL;
//    D3DXMATRIXA16* rgBoneMatrices= NULL;            
//
//
//    if(pmcMesh == NULL)
//        return false;
//
//    LPD3DXMESH pWorkingMesh;
//
//    
//    DWORD dwOldFVF;
//    
//    if( pmcMesh->m_pWorkingMesh == NULL)
//        return false;
//
//    D3DVERTEXELEMENT9 Decl[MAX_FVF_DECL_SIZE];
//    pmcMesh->m_pWorkingMesh->GetDeclaration(Decl);
//    hr = pmcMesh->m_pWorkingMesh->CloneMesh(D3DXMESH_SYSTEMMEM, Decl,
//        g_pKTDXApp->GetDevice(), &pWorkingMesh);
//
//
//    if (pmcMesh != NULL)
//    {
//        //pmcMesh->m_bSelected = false;
//
//        // calculate ray position in world space
//        vProjected = D3DXVECTOR3((float)dwX, (float)dwY, 0.0f);
//        D3DXVec3Unproject(&vRayPos, &vProjected, pViewport, pmatProjection, pmatView, NULL);
//
//
//        // calculate ray direction in world space
//        vProjected = D3DXVECTOR3((float)dwX, (float)dwY, 1.0f);
//        D3DXVec3Unproject(&vRayDirection, &vProjected, pViewport, pmatProjection, pmatView, NULL);
//        vRayDirection -= vRayPos;
//
//
//        // get the bone count
//
//        DWORD   cBones  = pmcMesh->pSkinInfo->GetNumBones();
//
//
//        // allocate bone transform array
//
//        rgBoneMatrices  = new D3DXMATRIXA16[cBones];
//
//        if (!rgBoneMatrices)
//            return FALSE;
//
//
//        // set up bone transforms
//
//        for (DWORD iBone = 0; iBone < cBones; ++iBone)
//        {
//            D3DXMatrixMultiply( &rgBoneMatrices[iBone],
//                &( pmcMesh->m_amxBoneOffsets[ iBone ] ),  
//                &pmcMesh->m_ppBoneFrames[ iBone ]->combineMatrix );
//           
//        }
//
//        hr= pmcMesh->m_pWorkingMesh->LockVertexBuffer(D3DLOCK_READONLY, (LPVOID*)&pbVerticesSrc);
//        if (FAILED(hr))
//        {
//            pmcMesh->m_pWorkingMesh->UnlockVertexBuffer();
//            return false;
//        }
//        hr= pWorkingMesh->LockVertexBuffer(0, (LPVOID*)&pbVerticesDest);
//        if (FAILED(hr))
//        {
//            pmcMesh->m_pWorkingMesh->UnlockVertexBuffer();
//            pWorkingMesh->UnlockVertexBuffer();
//            return false;
//        }
//        // generate skinned mesh, use the system memory copy
//        hr = pmcMesh->pSkinInfo->UpdateSkinnedMesh(rgBoneMatrices, NULL, pbVerticesSrc, pbVerticesDest);
//        pmcMesh->m_pWorkingMesh->UnlockVertexBuffer();
//        pWorkingMesh->UnlockVertexBuffer();
//        if (FAILED(hr))
//        {
//            delete[] rgBoneMatrices;
//            rgBoneMatrices= NULL;
//            return false;
//        }
//
//        // free bone transform array
//        delete[] rgBoneMatrices;
//        rgBoneMatrices= NULL;
//
//        // perform ray-mesh intersection
//        hr = D3DXIntersect(
//            pWorkingMesh,
//            &vRayPos, 
//            &vRayDirection, 
//            &bHit, 
//            &dwFace,
//            &fU,
//            &fV,
//            &fDist,
//            NULL,
//            NULL);
//        if (FAILED(hr))
//            return FALSE;
//
//        if (bHit)       // intersection found
//        {
//#if 1 
//            // normalize intersection distance
//            fDist  /= D3DXVec3Length(&vRayDirection);
//
//            if (fDist < *pfDistMin)     // intersection distance is the smallest seen so far
//            {
//                // update smallest intersection distance & intersected frame
//                *pfDistMin  = fDist;
//                //*ppmcHit = pmcMesh;
//                *pdwFaceHit = dwFace;                    
//
//                // indicate that we found a new smallest intersection distance
//                bFound      = TRUE;
//
//                //pmcMesh->m_bSelected = true;
//                return TRUE;
//            }
//#else
//            bFound      = TRUE;
//            return TRUE;
//#endif
//            //return FALSE;            
//        }
//    }
//
//}

void CX2ViewerSkinMesh::SetAttachPoint(WCHAR *szName)
{
    CKTDXDeviceXSkinMesh::MultiAnimFrame *pFrame;
    wstring wstrName = szName;

    if(wstrName.compare(L"") == 0)
    {
        m_bAttachPoint = false;
        return;
    }

    m_pRendererPoint->OnResetDevice();
    pFrame		= m_pXSkinAnim->GetCloneFrame( szName );

    if ( pFrame == NULL )
    {
        WARNINGMSG( L"본 이름이 정확하지 않아요!" );
        SAFE_CLOSE( m_pXMeshPoint );
        return;
    }                   

    m_bAttachPoint = true;
    //m_pMatrix = &(pFrame->combineMatrix);
    m_pMatrixPoint = &(pFrame->combineMatrix); 
}

