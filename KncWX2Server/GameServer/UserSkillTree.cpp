#include ".\userskilltree.h"
#include "X2Data/XSLSkillTree.h"

#include "NetError.h"


#ifdef SERV_UPGRADE_SKILL_SYSTEM_2013 // 적용날짜: 2013-06-27

KUserSkillTree::KUserSkillTree(void) :
m_wstrSkillSlotBEndDate( L"" ),
m_tSkillSlotBEndDate( 0 ),
m_iCSPoint( 0 ),
m_iMaxCSPoint( 0 ), 
m_wstrCSPointEndDate( L"" ),
m_tCSPointEndDate( 0 ),
m_iUnitClass( 0 ),
//{{ 2010. 03. 22  최육사	기술의 노트
m_cSkillNoteMaxPageNum( 0 )
{
	for( int i = 0; i < MAX_SKILL_SLOT; ++i )
	{
		m_aiSkillSlot[i] = 0;
	}
}

KUserSkillTree::~KUserSkillTree(void)
{
}

void KUserSkillTree::Reset( bool bResetSkillTree, bool bResetEquippedSkill, bool bResetUnsealedSkill, bool bResetCashSkillPoint, bool bResetSkillNote )
{
	if( true == bResetEquippedSkill )
	{
		for( int i = 0; i < MAX_SKILL_SLOT; ++i )
		{
			m_aiSkillSlot[i] = 0;
		}
	}

	if( true == bResetSkillTree )
	{
		m_mapSkillTree.clear();
	}

	if( true == bResetUnsealedSkill )
	{
		m_setUnsealedSkillID.clear();
	}

	if( true == bResetCashSkillPoint )
	{
		m_iCSPoint = 0;
		m_iMaxCSPoint = 0;
		KncUtil::ConvertStringToCTime( std::wstring( L"2000-01-01 00:00:00" ), m_tCSPointEndDate );
	}

	//{{ 2010. 03. 22  최육사	기술의 노트
	if( true == bResetSkillNote )
	{
		m_cSkillNoteMaxPageNum = 0;
		m_mapSkillNote.clear();
	}
}

void KUserSkillTree::InitSkill( IN std::vector<KUserSkillData>& vecSkillList, IN int aSkillSlot[], IN std::wstring& wstrSkillSlotBEndDate, IN std::vector<short int>& vecUnsealedSkillList, IN int iUnitClass )
{
	m_iUnitClass = iUnitClass;

	m_mapSkillTree.clear();

	for( UINT i=0; i<vecSkillList.size(); i++ )
	{
		const KUserSkillData& userSkillData = vecSkillList[i];

		SkillDataMap::iterator mit;
		mit = m_mapSkillTree.find( (int)userSkillData.m_iSkillID );
		if( mit != m_mapSkillTree.end() )
		{
			START_LOG( cerr, L"중복된 스킬을 보유하고 있음." )
				<< BUILD_LOG( userSkillData.m_iSkillID )
				<< BUILD_LOG( userSkillData.m_cSkillLevel )
				<< BUILD_LOG( userSkillData.m_cSkillCSPoint )
				<< END_LOG;

			continue;
		}

		int iSkillLevel = (int) userSkillData.m_cSkillLevel;

		// 스킬의 최고레벨보다 높은 스킬이 있다면 최고레벨로 보정
		const CXSLSkillTree::SkillTreeTemplet* pSkillTreeTemplet = SiCXSLSkillTree()->GetSkillTreeTemplet( m_iUnitClass, userSkillData.m_iSkillID );
		if( NULL != pSkillTreeTemplet )
		{
			if( (int) userSkillData.m_cSkillLevel > pSkillTreeTemplet->m_iMasterSkillLevel )
			{
				START_LOG( cerr, L"최고레벨보다 높은 레벨의 스킬이 DB에 있음" )
					<< BUILD_LOG( m_iUnitClass )
					<< BUILD_LOG( userSkillData.m_iSkillID )
					<< BUILD_LOG( userSkillData.m_cSkillLevel )
					<< BUILD_LOG( pSkillTreeTemplet->m_iMasterSkillLevel )
					<< BUILD_LOG( userSkillData.m_cSkillCSPoint )
					<< END_LOG;

				iSkillLevel = pSkillTreeTemplet->m_iMasterSkillLevel;
			}
		}


		// 이미 존재하면 삽입되지 않고 존재하지 않으면 삽입된다.
		m_mapSkillTree[ userSkillData.m_iSkillID ] = UserSkillData( iSkillLevel, (int)userSkillData.m_cSkillCSPoint );
	}

	if( !aSkillSlot )
	{
		START_LOG( cerr, L"스킬 슬롯이 NULL" )
			<< END_LOG;

		return;
	}

	// 장착 스킬
	for( int i = 0; i < MAX_SKILL_SLOT; i++ )
	{
		m_aiSkillSlot[i] = aSkillSlot[i];
	}

	//  봉인해제된 스킬 목록
	m_setUnsealedSkillID.clear();
	for( UINT i=0; i<vecUnsealedSkillList.size(); i++ )
	{
		m_setUnsealedSkillID.insert( (int) vecUnsealedSkillList[i] );
	}


	//{{ 2011. 01. 06  김민성  스킬슬롯체인지 체크(인벤토리-기간제) 기능 구현
	SetSkillSolotBEndDate( wstrSkillSlotBEndDate );
}


//{{ 2010. 03. 22  최육사	기술의 노트
void KUserSkillTree::InitSkillNote( IN char cSkillNoteMaxPageNum, IN const std::map< char, int >& mapSkillNote )
{
	m_mapSkillNote.clear();

	// 업데이트	
	m_cSkillNoteMaxPageNum = cSkillNoteMaxPageNum;
	m_mapSkillNote = mapSkillNote;
}

int KUserSkillTree::GetSkillLevel( IN int iSkillID )
{
	SkillDataMap::iterator mit;
	mit = m_mapSkillTree.find( iSkillID );
	if( mit != m_mapSkillTree.end() )
	{
		const UserSkillData& userSkillData = mit->second;
		return userSkillData.m_iSkillLevel;
	}

	return 0;
}

bool KUserSkillTree::GetSkillLevelAndCSP( IN int iSkillID, OUT int& iSkillLevel, OUT int& iSkillCSPoint )
{
	SkillDataMap::iterator mit;
	mit = m_mapSkillTree.find( iSkillID );
	if( mit != m_mapSkillTree.end() )
	{
		const UserSkillData& userSkillData = mit->second;
		iSkillLevel		= userSkillData.m_iSkillLevel;
		iSkillCSPoint	= userSkillData.m_iSkillCSPoint;
		return true;
	}
	else
	{
		iSkillLevel		= 0;
		iSkillCSPoint	= 0;
	}

	return false;
}


// 데이터가 있으면 값을 변경하고, 없으면 추가한다
bool KUserSkillTree::SetSkillLevelAndCSP( int iSkillID, int iSkillLevel, int iSkillCSPoint )
{
	SET_ERROR( NET_OK );

	if( iSkillLevel <= 0 )
	{
		SET_ERROR( ERR_SKILL_20 );

		return false;
	}

	const CXSLSkillTree::SkillTemplet* pSkillTemplet = SiCXSLSkillTree()->GetSkillTemplet( iSkillID );
	if( NULL == pSkillTemplet )
	{
		SET_ERROR( ERR_SKILL_19 );
		return false;
	}

	if( iSkillLevel > SiCXSLSkillTree()->GetMaxSkillLevel( m_iUnitClass, iSkillID ) )
	{
		SET_ERROR( ERR_SKILL_21 );

		return false;
	}

	SkillDataMap::iterator mit;
	mit = m_mapSkillTree.find( iSkillID );
	if( mit != m_mapSkillTree.end() )
	{
		UserSkillData& userSkillData = mit->second;
		userSkillData.m_iSkillLevel		= iSkillLevel;
		userSkillData.m_iSkillCSPoint	= iSkillCSPoint;
	}
	else 
	{
		m_mapSkillTree[ iSkillID ] = UserSkillData( iSkillLevel, iSkillCSPoint );
	}

	return true;
}

bool KUserSkillTree::IsExist( IN int iSkillID )
{
	SkillDataMap::iterator mit;
	mit = m_mapSkillTree.find( iSkillID );
	if( mit != m_mapSkillTree.end() )
	{
		return true;
	}

	//{{ 2013. 04. 01	 인연 시스템 - 김민성
	if( iSkillID == CXSLSkillTree::SI_ETC_WS_COMMON_LOVE ) // 6001
	{
		return true;
	}

	return false;
}

bool KUserSkillTree::ChangeSkillSlot( int iSlotID, int iSkillID )
{
	SET_ERROR( NET_OK );


	if( iSlotID < 0 || iSlotID >= MAX_SKILL_SLOT )
	{
		SET_ERROR( ERR_SKILL_09 );

		return false;
	}



	if( 0 == iSkillID )
	{
		m_aiSkillSlot[iSlotID] = 0;

		return true;
	}



	if( iSkillID > 0 && IsExist( iSkillID ) == false )
	{
		SET_ERROR( ERR_SKILL_10 );

		return false;
	}




	if( IsSkillSlotB( iSlotID )  &&  iSkillID != 0 )
	{
		// [고민] 인벤토리에 스킬슬롯B 확장 아이템이 있는지 검사를 할까? 말까?

		CTime tCurrentTime = CTime::GetCurrentTime();
		if( tCurrentTime > m_tSkillSlotBEndDate )
		{
			SET_ERROR( ERR_SKILL_13 );

			return false;
		}
	}

	m_aiSkillSlot[iSlotID] = iSkillID;

	return true;
}

//{{ 2012. 12. 3	박세훈	스킬 슬롯 체인지 패킷 통합
bool KUserSkillTree::ChangeSkillSlot( IN const KEGS_CHANGE_SKILL_SLOT_REQ& kPacket_, OUT KEGS_CHANGE_SKILL_SLOT_ACK& kPacket )
{
	SET_ERROR( NET_OK );

	const iSlotID	= kPacket_.m_iSlotID;
	const iSkillID	= kPacket_.m_iSkillID;
	const iSlotID2	= GetSlotID( kPacket_.m_iSkillID );
	const iSkillID2	= GetSkillID( kPacket_.m_iSlotID );

	if( ( iSlotID < 0 ) || ( iSlotID >= MAX_SKILL_SLOT ) )
	{
		SET_ERROR( ERR_SKILL_09 );
		return false;
	}

	if( 0 == iSkillID )
	{
		m_aiSkillSlot[iSlotID] = 0;
	}
	else
	{
		if( iSkillID > 0 && IsExist( iSkillID ) == false )
		{
			SET_ERROR( ERR_SKILL_10 );
			return false;
		}

		if( IsSkillSlotB( iSlotID ) )
		{
			// [고민] 인벤토리에 스킬슬롯B 확장 아이템이 있는지 검사를 할까? 말까?

			CTime tCurrentTime = CTime::GetCurrentTime();
			if( tCurrentTime > m_tSkillSlotBEndDate )
			{
				SET_ERROR( ERR_SKILL_13 );
				return false;
			}
		}
	}

	if( ( 0 <= iSlotID2 ) && ( iSlotID2 < MAX_SKILL_SLOT ) )
	{
		if( 0 == iSkillID2 )
		{
			m_aiSkillSlot[iSlotID2] = 0;
		}
		else if( ( iSkillID2 > 0 ) && ( IsExist( iSkillID2 ) == false ) )
		{
			SET_ERROR( ERR_SKILL_10 );
			return false;
		}
		else
		{
			if( IsSkillSlotB( iSlotID2 ) )
			{
				// [고민] 인벤토리에 스킬슬롯B 확장 아이템이 있는지 검사를 할까? 말까?

				CTime tCurrentTime = CTime::GetCurrentTime();
				if( tCurrentTime > m_tSkillSlotBEndDate )
				{
					SET_ERROR( ERR_SKILL_13 );
					return false;
				}
			}
			m_aiSkillSlot[iSlotID2] = iSkillID2;
		}
	}

	m_aiSkillSlot[iSlotID] = iSkillID;

	kPacket.m_iSlotID	= iSlotID;
	kPacket.m_iSkillID	= iSkillID;
	kPacket.m_iSlotID2	= iSlotID2;
	kPacket.m_iSkillID2	= iSkillID2;

	return true;
}

