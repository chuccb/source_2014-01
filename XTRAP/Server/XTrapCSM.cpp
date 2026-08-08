// XTrapCSM.cpp

#include "GSSimLayer.h"
#include "GSUser.h"
#include "XTrapCSM.h"
#include "NetError.h"

#ifdef SERV_USE_XTRAP

CXTrapCSM::CXTrapCSM() :
	m_bEnable(false),
	m_bTimerStart(false)
{

}
CXTrapCSM::~CXTrapCSM()
{

}

// 사용 설정
void CXTrapCSM::SetEnable( bool _bEnable )
{
	m_bEnable = _bEnable;
}

// 초기화
bool CXTrapCSM::InitCSM()
{
	if(m_bEnable)
	{
		// 리턴 0 : 참
		//XTrap_S_SessionInit(600, GetKGSSimLayer()->GetMapQuantity(), (unsigned char*)g_XTrapMap, m_cSessionBuf);
		XTrap_S_SessionInit( 600, GetKGSSimLayer()->GetMapQuantity(), GetKGSSimLayer()->GetAllMapPointer(), m_cSessionBuf );

		return true;
	}
	else
		return false;
}

// step1
// 패킷 만들어서 클라에 보낸다.
bool CXTrapCSM::CSMStep1( IN KGSUserPtr spUser )
{
	//------ 지헌 : XTRAP 서버 - 유저에게 정기적으로 정보 요청------------------
	if(m_bEnable)
	{
		static DWORD dwExecute = 0;
		if((GetTickCount() - dwExecute) > 20000)
		{	
			// 클라에게 보낼 패킷
			KEGS_XTRAP_REQ packet;

			INT nRet;

			packet.m_vecData.resize(200);

			unsigned char temp[200];

			nRet = XTrap_CS_Step1(m_cSessionBuf, temp);

			for( u_int ui = 0; ui < 200; ++ui )
			{
				packet.m_vecData[ui] = temp[ui];
			}

			// 리턴값이 정상이 아니면 로그 출력
			if(nRet != 0)
			{
				START_LOG( cerr, L"XTrap_CS_Step1 오류 리턴 :" << nRet );					
			}

			if (nRet == XTRAP_API_RETURN_DETECTHACK)
			{
				unsigned int DetectCode=0;
				memcpy(&DetectCode, ((unsigned char *)m_cSessionBuf+8), 4);

				START_LOG( cerr, L"스텝1, DetectCode 값 : " << std::hex << DetectCode );
			}



			spUser->SendPacket(EGS_XTRAP_REQ, packet);

			if(nRet != 0)
			{
				
				// 채널 이동실패로 인한 종료처리
				spUser->SetDisconnectReason(KStatistics::eSIColDR_nProtect_Hacking);
				spUser->ReserveDestroy();	
				return true;
				// 끊읍시다.
			}
			dwExecute = GetTickCount();

			if(!m_bTimerStart)
			{
				m_bTimerStart = true;
				m_kTimer.restart();
			}
			else
			{
				if(m_kTimer.elapsed() >= ST_LOOP_AUTH_TIME)
				{
					spUser->SetDisconnectReason(KStatistics::eSIColDR_nProtect_Hacking);
					spUser->ReserveDestroy();	
				}
			}
		}
		return true;
	}
	else
		return false;
}

// step3
// 클라에서 패킷을 받는다
bool CXTrapCSM::CSMStep3(KEGS_XTRAP_ACK* _packet)
{

	if(m_bEnable)
	{
		m_kTimer.restart();
		unsigned char arrResult[200];

		for( u_int ui = 0; ui < 200; ++ui )
		{
			arrResult[ui] = _packet->m_vecData[ui];
		}


		unsigned int nRet = XTrap_CS_Step3(m_cSessionBuf, arrResult);
		if(nRet != 0)
		{
			START_LOG( cerr, L"XTrap step3 오류 리턴 : " << nRet );

			if (nRet == XTRAP_API_RETURN_DETECTHACK)
			{
				unsigned int DetectCode=0;
				memcpy(&DetectCode, ((unsigned char *)m_cSessionBuf+8), 4);

				START_LOG( cerr, L"스텝3, DetectCode 값 : " << std::hex << DetectCode );
			}

			return false;
		}
		return true;
	}
	else
		return false;
}

#endif