void KUserSkillTree::GetSkillSlot( OUT std::vector<int>& vecSkillID )
{
	vecSkillID.resize(0);

	for( int i = 0; i < MAX_SKILL_SLOT; ++i )
	{
		vecSkillID.push_back( m_aiSkillSlot[i] );
	}
}




void KUserSkillTree::GetSkillSlot( OUT std::vector<KSkillData>& vecSkillSlot )
{
	vecSkillSlot.resize(0);

	for( int i = 0; i < MAX_SKILL_SLOT; ++i )
	{
		KSkillData kSkillData;
		kSkillData.m_iSkillID = m_aiSkillSlot[i];
		kSkillData.m_cSkillLevel = (char) GetSkillLevel( kSkillData.m_iSkillID );
		vecSkillSlot.push_back( kSkillData );
	}
}


void KUserSkillTree::GetSkillSlot( OUT KSkillData aSkillSlot[] )
{
	if( !aSkillSlot )
	{
		START_LOG( cerr, L"스킬 슬롯이 NULL" )
			<< END_LOG;

		return;
	}

	for( int i = 0; i < MAX_SKILL_SLOT; ++i )
	{
		aSkillSlot[i].m_iSkillID = m_aiSkillSlot[i];
		aSkillSlot[i].m_cSkillLevel = (char) GetSkillLevel( aSkillSlot[i].m_iSkillID );
	}
}

//{{ 2012. 12. 3	박세훈	스킬 슬롯 체인지 패킷 통합
int KUserSkillTree::GetSkillID( int iSlotID )
{
	if( iSlotID < 0 || iSlotID >= MAX_SKILL_SLOT )
	{
		return 0;
	}

	return m_aiSkillSlot[iSlotID];
}

int KUserSkillTree::GetSlotID( int iSkillID )
{
	if( IsExist( iSkillID ) == false )
	{
		return -1;
	}

	for( int i=0; i < MAX_SKILL_SLOT; ++i )
	{
		if( m_aiSkillSlot[i] == iSkillID )
		{
			return i;
		}
	}

	return -1;
}


// 패시브 스킬 ID와 레벨 정보만 추출해준다. 던전, PVP게임에서 내 유닛외의 다른 유닛의 스킬 정보를 알려주기 위해서
void KUserSkillTree::GetPassiveSkillData( OUT std::vector<KSkillData>& vecSkillSlot )
{
	vecSkillSlot.resize(0);

	SkillDataMap::iterator it;
	for( it = m_mapSkillTree.begin(); it != m_mapSkillTree.end(); it++ )
	{
		KSkillData kSkillData;
		kSkillData.m_iSkillID		= it->first;
		kSkillData.m_cSkillLevel	= (UCHAR) it->second.m_iSkillLevel;
		vecSkillSlot.push_back( kSkillData );
	}
}

void KUserSkillTree::CalcUsedSPointAndCSPoint( OUT int& iSPoint, OUT int& iCSPoint )
{
	iSPoint = 0;
	iCSPoint = 0;

	for( SkillDataMap::iterator mit = m_mapSkillTree.begin(); mit != m_mapSkillTree.end(); ++mit )
	{
		const int iSkillID = (int)mit->first;
		const UserSkillData& userSkillData = mit->second;

		const CXSLSkillTree::SkillTemplet* pSkillTemplet = SiCXSLSkillTree()->GetSkillTemplet( mit->first );
		if( pSkillTemplet == NULL )
		{
			START_LOG( cerr, L"존재하지 않는 스킬 정보 입니다." )
				<< BUILD_LOG( mit->first )
				<< END_LOG;
			continue;
		}

		if( userSkillData.m_iSkillLevel > 0 )
		{
			for( int i = 0 ; i < userSkillData.m_iSkillLevel ; ++i )
			{
				if( i == 0 )
				{
					if( SiCXSLSkillTree()->IsUnitTypeDefaultSkill( iSkillID ) == true )
						continue;

					iSPoint += pSkillTemplet->m_iRequireLearnSkillPoint;
				}
				else
				{
					iSPoint += pSkillTemplet->m_iRequireUpgradeSkillPoint;
				}				
			}

			iSPoint		= iSPoint - userSkillData.m_iSkillCSPoint;
			iCSPoint	+= userSkillData.m_iSkillCSPoint;
		}
		else if( userSkillData.m_iSkillLevel == 0 )
		{
			if( userSkillData.m_iSkillCSPoint > 0 )
			{
				START_LOG( cwarn, L"스킬 레벨이 0이하인데 SkillCSP가 0보다 큼!" )
					<< BUILD_LOG( iSkillID )
					<< BUILD_LOG( userSkillData.m_iSkillLevel )
					<< BUILD_LOG( userSkillData.m_iSkillCSPoint )
					<< END_LOG;
			}
		}
		else
		{
			START_LOG( cwarn, L"스킬 레벨이 0보다 작다!" )
				<< BUILD_LOG( iSkillID )
				<< BUILD_LOG( userSkillData.m_iSkillLevel )
				<< BUILD_LOG( userSkillData.m_iSkillCSPoint )
				<< END_LOG;
		}
	}
}

bool KUserSkillTree::IsCashSkillPointExpired()
{
	if( 0 == m_iMaxCSPoint )
		return true;

	return false;
}

// 실제 사용한 스킬 포인트를 계산만 해준다(캐시 스킬 포인트 제외)
// @iRetrievedSPoint: 돌려줄 SP 수치
void KUserSkillTree::CalcExpireCashSkillPoint( OUT int& iRetrievedSPoint, OUT std::vector<KUserSkillData>& vecModifiedUserSkillData )
{
	iRetrievedSPoint = 0;
	vecModifiedUserSkillData.clear();

	for( SkillDataMap::iterator mit = m_mapSkillTree.begin(); mit != m_mapSkillTree.end(); ++mit )
	{
		const int iSkillID = mit->first;
		const UserSkillData& userSkillData = mit->second;

		const CXSLSkillTree::SkillTemplet* pSkillTemplet = SiCXSLSkillTree()->GetSkillTemplet( iSkillID );
		if( pSkillTemplet == NULL )
			continue;

		bool isDefaultSkill = SiCXSLSkillTree()->IsUnitTypeDefaultSkill( iSkillID );

		// 투자된 캐시 포인트가 있을때만 처리하자
		if( userSkillData.m_iSkillCSPoint <= 0 )
			continue;
		
		// csp 로 투자한 스킬 레벨 추출(돌려야 할 스킬 레벨 스텝)
		// 스킬 레벨을 역계산 하여 되돌릴 레벨을 구하자
		int iSkillLevel = userSkillData.m_iSkillLevel;
		int iSkillCSP = userSkillData.m_iSkillCSPoint;
		int iRollBackLevelStep = 0;
		int iReturnSP = 0;		// 스킬을 해당 레벨까지 올리기 위해 필요한 스킬
		for(  ; 0 < iSkillLevel ; --iSkillLevel )
		{
			// 배우기위한 포인트
			if( iSkillLevel == 1 )
			{
				if( isDefaultSkill == false )
				{
					iReturnSP += pSkillTemplet->m_iRequireLearnSkillPoint;
					iSkillCSP -= pSkillTemplet->m_iRequireLearnSkillPoint;
				}
			}
			// 업그레이드 하기 위한 포인트
			else
			{
				iReturnSP += pSkillTemplet->m_iRequireUpgradeSkillPoint;
				iSkillCSP -= pSkillTemplet->m_iRequireUpgradeSkillPoint;
			}

			++iRollBackLevelStep;

			// csp 가 0 이하라면 더이상 구하
			if( iSkillCSP <= 0 )
			{
				break;
			}
		}
	
		// 돌려보니 초기화되는 스킬인가? 
		if( userSkillData.m_iSkillLevel <= iRollBackLevelStep )
		{
			// Learn point 도 고려해 야한다.	// upgrade point 도 고려하야 한다.
			for( int i = 0 ; i < userSkillData.m_iSkillLevel ; ++i )
			{
				// 배우기위한 포인트
				if( i == 0 )
				{
					if( isDefaultSkill == false )
					{
						iRetrievedSPoint += pSkillTemplet->m_iRequireLearnSkillPoint;
					}
				}
				// 업그레이드 하기 위한 포인트
				else
				{
					iRetrievedSPoint += pSkillTemplet->m_iRequireUpgradeSkillPoint;
				}
			}
		}
		else
		{
			// upgrade point 만 고려하면 된다
			iRetrievedSPoint += pSkillTemplet->m_iRequireUpgradeSkillPoint * iRollBackLevelStep;
		}

		// 스킬 레벨을 되돌리는 작업
		UCHAR uNewSkillLevel = userSkillData.m_iSkillLevel - iRollBackLevelStep;
		if( uNewSkillLevel == 0 )
		{
			if( isDefaultSkill == true )
			{
				uNewSkillLevel = 1;
			}
		}
		else if( uNewSkillLevel < 0 )
		{
			uNewSkillLevel = 0;
		}
		
		vecModifiedUserSkillData.push_back( KUserSkillData( (short) iSkillID, uNewSkillLevel, 0 ) );
	}

	// 스킬트리에 소모했던 CSP 와 남아있는 CSP 를 더해서 구매시의 CSP를 빼면 돌려줄 SP 수치가 계산된다
	iRetrievedSPoint += m_iCSPoint - m_iMaxCSPoint;
}

// 실제로 cash skill point로 획득한 스킬을 되돌린다.
void KUserSkillTree::ExpireCashSkillPoint()
{
	for( SkillDataMap::iterator mit = m_mapSkillTree.begin(); mit != m_mapSkillTree.end(); ++mit )
	{
		int iSkillID = mit->first;
		UserSkillData& userSkillData = mit->second;

		const CXSLSkillTree::SkillTemplet* pSkillTemplet = SiCXSLSkillTree()->GetSkillTemplet( iSkillID );
		if( pSkillTemplet == NULL )
			continue;

		bool isDefaultSkill = SiCXSLSkillTree()->IsUnitTypeDefaultSkill( iSkillID );

		// 투자된 캐시 포인트가 있을때만 처리하자
		if( userSkillData.m_iSkillCSPoint <= 0 )
			continue;

		// csp 로 투자한 스킬 레벨 추출(돌려야 할 스킬 레벨 스텝)
		// 스킬 레벨을 역계산 하여 되돌릴 레벨을 구하자
		int iSkillLevel = userSkillData.m_iSkillLevel;
		int iSkillCSP = userSkillData.m_iSkillCSPoint;
		int iRollBackLevelStep = 0;
		int iReturnSP = 0;		// 스킬을 해당 레벨까지 올리기 위해 필요한 스킬
		for(  ; 0 < iSkillLevel ; --iSkillLevel )
		{
			// 배우기위한 포인트
			if( iSkillLevel == 1 )
			{
				if( isDefaultSkill == false )
				{
					iReturnSP += pSkillTemplet->m_iRequireLearnSkillPoint;
					iSkillCSP -= pSkillTemplet->m_iRequireLearnSkillPoint;
				}
			}
			// 업그레이드 하기 위한 포인트
			else
			{
				iReturnSP += pSkillTemplet->m_iRequireUpgradeSkillPoint;
				iSkillCSP -= pSkillTemplet->m_iRequireUpgradeSkillPoint;
			}

			++iRollBackLevelStep;

			// csp 가 0 이하라면 더이상 구하
			if( iSkillCSP <= 0 )
			{
				break;
			}
		}

		// 스킬 레벨을 되돌리는 작업
		int iNewSkillLevel = userSkillData.m_iSkillLevel - iRollBackLevelStep;
		if( iNewSkillLevel == 0 )
		{
			if( isDefaultSkill == true )
			{
				iNewSkillLevel = 1;
			}
		}
		else if( iNewSkillLevel < 0 )
		{
			iNewSkillLevel = 0;
		}

		userSkillData.m_iSkillLevel		= iNewSkillLevel;
		userSkillData.m_iSkillCSPoint	= 0;
	}

	// 장착된 스킬 중에서 skill level이 0이하인 스킬이 있으면 탈착시킨다.
	for( int i=0; i<MAX_SKILL_SLOT; i++ )
	{
		if( GetSkillLevel( m_aiSkillSlot[i] ) <= 0 )
		{
			ChangeSkillSlot( i, 0 );
		}
	}

	m_iMaxCSPoint = 0;
	m_iCSPoint = 0;
}

bool KUserSkillTree::IsSkillUnsealed( int iSkillID )
{
	std::set< int >::iterator it = m_setUnsealedSkillID.find( iSkillID );
	if( it != m_setUnsealedSkillID.end() )
	{
		return true;
	}

	return false;
}

//{{ 2009. 8. 4  최육사		스킬봉인해제
bool KUserSkillTree::SkillUnseal( int iSkillID )
{
	if( IsSkillUnsealed( iSkillID ) )
	{
		START_LOG( cerr, L"이미 봉인해제된 스킬입니다. 일어나서는 안되는 에러! 검사했을텐데.." )
			<< BUILD_LOG( iSkillID )
			<< END_LOG;

		return false;
	}

	m_setUnsealedSkillID.insert( iSkillID );
	return true;
}

bool KUserSkillTree::SetCSPointEndDate( std::wstring wstrEndDate )
{
	if( true == wstrEndDate.empty() )
		return false;

	if( !KncUtil::ConvertStringToCTime( wstrEndDate, m_tCSPointEndDate ) )
		return false;

	m_wstrCSPointEndDate = wstrEndDate;

	return true;
}

void KUserSkillTree::ExpandSkillSlotB( std::wstring& wstrSkillSlotBEndDate )
{	
	m_wstrSkillSlotBEndDate = wstrSkillSlotBEndDate;

	if( !KncUtil::ConvertStringToCTime( wstrSkillSlotBEndDate, m_tSkillSlotBEndDate ) )
	{
		START_LOG( cerr, L"문자열 시간 변환 실패." )
			<< BUILD_LOG( wstrSkillSlotBEndDate )
			<< END_LOG;

		// 과거 시간으로 세팅
		m_wstrSkillSlotBEndDate = L"2000-01-01 00:00:00";
		KncUtil::ConvertStringToCTime( m_wstrSkillSlotBEndDate, m_tSkillSlotBEndDate );
	}

	for( int i = SKILL_SLOT_B1; i < MAX_SKILL_SLOT; ++i )
	{
		m_aiSkillSlot[i] = 0;
	}
}

KUserSkillTree::SKILL_SLOT_B_EXPIRATION_STATE KUserSkillTree::GetSkillSlotBExpirationState()
{
	// note!! 스킬 슬롯 확장 아이템 사용기간이 1년 이상 남았으면 영구 아이템으로 간주한다.
	const CTimeSpan MAGIC_PERMANENT_TIME_SPAN = CTimeSpan( 365, 0, 0, 0 );

	CTime tCurrentTime = CTime::GetCurrentTime();
	if( tCurrentTime >= m_tSkillSlotBEndDate )
	{
		return SSBES_EXPIRED;
	}
	else
	{
		CTimeSpan expirationTimeLeft = m_tSkillSlotBEndDate - tCurrentTime;

		if( expirationTimeLeft > MAGIC_PERMANENT_TIME_SPAN )
		{
			return SSBES_PERMANENT;
		}
		else
		{
			return SSBES_NOT_EXPIRED;
		}
	}
}

void KUserSkillTree::ExpireSkillSlotB()
{
	// 현재 시간을 구하여 종료시간과 비교
	CTime tCurrentTime = CTime::GetCurrentTime();
	if( tCurrentTime >= m_tSkillSlotBEndDate )
	{
		// 자주 호출되는 부분이기 때문에 코드 최적화 (for문 제거)
		m_aiSkillSlot[SKILL_SLOT_B1] = 0;
		m_aiSkillSlot[SKILL_SLOT_B2] = 0;
		m_aiSkillSlot[SKILL_SLOT_B3] = 0;
		m_aiSkillSlot[SKILL_SLOT_B4] = 0;
	}
}

bool KUserSkillTree::IsMyUnitClassSkill( int iSkillID )
{
	if( SiCXSLSkillTree()->GetMasterSkillLevel( m_iUnitClass, iSkillID ) > 0 )
		return true;

	return false;
}

bool KUserSkillTree::IsAllPrecedingSkillLearned( int iSkillID, std::map< int, KGetSkillInfo >& mapSkillList )
{
	const CXSLSkillTree::SkillTreeTemplet* pSkillTreeTemplet = SiCXSLSkillTree()->GetSkillTreeTemplet( m_iUnitClass, iSkillID );
	if( NULL == pSkillTreeTemplet )
	{
		START_LOG( cerr, L"스킬트리 템플릿이 없음_preceding" )
			<< BUILD_LOG( m_iUnitClass )
			<< BUILD_LOG( iSkillID )
			<< END_LOG;

		return false;
	}

	if( pSkillTreeTemplet->m_iPrecedingSkill > 0 )
	{
		if( IsSkillLearned( pSkillTreeTemplet->m_iPrecedingSkill ) == false )
		{
			std::map< int, KGetSkillInfo >::iterator mit = mapSkillList.find( pSkillTreeTemplet->m_iPrecedingSkill );
			if( mit != mapSkillList.end() )
			{
				return true;
			}
		}
	}
	
	return true;
}



bool KUserSkillTree::IsAllFollowingSkillLevelZero( int iSkillID )
{
	const CXSLSkillTree::SkillTreeTemplet* pSkillTreeTemplet = SiCXSLSkillTree()->GetSkillTreeTemplet( m_iUnitClass, iSkillID );
	if( NULL == pSkillTreeTemplet )
	{
		START_LOG( cerr, L"스킬트리 템플릿이 없음_following" )
			<< BUILD_LOG( m_iUnitClass )
			<< BUILD_LOG( iSkillID )
			<< END_LOG;

		return false;
	}

	BOOST_TEST_FOREACH( const int, iFollowingSkillID, pSkillTreeTemplet->m_vecFollowingSkill )
	{
		if( GetSkillLevel( iFollowingSkillID ) > 0 )
		{
			return false;
		}
	}

	return true;
}

//{{ 2010. 03. 22  최육사	기술의 노트
void KUserSkillTree::GetSkillNote( OUT std::vector< int >& vecSkillNote )
{
	vecSkillNote.clear();

	std::map< char, int >::const_iterator mit;
	for( mit = m_mapSkillNote.begin(); mit != m_mapSkillNote.end(); ++mit )
	{
		vecSkillNote.push_back( mit->second );
	}
}

bool KUserSkillTree::GetExpandSkillNotePage( IN u_char ucLevel, OUT char& cPageNum )
{
	cPageNum = 0;

	const u_char ucCheckNum = ucLevel / 10; // 10의 자리수 구하기 위해 10으로 나눔
	switch( ucCheckNum )
	{
	case 0:
	case 1:
		return false;

	case 2:
		cPageNum = 1;
		return true;

	case 3:
		cPageNum = 2;
		return true;

	case 4:
		cPageNum = 3;
		return true;

	case 5:
		cPageNum = 4;
		return true;

		//{{ 2011. 07. 13	최육사	만렙 확장
	case 6:
		cPageNum = 5;
		return true;
	}

	return false;
}

bool KUserSkillTree::IsExistSkillNotePage( IN char cPageNum )
{
	if( m_cSkillNoteMaxPageNum > cPageNum )
		return true;

	return false;
}

bool KUserSkillTree::IsExistSkillNoteMemoID( IN int iSkillNoteMemoID )
{
	std::map< char, int >::const_iterator mit;
	for( mit = m_mapSkillNote.begin(); mit != m_mapSkillNote.end(); ++mit )
	{
		if( mit->second == iSkillNoteMemoID )
			return true;
	}

	return false;
}

void KUserSkillTree::UpdateSkillNoteMemo( IN char cPageNum, IN int iMemoID )
{
	std::map< char, int >::iterator mit;
	mit = m_mapSkillNote.find( cPageNum );
	if( mit == m_mapSkillNote.end() )
	{
		m_mapSkillNote.insert( std::make_pair( cPageNum, iMemoID ) );
	}
	else
	{
		mit->second = iMemoID;
	}
}

//{{ 2011. 01. 06  김민성  스킬슬롯체인지 체크(인벤토리-기간제) 기능 구현
void KUserSkillTree::SetSkillSolotBEndDate( IN std::wstring& wstrSkillSlotBEndDate )
{
	// 스킬 슬롯 B
	m_wstrSkillSlotBEndDate = wstrSkillSlotBEndDate;

	if( true == wstrSkillSlotBEndDate.empty() )
	{
		// 과거 시간으로 세팅
		m_wstrSkillSlotBEndDate = L"2000-01-01 00:00:00";
		KncUtil::ConvertStringToCTime( m_wstrSkillSlotBEndDate, m_tSkillSlotBEndDate );
	}
	else if( !KncUtil::ConvertStringToCTime( wstrSkillSlotBEndDate, m_tSkillSlotBEndDate ) )
	{
		START_LOG( cerr, L"문자열 시간 변환 실패." )
			<< BUILD_LOG( wstrSkillSlotBEndDate )
			<< END_LOG;

		// 과거 시간으로 세팅
		m_wstrSkillSlotBEndDate = L"2000-01-01 00:00:00";
		KncUtil::ConvertStringToCTime( m_wstrSkillSlotBEndDate, m_tSkillSlotBEndDate );
	}
}

//{{ 2011. 11. 21  김민성	전직 변경 아이템
void KUserSkillTree::GetUnSealedSkillList( OUT std::vector< short >& vecUnsealedSkillID )
{
	std::set< int >::iterator sit = m_setUnsealedSkillID.begin();
	for( ; sit != m_setUnsealedSkillID.end() ; ++sit )
	{
		vecUnsealedSkillID.push_back( static_cast<short>(*sit) );
	}
}

void KUserSkillTree::SetClassChangeSkill( IN std::map< int, int >& mapSkill )
{
	std::set< int >::iterator sit;

	std::map< int, int >::iterator mit = mapSkill.begin();
	for(  ; mit != mapSkill.end() ; ++mit )
	{
		int itempfirst = mit->first;
		sit = m_setUnsealedSkillID.find( mit->first );
		if( sit != m_setUnsealedSkillID.end() )
		{
			m_setUnsealedSkillID.erase( sit );
			m_setUnsealedSkillID.insert( mit->second );
		}
		else
		{
			START_LOG( cerr, L"이런 스킬을 가지고 있지 않다고?! 있다고 해서 기록한거잖아!!" )
				<< BUILD_LOG( mit->first )
				<< BUILD_LOG( mit->second )
				<< END_LOG;
		}
	}
}

void KUserSkillTree::SetClassChangeMemo( IN std::map< int, int >& mapMemo )
{
	std::map< char, int >::iterator mymit;

	std::map< int, int >::iterator mit = mapMemo.begin();
	for(  ; mit != mapMemo.end() ; ++mit )
	{
		mymit = m_mapSkillNote.begin();
		for( ; mymit != m_mapSkillNote.end() ; ++mymit )
		{
			if( mymit->second == mit->first )
			{
				mymit->second = mit->second;
			}
		}
	}
}

void KUserSkillTree::CheckAddSkillStat_BaseHP( IN const KStat& kStat, IN OUT KStat& kModifiedBaseStatBySkill )
{
	const int iSkillLevel = GetSkillLevel( (int) CXSLSkillTree::SI_P_ES_POWERFUL_VITAL );
	if( 0 < iSkillLevel )
	{
		const CXSLSkillTree::SkillTemplet* pSkillTemplet = SiCXSLSkillTree()->GetSkillTemplet( (int)CXSLSkillTree::SI_P_ES_POWERFUL_VITAL );
		if( NULL != pSkillTemplet )
		{
			float fRate = pSkillTemplet->GetSkillAbilityValue( CXSLSkillTree::SA_MAX_HP_REL, iSkillLevel );
			kModifiedBaseStatBySkill.m_iBaseHP += static_cast<int>( kStat.m_iBaseHP * CXSLSkillTree::CalulateIncreaseingRate( fRate ) );
		}
	}

	//{{ kimhc // 2011.1.14 // 청 1차 전직, 퓨리가디언의 방어 숙련
	const int iSkillLevelGuardMastery = GetSkillLevel( static_cast<int>( CXSLSkillTree::SI_P_CFG_GUARD_MASTERY ) );
	if ( 0 < iSkillLevelGuardMastery )
	{
		const CXSLSkillTree::SkillTemplet* pSkillTemplet = SiCXSLSkillTree()->GetSkillTemplet( static_cast<int>( CXSLSkillTree::SI_P_CFG_GUARD_MASTERY ) );
		if ( NULL != pSkillTemplet )
		{
			float fRate = pSkillTemplet->GetSkillAbilityValue( CXSLSkillTree::SA_MAX_HP_REL, iSkillLevelGuardMastery );
			kModifiedBaseStatBySkill.m_iBaseHP += static_cast<int>( kStat.m_iBaseHP * CXSLSkillTree::CalulateIncreaseingRate( fRate ) );
		}
	}

	// 호신강기
	{
		const int iSkillLevel = GetSkillLevel( static_cast<int>( CXSLSkillTree::SI_P_ASD_SELF_PROTECTION_FORTITUDE ) );
		if ( 0 < iSkillLevel )
		{
			const CXSLSkillTree::SkillTemplet* pSkillTemplet = SiCXSLSkillTree()->GetSkillTemplet( static_cast<int>( CXSLSkillTree::SI_P_ASD_SELF_PROTECTION_FORTITUDE ) );
			if ( NULL != pSkillTemplet )
			{
				float fRate = pSkillTemplet->GetSkillAbilityValue( CXSLSkillTree::SA_MAX_HP_REL, iSkillLevel );
				kModifiedBaseStatBySkill.m_iBaseHP += static_cast<int>( kStat.m_iBaseHP * CXSLSkillTree::CalulateIncreaseingRate( fRate ) );
			}
		}
	}
}

void KUserSkillTree::GetSkillStat( KStat& kStat )
{
	kStat.Init();


	SkillDataMap::iterator mit;
	for( mit = m_mapSkillTree.begin(); mit != m_mapSkillTree.end(); ++mit )
	{
		const UserSkillData& userSkillData = mit->second;
		if( userSkillData.m_iSkillLevel <= 0 )
			continue;

		const CXSLSkillTree::SkillTemplet* pSkillTemplet = SiCXSLSkillTree()->GetSkillTemplet( mit->first );

		if( pSkillTemplet == NULL )
		{
			START_LOG( cerr, L"스킬 템플릿 포인트 이상.!" )
				<< BUILD_LOG( mit->first )
				<< BUILD_LOG( userSkillData.m_iSkillLevel )
				<< END_LOG;

			continue;
		}

		if( userSkillData.m_iSkillLevel <= 0 )
			continue;

		if( userSkillData.m_iSkillLevel > CXSLSkillTree::SkillTemplet::SLV_MAX_LEVEL )
			continue;

		switch( pSkillTemplet->m_eType )
		{
		case CXSLSkillTree::ST_PASSIVE_PHYSIC_ATTACK: 
		case CXSLSkillTree::ST_PASSIVE_MAGIC_ATTACK:  
		case CXSLSkillTree::ST_PASSIVE_MAGIC_DEFENCE: 
		case CXSLSkillTree::ST_PASSIVE_PHYSIC_DEFENCE:
			{
				if( pSkillTemplet->m_vecStat.size() <= 0)
				{
					START_LOG( cerr, L"[테스트] 이런게 찍히면 안된다!!!!!!!!!!!!!!!!!!!!!!!!!!!!!" )
						<< END_LOG;
					continue;
				}

				CXSLStat::Stat kSkillStat = pSkillTemplet->m_vecStat[userSkillData.m_iSkillLevel - 1];

				kStat.m_iBaseHP			+= (int)kSkillStat.m_fBaseHP;
				kStat.m_iAtkPhysic		+= (int)kSkillStat.m_fAtkPhysic;
				kStat.m_iAtkMagic		+= (int)kSkillStat.m_fAtkMagic;
				kStat.m_iDefPhysic		+= (int)kSkillStat.m_fDefPhysic;
				kStat.m_iDefMagic		+= (int)kSkillStat.m_fDefMagic;
			} break;
		}
	}
}

bool KUserSkillTree::IsMasterSkillLevel( IN int iSkillID )
{
	const CXSLSkillTree::SkillTemplet* pSkillTemplet = SiCXSLSkillTree()->GetSkillTemplet( iSkillID );
	if( NULL == pSkillTemplet )
	{
		START_LOG( cerr, L"스킬이 최고 레벨인지 확인하려는데 스킬 템플릿이 NULL" )
			<< BUILD_LOG( iSkillID )
			<< END_LOG;

		return false;
	}

	SkillDataMap::iterator mit;
	mit = m_mapSkillTree.find( iSkillID );
	if( mit != m_mapSkillTree.end() )
	{
		const UserSkillData& userSkillData = mit->second;
		int iMasterSkillLevel = SiCXSLSkillTree()->GetMasterSkillLevel( m_iUnitClass, iSkillID );
		if( userSkillData.m_iSkillLevel > iMasterSkillLevel )
		{
			START_LOG( cerr, L"스킬이 최고 레벨인지 확인하려는데 스킬 레벨이 최고레벨보다 높다" )
				<< BUILD_LOG( m_iUnitClass )
				<< BUILD_LOG( iSkillID )
				<< BUILD_LOG( userSkillData.m_iSkillLevel )
				<< BUILD_LOG( iMasterSkillLevel )
				<< END_LOG;

			return true;
		}
		else if( userSkillData.m_iSkillLevel == iMasterSkillLevel )
		{
			return true;
		}
		else 
		{
			return false;
		}
	}

	return false;
}

bool KUserSkillTree::IsSkillLearned( IN int iSkillID )
{
	const CXSLSkillTree::SkillTemplet* pSkillTemplet = SiCXSLSkillTree()->GetSkillTemplet( iSkillID );
	if( NULL == pSkillTemplet )
	{
		START_LOG( cerr, L"스킬이 최고 레벨인지 확인하려는데 스킬 템플릿이 NULL" )
			<< BUILD_LOG( iSkillID )
			<< END_LOG;

		return false;
	}

	SkillDataMap::iterator mit;
	mit = m_mapSkillTree.find( iSkillID );
	if( mit != m_mapSkillTree.end() )
	{
		const UserSkillData& userSkillData = mit->second;
		if( userSkillData.m_iSkillLevel > 0 )
		{
			return true;
		}
	}

	return false;
}

bool KUserSkillTree::GetNecessarySkillPoint( IN OUT std::map< int, KGetSkillInfo >& mapSkillList, IN int& iTotalSP, IN int& iTotalCSP )
{
	// 전체 필요 sp 량 구하기
	iTotalSP = 0;
	// 전체 필요 csp 량 구하기
	iTotalCSP = 0;

	std::map< int, KGetSkillInfo>::iterator mit = mapSkillList.begin();
	for( ; mit != mapSkillList.end() ; ++mit )
	{
		const CXSLSkillTree::SkillTemplet* pSkillTemplet = SiCXSLSkillTree()->GetSkillTemplet( mit->first );
		if( NULL == pSkillTemplet )
		{
			START_LOG( cerr, L"스킬 템플릿이 NULL" )
				<< BUILD_LOG( mit->first )
				<< END_LOG;

			return false;
		}

		SkillDataMap::iterator mitMySkill;
		mitMySkill = m_mapSkillTree.find( mit->first );
		if( mitMySkill != m_mapSkillTree.end() )
		{
			const UserSkillData& userSkillData = mitMySkill->second;
			if( userSkillData.m_iSkillLevel > 0 )
			{
				// 이미 습득했다, 강화에 필요한 포인트만 계산
				int iUpgradeLevel = mit->second.m_iSkillLevel - userSkillData.m_iSkillLevel;
				if( iUpgradeLevel < 0 )
				{
					START_LOG( cerr, L"스킬 레벨이 이상하다" )
						<< BUILD_LOG( mit->first )
						<< BUILD_LOG( iUpgradeLevel )
						<< END_LOG;

					return false;
				}

				// 캐시 스킬을 사용 중이 아니라면
				if( IsCashSkillPointExpired() == true )
				{
					iTotalSP += pSkillTemplet->m_iRequireUpgradeSkillPoint * iUpgradeLevel;
				}
				else
				{
					mit->second.m_iSpendSkillCSPoint = pSkillTemplet->m_iRequireUpgradeSkillPoint * iUpgradeLevel;
					iTotalSP += mit->second.m_iSpendSkillCSPoint;
				}
			}
			else
			{
				// 미습득 스킬, 습득과 강화에 필요한 포인트 계산
				int iUpgradeLevel = mit->second.m_iSkillLevel - 1; // 스킬 습득 레벨 제거
				if( iUpgradeLevel < 0 )
				{
					START_LOG( cerr, L"스킬 레벨이 이상하다" )
						<< BUILD_LOG( mit->first )
						<< BUILD_LOG( iUpgradeLevel )
						<< END_LOG;

					return false;
				}
				
				// 캐시 스킬을 사용 중이 아니라면
				if( IsCashSkillPointExpired() == true )
				{
					iTotalSP += pSkillTemplet->m_iRequireLearnSkillPoint + pSkillTemplet->m_iRequireUpgradeSkillPoint * iUpgradeLevel;
				}
				else
				{
					mit->second.m_iSpendSkillCSPoint = pSkillTemplet->m_iRequireLearnSkillPoint+ pSkillTemplet->m_iRequireUpgradeSkillPoint * iUpgradeLevel;
					iTotalSP += mit->second.m_iSpendSkillCSPoint;
				}
			}
		}
		else
		{
			// 미습득 스킬, 습득과 강화에 필요한 포인트 계산
			int iUpgradeLevel = mit->second.m_iSkillLevel - 1; // 스킬 습득 레벨 제거
			if( iUpgradeLevel < 0 )
			{
				START_LOG( cerr, L"스킬 레벨이 이상하다" )
					<< BUILD_LOG( mit->first )
					<< BUILD_LOG( iUpgradeLevel )
					<< END_LOG;

				return false;
			}

			// 캐시 스킬을 사용 중이 아니라면
			if( IsCashSkillPointExpired() == true )
			{
				iTotalSP += pSkillTemplet->m_iRequireLearnSkillPoint + pSkillTemplet->m_iRequireUpgradeSkillPoint * iUpgradeLevel;
			}
			else
			{
				mit->second.m_iSpendSkillCSPoint = pSkillTemplet->m_iRequireLearnSkillPoint+ pSkillTemplet->m_iRequireUpgradeSkillPoint * iUpgradeLevel;
				iTotalSP += mit->second.m_iSpendSkillCSPoint;
			}
		}
	}

	// 소모되는 sp 가 있다
	if( iTotalSP > 0 )
	{
		// 캐시 스킬을 사용 중이라면
		if( IsCashSkillPointExpired() == false )
		{
			if( GetCSPoint() > 0 )
			{
				iTotalSP -= GetCSPoint();
				iTotalCSP = GetCSPoint();
			}
		}
	}

	return true;
}

void KUserSkillTree::GetTierSkillList( IN int iTier, OUT std::vector< int >& vecTierSkillList, OUT bool& bDefaultSkillTire )
{
	vecTierSkillList.clear();

	SkillDataMap::iterator mitMySkill;
	mitMySkill = m_mapSkillTree.begin();
	for( ; mitMySkill != m_mapSkillTree.end() ; ++mitMySkill )
	{
		const CXSLSkillTree::SkillTemplet* pSkillTemplet = SiCXSLSkillTree()->GetSkillTemplet( mitMySkill->first );
		if( NULL == pSkillTemplet )
		{
			START_LOG( cerr, L"스킬 템플릿이 NULL" )
				<< BUILD_LOG( mitMySkill->first )
				<< END_LOG;

			continue;
		}

		const CXSLSkillTree::SkillTreeTemplet* pSkillTreeTemplet = SiCXSLSkillTree()->GetSkillTreeTemplet( m_iUnitClass, mitMySkill->first );
		if( NULL == pSkillTreeTemplet )
		{
			START_LOG( cerr, L"스킬 트리 템플릿이 NULL" )
				<< BUILD_LOG( mitMySkill->first )
				<< END_LOG;

			continue;
		}

		if( pSkillTreeTemplet->m_iTier == iTier )
		{
			if( SiCXSLSkillTree()->IsUnitTypeDefaultSkill( mitMySkill->first ) == true )
			{
				bDefaultSkillTire = true;
			}

			if( mitMySkill->second.m_iSkillLevel > 0 )
			{
				vecTierSkillList.push_back( mitMySkill->first );
			}			
		}
	}
}

void KUserSkillTree::GetHaveSkillList( OUT std::map< int, int >& mapHaveSkill )
{
	SkillDataMap::iterator mitMySkill;
	mitMySkill = m_mapSkillTree.begin();
	for( ; mitMySkill != m_mapSkillTree.end() ; ++mitMySkill )
	{
		mapHaveSkill.insert( std::make_pair( mitMySkill->first, mitMySkill->second.m_iSkillLevel ) );
	}
}

void KUserSkillTree::ResetSkill( IN int iSkillID, IN bool bDefaultSkill )
{
	SkillDataMap::iterator mitMySkill;
	mitMySkill = m_mapSkillTree.find( iSkillID );
	if( mitMySkill != m_mapSkillTree.end() )
	{
		if( bDefaultSkill == true )
		{
			mitMySkill->second.m_iSkillLevel = 1;
			mitMySkill->second.m_iSkillCSPoint = 0;
		}
		else
		{
			m_mapSkillTree.erase( mitMySkill );
		}
	}
}

bool KUserSkillTree::CheckGetNewSkill( IN std::map< int, KGetSkillInfo >& mapGetSkillList, IN int iUnitClass, IN int iLevel, OUT KEGS_GET_SKILL_ACK& kPacket )
{
	// 배우려는 스킬 검사 하자
	std::map< int, KGetSkillInfo >::iterator mit = mapGetSkillList.begin();
	for( ; mit != mapGetSkillList.end() ; ++mit )
	{
		// 1. 정상 스킬 인가?
		const CXSLSkillTree::SkillTemplet* pSkillTemplet = SiCXSLSkillTree()->GetSkillTemplet( mit->first );
		if( pSkillTemplet == NULL )
		{
			kPacket.m_iOK = NetError::ERR_SKILL_01;
			return false;
		}

		const CXSLSkillTree::SkillTreeTemplet* pSkillTreeTemplet = SiCXSLSkillTree()->GetSkillTreeTemplet( iUnitClass, mit->first );
		if( pSkillTreeTemplet == NULL )
		{
			kPacket.m_iOK = NetError::ERR_SKILL_01;
			return false;
		}

		// 2. 스킬 레벨 검사
		if( IsMasterSkillLevel( mit->first ) == true )
		{
			kPacket.m_iOK = NetError::ERR_SKILL_00;
			return false;
		}

		// 배울 스킬의 레벨 검사
		int iMasterSkillLevel = SiCXSLSkillTree()->GetMasterSkillLevel( m_iUnitClass, mit->first );
		if( mit->second.m_iSkillLevel > iMasterSkillLevel )
		{
			kPacket.m_iOK = NetError::ERR_SKILL_00;
			return false;
		}

		// 배울 스킬의 필요 레벨 검사
		if( (int)pSkillTemplet->m_vecRequireCharactorLevel.size() >= mit->second.m_iSkillLevel )
		{
			if( pSkillTemplet->m_vecRequireCharactorLevel[mit->second.m_iSkillLevel-1] > iLevel )
			{
				kPacket.m_iOK = NetError::ERR_SKILL_03;
				return false;
			}
		}

		// 3. 봉인 스킬 검사
		if( pSkillTemplet->m_bBornSealed == true )
		{
			if( IsSkillUnsealed( mit->first ) == false )
			{
				kPacket.m_iOK = NetError::ERR_SKILL_15;
				return false;
			}
		}

		// 4. 습득 가능 Unit Class 검사
		if( IsMyUnitClassSkill( mit->first ) == false )
		{
			START_LOG( cerr, L"자신의 클래스와 다른 스킬을 획득하려함.!" )
				<< BUILD_LOG( iUnitClass )
				<< BUILD_LOG( mit->first )
				<< END_LOG;

			kPacket.m_iOK = NetError::ERR_SKILL_16;
			return false;
		}

		// 5. 선행스킬을 습득 검사
		if( IsAllPrecedingSkillLearned( mit->first, mapGetSkillList ) == false )
		{
			kPacket.m_iOK = NetError::ERR_SKILL_07;
			return false;
		}

		// 6. 2지선다 스킬의 경우 동일 tier 스킬 습득 유무 검사
		if( pSkillTreeTemplet->m_iColumn == 0 || pSkillTreeTemplet->m_iColumn == 1 )
		{
			// 배운 스킬 중에 동일 2지선다 스킬이 있는지 확인
			std::vector< int > vecTierSkillList;
			vecTierSkillList.clear();
			bool bDefaultSkillTire = false;
			GetTierSkillList( pSkillTreeTemplet->m_iTier, vecTierSkillList, bDefaultSkillTire ); 

			// 기본 스킬이 아니어야 한다.
			if( bDefaultSkillTire == false )
			{
				BOOST_TEST_FOREACH( int, iTierSkillID, vecTierSkillList )
				{
					// 동일 스킬이라면 넘어가
					if( iTierSkillID == mit->first )
						continue;

					const CXSLSkillTree::SkillTreeTemplet* pTierSkillTreeTemplet = SiCXSLSkillTree()->GetSkillTreeTemplet( iUnitClass, iTierSkillID );
					if( NULL == pTierSkillTreeTemplet )
					{
						kPacket.m_iOK = NetError::ERR_SKILL_01;
						return false;
					}

					// 동일 티어의 스킬인데 2지선다 스킬을 배운 것이 있다.
					if( pTierSkillTreeTemplet->m_iColumn == 0 || pTierSkillTreeTemplet->m_iColumn == 1 )
					{
						kPacket.m_iOK = NetError::ERR_SKILL_29;
						return false;
					}
				}
			}
			
			// 배울 스킬 중에 동일 2지선다 스킬이 있는지 확인
			std::map< int, KGetSkillInfo >::iterator mitOther = mapGetSkillList.begin();
			for( ; mitOther != mapGetSkillList.end() ; ++mitOther )
			{
				// 동일 스킬이라면 패스
				if( mitOther->first == mit->first )
					continue;

				const CXSLSkillTree::SkillTreeTemplet* pOtherSkillTreeTemplet = SiCXSLSkillTree()->GetSkillTreeTemplet( iUnitClass, mitOther->first );
				if( pOtherSkillTreeTemplet == NULL )
				{
					kPacket.m_iOK = NetError::ERR_SKILL_01;
					return false;
				}

				// 동일 tier
				if( pOtherSkillTreeTemplet->m_iTier == pSkillTreeTemplet->m_iTier )
				{
					// 2지선다 스킬이다.
					if( pOtherSkillTreeTemplet->m_iColumn == 0 || pOtherSkillTreeTemplet->m_iColumn == 1 )
					{
						kPacket.m_iOK = NetError::ERR_SKILL_29;
						return false;
					}
				}
			}
		}
	}

	return true;
}

bool KUserSkillTree::CheckResetSkill( IN KEGS_RESET_SKILL_REQ& kPacket_, IN int iUnitClass, IN int iLevel, OUT int& iOK, OUT bool& bSKillInitLevel )
{
	// 0레벨로 만들 것인가?
	bSKillInitLevel = true;

	// 삭제 대상 SkillTemplet
	const CXSLSkillTree::SkillTemplet* pDelSkillTemplet = SiCXSLSkillTree()->GetSkillTemplet( kPacket_.m_iSkillID );
	if( pDelSkillTemplet == NULL )
	{
		iOK = NetError::ERR_SKILL_01;
		return false;
	}

	// 삭제 대상 SkillTreeTemplet
	const CXSLSkillTree::SkillTreeTemplet* pDelSkillTreeTemplet = SiCXSLSkillTree()->GetSkillTreeTemplet( iUnitClass, kPacket_.m_iSkillID );
	if( pDelSkillTreeTemplet == NULL )
	{
		iOK = NetError::ERR_SKILL_01;
		return false;
	}

	// 스킬 레벨이 정상인지 검사
	if( GetSkillLevel( kPacket_.m_iSkillID ) < 1 )
	{
		iOK = NetError::ERR_SKILL_20;
		return false;
	}

	// 기본 스킬이면 0렙 시키지 못한다
	bool isDefaultSkill = SiCXSLSkillTree()->IsUnitTypeDefaultSkill( kPacket_.m_iSkillID );

	// 선행 스킬이면 0렙 시키지 못한다
	bool bAllFollowingSkillLevelZero = IsAllFollowingSkillLevelZero( kPacket_.m_iSkillID );


	if( isDefaultSkill == true || bAllFollowingSkillLevelZero == false )
	{
		bSKillInitLevel = false;
	}

	return true;
}

#else	// SERV_UPGRADE_SKILL_SYSTEM_2013
/*
KUserSkillTree::KUserSkillTree(void) :
m_wstrSkillSlotBEndDate( L"" ),
m_tSkillSlotBEndDate( 0 ),
m_iCSPoint( 0 ),
m_iMaxCSPoint( 0 ), 
m_wstrCSPointEndDate( L"" ),
m_tCSPointEndDate( 0 ),
m_iUnitClass( 0 ),
//{{ 2010. 03. 22  최육사	기술의 노트
#ifdef SERV_SKILL_NOTE	
m_cSkillNoteMaxPageNum( 0 )
#endif SERV_SKILL_NOTE
//}}
{
	for( int i = 0; i < MAX_SKILL_SLOT; ++i )
	{
		m_aiSkillSlot[i] = 0;
	}
}

KUserSkillTree::~KUserSkillTree(void)
{
}

void KUserSkillTree::Reset( bool bResetSkillTree, bool bResetEquippedSkill, bool bResetUnsealedSkill, bool bResetCashSkillPoint, bool bResetSkillNote )
{
	if( true == bResetEquippedSkill )
	{
		for( int i = 0; i < MAX_SKILL_SLOT; ++i )
		{
			m_aiSkillSlot[i] = 0;
		}
	}

	if( true == bResetSkillTree )
	{
		m_mapSkillTree.clear();
	}

	if( true == bResetUnsealedSkill )
	{
		m_setUnsealedSkillID.clear();
	}

	//if( true == bResetSkillSlotB )
	//{
	//	m_bEnabledSkillSlotB = false;
	//}

	if( true == bResetCashSkillPoint )
	{
		m_iCSPoint = 0;
		m_iMaxCSPoint = 0;
		KncUtil::ConvertStringToCTime( std::wstring( L"2000-01-01 00:00:00" ), m_tCSPointEndDate );
	}

	//{{ 2010. 03. 22  최육사	기술의 노트
#ifdef SERV_SKILL_NOTE	
	if( true == bResetSkillNote )
	{
		m_cSkillNoteMaxPageNum = 0;
		m_mapSkillNote.clear();
	}
#endif SERV_SKILL_NOTE
	//}}
}

void KUserSkillTree::InitSkill( IN std::vector<KUserSkillData>& vecSkillList, IN int aSkillSlot[], IN std::wstring& wstrSkillSlotBEndDate, IN std::vector<short int>& vecUnsealedSkillList, IN int iUnitClass )
{
	m_iUnitClass = iUnitClass;

	m_mapSkillTree.clear();

	for( UINT i=0; i<vecSkillList.size(); i++ )
	{
		const KUserSkillData& userSkillData = vecSkillList[i];
		
		SkillDataMap::iterator mit;
		mit = m_mapSkillTree.find( (int)userSkillData.m_iSkillID );
		if( mit != m_mapSkillTree.end() )
		{
			START_LOG( cerr, L"중복된 스킬을 보유하고 있음." )
				<< BUILD_LOG( userSkillData.m_iSkillID )
				<< BUILD_LOG( userSkillData.m_cSkillLevel )
				<< BUILD_LOG( userSkillData.m_cSkillCSPoint )
				<< END_LOG;

			continue;
		}


		int iSkillLevel = (int) userSkillData.m_cSkillLevel;


		// 스킬의 최고레벨보다 높은 스킬이 있다면 최고레벨로 보정
		const CXSLSkillTree::SkillTreeTemplet* pSkillTreeTemplet = SiCXSLSkillTree()->GetSkillTreeTemplet( m_iUnitClass, userSkillData.m_iSkillID );
		if( NULL != pSkillTreeTemplet )
		{
			if( (int) userSkillData.m_cSkillLevel > pSkillTreeTemplet->m_iMaxSkillLevel )
			{
				START_LOG( cerr, L"최고레벨보다 높은 레벨의 스킬이 DB에 있음" )
					<< BUILD_LOG( m_iUnitClass )
					<< BUILD_LOG( userSkillData.m_iSkillID )
					<< BUILD_LOG( userSkillData.m_cSkillLevel )
					<< BUILD_LOG( pSkillTreeTemplet->m_iMaxSkillLevel )
					<< BUILD_LOG( userSkillData.m_cSkillCSPoint )
					<< END_LOG;

				iSkillLevel = pSkillTreeTemplet->m_iMaxSkillLevel;
			}
		}
		

		// 이미 존재하면 삽입되지 않고 존재하지 않으면 삽입된다.
		m_mapSkillTree[ userSkillData.m_iSkillID ] = UserSkillData( iSkillLevel, (int)userSkillData.m_cSkillCSPoint );
	}
	
	if( !aSkillSlot )
	{
		START_LOG( cerr, L"스킬 슬롯이 NULL" )
			<< END_LOG;

		return;
	}

	// 장착 스킬
	for( int i = 0; i < MAX_SKILL_SLOT; i++ )
	{
		m_aiSkillSlot[i] = aSkillSlot[i];
	}

	//  봉인해제된 스킬 목록
	m_setUnsealedSkillID.clear();
	for( UINT i=0; i<vecUnsealedSkillList.size(); i++ )
	{
		m_setUnsealedSkillID.insert( (int) vecUnsealedSkillList[i] );
	}

	
	//{{ 2011. 01. 06  김민성  스킬슬롯체인지 체크(인벤토리-기간제) 기능 구현
#ifdef SERV_SKILL_SLOT_CHANGE_INVENTORY
	
	SetSkillSolotBEndDate( wstrSkillSlotBEndDate );
    
#else
	// 스킬 슬롯 B
	m_wstrSkillSlotBEndDate = wstrSkillSlotBEndDate;

	if( true == wstrSkillSlotBEndDate.empty() )
	{
		// 과거 시간으로 세팅
		m_wstrSkillSlotBEndDate = L"2000-01-01 00:00:00";
		KncUtil::ConvertStringToCTime( m_wstrSkillSlotBEndDate, m_tSkillSlotBEndDate );
	}
	else if( !KncUtil::ConvertStringToCTime( wstrSkillSlotBEndDate, m_tSkillSlotBEndDate ) )
	{
		START_LOG( cerr, L"문자열 시간 변환 실패." )
			<< BUILD_LOG( wstrSkillSlotBEndDate )
			<< END_LOG;

		// 과거 시간으로 세팅
		m_wstrSkillSlotBEndDate = L"2000-01-01 00:00:00";
		KncUtil::ConvertStringToCTime( m_wstrSkillSlotBEndDate, m_tSkillSlotBEndDate );
	}
#endif SERV_SKILL_SLOT_CHANGE_INVENTORY
	//}}
}


//{{ 2010. 03. 22  최육사	기술의 노트
#ifdef SERV_SKILL_NOTE

void KUserSkillTree::InitSkillNote( IN char cSkillNoteMaxPageNum, IN const std::map< char, int >& mapSkillNote )
{
	m_mapSkillNote.clear();

	// 업데이트	
	m_cSkillNoteMaxPageNum = cSkillNoteMaxPageNum;
    m_mapSkillNote = mapSkillNote;
}

#endif SERV_SKILL_NOTE
//}}


int KUserSkillTree::GetSkillLevel( IN int iSkillID )
{
	SkillDataMap::iterator mit;
	mit = m_mapSkillTree.find( iSkillID );
	if( mit != m_mapSkillTree.end() )
	{
		const UserSkillData& userSkillData = mit->second;
		return userSkillData.m_iSkillLevel;
	}

	return 0;
}

bool KUserSkillTree::GetSkillLevelAndCSP( IN int iSkillID, OUT int& iSkillLevel, OUT int& iSkillCSPoint )
{
	SkillDataMap::iterator mit;
	mit = m_mapSkillTree.find( iSkillID );
	if( mit != m_mapSkillTree.end() )
	{
		const UserSkillData& userSkillData = mit->second;
		iSkillLevel		= userSkillData.m_iSkillLevel;
		iSkillCSPoint	= userSkillData.m_iSkillCSPoint;
		return true;
	}
	else
	{
		iSkillLevel		= 0;
		iSkillCSPoint	= 0;
	}

	return false;
}


// 데이터가 있으면 값을 변경하고, 없으면 추가한다
bool KUserSkillTree::SetSkillLevelAndCSP( int iSkillID, int iSkillLevel, int iSkillCSPoint )
{
	SET_ERROR( NET_OK );


	int iCheckSkillLevel = iSkillLevel;
	if( 0 == iSkillLevel )
	{
		iCheckSkillLevel = 1;
	}
	else if( iSkillLevel < 0 )
	{
		SET_ERROR( ERR_SKILL_20 );

		return false;
	}
	

	const CXSLSkillTree::SkillTemplet* pSkillTemplet = SiCXSLSkillTree()->GetSkillTemplet( iSkillID, iCheckSkillLevel );
	if( NULL == pSkillTemplet )
	{
		SET_ERROR( ERR_SKILL_19 );

		return false;
	}

	if( iSkillLevel < iSkillCSPoint )
	{
		SET_ERROR( ERR_SKILL_20 );

		return false;
	}


	if( iSkillLevel > SiCXSLSkillTree()->GetMaxSkillLevel( m_iUnitClass, iSkillID ) )
	{
		SET_ERROR( ERR_SKILL_21 );

		return false;
	}


	SkillDataMap::iterator mit;
	mit = m_mapSkillTree.find( iSkillID );
	if( mit != m_mapSkillTree.end() )
	{
		UserSkillData& userSkillData = mit->second;
		userSkillData.m_iSkillLevel		= iSkillLevel;
		userSkillData.m_iSkillCSPoint	= iSkillCSPoint;
	}
	else 
	{
		m_mapSkillTree[ iSkillID ] = UserSkillData( iSkillLevel, iSkillCSPoint );
	}

	return true;
}

bool KUserSkillTree::IsExist( IN int iSkillID )
{
	SkillDataMap::iterator mit;
	mit = m_mapSkillTree.find( iSkillID );
	if( mit != m_mapSkillTree.end() )
	{
		return true;
	}

	//{{ 2013. 04. 01	 인연 시스템 - 김민성
#ifdef SERV_RELATIONSHIP_SYSTEM
	if( iSkillID == CXSLSkillTree::SI_ETC_WS_COMMON_LOVE ) // 6001
	{
		return true;
	}
#endif SERV_RELATIONSHIP_SYSTEM
	//}

	return false;
}

bool KUserSkillTree::ChangeSkillSlot( int iSlotID, int iSkillID )
{
	SET_ERROR( NET_OK );


	if( iSlotID < 0 || iSlotID >= MAX_SKILL_SLOT )
	{
		SET_ERROR( ERR_SKILL_09 );

		return false;
	}

	
	
	if( 0 == iSkillID )
	{
		m_aiSkillSlot[iSlotID] = 0;

		return true;
	}



	if( iSkillID > 0 && IsExist( iSkillID ) == false )
	{
		SET_ERROR( ERR_SKILL_10 );

		return false;
	}




	if( IsSkillSlotB( iSlotID )  &&  iSkillID != 0 )
	{
		// [고민] 인벤토리에 스킬슬롯B 확장 아이템이 있는지 검사를 할까? 말까?

		CTime tCurrentTime = CTime::GetCurrentTime();
		if( tCurrentTime > m_tSkillSlotBEndDate )
		{
			SET_ERROR( ERR_SKILL_13 );

			return false;
		}
	}

	m_aiSkillSlot[iSlotID] = iSkillID;
	//m_aiSkillSlot[iSlotID].m_cSkillLevel = (UCHAR) GetSkillLevel( iSkillID );


	return true;
}

//{{ 2012. 12. 3	박세훈	스킬 슬롯 체인지 패킷 통합
#ifdef SERV_SKILL_SLOT_CHANGE_PACKET_INTEGRATE
bool KUserSkillTree::ChangeSkillSlot( IN const KEGS_CHANGE_SKILL_SLOT_REQ& kPacket_, OUT KEGS_CHANGE_SKILL_SLOT_ACK& kPacket )
{
	SET_ERROR( NET_OK );

	const iSlotID	= kPacket_.m_iSlotID;
	const iSkillID	= kPacket_.m_iSkillID;
	const iSlotID2	= GetSlotID( kPacket_.m_iSkillID );
	const iSkillID2	= GetSkillID( kPacket_.m_iSlotID );

	if( ( iSlotID < 0 ) || ( iSlotID >= MAX_SKILL_SLOT ) )
	{
		SET_ERROR( ERR_SKILL_09 );
		return false;
	}

	if( 0 == iSkillID )
	{
		m_aiSkillSlot[iSlotID] = 0;
	}
	else
	{
		if( iSkillID > 0 && IsExist( iSkillID ) == false )
		{
			SET_ERROR( ERR_SKILL_10 );
			return false;
		}

		if( IsSkillSlotB( iSlotID ) )
		{
			// [고민] 인벤토리에 스킬슬롯B 확장 아이템이 있는지 검사를 할까? 말까?

			CTime tCurrentTime = CTime::GetCurrentTime();
			if( tCurrentTime > m_tSkillSlotBEndDate )
			{
				SET_ERROR( ERR_SKILL_13 );
				return false;
			}
		}
	}

	if( ( 0 <= iSlotID2 ) && ( iSlotID2 < MAX_SKILL_SLOT ) )
	{
		if( 0 == iSkillID2 )
		{
			m_aiSkillSlot[iSlotID2] = 0;
		}
		else if( ( iSkillID2 > 0 ) && ( IsExist( iSkillID2 ) == false ) )
		{
			SET_ERROR( ERR_SKILL_10 );
			return false;
		}
		else
		{
			if( IsSkillSlotB( iSlotID2 ) )
			{
				// [고민] 인벤토리에 스킬슬롯B 확장 아이템이 있는지 검사를 할까? 말까?

				CTime tCurrentTime = CTime::GetCurrentTime();
				if( tCurrentTime > m_tSkillSlotBEndDate )
				{
					SET_ERROR( ERR_SKILL_13 );
					return false;
				}
			}
			m_aiSkillSlot[iSlotID2] = iSkillID2;
		}
	}

	m_aiSkillSlot[iSlotID] = iSkillID;

	kPacket.m_iSlotID	= iSlotID;
	kPacket.m_iSkillID	= iSkillID;
	kPacket.m_iSlotID2	= iSlotID2;
	kPacket.m_iSkillID2	= iSkillID2;

	return true;
}
#endif SERV_SKILL_SLOT_CHANGE_PACKET_INTEGRATE
//}}

void KUserSkillTree::GetSkillSlot( OUT std::vector<int>& vecSkillID )
{
	vecSkillID.resize(0);

	for( int i = 0; i < MAX_SKILL_SLOT; ++i )
	{
		vecSkillID.push_back( m_aiSkillSlot[i] );
	}
}




void KUserSkillTree::GetSkillSlot( OUT std::vector<KSkillData>& vecSkillSlot )
{
	vecSkillSlot.resize(0);

	for( int i = 0; i < MAX_SKILL_SLOT; ++i )
	{
		KSkillData kSkillData;
		kSkillData.m_iSkillID = m_aiSkillSlot[i];
		kSkillData.m_cSkillLevel = (char) GetSkillLevel( kSkillData.m_iSkillID );
		vecSkillSlot.push_back( kSkillData );
	}
}


void KUserSkillTree::GetSkillSlot( OUT KSkillData aSkillSlot[] )
{
	if( !aSkillSlot )
	{
		START_LOG( cerr, L"스킬 슬롯이 NULL" )
			<< END_LOG;

		return;
	}

	for( int i = 0; i < MAX_SKILL_SLOT; ++i )
	{
		aSkillSlot[i].m_iSkillID = m_aiSkillSlot[i];
		aSkillSlot[i].m_cSkillLevel = (char) GetSkillLevel( aSkillSlot[i].m_iSkillID );
	}
}

//{{ 2012. 12. 3	박세훈	스킬 슬롯 체인지 패킷 통합
#ifdef SERV_SKILL_SLOT_CHANGE_PACKET_INTEGRATE
int KUserSkillTree::GetSkillID( int iSlotID )
{
	if( iSlotID < 0 || iSlotID >= MAX_SKILL_SLOT )
	{
		return 0;
	}

	return m_aiSkillSlot[iSlotID];
}

int KUserSkillTree::GetSlotID( int iSkillID )
{
	if( IsExist( iSkillID ) == false )
	{
		return -1;
	}

	for( int i=0; i < MAX_SKILL_SLOT; ++i )
	{
		if( m_aiSkillSlot[i] == iSkillID )
		{
			return i;
		}
	}

	return -1;
}
#endif SERV_SKILL_SLOT_CHANGE_PACKET_INTEGRATE
//}}

// 패시브 스킬 ID와 레벨 정보만 추출해준다. 던전, PVP게임에서 내 유닛외의 다른 유닛의 스킬 정보를 알려주기 위해서
void KUserSkillTree::GetPassiveSkillData( OUT std::vector<KSkillData>& vecSkillSlot )
{
	vecSkillSlot.resize(0);

	SkillDataMap::iterator it;
	for( it = m_mapSkillTree.begin(); it != m_mapSkillTree.end(); it++ )
	{
		KSkillData kSkillData;
		kSkillData.m_iSkillID		= it->first;
		kSkillData.m_cSkillLevel	= (UCHAR) it->second.m_iSkillLevel;
		vecSkillSlot.push_back( kSkillData );
	}
}

void KUserSkillTree::CalcUsedSPointAndCSPoint( OUT int& iSPoint, OUT int& iCSPoint )
{
	iSPoint = 0;
	iCSPoint = 0;

	for( SkillDataMap::iterator mit = m_mapSkillTree.begin(); mit != m_mapSkillTree.end(); ++mit )
	{
		const int iSkillID = (int)mit->first;
		const UserSkillData& userSkillData = mit->second;


		if( userSkillData.m_iSkillLevel > 0 )
		{
			if( userSkillData.m_iSkillCSPoint > userSkillData.m_iSkillLevel )
			{
				START_LOG( cerr, L"스킬 레벨 < 스킬 CSP" )
					<< BUILD_LOG( iSkillID )
					<< BUILD_LOG( userSkillData.m_iSkillLevel )
					<< BUILD_LOG( userSkillData.m_iSkillCSPoint )
					<< END_LOG;
			}

			iSPoint		+= userSkillData.m_iSkillLevel - userSkillData.m_iSkillCSPoint;
			iCSPoint	+= userSkillData.m_iSkillCSPoint;
		}
		else if( userSkillData.m_iSkillLevel == 0 )
		{
			if( userSkillData.m_iSkillCSPoint > 0 )
			{
				START_LOG( cwarn, L"스킬 레벨이 0이하인데 SkillCSP가 0보다 큼!" )
					<< BUILD_LOG( iSkillID )
					<< BUILD_LOG( userSkillData.m_iSkillLevel )
					<< BUILD_LOG( userSkillData.m_iSkillCSPoint )
					<< END_LOG;
			}
		}
		else
		{
			START_LOG( cwarn, L"스킬 레벨이 0보다 작다!" )
				<< BUILD_LOG( iSkillID )
				<< BUILD_LOG( userSkillData.m_iSkillLevel )
				<< BUILD_LOG( userSkillData.m_iSkillCSPoint )
				<< END_LOG;
		}
	}
}

// tier별로 소모된 누적 SP+CSP를 계산해준다
void KUserSkillTree::CalcCumulativeUsedSPointOnEachTier( OUT std::vector< int >& vecTierSPoint )
{
	int iMaxTierIndex = 0;
	for( SkillDataMap::iterator mit = m_mapSkillTree.begin(); mit != m_mapSkillTree.end(); ++mit )
	{
		const UserSkillData& userSkillData = mit->second;
	
		// 스킬 레벨값이 유효한지 검사
		if( userSkillData.m_iSkillLevel <= 0 )
			continue;

		const CXSLSkillTree::SkillTreeTemplet* pSkillTreeTemplet = SiCXSLSkillTree()->GetSkillTreeTemplet( m_iUnitClass, mit->first );
		if( pSkillTreeTemplet == NULL )
			continue;

		if( iMaxTierIndex < pSkillTreeTemplet->m_iTier )
			iMaxTierIndex = pSkillTreeTemplet->m_iTier;
	}

	vecTierSPoint.resize(0);
	for( int i = 0; i <= iMaxTierIndex; ++i )
	{
		vecTierSPoint.push_back( 0 );
	}

	for( SkillDataMap::iterator mit = m_mapSkillTree.begin(); mit != m_mapSkillTree.end(); ++mit )
	{
		const UserSkillData& userSkillData = mit->second;
	
		// 스킬 레벨값이 유효한지 검사
		if( userSkillData.m_iSkillLevel <= 0 )
			continue;

		const CXSLSkillTree::SkillTreeTemplet* pSkillTreeTemplet = SiCXSLSkillTree()->GetSkillTreeTemplet( m_iUnitClass, mit->first );
		if( pSkillTreeTemplet == NULL )
			continue;

		//{{ 2010. 8. 17	최육사	스킬 되돌리기 버그 수정
#ifdef SERV_RESET_SKILL_BUG_FIX
		// 실제 각 Tier별로 SPoint 더할땐 CSP는 제외하고 더합니다!
		vecTierSPoint[ pSkillTreeTemplet->m_iTier ] += ( userSkillData.m_iSkillLevel - userSkillData.m_iSkillCSPoint );
#else
		vecTierSPoint[ pSkillTreeTemplet->m_iTier ] += userSkillData.m_iSkillLevel;
#endif SERV_RESET_SKILL_BUG_FIX
		//}}
	}

	int iCumulativeTierSPoint = 0;
	for( UINT ui = 0; ui < vecTierSPoint.size(); ++ui )
	{
		iCumulativeTierSPoint += vecTierSPoint[ui];
		vecTierSPoint[ui] = iCumulativeTierSPoint;
	}
}

bool KUserSkillTree::IsCashSkillPointExpired()
{
	if( 0 == m_iMaxCSPoint )
		return true;
	
	return false;
}



// cash skill point로 찍은 스킬의 레벨을 조정하고 복구될 스킬 포인트를 계산만 해준다
// @iRetrievedSPoint: 돌려줄 SP 수치
void KUserSkillTree::CalcExpireCashSkillPoint( OUT int& iRetrievedSPoint, OUT std::vector<KUserSkillData>& vecModifiedUserSkillData )
{
	iRetrievedSPoint = 0;
	vecModifiedUserSkillData.clear();
	
	for( SkillDataMap::iterator mit = m_mapSkillTree.begin(); mit != m_mapSkillTree.end(); ++mit )
	{
		const int iSkillID = mit->first;
		const UserSkillData& userSkillData = mit->second;
		
		// 캐쉬 스킬을 쓴 기록이 있다면?
		if( userSkillData.m_iSkillCSPoint > 0 )
		{
			iRetrievedSPoint	+= userSkillData.m_iSkillCSPoint;
			UCHAR uNewSkillLevel = (UCHAR) ( userSkillData.m_iSkillLevel - userSkillData.m_iSkillCSPoint );

			// 기본스킬 레벨이 0이 되는 경우 오류 로그를 남기고 1로 만들어 준다
			if( SiCXSLSkillTree()->IsUnitTypeDefaultSkill( iSkillID ) == true )
			{
				if( uNewSkillLevel <= 0 )
				{
					START_LOG( cerr, L"CSP 기간만료시에 기본스킬 레벨을 0이하의 값으로 바꾸려고 계산했습니다!!" )
						<< BUILD_LOG( iSkillID )
						<< BUILD_LOG( userSkillData.m_iSkillLevel )
						<< BUILD_LOG( userSkillData.m_iSkillCSPoint )
						<< END_LOG;

					uNewSkillLevel = 1;
				}
			}

			vecModifiedUserSkillData.push_back( KUserSkillData( (short) iSkillID, uNewSkillLevel, 0 ) );
		}
	}

	// 스킬트리에 소모했던 CSP 와 남아있는 CSP 를 더해서 구매시의 CSP를 빼면 돌려줄 SP 수치가 계산된다
	iRetrievedSPoint += m_iCSPoint - m_iMaxCSPoint;
}


// 실제로 cash skill point로 획득한 스킬을 되돌린다.
void KUserSkillTree::ExpireCashSkillPoint()
{
	for( SkillDataMap::iterator mit = m_mapSkillTree.begin(); mit != m_mapSkillTree.end(); ++mit )
	{
		int iSkillID = mit->first;
		UserSkillData& userSkillData = mit->second;

		if( userSkillData.m_iSkillCSPoint > 0 )
		{
			int iNewSkillLevel = userSkillData.m_iSkillLevel - userSkillData.m_iSkillCSPoint;
						
			// 기본스킬 레벨이 0이 되는 경우 오류 로그를 남기고 1로 만들어 준다
			if( true == SiCXSLSkillTree()->IsUnitTypeDefaultSkill( iSkillID ) )
			{
				if( iNewSkillLevel <= 0 )
				{
					START_LOG( cerr, L"CSP 기간만료시에 기본스킬 레벨을 0이하의 값으로 바꾸려고 했습니다!!" )
						<< BUILD_LOG( iSkillID )
						<< BUILD_LOG( userSkillData.m_iSkillLevel )
						<< BUILD_LOG( userSkillData.m_iSkillCSPoint )
						<< END_LOG;

					iNewSkillLevel = 1;
				}
			}
			
			userSkillData.m_iSkillLevel		= iNewSkillLevel;
			userSkillData.m_iSkillCSPoint	= 0;
		}
	}

	// 장착된 스킬 중에서 skill level이 0이하인 스킬이 있으면 탈착시킨다.
	for( int i=0; i<MAX_SKILL_SLOT; i++ )
	{
		if( GetSkillLevel( m_aiSkillSlot[i] ) <= 0 )
		{
			ChangeSkillSlot( i, 0 );
		}
	}

	m_iMaxCSPoint = 0;
	m_iCSPoint = 0;
}








bool KUserSkillTree::IsSkillUnsealed( int iSkillID )
{
	std::set< int >::iterator it = m_setUnsealedSkillID.find( iSkillID );
	if( it != m_setUnsealedSkillID.end() )
	{
		return true;
	}

	return false;
}

//{{ 2009. 8. 4  최육사		스킬봉인해제
bool KUserSkillTree::SkillUnseal( int iSkillID )
{
	if( IsSkillUnsealed( iSkillID ) )
	{
		START_LOG( cerr, L"이미 봉인해제된 스킬입니다. 일어나서는 안되는 에러! 검사했을텐데.." )
			<< BUILD_LOG( iSkillID )
			<< END_LOG;

		return false;
	}

	m_setUnsealedSkillID.insert( iSkillID );
	return true;
}
//}}


bool KUserSkillTree::SetCSPointEndDate( std::wstring wstrEndDate )
{
	if( true == wstrEndDate.empty() )
		return false;

	if( !KncUtil::ConvertStringToCTime( wstrEndDate, m_tCSPointEndDate ) )
		return false;

	m_wstrCSPointEndDate = wstrEndDate;

	return true;
}



void KUserSkillTree::ExpandSkillSlotB( std::wstring& wstrSkillSlotBEndDate )
{	
	m_wstrSkillSlotBEndDate = wstrSkillSlotBEndDate;

	if( !KncUtil::ConvertStringToCTime( wstrSkillSlotBEndDate, m_tSkillSlotBEndDate ) )
	{
		START_LOG( cerr, L"문자열 시간 변환 실패." )
			<< BUILD_LOG( wstrSkillSlotBEndDate )
			<< END_LOG;

		// 과거 시간으로 세팅
		m_wstrSkillSlotBEndDate = L"2000-01-01 00:00:00";
		KncUtil::ConvertStringToCTime( m_wstrSkillSlotBEndDate, m_tSkillSlotBEndDate );
	}

	for( int i = SKILL_SLOT_B1; i < MAX_SKILL_SLOT; ++i )
	{
		m_aiSkillSlot[i] = 0;
	}
}




KUserSkillTree::SKILL_SLOT_B_EXPIRATION_STATE KUserSkillTree::GetSkillSlotBExpirationState()
{
	// note!! 스킬 슬롯 확장 아이템 사용기간이 1년 이상 남았으면 영구 아이템으로 간주한다.
	const CTimeSpan MAGIC_PERMANENT_TIME_SPAN = CTimeSpan( 365, 0, 0, 0 );

	CTime tCurrentTime = CTime::GetCurrentTime();
	if( tCurrentTime >= m_tSkillSlotBEndDate )
	{
		return SSBES_EXPIRED;
	}
	else
	{
		CTimeSpan expirationTimeLeft = m_tSkillSlotBEndDate - tCurrentTime;

		if( expirationTimeLeft > MAGIC_PERMANENT_TIME_SPAN )
		{
			return SSBES_PERMANENT;
		}
		else
		{
			return SSBES_NOT_EXPIRED;
		}
	}
}



void KUserSkillTree::ExpireSkillSlotB()
{
	// 현재 시간을 구하여 종료시간과 비교
	CTime tCurrentTime = CTime::GetCurrentTime();
	if( tCurrentTime >= m_tSkillSlotBEndDate )
	{
		// 자주 호출되는 부분이기 때문에 코드 최적화 (for문 제거)
		m_aiSkillSlot[SKILL_SLOT_B1] = 0;
		m_aiSkillSlot[SKILL_SLOT_B2] = 0;
		m_aiSkillSlot[SKILL_SLOT_B3] = 0;
		m_aiSkillSlot[SKILL_SLOT_B4] = 0;
	}
}




bool KUserSkillTree::IsMyUnitClassSkill( int iSkillID )
{
	if( SiCXSLSkillTree()->GetMaxSkillLevel( m_iUnitClass, iSkillID ) > 0 )
		return true;

	return false;
}



bool KUserSkillTree::IsAllPrecedingSkillMaxLevel( int iSkillID )
{
	const CXSLSkillTree::SkillTreeTemplet* pSkillTreeTemplet = SiCXSLSkillTree()->GetSkillTreeTemplet( m_iUnitClass, iSkillID );
	if( NULL == pSkillTreeTemplet )
	{
		START_LOG( cerr, L"스킬트리 템플릿이 없음_preceding" )
			<< BUILD_LOG( m_iUnitClass )
			<< BUILD_LOG( iSkillID )
			<< END_LOG;

		return false;
	}


	for( UINT i=0; i<pSkillTreeTemplet->m_vecPrecedingSkill.size(); i++ )
	{
		int iPrecedingSkillID = pSkillTreeTemplet->m_vecPrecedingSkill[i];
		if( false == IsMaxSkillLevel( iPrecedingSkillID ) )
		{
			return false;
		}
	}

	return true;
}



bool KUserSkillTree::IsAllFollowingSkillLevelZero( int iSkillID )
{
	const CXSLSkillTree::SkillTreeTemplet* pSkillTreeTemplet = SiCXSLSkillTree()->GetSkillTreeTemplet( m_iUnitClass, iSkillID );
	if( NULL == pSkillTreeTemplet )
	{
		START_LOG( cerr, L"스킬트리 템플릿이 없음_following" )
			<< BUILD_LOG( m_iUnitClass )
			<< BUILD_LOG( iSkillID )
			<< END_LOG;

		return false;
	}

	BOOST_TEST_FOREACH( const int, iFollowingSkillID, pSkillTreeTemplet->m_vecFollowingSkill )
	{
		if( GetSkillLevel( iFollowingSkillID ) > 0 )
		{
			return false;
		}
	}

	return true;
}


// 이 스킬을 배우기에 충분할 만큼 SP를 소모했는지
bool KUserSkillTree::IsTierOpened( int iSkillID )
{
	const CXSLSkillTree::SkillTreeTemplet* pSkillTreeTemplet = SiCXSLSkillTree()->GetSkillTreeTemplet( m_iUnitClass, iSkillID );
	if( NULL == pSkillTreeTemplet )
	{
		START_LOG( cerr, L"스킬트리 템플릿이 없음_IsTierOpened" )
			<< BUILD_LOG( m_iUnitClass )
			<< BUILD_LOG( iSkillID )
			<< END_LOG;

		return false;
	}


	const int MAGIC_SKILL_POINT_PER_TIER = 5;
	int iUsedSPoint = 0;
	int iUsedCSPoint = 0;
	CalcUsedSPointAndCSPoint( iUsedSPoint, iUsedCSPoint );
	if( iUsedSPoint + iUsedCSPoint < pSkillTreeTemplet->m_iTier * MAGIC_SKILL_POINT_PER_TIER )
	{
		return false;
	}

	return true;
}


//{{ 2010. 03. 22  최육사	기술의 노트
#ifdef SERV_SKILL_NOTE

void KUserSkillTree::GetSkillNote( OUT std::vector< int >& vecSkillNote )
{
	vecSkillNote.clear();

	std::map< char, int >::const_iterator mit;
	for( mit = m_mapSkillNote.begin(); mit != m_mapSkillNote.end(); ++mit )
	{
		vecSkillNote.push_back( mit->second );
	}
}

bool KUserSkillTree::GetExpandSkillNotePage( IN u_char ucLevel, OUT char& cPageNum )
{
	cPageNum = 0;

	const u_char ucCheckNum = ucLevel / 10; // 10의 자리수 구하기 위해 10으로 나눔
	switch( ucCheckNum )
	{
	case 0:
	case 1:
		return false;

	case 2:
		cPageNum = 1;
		return true;

	case 3:
		cPageNum = 2;
		return true;

	case 4:
		cPageNum = 3;
		return true;

	case 5:
		cPageNum = 4;
		return true;
		//{{ 2011. 07. 13	최육사	만렙 확장
#ifdef SERV_EXPAND_60_LIMIT_LEVEL
	case 6:
		cPageNum = 5;
		return true;
#endif SERV_EXPAND_60_LIMIT_LEVEL
		//}}
	}

	return false;
}

bool KUserSkillTree::IsExistSkillNotePage( IN char cPageNum )
{
    if( m_cSkillNoteMaxPageNum > cPageNum )
		return true;

	return false;
}

bool KUserSkillTree::IsExistSkillNoteMemoID( IN int iSkillNoteMemoID )
{
	std::map< char, int >::const_iterator mit;
	for( mit = m_mapSkillNote.begin(); mit != m_mapSkillNote.end(); ++mit )
	{
        if( mit->second == iSkillNoteMemoID )
			return true;
	}

	return false;
}

void KUserSkillTree::UpdateSkillNoteMemo( IN char cPageNum, IN int iMemoID )
{
	std::map< char, int >::iterator mit;
	mit = m_mapSkillNote.find( cPageNum );
	if( mit == m_mapSkillNote.end() )
	{
		m_mapSkillNote.insert( std::make_pair( cPageNum, iMemoID ) );
	}
	else
	{
        mit->second = iMemoID;
	}
}

#endif SERV_SKILL_NOTE
//}}

//{{ 2011. 01. 06  김민성  스킬슬롯체인지 체크(인벤토리-기간제) 기능 구현
#ifdef SERV_SKILL_SLOT_CHANGE_INVENTORY
void KUserSkillTree::SetSkillSolotBEndDate( IN std::wstring& wstrSkillSlotBEndDate )
{
	// 스킬 슬롯 B
	m_wstrSkillSlotBEndDate = wstrSkillSlotBEndDate;

	if( true == wstrSkillSlotBEndDate.empty() )
	{
		// 과거 시간으로 세팅
		m_wstrSkillSlotBEndDate = L"2000-01-01 00:00:00";
		KncUtil::ConvertStringToCTime( m_wstrSkillSlotBEndDate, m_tSkillSlotBEndDate );
	}
	else if( !KncUtil::ConvertStringToCTime( wstrSkillSlotBEndDate, m_tSkillSlotBEndDate ) )
	{
		START_LOG( cerr, L"문자열 시간 변환 실패." )
			<< BUILD_LOG( wstrSkillSlotBEndDate )
			<< END_LOG;

		// 과거 시간으로 세팅
		m_wstrSkillSlotBEndDate = L"2000-01-01 00:00:00";
		KncUtil::ConvertStringToCTime( m_wstrSkillSlotBEndDate, m_tSkillSlotBEndDate );
	}
}
#endif SERV_SKILL_SLOT_CHANGE_INVENTORY
//}}

//{{ 2011. 11. 21  김민성	전직 변경 아이템
#ifdef SERV_UNIT_CLASS_CHANGE_ITEM
void KUserSkillTree::GetUnSealedSkillList( OUT std::vector< short >& vecUnsealedSkillID )
{
	std::set< int >::iterator sit = m_setUnsealedSkillID.begin();
	for( ; sit != m_setUnsealedSkillID.end() ; ++sit )
	{
		vecUnsealedSkillID.push_back( static_cast<short>(*sit) );
	}
}

void KUserSkillTree::SetClassChangeSkill( IN std::map< int, int >& mapSkill )
{
	std::set< int >::iterator sit;

	std::map< int, int >::iterator mit = mapSkill.begin();
	for(  ; mit != mapSkill.end() ; ++mit )
	{
		int itempfirst = mit->first;
		sit = m_setUnsealedSkillID.find( mit->first );
		if( sit != m_setUnsealedSkillID.end() )
		{
			m_setUnsealedSkillID.erase( sit );
			m_setUnsealedSkillID.insert( mit->second );
		}
		else
		{
			START_LOG( cerr, L"이런 스킬을 가지고 있지 않다고?! 있다고 해서 기록한거잖아!!" )
				<< BUILD_LOG( mit->first )
				<< BUILD_LOG( mit->second )
				<< END_LOG;
		}
	}
}

void KUserSkillTree::SetClassChangeMemo( IN std::map< int, int >& mapMemo )
{
	std::map< char, int >::iterator mymit;
	
	std::map< int, int >::iterator mit = mapMemo.begin();
	for(  ; mit != mapMemo.end() ; ++mit )
	{
		mymit = m_mapSkillNote.begin();
		for( ; mymit != m_mapSkillNote.end() ; ++mymit )
		{
			if( mymit->second == mit->first )
			{
				mymit->second = mit->second;
			}
		}
	}
}
#endif SERV_UNIT_CLASS_CHANGE_ITEM
//}}

void KUserSkillTree::GetSkillStat( KStat& kStat )
{
	kStat.Init();


	SkillDataMap::iterator mit;
	for( mit = m_mapSkillTree.begin(); mit != m_mapSkillTree.end(); ++mit )
	{
		const UserSkillData& userSkillData = mit->second;
		if( userSkillData.m_iSkillLevel <= 0 )
			continue;

		const CXSLSkillTree::SkillTemplet* pSkillTemplet = SiCXSLSkillTree()->GetSkillTemplet( mit->first, userSkillData.m_iSkillLevel );

		if( pSkillTemplet == NULL )
		{
			START_LOG( cerr, L"스킬 템플릿 포인트 이상.!" )
				<< BUILD_LOG( mit->first )
				<< BUILD_LOG( userSkillData.m_iSkillLevel )
				<< END_LOG;

			continue;
		}

		switch( pSkillTemplet->m_eType )
		{
		case CXSLSkillTree::ST_PASSIVE_PHYSIC_ATTACK: 
		case CXSLSkillTree::ST_PASSIVE_MAGIC_ATTACK:  
		case CXSLSkillTree::ST_PASSIVE_MAGIC_DEFENCE: 
		case CXSLSkillTree::ST_PASSIVE_PHYSIC_DEFENCE:
			{
				kStat.m_iBaseHP			+= (int)pSkillTemplet->m_Stat.m_fBaseHP;
				kStat.m_iAtkPhysic		+= (int)pSkillTemplet->m_Stat.m_fAtkPhysic;
				kStat.m_iAtkMagic		+= (int)pSkillTemplet->m_Stat.m_fAtkMagic;
				kStat.m_iDefPhysic		+= (int)pSkillTemplet->m_Stat.m_fDefPhysic;
				kStat.m_iDefMagic		+= (int)pSkillTemplet->m_Stat.m_fDefMagic;
			} break;
		}
	}
}

bool KUserSkillTree::IsMaxSkillLevel( IN int iSkillID )
{
	const CXSLSkillTree::SkillTemplet* pSkillTemplet = SiCXSLSkillTree()->GetSkillTemplet( iSkillID, 1 );
	if( NULL == pSkillTemplet )
	{
		START_LOG( cerr, L"스킬이 최고 레벨인지 확인하려는데 스킬 템플릿이 NULL" )
			<< BUILD_LOG( iSkillID )
			<< END_LOG;

		return false;
	}

	SkillDataMap::iterator mit;
	mit = m_mapSkillTree.find( iSkillID );
	if( mit != m_mapSkillTree.end() )
	{
		const UserSkillData& userSkillData = mit->second;
		int iMaxSkillLevel = SiCXSLSkillTree()->GetMaxSkillLevel( m_iUnitClass, iSkillID );
		if( userSkillData.m_iSkillLevel > iMaxSkillLevel )
		{
			START_LOG( cerr, L"스킬이 최고 레벨인지 확인하려는데 스킬 레벨이 최고레벨보다 높다" )
				<< BUILD_LOG( m_iUnitClass )
				<< BUILD_LOG( iSkillID )
				<< BUILD_LOG( userSkillData.m_iSkillLevel )
				<< BUILD_LOG( iMaxSkillLevel )
				<< END_LOG;

			return true;
		}
		else if( userSkillData.m_iSkillLevel == iMaxSkillLevel )
		{
			return true;
		}
		else 
		{
			return false;
		}
	}

	return false;
}
*/
#endif	// SERV_UPGRADE_SKILL_SYSTEM_2013

