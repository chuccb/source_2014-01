#include "dxstdafx.h"
#include ".\x2viewerui.h"

#define SiSelf()			ms_pSelfInstance
#define SiMain()			ms_pSelfInstance->m_pMain
#define SiGetObject( s )	ms_pSelfInstance->m_pMain->GetObject( CX2ViewerObject::##s )
#define SiGetBaseDlg( s )	ms_pSelfInstance->m_BaseOption.GetControl( s )
#define SiGetUnitDlg( s )	ms_pSelfInstance->m_Unit.GetControl( s )
#define SiGetMeshDlg( s )	ms_pSelfInstance->m_Mesh.GetControl( s )
#define SiGetRPDlg( s )		ms_pSelfInstance->m_RenderParam.GetControl( s )
#define SiGetParticleDlg( s )	ms_pSelfInstance->m_ParticleBasic.GetControl( s )
#define SiGetParticleEditorDlg( s )	ms_pSelfInstance->m_ParticleEditor.GetControl( s )

#define	GET_LUA_POS( s, x, y, w, h ) \
{ \
	LUA_GET_VALUE( luaManager, #s "_X", x, 0 ); \
	LUA_GET_VALUE( luaManager, #s "_Y", y, 0 ); \
	LUA_GET_VALUE( luaManager, #s "_W", w, 0 ); \
	LUA_GET_VALUE( luaManager, #s "_H", h, 0 ); \
} \

CX2ViewerUI*	CX2ViewerUI::ms_pSelfInstance	= NULL;

template <class T>
bool from_string(T& t, 
				 const std::string& s, 
				 std::ios_base& (*f)(std::ios_base&))
{
	std::istringstream iss(s);
	return !(iss >> f >> t).fail();
}

void DropAnimationFile( CX2ViewerUI* pViewerUI, const WCHAR* pFileName, const WCHAR* pDir )
{
	pViewerUI->DropFile( pFileName, pDir );
}


CX2ViewerUI::CX2ViewerUI(CX2ViewerMain* pMain) :
m_pMain( pMain ),
m_fElapsedTime( 0.0f ),
m_bIsInit( false ),
m_MeshSel( MS_NONE )
{
	CX2ViewerObject::SetObjectStyle( CX2ViewerObject::OS_UI );

	ms_pSelfInstance = this;
	m_vecDialog.clear();

	m_fAnimTimeInc = 0.1f;

	//기본옵션 기능
	m_BaseOption.Init( &g_DialogResourceManager );
	m_BaseOption.SetCallback( OnGUIEvent );

	m_BaseOption.AddCheckBox( UI_CHECK_GRID, L"Grid On/Off(G)", 0, 0, 0, 0, true );
	m_BaseOption.AddCheckBox( UI_CHECK_WIREFRAME, L"WireFrame Mode", 0, 0, 0, 0 );
	m_BaseOption.AddButton( UI_BUT_RESET, L"RESET", 0, 0, 0, 0 );
	m_BaseOption.AddButton( UI_BUT_UI_INIT, L"UI INIT", 0, 0, 0, 0 );
    
	m_BaseOption.AddStatic( UI_STATIC_CAMERAMODE, L"CAMERA MODE", 0, 0, 0, 0 );
	m_BaseOption.AddRadioButton( UI_RADIO_CAMERA_NORMAL, 1, L"NORMAL", 0, 0, 0, 0, true );
	m_BaseOption.AddRadioButton( UI_RADIO_CAMERA_NAVIGATION, 1, L"NAVIGATION", 0, 0, 0, 0 );
	m_BaseOption.AddButton( UI_BUT_CAMERA_RESET, L"CAMERA\nRESET", 0, 0, 0, 0 );

	m_BaseOption.AddStatic( UI_STATIC_BGSET, L"B.G Color", 0, 0, 0, 0 );
	m_BaseOption.AddStatic( UI_STATIC_BG_A, L"A", 0, 0, 0, 0 );
	m_BaseOption.AddStatic( UI_STATIC_BG_R, L"R", 0, 0, 0, 0 );
	m_BaseOption.AddStatic( UI_STATIC_BG_G, L"G", 0, 0, 0, 0 );
	m_BaseOption.AddStatic( UI_STATIC_BG_B, L"B", 0, 0, 0, 0 );
	m_BaseOption.AddEditBox( UI_EDIT_BG_A, L"A", 0, 0, 0, 0 );
	m_BaseOption.AddEditBox( UI_EDIT_BG_R, L"R", 0, 0, 0, 0 );
	m_BaseOption.AddEditBox( UI_EDIT_BG_G, L"G", 0, 0, 0, 0 );
	m_BaseOption.AddEditBox( UI_EDIT_BG_B, L"B", 0, 0, 0, 0 );

	m_BaseOption.AddStatic( UI_STATIC_WORLD_MESH, L"World Mesh", 0, 0, 0, 0 );
	m_BaseOption.AddButton( UI_BUT_WORLD_MESH, L"File Open", 0, 0, 0, 0 );

	m_BaseOption.AddButton( UI_BUT_WORLD_MESH_RESET, L"WORLD\nMESH\nRESET", 0, 0, 0, 0 );
	m_BaseOption.AddStatic( UI_STATIC_WORLD_MESH_X, L"X", 0, 0, 0, 0 );
	m_BaseOption.AddStatic( UI_STATIC_WORLD_MESH_Y, L"Y", 0, 0, 0, 0 );
	m_BaseOption.AddStatic( UI_STATIC_WORLD_MESH_Z, L"Z", 0, 0, 0, 0 );
	m_BaseOption.AddEditBox( UI_EDIT_WORLD_MESH_X, L"", 0, 0, 0, 0 );
	m_BaseOption.AddEditBox( UI_EDIT_WORLD_MESH_Y, L"", 0, 0, 0, 0 );
	m_BaseOption.AddEditBox( UI_EDIT_WORLD_MESH_Z, L"", 0, 0, 0, 0 );

	m_BaseOption.AddButton( UI_BUT_EFFECT_SET, L"EFFECT SET", 0, 0, 0, 0 );

	m_vecDialog.push_back( &m_BaseOption );

	//Unit 기능
	m_Unit.Init( &g_DialogResourceManager );
	m_Unit.SetCallback( OnGUIUnitEvent );

	m_Unit.AddStatic( UI_STATIC_SCALE, L"Scale", 0, 0, 0, 0 );
	m_Unit.AddStatic( UI_STATIC_SCALE_X, L"X", 0, 0, 0, 0 );
	m_Unit.AddStatic( UI_STATIC_SCALE_Y, L"Y", 0, 0, 0, 0 );
	m_Unit.AddStatic( UI_STATIC_SCALE_Z, L"Z", 0, 0, 0, 0 );
	m_Unit.AddEditBox( UI_EDIT_SCALE_X, L"", 0, 0, 0, 0 );
	m_Unit.AddEditBox( UI_EDIT_SCALE_Y, L"", 0, 0, 0, 0 );
	m_Unit.AddEditBox( UI_EDIT_SCALE_Z, L"", 0, 0, 0, 0 );

	m_Unit.AddStatic( UI_STATIC_LIGHT_POS, L"LightPos", 0, 0, 0, 0 );
	m_Unit.AddStatic( UI_STATIC_LIGHT_POS_X, L"X", 0, 0, 0, 0 );
	m_Unit.AddStatic( UI_STATIC_LIGHT_POS_Y, L"Y", 0, 0, 0, 0 );
	m_Unit.AddStatic( UI_STATIC_LIGHT_POS_Z, L"Z", 0, 0, 0, 0 );
	m_Unit.AddEditBox( UI_EDIT_LIGHT_POS_X, L"", 0, 0, 0, 0 );
	m_Unit.AddEditBox( UI_EDIT_LIGHT_POS_Y, L"", 0, 0, 0, 0 );
	m_Unit.AddEditBox( UI_EDIT_LIGHT_POS_Z, L"", 0, 0, 0, 0 );
	m_Unit.AddCheckBox( UI_CHECK_LIGHT_ONOFF, L"Light On/Off", 0, 0, 0, 0, true );

	m_Unit.AddStatic( UI_STATIC_OBJECT, L"Object List", 0, 0, 0, 0 );
	m_Unit.AddListBox( UI_LIST_OBJECT, 0, 0, 0, 0 );

	m_Unit.AddStatic( UI_STATIC_ANIMATION, L"Animation List", 0, 0, 0, 0 );
	m_Unit.AddListBox( UI_LIST_ANIMATION, 0, 0, 0, 0 );
    m_Unit.AddListBox( UI_LIST_BONE, 0, 0, 0, 0 );
	m_Unit.AddEditBox( UI_EDIT_ANIM_NUM, L"0", 0, 0, 0, 0 );
	m_Unit.AddEditBox( UI_EDIT_ANIM_NAME, L"Not Selected", 0, 0, 0, 0 );
	m_Unit.AddButton( UI_BUT_ANIM_NAME_CHANGE, L"AnimName Change", 0, 0, 0, 0 );

	m_Unit.AddButton( UI_BUT_PLAY_ONOFF, L"stop ■", 0, 0, 0, 0 );
	m_Unit.AddStatic( UI_STATIC_ANIM_SPEED, L"speed : 100", 0, 0, 0, 0 );
	m_Unit.AddSlider( UI_SLIDE_ANIM_SPEED, 0, 0, 0, 0, 1, 100, 100 );
	m_Unit.AddStatic( UI_STATIC_ANIM_FRAME, L"Anim Frame : 1/31", 0, 0, 0, 0 );
	m_Unit.AddCheckBox( UI_CHECK_MOTION_ONOFF, L"Motion On/Off", 0, 0, 0, 0, true );
	m_Unit.AddButton( UI_BUT_RENDER_PARAM, L"Render\nParam", 0, 0, 0, 0 );

	m_Unit.AddStatic( UI_STATIC_PLAY_TYPE, L"PLAY TYPE", 0, 0, 0, 0 );
	m_Unit.AddComboBox( UI_COMBO_PLAY_TYPE, 0, 0, 0, 0 );
	m_Unit.AddCheckBox( UI_CHECK_BOUNDING, L"Bounding Box", 0, 0, 0, 0, false );
	m_Unit.AddCheckBox( UI_CHECK_ATTACK_BOX, L"Attack Box", 0, 0, 0, 0, false );

	m_Unit.AddEditBox( UI_EDIT_FRAME_TIME_INC, L"FRAME_TIME_INC", 0, 0, 0, 0, false );
	m_Unit.AddButton( UI_BUT_PREV_FRAME, L"PREV", 0, 0, 0, 0 );
	m_Unit.AddButton( UI_BUT_NEXT_FRAME, L"NEXT", 0, 0, 0, 0 );

	m_Unit.AddButton( UI_BUT_ATTACH_WEAPON, L"무기 붙이기", 0, 0, 0, 0 );
	m_Unit.AddEditBox( UI_EDIT_ATTACH_WEAPON_BONE_NAME, L"Dummy1_Rhand", 0, 0, 0, 0 );
	m_Unit.AddStatic( UI_STATIC_ATTACH_WEAPON_BONE_NAME, L"Bone Name", 0, 0, 0, 0 );

	m_Unit.AddStatic( UI_STATIC_WEAPON_ROT_X, L"Weapon Rotation X", 0, 0, 0, 0 );
	m_Unit.AddEditBox( UI_EDIT_WEAPON_ROT_X, L"0", 0, 0, 0, 0 );
	m_Unit.AddStatic( UI_STATIC_WEAPON_ROT_Y, L"Weapon Rotation Y", 0, 0, 0, 0 );
	m_Unit.AddEditBox( UI_EDIT_WEAPON_ROT_Y, L"0", 0, 0, 0, 0 );
	m_Unit.AddStatic( UI_STATIC_WEAPON_ROT_Z, L"Weapon Rotation Z", 0, 0, 0, 0 );
	m_Unit.AddEditBox( UI_EDIT_WEAPON_ROT_Z, L"0", 0, 0, 0, 0 );

    m_Unit.AddButton( UI_BUT_ATTACH_ACCESSORY, L"악세 붙이기", 0, 0, 0, 0 );
    m_Unit.AddEditBox( UI_EDIT_ATTACH_ACCESSORY_BONE_NAME, L"Dummy2_Lhand", 0, 0, 0, 0 );
    m_Unit.AddStatic( UI_STATIC_ATTACH_ACCESSORY_BONE_NAME, L"Bone Name", 0, 0, 0, 0 );

    m_Unit.AddStatic( UI_STATIC_ACCESSORY_TRANS_X, L"Accessory Position X", 0, 0, 0, 0 );
    m_Unit.AddEditBox( UI_EDIT_ACCESSORY_TRANS_X, L"0", 0, 0, 0, 0 );
    m_Unit.AddStatic( UI_STATIC_ACCESSORY_TRANS_Y, L"Accessory Position Y", 0, 0, 0, 0 );
    m_Unit.AddEditBox( UI_EDIT_ACCESSORY_TRANS_Y, L"0", 0, 0, 0, 0 );
    m_Unit.AddStatic( UI_STATIC_ACCESSORY_TRANS_Z, L"Accessory Position Z", 0, 0, 0, 0 );
    m_Unit.AddEditBox( UI_EDIT_ACCESSORY_TRANS_Z, L"0", 0, 0, 0, 0 );

    m_Unit.AddStatic(  UI_STATIC_ACCESSORY_SCALE,   L"Scale", 0, 0, 0, 0 );
    m_Unit.AddEditBox( UI_EDIT_ACCESSORY_SCALE_X,   L"100", 0, 0, 0, 0 );
    m_Unit.AddEditBox( UI_EDIT_ACCESSORY_SCALE_Y,   L"100", 0, 0, 0, 0 );
    m_Unit.AddEditBox( UI_EDIT_ACCESSORY_SCALE_Z,   L"100", 0, 0, 0, 0 );

    m_Unit.AddStatic(  UI_STATIC_ACCESSORY_ROTATE,      L"Rotate", 0, 0, 0, 0 );
    m_Unit.AddEditBox( UI_EDIT_ACCESSORY_ROTATE_X,      L"0", 0, 0, 0, 0 );
    m_Unit.AddEditBox( UI_EDIT_ACCESSORY_ROTATE_Y,      L"0", 0, 0, 0, 0 );
    m_Unit.AddEditBox( UI_EDIT_ACCESSORY_ROTATE_Z,      L"0", 0, 0, 0, 0 );
	m_vecDialog.push_back( &m_Unit );

	//Mesh UI 설정--
	m_Mesh.Init( &g_DialogResourceManager );
	m_Mesh.SetCallback( OnGUIMeshEvent );

	m_Mesh.AddStatic( UI_STATIC_MESH_SCALE, L"Scale", 0, 0, 0, 0 );
	m_Mesh.AddStatic( UI_STATIC_MESH_SCALE_X, L"X", 0, 0, 0, 0 );
	m_Mesh.AddStatic( UI_STATIC_MESH_SCALE_Y, L"Y", 0, 0, 0, 0 );
	m_Mesh.AddStatic( UI_STATIC_MESH_SCALE_Z, L"Z", 0, 0, 0, 0 );
	m_Mesh.AddEditBox( UI_EDIT_MESH_SCALE_X, L"", 0, 0, 0, 0 );
	m_Mesh.AddEditBox( UI_EDIT_MESH_SCALE_Y, L"", 0, 0, 0, 0 );
	m_Mesh.AddEditBox( UI_EDIT_MESH_SCALE_Z, L"", 0, 0, 0, 0 );

	m_Mesh.AddStatic( UI_STATIC_MESH_LIGHT_POS, L"LightPos", 0, 0, 0, 0 );
	m_Mesh.AddStatic( UI_STATIC_MESH_LIGHT_POS_X, L"X", 0, 0, 0, 0 );
	m_Mesh.AddStatic( UI_STATIC_MESH_LIGHT_POS_Y, L"Y", 0, 0, 0, 0 );
	m_Mesh.AddStatic( UI_STATIC_MESH_LIGHT_POS_Z, L"Z", 0, 0, 0, 0 );
	m_Mesh.AddEditBox( UI_EDIT_MESH_LIGHT_POS_X, L"", 0, 0, 0, 0 );
	m_Mesh.AddEditBox( UI_EDIT_MESH_LIGHT_POS_Y, L"", 0, 0, 0, 0 );
	m_Mesh.AddEditBox( UI_EDIT_MESH_LIGHT_POS_Z, L"", 0, 0, 0, 0 );
	m_Mesh.AddCheckBox( UI_CHECK_MESH_LIGHT_ONOFF, L"Light On/Off", 0, 0, 0, 0, true );

	m_Mesh.AddButton( UI_BUT_MESH_RENDER_PARAM, L"Render\nParam", 0, 0, 0, 0 );

	m_vecDialog.push_back( &m_Mesh );
	//--Mesh UI 설정

	//랜더 파라메터 설정
	m_RenderParam.Init( &g_DialogResourceManager );
	m_RenderParam.SetCallback( OnGUIRPEvent );
	m_RenderParam.SetSize( RP_SIZE_X, RP_SIZE_Y );
	m_RenderParam.SetBackgroundColors( 0x0a6588c8 );

	m_RenderParam.AddStatic( UI_STATIC_RENDERTYPE, L"RENDER TYPE", 0, 0, 0, 0 );
	m_RenderParam.AddComboBox( UI_COMBO_RENDERTYPE, 0, 0, 0, 0 );

	m_RenderParam.AddStatic( UI_STATIC_CARTOON_TEX_TYPE, L"CARTOON TEXTURE TYPE", 0, 0, 0, 0 );
	m_RenderParam.AddComboBox( UI_COMBO_CARTOON_TEX_TYPE, 0, 0, 0, 0 );

	m_RenderParam.AddStatic( UI_STATIC_OUTLINE_WIDE, L"OUTLINE WIDE(폭)", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_OUTLINE_WIDE, L"", 0, 0, 0, 0 );

	m_RenderParam.AddStatic( UI_STATIC_OUTLINE_COLOR, L"OUTLINE COLOR", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_OUTLINE_COLOR_A, L"A", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_OUTLINE_COLOR_R, L"R", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_OUTLINE_COLOR_G, L"G", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_OUTLINE_COLOR_B, L"B", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_OUTLINE_COLOR_A, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_OUTLINE_COLOR_R, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_OUTLINE_COLOR_G, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_OUTLINE_COLOR_B, L"", 0, 0, 0, 0 );

	m_RenderParam.AddStatic( UI_STATIC_COLOR, L"COLOR", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_COLOR_A, L"A", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_COLOR_R, L"R", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_COLOR_G, L"G", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_COLOR_B, L"B", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_COLOR_A, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_COLOR_R, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_COLOR_G, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_COLOR_B, L"", 0, 0, 0, 0 );

	m_RenderParam.AddStatic( UI_STATIC_LIGHTFLOW_WIDE, L"LIGHT FLOW WIDE(폭)", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_LIGHTFLOW_WIDE, L"", 0, 0, 0, 0 );

	m_RenderParam.AddStatic( UI_STATIC_LIGHTFLOW_IMPACT, L"LIGHT FLOW IMPACT", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_LIGHTFLOW_IMPACT_MIN, L"Min", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_LIGHTFLOW_IMPACT_MAX, L"Max", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_LIGHTFLOW_IMPACT_ANIMTIME, L"AnimTime", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_LIGHTFLOW_IMPACT_MIN, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_LIGHTFLOW_IMPACT_MAX, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_LIGHTFLOW_IMPACT_ANIMTIME, L"", 0, 0, 0, 0 );

	m_RenderParam.AddStatic( UI_STATIC_LIGHTFLOW_POINT,   L"LIGHT FLOW POINT", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_LIGHTFLOW_POINT_X, L"X", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_LIGHTFLOW_POINT_Y, L"Y", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_LIGHTFLOW_POINT_Z, L"Z", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_LIGHTFLOW_POINT_X, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_LIGHTFLOW_POINT_Y, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_LIGHTFLOW_POINT_Z, L"", 0, 0, 0, 0 );

	m_RenderParam.AddStatic( UI_STATIC_TEXOFFSET_STAGE0,   L"TEXTURE OFFSET STAGE 0", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_TEXOFFSET_STAGE0_X, L"X", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_TEXOFFSET_STAGE0_Y, L"Y", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_TEXOFFSET_STAGE0_MIN, L"Min", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_TEXOFFSET_STAGE0_MAX, L"Max", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_TEXOFFSET_STAGE0_ANIMTIME, L"Anim Time", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_TEXOFFSET_STAGE0_MIN_X, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_TEXOFFSET_STAGE0_MIN_Y, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_TEXOFFSET_STAGE0_MAX_X, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_TEXOFFSET_STAGE0_MAX_Y, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_TEXOFFSET_STAGE0_ANIMTIME, L"", 0, 0, 0, 0 );

	m_RenderParam.AddStatic( UI_STATIC_TEXOFFSET_STAGE1,   L"TEXTURE OFFSET STAGE 1", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_TEXOFFSET_STAGE1_X, L"X", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_TEXOFFSET_STAGE1_Y, L"Y", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_TEXOFFSET_STAGE1_MIN, L"Min", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_TEXOFFSET_STAGE1_MAX, L"Max", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_TEXOFFSET_STAGE1_ANIMTIME, L"Anim Time", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_TEXOFFSET_STAGE1_MIN_X, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_TEXOFFSET_STAGE1_MIN_Y, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_TEXOFFSET_STAGE1_MAX_X, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_TEXOFFSET_STAGE1_MAX_Y, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_TEXOFFSET_STAGE1_ANIMTIME, L"", 0, 0, 0, 0 );

	m_RenderParam.AddStatic( UI_STATIC_TEXOFFSET_STAGE2,   L"TEXTURE OFFSET STAGE 2", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_TEXOFFSET_STAGE2_X, L"X", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_TEXOFFSET_STAGE2_Y, L"Y", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_TEXOFFSET_STAGE2_MIN, L"Min", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_TEXOFFSET_STAGE2_MAX, L"Max", 0, 0, 0, 0 );
	m_RenderParam.AddStatic( UI_STATIC_TEXOFFSET_STAGE2_ANIMTIME, L"Anim Time", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_TEXOFFSET_STAGE2_MIN_X, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_TEXOFFSET_STAGE2_MIN_Y, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_TEXOFFSET_STAGE2_MAX_X, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_TEXOFFSET_STAGE2_MAX_Y, L"", 0, 0, 0, 0 );
	m_RenderParam.AddEditBox( UI_EDIT_TEXOFFSET_STAGE2_ANIMTIME, L"", 0, 0, 0, 0 );

	m_RenderParam.AddStatic( UI_STATIC_ALPHA_BLEND, L"ALPHA BLEND", 0, 0, 0, 0 );
	m_RenderParam.AddComboBox( UI_COMBO_ALPHA_BLEND, 0, 0, 0, 0 );

	m_RenderParam.AddStatic( UI_STATIC_ZENABLE, L"ZENABLE", 0, 0, 0, 0 );
	m_RenderParam.AddComboBox( UI_COMBO_ZENABLE, 0, 0, 0, 0 );

	m_RenderParam.AddStatic( UI_STATIC_CULLMODE, L"CULLMODE", 0, 0, 0, 0 );
	m_RenderParam.AddComboBox( UI_COMBO_CULLMODE, 0, 0, 0, 0 );

	m_RenderParam.AddStatic( UI_STATIC_SRCBLEND, L"SRC BLEND", 0, 0, 0, 0 );
	m_RenderParam.AddComboBox( UI_COMBO_SRCBLEND, 0, 0, 0, 0 );

	m_RenderParam.AddStatic( UI_STATIC_DESTBLEND, L"DEST BLEND", 0, 0, 0, 0 );
	m_RenderParam.AddComboBox( UI_COMBO_DESTBLEND, 0, 0, 0, 0 );

	m_RenderParam.AddButton( UI_BUT_PARAM_OK, L"적 용", 0, 0, 0, 0 );
	m_RenderParam.AddButton( UI_BUT_PARAM_CANCEL, L"창닫기", 0, 0, 0, 0 );

	m_vecDialog.push_back( &m_RenderParam );

	// 파티클 관련 버튼들
	m_ParticleBasic.Init( &g_DialogResourceManager );
	m_ParticleBasic.SetCallback( OnGUIParticleEvent );

	m_ParticleBasic.AddListBox( UI_LIST_PARTICLE_LIST, 0, 0, 0, 0 );

	m_ParticleBasic.AddButton( UI_BUT_PARTICLE_DELETE, L"Del", 0, 0, 0, 0 );

	m_ParticleBasic.AddStatic( UI_STATIC_PARTICLE_TIME, L"Time", 0, 0, 0,0 );
	m_ParticleBasic.AddEditBox( UI_EDIT_PARTICLE_TIME, L"0.0", 0, 0, 0, 0 );

	m_ParticleBasic.AddButton( UI_BUT_PARTICLE_BONESET, L"SetBone", 0, 0, 0, 0 );
	m_ParticleBasic.AddButton( UI_BUT_PARTICLE_BONECLEAR, L"ClearBone", 0, 0, 0, 0 );
	m_ParticleBasic.AddCheckBox( UI_CHECK_PARTICLE_TRACE, L"Trace", 0,0,0,0, true );

	m_ParticleBasic.AddStatic( UI_STATIC_PARTICLE_OFFSET, L"Pos", 0, 0, 0,0 );
	m_ParticleBasic.AddEditBox( UI_EDIT_PARTICLE_OFFSET_X, L"0", 0, 0, 0, 0 );
	m_ParticleBasic.AddEditBox( UI_EDIT_PARTICLE_OFFSET_Y, L"0", 0, 0, 0, 0 );
	m_ParticleBasic.AddEditBox( UI_EDIT_PARTICLE_OFFSET_Z, L"0", 0, 0, 0, 0 );
	m_ParticleBasic.AddCheckBox( UI_CHECK_PARTICLE_LANDPOS, L"LandPos", 0, 0, 0, 0, false );

	m_ParticleBasic.AddStatic( UI_STATIC_PARTICLE_ROT, L"Rot", 0, 0, 0,0 );
	m_ParticleBasic.AddEditBox( UI_EDIT_PARTICLE_ROT_X, L"0", 0, 0, 0, 0 );
	m_ParticleBasic.AddEditBox( UI_EDIT_PARTICLE_ROT_Y, L"0", 0, 0, 0, 0 );
	m_ParticleBasic.AddEditBox( UI_EDIT_PARTICLE_ROT_Z, L"0", 0, 0, 0, 0 );
	m_ParticleBasic.AddCheckBox( UI_CHECK_PARTICLE_APPUNITROT, L"Apply Unit Rot", 0, 0, 0, 0, true );
	m_ParticleBasic.AddButton( UI_BUT_PARTICLE_SAVESEQUENCE, L"Save\nSequence", 0, 0, 0, 0 );
	
	m_ParticleBasic.AddButton( UI_BUT_PARTICLE_PARTICLEEDITOR, L"ParticleEditor", 0, 0, 0, 0 );

	m_vecDialog.push_back( &m_ParticleBasic );

	// Particle Editor
	m_ParticleEditor.Init( &g_DialogResourceManager );
	m_ParticleEditor.SetCallback( OnGUIParticleEditorEvent );
	m_ParticleEditor.AddListBox( UI_LIST_PARTICLE_EDITOR_MYPARTICLE, 0, 0, 0, 0 );
	m_ParticleEditor.AddButton( UI_BUT_PARTICLE_EDITOR_MYPARTICLE_LOAD, L"Load", 0, 0, 0, 0 );
	m_ParticleEditor.AddButton( UI_BUT_PARTICLE_EDITOR_MYPARTICLE_DELETE, L"Delete", 0, 0, 0, 0 );
	m_ParticleEditor.AddButton( UI_BUT_PARTICLE_EDITOR_MYPARTICLE_SAVE, L"Save", 0, 0, 0, 0 );

	m_ParticleEditor.AddListBox( UI_LIST_PARTICLE_EDITOR_PARTICLETEMPLET, 0, 0, 0, 0 );
	m_ParticleEditor.AddButton( UI_BUT_PARTICLE_EDITOR_PARTICLETEMPLET_COPY, L"Copy", 0, 0, 0, 0 );
	m_ParticleEditor.AddButton( UI_BUT_PARTICLE_EDITOR_PARTICLETEMPLET_RELOAD, L"Reload", 0, 0, 0, 0 );
	
	m_ParticleEditor.AddStatic( UI_STATIC_PARTICLE_EDITOR_EMITTERATTRIBUTE, L"EmitterProperties", 0, 0, 0,0 );
	m_ParticleEditor.AddListBox( UI_LIST_PARTICLE_EDITOR_EMITTERATTRIBUTE, 0, 0, 0, 0 );
	m_ParticleEditor.AddStatic( UI_STATIC_PARTICLE_EDITOR_EMITTERATTRIBUTE_VALUE, L"Value", 0, 0, 0,0 );
	m_ParticleEditor.AddEditBox( UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_SINGLE, L"0.0", 0, 0, 0, 0 );
	m_ParticleEditor.AddEditBox( UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_X, L"0.0", 0, 0, 0, 0 );
	m_ParticleEditor.AddEditBox( UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_Y, L"0.0", 0, 0, 0, 0 );
	m_ParticleEditor.AddEditBox( UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_Z, L"0.0", 0, 0, 0, 0 );
	m_ParticleEditor.AddComboBox( UI_COMBO_PARTICLE_EDITOR_EMITTERATTRIBUTE, 0, 0, 0, 0 );
	//m_ParticleEditor.AddButton( UI_BUT_PARTICLE_EDITOR_EMITTERATTRIBUTE_DEFAULT, L"DefaultValue", 0, 0, 0, 0 );
	m_ParticleEditor.AddButton( UI_BUT_PARTICLE_EDITOR_EMITTERATTRIBUTE_APPLY, L"Apply", 0, 0, 0, 0 );

	m_ParticleEditor.AddListBox( UI_LIST_PARTICLE_EDITOR_EVENT, 0, 0, 0, 0 );
	m_ParticleEditor.AddButton( UI_BUT_PARTICLE_EDITOR_EVENT_DELETE, L"Delete", 0, 0, 0, 0 );
		
	m_ParticleEditor.AddStatic( UI_STATIC_PARTICLE_EDITOR_EVENT_TYPE, L"EventType", 0, 0, 0,0 );
	m_ParticleEditor.AddListBox( UI_LIST_PARTICLE_EDITOR_EVENT_TYPE, 0, 0, 0, 0 );
	m_ParticleEditor.AddStatic( UI_STATIC_PARTICLE_EDITOR_EVENT_TIME, L"Time", 0, 0, 0,0 );
	m_ParticleEditor.AddCheckBox( UI_CHECK_PARTICLE_EDITOR_EVENT_FADE, L"Fade", 0, 0, 0, 0 );
	m_ParticleEditor.AddStatic( UI_STATIC_PARTICLE_EDITOR_EVENT_FROM, L"From", 0, 0, 0,0 );
	m_ParticleEditor.AddStatic( UI_STATIC_PARTICLE_EDITOR_EVENT_TO, L"To", 0, 0, 0,0 );
	m_ParticleEditor.AddEditBox( UI_EDIT_PARTICLE_EDITOR_EVENT_TIME1, L"0.0", 0, 0, 0, 0 );
	m_ParticleEditor.AddEditBox( UI_EDIT_PARTICLE_EDITOR_EVENT_TIME2, L"0.0", 0, 0, 0, 0 );
	m_ParticleEditor.AddStatic( UI_STATIC_PARTICLE_EDITOR_EVENT_VALUE, L"Value", 0, 0, 0,0 );
	m_ParticleEditor.AddEditBox( UI_EDIT_PARTICLE_EDITOR_EVENT_SINGLE, L"0.0", 0, 0, 0, 0 );
	m_ParticleEditor.AddEditBox( UI_EDIT_PARTICLE_EDITOR_EVENT_X, L"0.0", 0, 0, 0, 0 );
	m_ParticleEditor.AddEditBox( UI_EDIT_PARTICLE_EDITOR_EVENT_Y, L"0.0", 0, 0, 0, 0 );
	m_ParticleEditor.AddEditBox( UI_EDIT_PARTICLE_EDITOR_EVENT_Z, L"0.0", 0, 0, 0, 0 );

	m_ParticleEditor.AddEditBox( UI_EDIT_PARTICLE_EDITOR_EVENT_R, L"0.0", 0, 0, 0, 0 );
	m_ParticleEditor.AddEditBox( UI_EDIT_PARTICLE_EDITOR_EVENT_G, L"0.0", 0, 0, 0, 0 );
	m_ParticleEditor.AddEditBox( UI_EDIT_PARTICLE_EDITOR_EVENT_B, L"0.0", 0, 0, 0, 0 );
	m_ParticleEditor.AddEditBox( UI_EDIT_PARTICLE_EDITOR_EVENT_A, L"0.0", 0, 0, 0, 0 );

	m_ParticleEditor.AddComboBox( UI_COMBO_PARTICLE_EDITOR_EVENT, 0, 0, 0, 0 );
	m_ParticleEditor.AddButton( UI_BUT_PARTICLE_EDITOR_EVENT_APPLY, L"Apply", 0, 0, 0, 0 );
	m_ParticleEditor.AddButton( UI_BUT_PARTICLE_EDITOR_EVENT_NEW, L"New", 0, 0, 0, 0 );
	

	m_ParticleEditor.AddListBox( UI_LIST_PARTICLE_EDITOR_MODEL_LIST, 0, 0, 0, 0 );
	m_ParticleEditor.AddButton( UI_BUT_PARTICLE_EDITOR_MODEL_ADD, L"Add", 0, 0, 0, 0 );
	m_ParticleEditor.AddButton( UI_BUT_PARTICLE_EDITOR_MODEL_DELETE, L"Delete", 0, 0, 0, 0 );
	
	m_ParticleEditor.AddStatic( UI_STATIC_PARTICLE_EDITOR_PLAY_TYPE, L"Play Type", 0, 0, 0,0 );
	m_ParticleEditor.AddComboBox( UI_COMBO_PARTICLE_EDITOR_PLAY_TYPE, 0, 0, 0, 0 );
	m_ParticleEditor.AddButton( UI_BUT_PARTICLE_EDITOR_PLAY, L"Play▶", 0, 0, 0, 0 );

	m_ParticleEditor.AddStatic( UI_STATIC_PARTICLE_EDITOR_SPEED, L"Play Type", 0, 0, 0,0 );
	m_ParticleEditor.AddSlider( UI_SLIDE_PARTICLE_EDITOR_SPEED, 0, 0, 0, 0, 0, 100, 100 );
	m_ParticleEditor.AddStatic( UI_STATIC_PARTICLE_EDITOR_TIME, L"0.000 / 0.000", 0, 0, 0,0 );
	
	m_ParticleEditor.AddButton( UI_BUT_PARTICLE_EDITOR_EXIT, L"Exit Particle Editor", 0, 0, 0, 0 );

	//m_vecDialog.push_back( &m_ParticleEditor );
	// m_vecDialog에 Push하는 다이얼로그를 더 추가할 경우, OnGUIParticleEditorEvent의 case UI_BUT_PARTICLE_EDITOR_EXIT 항목
	// 내부에 있는 m_vecDialog.push_back 관련 부분에도 함께 추가해 주세요. 파티클 에디터로 갔다가 돌아올 때 UI 표시상태를
	// 원상복구 시키기 위해서 필요합니다.

	SetRenderParamOnOff( false );
	SetUnitOnOff( false );
	SetMeshOnOff( false );
	SetEffectButOnOff( false );
	SetParticleButOnOff( true );
	SetParticleEditButOnOff( false );

	SiMain()->SetSelectedAnimIndex( -1 );

}

CX2ViewerUI::~CX2ViewerUI(void)
{
	m_vecDialog.clear();
}

HRESULT CX2ViewerUI::OnFrameMove( double fTime, float fElapsedTime )
{
	m_fElapsedTime = fElapsedTime;

	DrawAnimFrame();
	DrawParticleTime();

	return S_OK;
}

HRESULT CX2ViewerUI::OnFrameRender()
{
	for( int i = 0; i < (int)m_vecDialog.size(); ++i )
	{
		m_vecDialog[i]->OnRender( m_fElapsedTime );
	}

	return S_OK;
}

HRESULT CX2ViewerUI::OnResetDevice()
{
	for( int i = 0; i < (int)m_vecDialog.size(); ++i )
	{
		m_vecDialog[i]->SetLocation( 0, 0 );
	}

	Init();

	return S_OK;
}

HRESULT CX2ViewerUI::OnLostDevice()
{
	return S_OK;
}

bool CX2ViewerUI::MsgProc( HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam )
{
	if( uMsg == WM_DROPFILES )
	{
		WCHAR PullFileName[MAX_PATH] = L"";
		
        int nFiles;

        nFiles = DragQueryFile((HDROP) wParam, 0xFFFFFFFF, PullFileName, MAX_PATH);

        for(int index = 0; index < nFiles; ++index)
        {
            WCHAR FileName[256] = L"";
            WCHAR PullDir[MAX_PATH] = L"";
            WCHAR drive[_MAX_DRIVE] = L"";
            WCHAR dir[_MAX_DIR] = L"";
            WCHAR fname[_MAX_FNAME] = L"";
            WCHAR ext[_MAX_EXT] = L"";

            DragQueryFile((HDROP) wParam, index, PullFileName, MAX_PATH);
            _wsplitpath( PullFileName, drive, dir, fname, ext);

            wcscat( FileName, fname);
            wcscat( FileName, ext);

            wcscat( PullDir, drive );
            wcscat( PullDir, dir );

            DropFile( FileName, PullDir );
        }
		
	}

	for( int i = 0; i < (int)m_vecDialog.size(); ++i )
	{
		if( m_vecDialog[i]->MsgProc( hWnd, uMsg, wParam, lParam ) == true )
			return true;
	}

	return false;
}

void CALLBACK CX2ViewerUI::OnGUIEvent( UINT nEvent, int nControlID, CDXUTControl* pControl, void* pUserContext )
{
	switch( nControlID )
	{
	case UI_CHECK_GRID:
		{
			bool bIsChecked = ((CDXUTCheckBox*)SiGetBaseDlg( UI_CHECK_GRID ))->GetChecked();
			CX2ViewerGrid*	pGrid = (CX2ViewerGrid*)SiGetObject( OS_GRID );
			pGrid->SetOnOff( bIsChecked );
		}
		return;

	case UI_CHECK_WIREFRAME:
		{
			bool bIsChecked = ((CDXUTCheckBox*)SiGetBaseDlg( UI_CHECK_WIREFRAME ))->GetChecked();
			CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
			pSkinMesh->SetWireFrameMode( bIsChecked );
		}
		return;

	case UI_BUT_RESET:
		{
			CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
			pSkinMesh->Reset();

			CX2ViewerMesh*	pMesh = (CX2ViewerMesh*)SiGetObject( OS_MESH );
			pMesh->Reset();

			SiSelf()->m_Param.Reset();

			SiSelf()->Init();
			SiSelf()->SetUnitOnOff( false );
			SiSelf()->SetMeshOnOff( false );
			SiSelf()->SetEffectButOnOff( false );
		}
		return;

	case UI_BUT_UI_INIT:
		{
			SiSelf()->Init();
		}
		return;

	case UI_RADIO_CAMERA_NORMAL:
		{
			CX2ViewerCamera*	pCamera = (CX2ViewerCamera*)SiGetObject( OS_CAMERA );
			pCamera->SetCameraMode( CX2ViewerCamera::CM_NORMAL );
		}
		return;

	case UI_RADIO_CAMERA_NAVIGATION:
		{
			CX2ViewerCamera*	pCamera = (CX2ViewerCamera*)SiGetObject( OS_CAMERA );
			pCamera->SetCameraMode( CX2ViewerCamera::CM_NAVIGATION );
		}
		return;

	case UI_BUT_CAMERA_RESET:
		{
			CX2ViewerCamera*	pCamera = (CX2ViewerCamera*)SiGetObject( OS_CAMERA );
			pCamera->CameraReset();
		}
		return;

	case UI_EDIT_BG_A:
	case UI_EDIT_BG_R:
	case UI_EDIT_BG_G:
	case UI_EDIT_BG_B:
		{
			if( nEvent == EVENT_EDITBOX_STRING )
			{
				CDXUTEditBox*	pEditBox = NULL;
				pEditBox = (CDXUTEditBox*)SiGetBaseDlg( UI_EDIT_BG_A );
				g_pKTDXApp->GetDGManager()->SetClearColorA( _wtoi( pEditBox->GetText() ) );
				pEditBox = (CDXUTEditBox*)SiGetBaseDlg( UI_EDIT_BG_R );
				g_pKTDXApp->GetDGManager()->SetClearColorR( _wtoi( pEditBox->GetText() ) );
				pEditBox = (CDXUTEditBox*)SiGetBaseDlg( UI_EDIT_BG_G );
				g_pKTDXApp->GetDGManager()->SetClearColorG( _wtoi( pEditBox->GetText() ) );
				pEditBox = (CDXUTEditBox*)SiGetBaseDlg( UI_EDIT_BG_B );
				g_pKTDXApp->GetDGManager()->SetClearColorB( _wtoi( pEditBox->GetText() ) );
			}
		}
		return;

	case UI_BUT_WORLD_MESH:
		{
			CX2ViewerWorldMesh* pWorld = (CX2ViewerWorldMesh*)SiGetObject( OS_WORLD_MESH );
			if( SiSelf()->m_FileOS.FileOpen( L"X-file(*.x)\0*.x\0" ) == CX2ViewerFileOS::FS_XFILE )
			{
				pWorld->SetMesh( SiSelf()->m_FileOS.GetPullFileName().c_str() );
			}
		}
		return;


	case UI_BUT_WORLD_MESH_RESET:
		{
			CX2ViewerWorldMesh* pWorld = (CX2ViewerWorldMesh*)SiGetObject( OS_WORLD_MESH );
			pWorld->Reset();
		}
		return;

	case UI_EDIT_WORLD_MESH_X:
	case UI_EDIT_WORLD_MESH_Y:
	case UI_EDIT_WORLD_MESH_Z:
		{
			if( nEvent == EVENT_EDITBOX_STRING )
			{
				CX2ViewerWorldMesh* pWorld = (CX2ViewerWorldMesh*)SiGetObject( OS_WORLD_MESH );

				D3DXVECTOR3 vec3;
				CDXUTEditBox* pEditBox = (CDXUTEditBox*)SiGetBaseDlg( UI_EDIT_WORLD_MESH_X );
				vec3.x = (float)_wtof( pEditBox->GetText() );
				pEditBox = (CDXUTEditBox*)SiGetBaseDlg( UI_EDIT_WORLD_MESH_Y );
				vec3.y = (float)_wtof( pEditBox->GetText() );
				pEditBox = (CDXUTEditBox*)SiGetBaseDlg( UI_EDIT_WORLD_MESH_Z );
				vec3.z = (float)_wtof( pEditBox->GetText() );
				pWorld->GetMatrix()->Move( vec3 );
			}
		}
		return;

	case UI_BUT_EFFECT_SET:
		{
			SiSelf()->m_Param.SetEffect();

			switch( SiSelf()->m_MeshSel )
			{
			case MS_MESH:
				{
					CX2ViewerMesh*	pMesh = (CX2ViewerMesh*)SiGetObject( OS_MESH );
					SiSelf()->m_Param.GetRenderParam( pMesh->GetRenderParam(), pMesh->GetImpactData(), *(pMesh->GetTexStageData()) );
				}
				break;

			case MS_SKIN_MESH:
				{
					CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
					SiSelf()->m_Param.GetRenderParam( pSkinMesh->GetRenderParam(), pSkinMesh->GetImpactData(), *(pSkinMesh->GetTexStageData()) );
				}
				break;
			}

			SiSelf()->m_Param.SetParamDlg( &(SiSelf()->m_RenderParam) );
		}
		break;
	}
}

void CALLBACK CX2ViewerUI::OnGUIUnitEvent( UINT nEvent, int nControlID, CDXUTControl* pControl, void* pUserContext )
{
	switch( nControlID )
	{
	case UI_EDIT_SCALE_X:
	case UI_EDIT_SCALE_Y:
	case UI_EDIT_SCALE_Z:
		{
			if( nEvent == EVENT_EDITBOX_STRING )
			{
				CDXUTEditBox*		pEditBox = NULL;
				CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
				pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_SCALE_X );
				pSkinMesh->SetScaleX( (float)_wtof( pEditBox->GetText() ) );
				pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_SCALE_Y );
				pSkinMesh->SetScaleY( (float)_wtof( pEditBox->GetText() ) );
				pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_SCALE_Z );
				pSkinMesh->SetScaleZ( (float)_wtof( pEditBox->GetText() ) );
			}
		}
		return;

	case UI_EDIT_LIGHT_POS_X:
	case UI_EDIT_LIGHT_POS_Y:
	case UI_EDIT_LIGHT_POS_Z:
		{
			CDXUTEditBox*		pEditBox = NULL;
			CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
			pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_LIGHT_POS_X );
			pSkinMesh->SetLightPosX( (float)_wtof( pEditBox->GetText() ) );
			pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_LIGHT_POS_Y );
			pSkinMesh->SetLightPosY( (float)_wtof( pEditBox->GetText() ) );
			pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_LIGHT_POS_Z );
			pSkinMesh->SetLightPosZ( (float)_wtof( pEditBox->GetText() ) );
		}
		return;

	case UI_CHECK_LIGHT_ONOFF:
		{
			CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );

			bool bIsChecked = ((CDXUTCheckBox*)SiGetUnitDlg( UI_CHECK_LIGHT_ONOFF ))->GetChecked();
			pSkinMesh->SetLightOnOff( bIsChecked );
		}
		return;

	case UI_LIST_OBJECT:
		{
			if( nEvent == EVENT_LISTBOX_ITEM_DBLCLK )
			{
				CDXUTListBox* pListBox = (CDXUTListBox*)SiGetUnitDlg( UI_LIST_OBJECT );
				int nIndex = pListBox->GetSelectedIndex( -1 );

				CX2ViewerSkinMesh* pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
				if( pSkinMesh->DelModelXSkinMesh( pListBox->GetItem( nIndex )->strText ) == true )
				{
					pListBox->RemoveItem( nIndex );
				}
				else
				{
					WARNINGMSG( L"Object 삭제실패..?" );
				}
			}
		}
		return;

	case UI_LIST_ANIMATION:
		{
			if( nEvent == EVENT_LISTBOX_SELECTION )
			{
				CDXUTListBox* pListBox = (CDXUTListBox*)SiGetUnitDlg( UI_LIST_ANIMATION );
				int nIndex = pListBox->GetSelectedIndex( -1 );

				CX2ViewerSkinMesh* pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
				pSkinMesh->ChangeAnim( pListBox->GetItem( nIndex )->strText );

				CDXUTEditBox*		pEditBox = NULL;
				pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_ANIM_NAME );
				pEditBox->SetText(  pListBox->GetItem( nIndex )->strText );

				SiMain()->SetSelectedAnimIndex( nIndex );
			}
		}
		return;

    case UI_LIST_BONE:
        {
            if( nEvent == EVENT_LISTBOX_SELECTION )
            {
                CDXUTListBox* pListBox = (CDXUTListBox*)SiGetUnitDlg( UI_LIST_BONE );
                int nIndex = pListBox->GetSelectedIndex( -1 );

                for(int i=0; i < static_cast<int>(SiSelf()->m_vecFrameNameList.size()); ++i)
                {
                    CKTDXDeviceXSkinMesh::MultiAnimFrame *pMA = SiSelf()->GetFrameList(i);
                    pMA->m_bSelected = false;
                }

                CKTDXDeviceXSkinMesh::MultiAnimFrame *pMA = SiSelf()->GetFrameList(nIndex); 
                pMA->m_bSelected = true;


                LPCSTR			szName;
                std::wstring	wstrName;
                WCHAR			wszName[128] = L"";
                    
                szName = pMA->Name;
                MultiByteToWideChar( CP_ACP, 0, szName, -1, wszName, MAX_PATH);
                wstrName = wszName;

                CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
                pSkinMesh->SetAttachPoint(wszName);
                
            }
        }
        return;
        
	case UI_BUT_PLAY_ONOFF:
		{
			CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
			CDXUTButton* pButton = (CDXUTButton*)SiGetUnitDlg( UI_BUT_PLAY_ONOFF );
			if( pSkinMesh->SetPlayOnOff() == true )
				pButton->SetText( L"stop ■" );
			else
				pButton->SetText( L"play ▶" );
		}
		return;

	case UI_SLIDE_ANIM_SPEED:
		{
			CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
			CDXUTSlider* pSlider = (CDXUTSlider*)SiGetUnitDlg( UI_SLIDE_ANIM_SPEED );
			int nValue = pSlider->GetValue();

			pSkinMesh->SetPlaySpeed( nValue/100.0f );

			//Static 문구 출력
			CDXUTStatic* pStatic = (CDXUTStatic*)SiGetUnitDlg( UI_STATIC_ANIM_SPEED );
			WCHAR wszSpeed[128] = L"";
			swprintf( wszSpeed, L"speed : %d", nValue );
			pStatic->SetText( wszSpeed );
		}
		return;

	case UI_CHECK_MOTION_ONOFF:
		{
			CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
			CDXUTCheckBox* pCheckbox = (CDXUTCheckBox*)SiGetUnitDlg( UI_CHECK_MOTION_ONOFF );

			pSkinMesh->SetMotionOnOff( pCheckbox->GetChecked() );
		}
		return;

	case UI_BUT_RENDER_PARAM:
		{
			SiSelf()->SetRenderParamOnOff( !(SiSelf()->m_RenderParam.GetVisible()) );
		}
		return;

	case UI_COMBO_PLAY_TYPE:
		{
			if( nEvent == EVENT_COMBOBOX_SELECTION_CHANGED )
			{
				CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
				CDXUTComboBox* pComboBox = (CDXUTComboBox*)SiGetUnitDlg( UI_COMBO_PLAY_TYPE );
				DXUTComboBoxItem* pItem = pComboBox->GetSelectedItem();
				pSkinMesh->SetPlayType( pItem->strText );
			}
		}
		return;

	case UI_BUT_ATTACH_WEAPON:
		{
			if( SiSelf()->m_FileOS.FileOpen( L"X-file(*.x)\0*.x\0" ) == CX2ViewerFileOS::FS_XFILE )
			{
				CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );

				if ( pSkinMesh != NULL )
				{

					CDXUTEditBox* pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_ATTACH_WEAPON_BONE_NAME );
					wstring attachFrameName = pEditBox->GetText();

					

					D3DXVECTOR3 addRotWeapon;

					pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_WEAPON_ROT_X );
					wstring rotValue = pEditBox->GetText();
					float fRotValue = 0;

					if ( rotValue.compare(L"") == 0 )
					{
						addRotWeapon.x = 0;
					}
					else
					{
						fRotValue = (float)_wtof( rotValue.c_str() );
						addRotWeapon.x = D3DXToRadian(fRotValue);
					}
					

					pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_WEAPON_ROT_Y );
					rotValue = pEditBox->GetText();
					fRotValue = 0;

					if ( rotValue.compare(L"") == 0 )
					{
						addRotWeapon.y = 0;
					}
					else
					{
						fRotValue = (float)_wtof( rotValue.c_str() );
						addRotWeapon.y = D3DXToRadian(fRotValue);
					}


					pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_WEAPON_ROT_Z );
					rotValue = pEditBox->GetText();
					fRotValue = 0;

					if ( rotValue.compare(L"") == 0 )
					{
						addRotWeapon.z = 0;
					}
					else
					{
						fRotValue = (float)_wtof( rotValue.c_str() );
						addRotWeapon.z = D3DXToRadian(fRotValue);
					}


					pSkinMesh->AddWeapon( SiSelf()->m_FileOS.GetPullFileName().c_str(), attachFrameName.c_str(), addRotWeapon );
				}
			}
		}
		return;

    case UI_BUT_ATTACH_ACCESSORY :
        {
            if( SiSelf()->m_FileOS.FileOpen( L"Y-file(*.y)\0*.y\0X-file(*.x)\0*.x" ) == CX2ViewerFileOS::FS_XFILE )
            {
                CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );

                if ( pSkinMesh != NULL )
                {

                    CDXUTEditBox* pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_ATTACH_ACCESSORY_BONE_NAME );
                    wstring attachFrameName = pEditBox->GetText();



                    D3DXVECTOR3 addTransAccessory = D3DXVECTOR3(0.0f, 0.0f, 0.0f);   

                    pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_ACCESSORY_TRANS_X );
                    wstring transValue = pEditBox->GetText();
                    float fTransValue = 0;

                    if ( transValue.compare(L"") == 0 )
                    {
                        addTransAccessory.x = 0;
                    }
                    else
                    {
                        fTransValue = (float)_wtof( transValue.c_str() );
                        addTransAccessory.x = fTransValue;
                    }


                    pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_ACCESSORY_TRANS_Y );
                    transValue = pEditBox->GetText();
                    fTransValue = 0;

                    if ( transValue.compare(L"") == 0 )
                    {
                        addTransAccessory.y = 0;
                    }
                    else
                    {
                        fTransValue = (float)_wtof( transValue.c_str() );
                        addTransAccessory.y = fTransValue;
                    }


                    pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_ACCESSORY_TRANS_Z );
                    transValue = pEditBox->GetText();
                    fTransValue = 0;

                    if ( transValue.compare(L"") == 0 )
                    {
                        addTransAccessory.z = 0;
                    }
                    else
                    {
                        fTransValue = (float)_wtof( transValue.c_str() );
                        addTransAccessory.z = fTransValue;
                    }

                    pSkinMesh->AddAccessory( SiSelf()->m_FileOS.GetPullFileName().c_str(), attachFrameName.c_str(), addTransAccessory );

                    SiSelf()->SetRenderParamOnOff( false );

                    CX2ViewerMesh*	pMesh = (CX2ViewerMesh*)SiGetObject( OS_MESH );
                    SiSelf()->m_Param.GetRenderParam( pMesh->GetRenderParam(), pMesh->GetImpactData(), *(pMesh->GetTexStageData()) );
                    
                }
            }
        }
        return;


	case UI_BUT_ANIM_NAME_CHANGE:
		{
			if( SiMain()->GetSelectedAnimIndex() == -1 )
				return;

			//MessageBox( g_pKTDXApp->GetHWND(), L"훈형을 갈궈요..ㅎ", L"미구현", MB_OK );

			int animIndex = SiMain()->GetSelectedAnimIndex();

			CX2ViewerSkinMesh* pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );

			LPD3DXANIMATIONSET pAnimSet = pSkinMesh->GetXSkinAnim()->GetAnimSet( animIndex );
			LPSTR pAnimSetName = (LPSTR)pAnimSet->GetName();
			
			CDXUTEditBox*		pEditBox = NULL;
			pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_ANIM_NAME );

			string changeAnimName;
			ConvertWCHARToChar( changeAnimName, pEditBox->GetText() );
			sprintf( pAnimSetName, changeAnimName.c_str() );

			
			HRESULT			hr = S_OK;
			LPD3DXMESH		pMeshMerged = NULL;
			LPD3DXBUFFER	pbufAdjacencyMerged = NULL;
			LPD3DXBUFFER	pbufMaterialsMerged = NULL;

			BOOL bSaveHierarchy = TRUE;
			DWORD xFormat = D3DXF_FILEFORMAT_COMPRESSED;

			//OPENFILENAME m_OFN;

			WCHAR animFileName[255] = {0,};

			wcscpy(animFileName, SiMain()->GetAnimFileName().c_str());

			WCHAR animDirName[255] = {0};
			wcscpy(animDirName, SiMain()->GetAnimDirName().c_str());


			WCHAR FullPath[255] = {0};
			WCHAR* pFullPath = NULL;
			
			pFullPath = wcscat( animDirName, animFileName );

			wcscpy( FullPath, pFullPath );

			CKTDXDeviceXSkinMesh* pMotion = NULL;
			pMotion = g_pKTDXApp->GetDeviceManager()->OpenXSkinMesh( SiMain()->GetAnimFileName() );

			if ( pMotion == NULL )
			{
				MessageBox(g_pKTDXApp->GetHWND(), L"모션 XSkinMesh 로드 에러", L"에러", MB_OK );
				return;
			}


			LPD3DXANIMATIONCONTROLLER pAC = pMotion->GetCloneAC();

			if( strcmp( pMotion->GetFrameRoot()->Name, "<no_name>" ) == 0 )
			{
				hr = D3DXSaveMeshHierarchyToFile(FullPath,
					xFormat,
					(LPD3DXFRAME) pMotion->GetFrameRoot()->pFrameFirstChild,
					pAC,
					NULL);

				SAFE_RELEASE( pAC );

				if (FAILED(hr))
				{
					MessageBox( NULL, L"에러", L"세이브 확인", MB_OK);
					return;
				}
			}
			else
			{
				hr = D3DXSaveMeshHierarchyToFile(FullPath,
					xFormat,
					(LPD3DXFRAME) pMotion->GetFrameRoot(),
					pAC,
					NULL);

				SAFE_RELEASE( pAC );

				if (FAILED(hr))
				{
					MessageBox( NULL, L"에러", L"세이브 확인", MB_OK);
					return;
				}
			}

			DropAnimationFile( SiSelf(), animFileName, animDirName );

			pSkinMesh->ChangeAnim( animIndex );
		}
		return;


	case UI_CHECK_BOUNDING:
		{
			CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
			CDXUTCheckBox* pCheckbox = (CDXUTCheckBox*)SiGetUnitDlg( UI_CHECK_BOUNDING );

			pSkinMesh->SetBounding( pCheckbox->GetChecked() );
		}
		return;


	case UI_CHECK_ATTACK_BOX:
		{
			CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
			CDXUTCheckBox* pCheckbox = (CDXUTCheckBox*)SiGetUnitDlg( UI_CHECK_ATTACK_BOX );

			pSkinMesh->SetShowAttackBox( pCheckbox->GetChecked() );
		}
		return;

	case UI_EDIT_FRAME_TIME_INC:
		{
			switch( nEvent )
			{
			case EVENT_EDITBOX_STRING:
				{
					CDXUTEditBox*	pEditBox = NULL;
					pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_FRAME_TIME_INC );

					float fValue = (float) _wtof( pEditBox->GetText() );
					if( fValue <= 0.f )
					{
						fValue= SiSelf()->GetAnimTimeInc();
					}

					pEditBox->SetTextFloatArray( &fValue, 1 );
					SiSelf()->SetAnimTimeInc( fValue );

				} break;
			}

		}
		return;

	case UI_BUT_PREV_FRAME:
		{

			CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
			if( NULL != pSkinMesh )
			{
				float fNowTime, fMaxTime;
				pSkinMesh->GetAnimTime( fNowTime, fMaxTime );
				fNowTime = fNowTime - SiSelf()->GetAnimTimeInc();
				if( fNowTime < 0.f )
					fNowTime = 0.f;
				else if( fNowTime > fMaxTime )
					fNowTime = fMaxTime;

				pSkinMesh->SetAnimTime( fNowTime );
			}
		}
		return;

	case UI_BUT_NEXT_FRAME:
		{

			CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
			if( NULL != pSkinMesh )
			{
				float fNowTime, fMaxTime;
				pSkinMesh->GetAnimTime( fNowTime, fMaxTime );
				fNowTime = fNowTime + SiSelf()->GetAnimTimeInc();
				if( fNowTime < 0.f )
					fNowTime = 0.f;
				else if( fNowTime > fMaxTime )
					fNowTime = fMaxTime;

				pSkinMesh->SetAnimTime( fNowTime );
			}


		}
		return;

    case UI_EDIT_ACCESSORY_TRANS_X:        
    case UI_EDIT_ACCESSORY_TRANS_Y:        
    case UI_EDIT_ACCESSORY_TRANS_Z:
        {
            if( nEvent == EVENT_EDITBOX_STRING )
            {
                CDXUTEditBox*		pEditBox = NULL;
                CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
                pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_ACCESSORY_TRANS_X );
                pSkinMesh->SetTransX( (float)_wtof( pEditBox->GetText() ) );
                pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_ACCESSORY_TRANS_Y );
                pSkinMesh->SetTransY( (float)_wtof( pEditBox->GetText() ) );
                pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_ACCESSORY_TRANS_Z );
                pSkinMesh->SetTransZ( (float)_wtof( pEditBox->GetText() ) );
            }
        }
        return;

    case UI_EDIT_WEAPON_ROT_X:        
    case UI_EDIT_WEAPON_ROT_Y:        
    case UI_EDIT_WEAPON_ROT_Z:
        {
            if( nEvent == EVENT_EDITBOX_STRING )
            {
                CDXUTEditBox*		pEditBox = NULL;
                CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
                pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_WEAPON_ROT_X );
                pSkinMesh->SetRotX( (float)_wtof( pEditBox->GetText() ) );
                pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_WEAPON_ROT_Y );
                pSkinMesh->SetRotY( (float)_wtof( pEditBox->GetText() ) );
                pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_WEAPON_ROT_Z );
                pSkinMesh->SetRotZ( (float)_wtof( pEditBox->GetText() ) );
            }
        }
        return;

    case UI_EDIT_ACCESSORY_ROTATE_X:        
    case UI_EDIT_ACCESSORY_ROTATE_Y:        
    case UI_EDIT_ACCESSORY_ROTATE_Z:
        {
            if( nEvent == EVENT_EDITBOX_STRING )
            {
                CDXUTEditBox*		pEditBox = NULL;
                CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
                pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_ACCESSORY_ROTATE_X );
                pSkinMesh->SetAccRotX( (float)_wtof( pEditBox->GetText() ) );
                pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_ACCESSORY_ROTATE_Y );
                pSkinMesh->SetAccRotY( (float)_wtof( pEditBox->GetText() ) );
                pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_ACCESSORY_ROTATE_Z );
                pSkinMesh->SetAccRotZ( (float)_wtof( pEditBox->GetText() ) );
            }
        }
        return;

    case UI_EDIT_ACCESSORY_SCALE_X:        
    case UI_EDIT_ACCESSORY_SCALE_Y:        
    case UI_EDIT_ACCESSORY_SCALE_Z:
        {
            if( nEvent == EVENT_EDITBOX_STRING )
            {
                CDXUTEditBox*		pEditBox = NULL;
                CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
                pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_ACCESSORY_SCALE_X );
                pSkinMesh->SetAccScaleX( (float)_wtof( pEditBox->GetText() ) );
                pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_ACCESSORY_SCALE_Y );
                pSkinMesh->SetAccScaleY( (float)_wtof( pEditBox->GetText() ) );
                pEditBox = (CDXUTEditBox*)SiGetUnitDlg( UI_EDIT_ACCESSORY_SCALE_Z );
                pSkinMesh->SetAccScaleZ( (float)_wtof( pEditBox->GetText() ) );
            }
        }
        return;
	}
}

void CALLBACK CX2ViewerUI::OnGUIMeshEvent( UINT nEvent, int nControlID, CDXUTControl* pControl, void* pUserContext )
{
	switch( nControlID )
	{
	case UI_EDIT_MESH_SCALE_X:
	case UI_EDIT_MESH_SCALE_Y:
	case UI_EDIT_MESH_SCALE_Z:
		{
			if( nEvent == EVENT_EDITBOX_STRING )
			{
				CDXUTEditBox*	pEditBox = NULL;
				CX2ViewerMesh*	pMesh = (CX2ViewerMesh*)SiGetObject( OS_MESH );
				pEditBox = (CDXUTEditBox*)SiGetMeshDlg( UI_EDIT_MESH_SCALE_X );
				pMesh->SetScaleX( (float)_wtof( pEditBox->GetText() ) );
				pEditBox = (CDXUTEditBox*)SiGetMeshDlg( UI_EDIT_MESH_SCALE_Y );
				pMesh->SetScaleY( (float)_wtof( pEditBox->GetText() ) );
				pEditBox = (CDXUTEditBox*)SiGetMeshDlg( UI_EDIT_MESH_SCALE_Z );
				pMesh->SetScaleZ( (float)_wtof( pEditBox->GetText() ) );
			}
		}
		return;

	case UI_EDIT_MESH_LIGHT_POS_X:
	case UI_EDIT_MESH_LIGHT_POS_Y:
	case UI_EDIT_MESH_LIGHT_POS_Z:
		{
			CDXUTEditBox*	pEditBox = NULL;
			CX2ViewerMesh*	pMesh = (CX2ViewerMesh*)SiGetObject( OS_MESH );
			pEditBox = (CDXUTEditBox*)SiGetMeshDlg( UI_EDIT_MESH_LIGHT_POS_X );
			pMesh->SetLightPosX( (float)_wtof( pEditBox->GetText() ) );
			pEditBox = (CDXUTEditBox*)SiGetMeshDlg( UI_EDIT_MESH_LIGHT_POS_Y );
			pMesh->SetLightPosY( (float)_wtof( pEditBox->GetText() ) );
			pEditBox = (CDXUTEditBox*)SiGetMeshDlg( UI_EDIT_MESH_LIGHT_POS_Z );
			pMesh->SetLightPosZ( (float)_wtof( pEditBox->GetText() ) );
		}
		return;

	case UI_CHECK_MESH_LIGHT_ONOFF:
		{
			CX2ViewerMesh*	pMesh = (CX2ViewerMesh*)SiGetObject( OS_MESH );

			bool bIsChecked = ((CDXUTCheckBox*)SiGetMeshDlg( UI_CHECK_MESH_LIGHT_ONOFF ))->GetChecked();
			pMesh->SetLightOnOff( bIsChecked );
		}
		return;

	case UI_BUT_MESH_RENDER_PARAM:
		{
			SiSelf()->SetRenderParamOnOff( !(SiSelf()->m_RenderParam.GetVisible()) );
		}
		return;
	}
}

void CALLBACK CX2ViewerUI::OnGUIRPEvent( UINT nEvent, int nControlID, CDXUTControl* pControl, void* pUserContext )
{
	switch( nControlID )
	{
	case UI_BUT_PARAM_OK:
		{
			SiSelf()->m_Param.GetParamDlg( &(SiSelf()->m_RenderParam) );

			switch( SiSelf()->m_MeshSel )
			{
			case MS_MESH:
				{
					CX2ViewerMesh*	pMesh = (CX2ViewerMesh*)SiGetObject( OS_MESH );
					SiSelf()->m_Param.GetRenderParam( pMesh->GetRenderParam(), pMesh->GetImpactData(), *(pMesh->GetTexStageData()) );
				}
				break;

			case MS_SKIN_MESH:
				{
					CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
					SiSelf()->m_Param.GetRenderParam( pSkinMesh->GetRenderParam(), pSkinMesh->GetImpactData(), *(pSkinMesh->GetTexStageData()) );
				}
				break;
			}

			SiSelf()->m_Param.SetParamDlg( &(SiSelf()->m_RenderParam) );

			//SiSelf()->SetRenderParamOnOff();
		}
		return;

	case UI_BUT_PARAM_CANCEL:
		{
			SiSelf()->m_Param.SetParamDlg( &(SiSelf()->m_RenderParam) );
			SiSelf()->SetRenderParamOnOff( false );
		}
		return;
	}
}

// 모델뷰 모드에서 표시되는 파티클 관련 UI들
void CALLBACK CX2ViewerUI::OnGUIParticleEvent( UINT nEvent, int nControlID, CDXUTControl* pControl, void* pUserContext )
{
	CDXUTListBox* pListBox = (CDXUTListBox*)SiGetParticleDlg(UI_LIST_PARTICLE_LIST);

	wstring Name;
	
	if( pListBox->GetSelectedItem() != NULL )
		Name = pListBox->GetSelectedItem()->strText;
	int SelectedIndex = pListBox->GetSelectedIndex();
	CX2ViewerParticle* pParticleObj = (CX2ViewerParticle*)(SiGetObject( OS_PARTICLE ));

	switch( nControlID )
	{
	case UI_LIST_PARTICLE_LIST:
		{
			if( nEvent == EVENT_LISTBOX_SELECTION )
			{
				if( pParticleObj->GetParticleEffectDataByName(Name) != NULL )
				{
					CX2ViewerParticle::ParticleEffectData* pData = pParticleObj->GetParticleEffectDataByName(Name);
					CDXUTEditBox* pEditBox;
					CDXUTCheckBox* pCheckBox;

					WCHAR buf[256] = {0};
					// Particle Basic

					pEditBox = (CDXUTEditBox*)SiGetParticleDlg( UI_EDIT_PARTICLE_TIME );
					swprintf(buf, L"%.4f", pData->m_fTime );  
					pEditBox->SetText(buf);

					pCheckBox = (CDXUTCheckBox*)SiGetParticleDlg( UI_CHECK_PARTICLE_TRACE );
					pCheckBox->SetChecked( pData->m_bTrace );

					pEditBox = (CDXUTEditBox*)SiGetParticleDlg( UI_EDIT_PARTICLE_OFFSET_X );
					swprintf(buf, L"%.4f", pData->m_vOffset.x );  
					pEditBox->SetText(buf);

					pEditBox = (CDXUTEditBox*)SiGetParticleDlg( UI_EDIT_PARTICLE_OFFSET_Y );
					swprintf(buf, L"%.4f", pData->m_vOffset.y );  
					pEditBox->SetText(buf);

					pEditBox = (CDXUTEditBox*)SiGetParticleDlg( UI_EDIT_PARTICLE_OFFSET_Z );
					swprintf(buf, L"%.4f", pData->m_vOffset.z );  
					pEditBox->SetText(buf);

					pCheckBox = (CDXUTCheckBox*)SiGetParticleDlg( UI_CHECK_PARTICLE_LANDPOS );
					pCheckBox->SetChecked( pData->m_bLandPosition );

					pEditBox = (CDXUTEditBox*)SiGetParticleDlg( UI_EDIT_PARTICLE_ROT_X );
					swprintf(buf, L"%.4f", pData->m_vRotation.x );  
					pEditBox->SetText(buf);

					pEditBox = (CDXUTEditBox*)SiGetParticleDlg( UI_EDIT_PARTICLE_ROT_Y );
					swprintf(buf, L"%.4f", pData->m_vRotation.y );  
					pEditBox->SetText(buf);

					pEditBox = (CDXUTEditBox*)SiGetParticleDlg( UI_EDIT_PARTICLE_ROT_Z );
					swprintf(buf, L"%.4f", pData->m_vRotation.z );  
					pEditBox->SetText(buf);

					pCheckBox = (CDXUTCheckBox*)SiGetParticleDlg( UI_CHECK_PARTICLE_APPUNITROT ); 
					pCheckBox->SetChecked( pData->m_bApplyUnitRotation );

				}
			}

		} break;

	case UI_BUT_PARTICLE_DELETE:
		{
			if( true == pParticleObj->DeleteParticleEffectData( Name ) )
			{
				pListBox->RemoveItem( SelectedIndex );
				pListBox->SelectItem(-1);
			}
			else
			{
				WARNINGMSG( L"내부 오류!");
			}
			
		} break;

	case UI_EDIT_PARTICLE_TIME:
		{
			CDXUTEditBox* pEditBox = (CDXUTEditBox*) pControl;
			if( pParticleObj->GetParticleEffectDataByName(Name) != NULL )
				pParticleObj->GetParticleEffectDataByName(Name)->m_fTime = (float)_wtof( pEditBox->GetText() );
		} break;

	case UI_BUT_PARTICLE_BONESET:
		{
			CDXUTListBox* pListBox = (CDXUTListBox*)SiGetUnitDlg( UI_LIST_BONE );
			wstring BoneName;
			if( pListBox->GetSelectedItem() != NULL )
			{
				BoneName = pListBox->GetSelectedItem()->strText;
			}

			if( pParticleObj->GetParticleEffectDataByName(Name) != NULL )
				pParticleObj->GetParticleEffectDataByName(Name)->m_Pos = BoneName;
		} break;
	case UI_BUT_PARTICLE_BONECLEAR:
		{
			if( pParticleObj->GetParticleEffectDataByName(Name) != NULL )
				pParticleObj->GetParticleEffectDataByName(Name)->m_Pos = L"";
		} break;
	case UI_CHECK_PARTICLE_TRACE:
		{
			if( pParticleObj->GetParticleEffectDataByName(Name) != NULL )
				pParticleObj->GetParticleEffectDataByName(Name)->m_bTrace = ((CDXUTCheckBox*)pControl)->GetChecked();

		} break;

	case UI_EDIT_PARTICLE_OFFSET_X:
	case UI_EDIT_PARTICLE_OFFSET_Y:
	case UI_EDIT_PARTICLE_OFFSET_Z:
		{
			if( pParticleObj->GetParticleEffectDataByName(Name) != NULL )
			{
				D3DXVECTOR3 vOff(0,0,0);
				CDXUTEditBox*	pEditBox = NULL;
				pEditBox = (CDXUTEditBox*)SiGetParticleDlg( UI_EDIT_PARTICLE_OFFSET_X );
				vOff.x = (float)_wtof( pEditBox->GetText() );
				pEditBox = (CDXUTEditBox*)SiGetParticleDlg( UI_EDIT_PARTICLE_OFFSET_Y );
				vOff.y = (float)_wtof( pEditBox->GetText() );
				pEditBox = (CDXUTEditBox*)SiGetParticleDlg( UI_EDIT_PARTICLE_OFFSET_Z );
				vOff.z = (float)_wtof( pEditBox->GetText() );
				pParticleObj->GetParticleEffectDataByName(Name)->m_vOffset = vOff;

			}
			

		} break;
	case UI_CHECK_PARTICLE_LANDPOS:
		{
			if( pParticleObj->GetParticleEffectDataByName(Name) != NULL )
				pParticleObj->GetParticleEffectDataByName(Name)->m_bLandPosition = ((CDXUTCheckBox*)pControl)->GetChecked();

		} break;

	case UI_EDIT_PARTICLE_ROT_X:
	case UI_EDIT_PARTICLE_ROT_Y:
	case UI_EDIT_PARTICLE_ROT_Z:
		{
			D3DXVECTOR3 vRot(0,0,0);
			CDXUTEditBox*	pEditBox = NULL;
			pEditBox = (CDXUTEditBox*)SiGetParticleDlg( UI_EDIT_PARTICLE_ROT_X );
			vRot.x = (float)_wtof( pEditBox->GetText() );
			pEditBox = (CDXUTEditBox*)SiGetParticleDlg( UI_EDIT_PARTICLE_ROT_Y );
			vRot.y = (float)_wtof( pEditBox->GetText() );
			pEditBox = (CDXUTEditBox*)SiGetParticleDlg( UI_EDIT_PARTICLE_ROT_Z );
			vRot.z = (float)_wtof( pEditBox->GetText() );
			if( pParticleObj->GetParticleEffectDataByName(Name) != NULL )
				pParticleObj->GetParticleEffectDataByName(Name)->m_vRotation = vRot;

		} break;
	case UI_CHECK_PARTICLE_APPUNITROT:
		{
			if( pParticleObj->GetParticleEffectDataByName(Name) != NULL )
				pParticleObj->GetParticleEffectDataByName(Name)->m_bApplyUnitRotation = ((CDXUTCheckBox*)pControl)->GetChecked();
		} break;
	case UI_BUT_PARTICLE_SAVESEQUENCE:
		{
			OPENFILENAME ofn;        // common dialog box structure
			WCHAR wszFileName[512];  // path까지 포함한 파일 이름

			ZeroMemory(&ofn, sizeof(ofn));
			ofn.lStructSize		= sizeof(ofn);
			ofn.hwndOwner		= g_pKTDXApp->GetHWND(); 
			ofn.lpstrFile		= (LPWSTR)wszFileName;
			ofn.lpstrFile[0]	= '\0';
			ofn.nMaxFile		= sizeof(wszFileName);
			ofn.lpstrFilter		= L"lua script\0*.lua\0";
			ofn.nFilterIndex	= 1;
			ofn.lpstrFileTitle	= NULL;
			ofn.nMaxFileTitle	= 0;
			ofn.lpstrInitialDir = NULL;
			ofn.Flags			= OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST;

			if( TRUE == GetSaveFileName( &ofn ) )
			{
				pParticleObj->SaveParticleEffectData( ofn.lpstrFile );				
			}

		} break;
	case UI_BUT_PARTICLE_PARTICLEEDITOR:
		{
			// 모델 안보이게 가려주고..
			CX2ViewerSkinMesh*	pSkinMesh	= (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
			pParticleObj->SetMode( CX2ViewerParticle::PM_EDITOR );

			if(pSkinMesh != NULL)
			{
				if( pSkinMesh->GetXSkinAnim() != NULL )
					pSkinMesh->GetXSkinAnim()->SetShowObject(false);

				if( NULL != pSkinMesh->GetWeaponXSkinAnim() )
					pSkinMesh->GetWeaponXSkinAnim()->SetShowObject(false);
			}
						
			// Toggle 했을 때 기존의 On/Off를 유지하기 위해서 아예 벡터에서 빼는 식으로 제작.
			// 이후 UI_BUT_PARTICLE_EDITOR_EXIT 입력이 들어왔을 때 벡터를 다시 원상복구 시킨다.
			SiSelf()->m_vecDialog.clear();
			SiSelf()->m_vecDialog.push_back( &(SiSelf()->m_ParticleEditor) );

			SiSelf()->SetParticleEditButOnOff( true );

			//////////////////////////////////////////////////////////////////////////
			// 에디터 초기화
			SiSelf()->InitParticleEditor();
		} break;
	}

	return;
}

// 파티클 수정 모드
void CALLBACK CX2ViewerUI::OnGUIParticleEditorEvent( UINT nEvent, int nControlID, CDXUTControl* pControl, void* pUserContext )
{
	CX2ViewerParticle* pParticleObj = (CX2ViewerParticle*)(SiGetObject( OS_PARTICLE ));
	CX2ViewerParticleEditor& refParticleEditor = pParticleObj->GetParticleEditor();
	CDXUTListBox* pTempletListBox = (CDXUTListBox*) SiGetParticleEditorDlg(UI_LIST_PARTICLE_EDITOR_PARTICLETEMPLET);
	CDXUTListBox* pCustomListBox = (CDXUTListBox*) SiGetParticleEditorDlg(UI_LIST_PARTICLE_EDITOR_MYPARTICLE);
	CDXUTListBox* pEmitterPropertiesListBox = (CDXUTListBox*) SiGetParticleEditorDlg(UI_LIST_PARTICLE_EDITOR_EMITTERATTRIBUTE);
	
	CDXUTListBox* pParticleEventListBox = (CDXUTListBox*) SiGetParticleEditorDlg(UI_LIST_PARTICLE_EDITOR_EVENT);
	CDXUTListBox* pEventTempletBox = (CDXUTListBox*) SiGetParticleEditorDlg(UI_LIST_PARTICLE_EDITOR_EVENT_TYPE);
	
	CDXUTListBox* pModelViewListBox = (CDXUTListBox*) SiGetParticleEditorDlg(UI_LIST_PARTICLE_EDITOR_MODEL_LIST);

	switch(nControlID)
	{
	case UI_LIST_PARTICLE_EDITOR_MYPARTICLE:		// 수정중인 파티클 목록 선택시
		{
			if( nEvent == EVENT_LISTBOX_SELECTION )
			{
				if( pCustomListBox->GetSelectedItem() == NULL )
					break;

				pTempletListBox->m_nSelected = -1;
				pEventTempletBox->m_nSelected = -1;
				pParticleEventListBox->m_nSelected = -1;
				pModelViewListBox->m_nSelected = -1;
				wstring name(pCustomListBox->GetSelectedItem()->strText);

				if( pParticleObj != NULL)
				{
					pParticleObj->SetPreviewParticle( name, false );
				}

				SiSelf()->RefreshEventList();

			}

		} break;
	case UI_BUT_PARTICLE_EDITOR_MYPARTICLE_LOAD:
		{
			OPENFILENAME ofn;        // common dialog box structure
			WCHAR wszFileName[512];  // path까지 포함한 파일 이름

			ZeroMemory(&ofn, sizeof(ofn));
			ofn.lStructSize		= sizeof(ofn);
			ofn.hwndOwner		= g_pKTDXApp->GetHWND(); 
			ofn.lpstrFile		= (LPWSTR)wszFileName;
			ofn.lpstrFile[0]	= '\0';
			ofn.nMaxFile		= sizeof(wszFileName);
			ofn.lpstrFilter		= L"text script\0*.txt\0";
			ofn.nFilterIndex	= 1;
			ofn.lpstrFileTitle	= NULL;
			ofn.nMaxFileTitle	= 0;
			ofn.lpstrInitialDir = NULL;
			ofn.Flags			= OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST;

			if( TRUE == GetOpenFileName( &ofn ) )
			{				
				// file path와 name을 분리
				WCHAR drive[_MAX_DRIVE];
				WCHAR dir[_MAX_DIR];
				WCHAR fname[_MAX_FNAME];
				WCHAR ext[_MAX_EXT];
				_wsplitpath( ofn.lpstrFile, drive, dir, fname, ext);

				wstring strFileName(fname);
				strFileName += ext;				

				pParticleObj->GetCustomParticleSystem()->OpenScriptFile( strFileName.c_str() );
			}
			// list update
			{
				pCustomListBox->RemoveAllItems();
				const map<wstring, CKTDGParticleSystem::CParticleEventSequence*>& templetSeq = pParticleObj->GetCustomParticleSystem()->GetTempletSequences();
				map<wstring, CKTDGParticleSystem::CParticleEventSequence*>::const_iterator it;
				for( it=templetSeq.begin(); it != templetSeq.end(); it++ )
				{
					wstring& wstrName = (wstring)it->first;
					pCustomListBox->AddItem( wstrName.c_str(), NULL );
				}
			}

		} break;
	case UI_BUT_PARTICLE_EDITOR_MYPARTICLE_DELETE:
		{
			if( pCustomListBox->GetSelectedItem() == NULL )
				break;

			wstring name = pCustomListBox->GetSelectedItem()->strText;
			pParticleObj->GetCustomParticleSystem()->DeleteTempletSequence( name );
			pCustomListBox->RemoveItem( pCustomListBox->GetSelectedIndex());

			if( pParticleObj != NULL)
			{
				pParticleObj->SetPreviewParticle( L"", true );
			}

		} break;
	case UI_BUT_PARTICLE_EDITOR_MYPARTICLE_SAVE:	// Script Export는 사실 물밑에서 자주 되고 있지만, 그걸 강제로 해주는 버튼. 임시 파일과 실제 저장하는 파일을 분리해야겠다..
		{
			OPENFILENAME ofn;        // common dialog box structure
			WCHAR wszFileName[512];  // path까지 포함한 파일 이름

			ZeroMemory(&ofn, sizeof(ofn));
			ofn.lStructSize		= sizeof(ofn);
			ofn.hwndOwner		= g_pKTDXApp->GetHWND(); 
			ofn.lpstrFile		= (LPWSTR)wszFileName;
			ofn.lpstrFile[0]	= '\0';
			ofn.nMaxFile		= sizeof(wszFileName);
			ofn.lpstrFilter		= L"text script\0*.txt\0";
			ofn.nFilterIndex	= 1;
			ofn.lpstrFileTitle	= NULL;
			ofn.nMaxFileTitle	= 0;
			ofn.lpstrInitialDir = NULL;
			ofn.Flags			= OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST;

			if( TRUE == GetSaveFileName( &ofn ) )
			{
				refParticleEditor.ExportParticleScript( pParticleObj->GetCustomParticleSystem(), ofn.lpstrFile );
			}
			refParticleEditor.ExportParticleScript( pParticleObj->GetCustomParticleSystem(), L"ParticleTemp.txt" );
						
		} break;
	case UI_LIST_PARTICLE_EDITOR_PARTICLETEMPLET:	// 파티클 템플릿 리스트를 누른 경우
		{
			if( nEvent == EVENT_LISTBOX_SELECTION )
			{
				pEmitterPropertiesListBox->m_nSelected = -1;
				pCustomListBox->m_nSelected = -1;
				pEventTempletBox->m_nSelected = -1;
				pParticleEventListBox->m_nSelected = -1;
				pModelViewListBox->m_nSelected = -1;

				pParticleEventListBox->RemoveAllItems();

				if( pTempletListBox->GetSelectedItem() == NULL )
					break;
				wstring name(pTempletListBox->GetSelectedItem()->strText);

				if( pParticleObj != NULL)
				{
					pParticleObj->SetPreviewParticle( name, true );
				}			
			}
			

		} break;
	case UI_BUT_PARTICLE_EDITOR_PARTICLETEMPLET_COPY:		// 템플릿을 수정하고자 하는 파티클 리스트로 카피한다
		{
			if( pTempletListBox->GetSelectedIndex() == -1 ) 
				return;
			pParticleObj->CopyTempletParticleToCustom( pTempletListBox->GetSelectedItem()->strText );

			// list update
			pCustomListBox->RemoveAllItems();
			const map<wstring, CKTDGParticleSystem::CParticleEventSequence*>& templetSeq = pParticleObj->GetCustomParticleSystem()->GetTempletSequences();
			map<wstring, CKTDGParticleSystem::CParticleEventSequence*>::const_iterator it;
			for( it=templetSeq.begin(); it != templetSeq.end(); it++ )
			{
				wstring& wstrName = (wstring)it->first;
				pCustomListBox->AddItem( wstrName.c_str(), NULL );
			}



		} break;
	case UI_BUT_PARTICLE_EDITOR_PARTICLETEMPLET_RELOAD:		// 템플릿을 다시 로드
		{
			if(pParticleObj != NULL)
			{
				pParticleObj->ReloadParticleFile();


				CKTDGParticleSystem* pParticleSystem = pParticleObj->GetParticleSystem();
				 if( pParticleSystem != NULL )
				 {
					 const map<wstring, CKTDGParticleSystem::CParticleEventSequence*>& templetSeq = pParticleSystem->GetTempletSequences();

					 CDXUTListBox* pListBox		= (CDXUTListBox*)SiGetParticleEditorDlg( UI_LIST_PARTICLE_EDITOR_PARTICLETEMPLET );

					 pListBox->RemoveAllItems();
					 map<wstring, CKTDGParticleSystem::CParticleEventSequence*>::const_iterator it;
					 for( it=templetSeq.begin(); it != templetSeq.end(); it++ )
					 {
						 wstring& wstrName = (wstring)it->first;
						 pListBox->AddItem( wstrName.c_str(), NULL );
					 }

				 }
			}

		} break;

	case UI_LIST_PARTICLE_EDITOR_EMITTERATTRIBUTE:		// 수정하고자 하는 파티클의 Emitter 정보를 클릭시
		{

			if( nEvent == EVENT_LISTBOX_SELECTION )
			{
				if( pCustomListBox->GetSelectedItem() == NULL )
					return;

				CX2ViewerParticleEditor::PropertyData* pData = (CX2ViewerParticleEditor::PropertyData*)pEmitterPropertiesListBox->GetSelectedItem()->pData;
				if( pData != NULL )
				{
					// 주의 : const_cast 사용! 
					CKTDGParticleSystem::CParticleEventSequence* pSeq = 
						const_cast<CKTDGParticleSystem::CParticleEventSequence*>(
						pParticleObj->GetCustomParticleSystem()->GetTempletSequencesByName( pCustomListBox->GetSelectedItem()->strText ) );

					CDXUTStatic* pStatic = (CDXUTStatic*)SiGetParticleEditorDlg( UI_STATIC_PARTICLE_EDITOR_EMITTERATTRIBUTE_VALUE );
					CDXUTEditBox* pSingleEdit = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_SINGLE );
					CDXUTEditBox* pEditX = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_X );
					CDXUTEditBox* pEditY = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_Y );
					CDXUTEditBox* pEditZ = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_Z );
					CDXUTComboBox* pCombo = (CDXUTComboBox*)SiGetParticleEditorDlg( UI_COMBO_PARTICLE_EDITOR_EMITTERATTRIBUTE );

					if( pData->m_bAllowMinMax )
					{
						pStatic->SetText( L"Value(Random Allowed)" );
					}
					else
					{
						pStatic->SetText( L"Value(NO RANDOM)" );
					}

					pSingleEdit->SetVisible( false );
					pEditX->SetVisible( false );
					pEditY->SetVisible( false );
					pEditZ->SetVisible( false );
					pCombo->SetVisible( false );

					switch( pData->m_valuetype )
					{
					case CX2ViewerParticleEditor::VT_XYZ:
						{
							pEditX->SetVisible( true );
							pEditY->SetVisible( true );
							pEditZ->SetVisible( true );

							CMinMax<D3DXVECTOR3> val = refParticleEditor.GetXYZValue( pSeq, (CX2ViewerParticleEditor::EMITTER_PROPERTIES)pData->m_Type );
							pEditX->SetText( refParticleEditor.GetMinMaxString( val.m_Min.x, val.m_Max.x ).c_str() );
							pEditY->SetText( refParticleEditor.GetMinMaxString( val.m_Min.y, val.m_Max.y ).c_str() );
							pEditZ->SetText( refParticleEditor.GetMinMaxString( val.m_Min.z, val.m_Max.z ).c_str() );

						} break;
					case CX2ViewerParticleEditor::VT_XY:
						{
							pEditX->SetVisible( true );
							pEditY->SetVisible( true );

							CMinMax<D3DXVECTOR2> val = refParticleEditor.GetXYValue( pSeq, (CX2ViewerParticleEditor::EMITTER_PROPERTIES)pData->m_Type );
							pEditX->SetText( refParticleEditor.GetMinMaxString( val.m_Min.x, val.m_Max.x ).c_str() );
							pEditY->SetText( refParticleEditor.GetMinMaxString( val.m_Min.y, val.m_Max.y ).c_str() );
						} break;
					case CX2ViewerParticleEditor::VT_STRING:
						{
							pSingleEdit->SetVisible( true );
							wstring val = refParticleEditor.GetStringValue( pSeq, (CX2ViewerParticleEditor::EMITTER_PROPERTIES)pData->m_Type );
							pSingleEdit->SetText( val.c_str() );

						} break;
					case CX2ViewerParticleEditor::VT_FLOAT:
						{
							pSingleEdit->SetVisible( true );
							CMinMax<float> val = refParticleEditor.GetFloatValue( pSeq, (CX2ViewerParticleEditor::EMITTER_PROPERTIES)pData->m_Type );
							pSingleEdit->SetText( refParticleEditor.GetMinMaxString( val ).c_str() );

						} break;
					case CX2ViewerParticleEditor::VT_INT:
						{
							wstringstream wstrm;
							pSingleEdit->SetVisible( true );
							int val = refParticleEditor.GetIntValue( pSeq, (CX2ViewerParticleEditor::EMITTER_PROPERTIES)pData->m_Type );
							wstrm << val;
							pSingleEdit->SetText( wstrm.str().c_str() );
						} break;
					case CX2ViewerParticleEditor::VT_BOOL:
						{
							pCombo->SetVisible( true );
							pCombo->RemoveAllItems();
							pCombo->AddItem( L"True", NULL );
							pCombo->AddItem( L"False", NULL );

							if( refParticleEditor.GetBoolValue( pSeq, (CX2ViewerParticleEditor::EMITTER_PROPERTIES)pData->m_Type ) )
							{
								pCombo->SetSelectedByText( L"True" );
							}
							else
							{
								pCombo->SetSelectedByText( L"False" );
							}

						} break;

					case CX2ViewerParticleEditor::VT_COMBO_CUSTOM:
						{
							pCombo->SetVisible( true );
							refParticleEditor.GetCustomComboBoxValue( pSeq, (CX2ViewerParticleEditor::EMITTER_PROPERTIES)pData->m_Type, pCombo );
						} break;

					}
				}
			}
		} break;

// 	case UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_SINGLE:
// 		{
// 
// 		} break;
// 	case UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_X:
// 		{
// 
// 		} break;
// 	case UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_Y:
// 		{
// 
// 		} break;
// 	case UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_Z:
// 		{
// 
// 		} break;
// 	case UI_COMBO_PARTICLE_EDITOR_EMITTERATTRIBUTE:
// 		{
// 
// 		} break;
// 	case UI_BUT_PARTICLE_EDITOR_EMITTERATTRIBUTE_DEFAULT:
// 		{
// 
// 		} break;
	case UI_BUT_PARTICLE_EDITOR_EMITTERATTRIBUTE_APPLY:		// Emitter 속성변경 적용
		{
			///
			if( pCustomListBox->GetSelectedItem() == NULL )
				break;
			if( pEmitterPropertiesListBox->GetSelectedItem() == NULL )
				break;
			CX2ViewerParticleEditor::PropertyData* pData = (CX2ViewerParticleEditor::PropertyData*)pEmitterPropertiesListBox->GetSelectedItem()->pData;
			if( pData != NULL )
			{
				// 주의 : const_cast 사용! 여기서는 실제로 값도 변화시키고 있으므로 주의.
				CKTDGParticleSystem::CParticleEventSequence* pSeq = 
					const_cast<CKTDGParticleSystem::CParticleEventSequence*>(
					pParticleObj->GetCustomParticleSystem()->GetTempletSequencesByName( pCustomListBox->GetSelectedItem()->strText ) );

				CDXUTStatic* pStatic = (CDXUTStatic*)SiGetParticleEditorDlg( UI_STATIC_PARTICLE_EDITOR_EMITTERATTRIBUTE_VALUE );
				CDXUTEditBox* pSingleEdit = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_SINGLE );
				CDXUTEditBox* pEditX = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_X );
				CDXUTEditBox* pEditY = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_Y );
				CDXUTEditBox* pEditZ = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_Z );
				CDXUTComboBox* pCombo = (CDXUTComboBox*)SiGetParticleEditorDlg( UI_COMBO_PARTICLE_EDITOR_EMITTERATTRIBUTE );

				switch( pData->m_valuetype )
				{
				case CX2ViewerParticleEditor::VT_XYZ:
					{
						CMinMax<D3DXVECTOR3> val;
						val = refParticleEditor.ParseCMinMaxD3DVECTOR3( pEditX->GetText(), pEditY->GetText(), pEditZ->GetText() );

						refParticleEditor.SetXYZValue( pSeq, (CX2ViewerParticleEditor::EMITTER_PROPERTIES)pData->m_Type, val );
					} break;
				case CX2ViewerParticleEditor::VT_XY:
					{
						CMinMax<D3DXVECTOR2> val;
						val = refParticleEditor.ParseCMinMaxD3DVECTOR2( pEditX->GetText(), pEditY->GetText() );

						refParticleEditor.SetXYValue( pSeq, (CX2ViewerParticleEditor::EMITTER_PROPERTIES)pData->m_Type, val );
					} break;
				case CX2ViewerParticleEditor::VT_STRING:
					{
						refParticleEditor.SetStringValue( pSeq, (CX2ViewerParticleEditor::EMITTER_PROPERTIES)pData->m_Type, pSingleEdit->GetText() );

					} break;
				case CX2ViewerParticleEditor::VT_FLOAT:
					{
						CMinMax<float> val = refParticleEditor.ParseCMinMaxFloat( pSingleEdit->GetText() );
						refParticleEditor.SetFloatValue( pSeq, (CX2ViewerParticleEditor::EMITTER_PROPERTIES)pData->m_Type, val );
					} break;
				case CX2ViewerParticleEditor::VT_INT:
					{
						int val = _wtoi( pSingleEdit->GetText() );
						refParticleEditor.SetIntValue( pSeq, (CX2ViewerParticleEditor::EMITTER_PROPERTIES)pData->m_Type, val );
						
					} break;
				case CX2ViewerParticleEditor::VT_BOOL:
					{
						if( pCombo->GetSelectedItem() == NULL )
							break;

						if( 0 == wcscmp( pCombo->GetSelectedItem()->strText, L"True" ) )
						{
							refParticleEditor.SetBoolValue( pSeq, (CX2ViewerParticleEditor::EMITTER_PROPERTIES)pData->m_Type, true );
						}
						else
						{
							refParticleEditor.SetBoolValue( pSeq, (CX2ViewerParticleEditor::EMITTER_PROPERTIES)pData->m_Type, false );
						}
					} break;

				case CX2ViewerParticleEditor::VT_COMBO_CUSTOM:
					{
						refParticleEditor.SetCustomComboBoxValue( pSeq, (CX2ViewerParticleEditor::EMITTER_PROPERTIES)pData->m_Type, pCombo->GetSelectedItem() );
					} break;

				}

			}

			pParticleObj->ReplayPreviewParticle();

		} break;

	case UI_LIST_PARTICLE_EDITOR_EVENT:				// 파티클에 이미 만들어져 있는 이벤트들 리스트
		{

			if( nEvent == EVENT_LISTBOX_SELECTION )
			{
				pEventTempletBox->m_nSelected = -1;

				if( pParticleEventListBox->GetSelectedItem() == NULL )
					return;

				// 아래쪽 선택칸 변경. 기존값을 읽어와 보여준다.
				CDXUTButton* pButApply = (CDXUTButton*)SiGetParticleEditorDlg( UI_BUT_PARTICLE_EDITOR_EVENT_APPLY );
				//CDXUTButton* pButNew = (CDXUTButton*)SiGetParticleEditorDlg( UI_BUT_PARTICLE_EDITOR_EVENT_NEW );

				CDXUTStatic* pStatic = (CDXUTStatic*)SiGetParticleEditorDlg( UI_STATIC_PARTICLE_EDITOR_EVENT_VALUE );
				CDXUTEditBox* pSingleEdit = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_SINGLE );
				CDXUTEditBox* pEditX = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_X );
				CDXUTEditBox* pEditY = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_Y );
				CDXUTEditBox* pEditZ = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_Z );
				CDXUTComboBox* pCombo = (CDXUTComboBox*)SiGetParticleEditorDlg( UI_COMBO_PARTICLE_EDITOR_EVENT );
				CDXUTEditBox* pEditR = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_R );
				CDXUTEditBox* pEditG = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_G );
				CDXUTEditBox* pEditB = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_B );
				CDXUTEditBox* pEditA = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_A );
				CDXUTCheckBox* pCheckFade = (CDXUTCheckBox*)SiGetParticleEditorDlg( UI_CHECK_PARTICLE_EDITOR_EVENT_FADE );

				CDXUTEditBox* pEditTime1 = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_TIME1 );
				CDXUTEditBox* pEditTime2 = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_TIME2 );

				CKTDGParticleSystem::CParticleEvent* pEvent = (CKTDGParticleSystem::CParticleEvent*) pParticleEventListBox->GetSelectedItem()->pData;
				if( pEvent == NULL )
					return;
				pButApply->SetVisible( true );

				// 시간 세팅해주고
				WCHAR buf[64];
				swprintf( buf, L"%.4f", pEvent->GetActualTime().m_Min );
				pEditTime1->SetText( buf );
				swprintf( buf, L"%.4f", pEvent->GetActualTime().m_Max );
				pEditTime2->SetText( buf );
				pCheckFade->SetChecked( pEvent->IsFade() );
				if( pEvent->IsFade() )
				{
					pEditTime2->SetVisible( true );
				}
				else
				{
					pEditTime2->SetVisible( false );
				}

				CX2ViewerParticleEditor::PropertyData* pPropertyData = refParticleEditor.GetEventProperties( pEvent->GetEventType() );
				if( pPropertyData == NULL )
					return;

				if( pPropertyData->m_bAllowMinMax )
				{
					pStatic->SetText( L"Value(Random Allowed)" );
				}
				else
				{
					pStatic->SetText( L"Value(NO RANDOM)" );
				}

				pSingleEdit->SetVisible( false );
				pEditX->SetVisible( false );
				pEditY->SetVisible( false );
				pEditZ->SetVisible( false );
				pCombo->SetVisible( false );
				pEditR->SetVisible( false );
				pEditG->SetVisible( false );
				pEditB->SetVisible( false );
				pEditA->SetVisible( false );

				switch( pPropertyData->m_valuetype )
				{
				case CX2ViewerParticleEditor::VT_XYZ:
					{
						pEditX->SetVisible( true );
						pEditY->SetVisible( true );
						pEditZ->SetVisible( true );

						CMinMax<D3DXVECTOR3> val = refParticleEditor.GetXYZValue( pEvent, pEvent->GetEventType() );
						pEditX->SetText( refParticleEditor.GetMinMaxString( val.m_Min.x, val.m_Max.x ).c_str() );
						pEditY->SetText( refParticleEditor.GetMinMaxString( val.m_Min.y, val.m_Max.y ).c_str() );
						pEditZ->SetText( refParticleEditor.GetMinMaxString( val.m_Min.z, val.m_Max.z ).c_str() );

					} break;
				case CX2ViewerParticleEditor::VT_XY:
					{
						pEditX->SetVisible( true );
						pEditY->SetVisible( true );

						CMinMax<D3DXVECTOR2> val = refParticleEditor.GetXYValue( pEvent, pEvent->GetEventType() );
						pEditX->SetText( refParticleEditor.GetMinMaxString( val.m_Min.x, val.m_Max.x ).c_str() );
						pEditY->SetText( refParticleEditor.GetMinMaxString( val.m_Min.y, val.m_Max.y ).c_str() );
					} break;
				case CX2ViewerParticleEditor::VT_STRING:
					{
						pSingleEdit->SetVisible( true );
						wstring val = refParticleEditor.GetStringValue( pEvent, pEvent->GetEventType() );
						pSingleEdit->SetText( val.c_str() );

					} break;
				case CX2ViewerParticleEditor::VT_FLOAT:
					{
						pSingleEdit->SetVisible( true );
						CMinMax<float> val = refParticleEditor.GetFloatValue( pEvent, pEvent->GetEventType() );
						pSingleEdit->SetText( refParticleEditor.GetMinMaxString( val ).c_str() );

					} break;
				case CX2ViewerParticleEditor::VT_RGBA:
					{
						pEditR->SetVisible( true );
						pEditG->SetVisible( true );
						pEditB->SetVisible( true );
						pEditA->SetVisible( true );
						CMinMax<D3DXCOLOR> val = refParticleEditor.GetRGBAValue( pEvent, pEvent->GetEventType() );
						pEditR->SetText( refParticleEditor.GetMinMaxString( val.m_Min.r, val.m_Max.r ).c_str() );
						pEditG->SetText( refParticleEditor.GetMinMaxString( val.m_Min.g, val.m_Max.g ).c_str() );
						pEditB->SetText( refParticleEditor.GetMinMaxString( val.m_Min.b, val.m_Max.b ).c_str() );
						pEditA->SetText( refParticleEditor.GetMinMaxString( val.m_Min.a, val.m_Max.a ).c_str() );

					} break;
				}

			}
			

		} break;
	case UI_BUT_PARTICLE_EDITOR_EVENT_DELETE:
		{
			if( pCustomListBox->GetSelectedItem() == NULL )
				return;
			if( pParticleEventListBox->GetSelectedItem() == NULL )
				return;

			CKTDGParticleSystem::CParticleEvent* pEvent = NULL;
			CX2ViewerParticleEditor::PropertyData* pPropData = NULL;
			CKTDGParticleSystem::CParticleEventSequence* pSeq = 
				const_cast<CKTDGParticleSystem::CParticleEventSequence*>(
				pParticleObj->GetCustomParticleSystem()->GetTempletSequencesByName( pCustomListBox->GetSelectedItem()->strText ) );
			if( pSeq == NULL )
				return;

			pEvent = (CKTDGParticleSystem::CParticleEvent*)pParticleEventListBox->GetSelectedItem()->pData;

			vector<CKTDGParticleSystem::CParticleEvent*>* pVecEventList = pSeq->GetEventList();
			for( vector<CKTDGParticleSystem::CParticleEvent*>::iterator it = pVecEventList->begin(); it < pVecEventList->end(); ++it )
			{				
				if( *it == pEvent )
				{
					SAFE_DELETE( pEvent );
					pVecEventList->erase( it );
					pParticleEventListBox->RemoveItem( pParticleEventListBox->GetSelectedIndex() );
				}
			}

		} break;
	case UI_LIST_PARTICLE_EDITOR_EVENT_TYPE:		// 오른쪽에 있는 파티클 이벤트 템플릿 목록
		{

			if( nEvent == EVENT_LISTBOX_SELECTION )
			{
				pParticleEventListBox->m_nSelected = -1;
				if( pEventTempletBox->GetSelectedItem() == NULL )
					return;
				// 아래쪽 선택칸만 변경. Apply 버튼을 New로 바꾼다. 값은 Clear 해준다.
				// 아래쪽 선택칸만 변경. New 버튼을 Apply로 바꾼다. 기존값을 읽어와 보여준다.
				CDXUTButton* pButApply = (CDXUTButton*)SiGetParticleEditorDlg( UI_BUT_PARTICLE_EDITOR_EVENT_APPLY );
				//CDXUTButton* pButNew = (CDXUTButton*)SiGetParticleEditorDlg( UI_BUT_PARTICLE_EDITOR_EVENT_NEW );


				CDXUTStatic* pStatic = (CDXUTStatic*)SiGetParticleEditorDlg( UI_STATIC_PARTICLE_EDITOR_EVENT_VALUE );
				CDXUTEditBox* pSingleEdit = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_SINGLE );
				CDXUTEditBox* pEditX = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_X );
				CDXUTEditBox* pEditY = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_Y );
				CDXUTEditBox* pEditZ = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_Z );
				CDXUTComboBox* pCombo = (CDXUTComboBox*)SiGetParticleEditorDlg( UI_COMBO_PARTICLE_EDITOR_EVENT );
				CDXUTEditBox* pEditR = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_R );
				CDXUTEditBox* pEditG = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_G );
				CDXUTEditBox* pEditB = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_B );
				CDXUTEditBox* pEditA = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_A );
				CDXUTCheckBox* pCheckFade = (CDXUTCheckBox*)SiGetParticleEditorDlg( UI_CHECK_PARTICLE_EDITOR_EVENT_FADE );

				CDXUTEditBox* pEditTime1 = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_TIME1 );
				CDXUTEditBox* pEditTime2 = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_TIME2 );

				pButApply->SetVisible( false );

				// 시간 세팅해주고			
				pEditTime1->SetText( L"0" );
				pEditTime2->SetText( L"0" );
				pEditTime2->SetVisible( false );
				pCheckFade->SetChecked( false );

				CX2ViewerParticleEditor::PropertyData* pPropertyData = (CX2ViewerParticleEditor::PropertyData*)pEventTempletBox->GetSelectedItem()->pData;
				if( pPropertyData == NULL )
					return;

				if( pPropertyData->m_bAllowMinMax )
				{
					pStatic->SetText( L"Value(Random Allowed)" );
				}
				else
				{
					pStatic->SetText( L"Value(NO RANDOM)" );
				}

				pSingleEdit->SetVisible( false );
				pEditX->SetVisible( false );
				pEditY->SetVisible( false );
				pEditZ->SetVisible( false );
				pCombo->SetVisible( false );
				pEditR->SetVisible( false );
				pEditG->SetVisible( false );
				pEditB->SetVisible( false );
				pEditA->SetVisible( false );

				switch( pPropertyData->m_valuetype )
				{
				case CX2ViewerParticleEditor::VT_XYZ:
					{
						pEditX->SetVisible( true );
						pEditY->SetVisible( true );
						pEditZ->SetVisible( true );

						pEditX->SetText( L"0" );
						pEditY->SetText( L"0" );
						pEditZ->SetText( L"0" );

					} break;
				case CX2ViewerParticleEditor::VT_XY:
					{
						pEditX->SetVisible( true );
						pEditY->SetVisible( true );

						pEditX->SetText( L"0" );
						pEditY->SetText( L"0" );
					} break;
				case CX2ViewerParticleEditor::VT_STRING:
					{
						pSingleEdit->SetVisible( true );
						pSingleEdit->SetText( L"" );

					} break;
				case CX2ViewerParticleEditor::VT_FLOAT:
					{
						pSingleEdit->SetVisible( true );
						pSingleEdit->SetText( L"0" );

					} break;
				case CX2ViewerParticleEditor::VT_RGBA:
					{
						pEditR->SetVisible( true );
						pEditG->SetVisible( true );
						pEditB->SetVisible( true );
						pEditA->SetVisible( true );

						pEditR->SetText( L"0" );
						pEditG->SetText( L"0" );
						pEditB->SetText( L"0" );
						pEditA->SetText( L"0" );

					} break;
				}


			}
			
		} break;

	case UI_CHECK_PARTICLE_EDITOR_EVENT_FADE:	// 이벤트 시간설정 관련..
		{
			//CDXUTEditBox* pEditTime1 = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_TIME1 );
			CDXUTEditBox* pEditTime2 = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_TIME2 );

			CDXUTCheckBox* pCheck = (CDXUTCheckBox*)pControl;
			if( pCheck->GetChecked() )
			{
				pEditTime2->SetVisible( true );
			}
			else
			{
				pEditTime2->SetVisible( false );
			}

		} break;
// 	case UI_EDIT_PARTICLE_EDITOR_EVENT_TIME1:
// 		{
// 
// 		} break;
	case UI_EDIT_PARTICLE_EDITOR_EVENT_TIME2:		// 시간이 Lifetime을 넘으면 거기까지만으로
		{
			if( pCustomListBox->GetSelectedItem() == NULL )
				return;

			CDXUTEditBox* pEditBox = (CDXUTEditBox*)pControl;
			
			wstring name = pCustomListBox->GetSelectedItem()->strText;
			CKTDGParticleSystem::CParticleEventSequence* pTempletSeq = 
				const_cast<CKTDGParticleSystem::CParticleEventSequence*>(pParticleObj->GetCustomParticleSystem()->GetTempletSequencesByName( name ));

			if( pTempletSeq->GetLifeTime().m_Max < (float)_wtof( pEditBox->GetText() ) )
			{
				wstringstream wstrm;
				wstrm << pTempletSeq->GetLifeTime().m_Max;
				pEditBox->SetText( wstrm.str().c_str() );
			}

		} break;

// 	case UI_EDIT_PARTICLE_EDITOR_EVENT_SINGLE:
// 		{
// 
// 		} break;
// 	case UI_EDIT_PARTICLE_EDITOR_EVENT_X:
// 		{
// 
// 		} break;
// 	case UI_EDIT_PARTICLE_EDITOR_EVENT_Y:
// 		{
// 
// 		} break;
// 	case UI_EDIT_PARTICLE_EDITOR_EVENT_Z:
// 		{
// 
// 		} break;
// 	case UI_COMBO_PARTICLE_EDITOR_EVENT:
// 		{
// 
// 		} break;
// 	case UI_EDIT_PARTICLE_EDITOR_EVENT_R:
// 		{
// 		}break;
// 	case UI_EDIT_PARTICLE_EDITOR_EVENT_G:
// 		{
// 		}break;
// 	case UI_EDIT_PARTICLE_EDITOR_EVENT_B:
// 		{
// 		} break;
// 	case UI_EDIT_PARTICLE_EDITOR_EVENT_A:
// 		{
// 
// 		} break;
	case UI_BUT_PARTICLE_EDITOR_EVENT_APPLY:
		{
			//////////////////////////////////////////////////////////////////////////
			if( pCustomListBox->GetSelectedItem() == NULL )
				return;

			CKTDGParticleSystem::CParticleEvent* pEvent = NULL;
			CX2ViewerParticleEditor::PropertyData* pPropData = NULL;
			CKTDGParticleSystem::CParticleEventSequence* pSeq = 
				const_cast<CKTDGParticleSystem::CParticleEventSequence*>(
				pParticleObj->GetCustomParticleSystem()->GetTempletSequencesByName( pCustomListBox->GetSelectedItem()->strText ) );
			if( pSeq == NULL )
				return;

			if( pParticleEventListBox->GetSelectedItem() == NULL )
				return;
			// 기존 이벤트 수정
			pEvent = (CKTDGParticleSystem::CParticleEvent*)pParticleEventListBox->GetSelectedItem()->pData;
			pPropData = refParticleEditor.GetEventProperties( pEvent->GetEventType() );
			
			if( pPropData == NULL || pEvent == NULL )
				return;
			
			// 값 설정 시작
			CDXUTCheckBox* pCheckFade = (CDXUTCheckBox*)SiGetParticleEditorDlg( UI_CHECK_PARTICLE_EDITOR_EVENT_FADE );
			CDXUTEditBox* pEditTime1 = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_TIME1 );
			CDXUTEditBox* pEditTime2 = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_TIME2 );
			pEvent->SetFade( pCheckFade->GetChecked() );
			if( pCheckFade->GetChecked() )
			{
				CMinMax<float> val;
				val.m_Min = (float)_wtof( pEditTime1->GetText() );
				val.m_Max = (float)_wtof( pEditTime2->GetText() );
				pEvent->SetActualTime( val );
			}
			else
			{
				CMinMax<float> val;
				val.m_Min = (float)_wtof( pEditTime1->GetText() );
				val.m_Max = val.m_Min;
				pEvent->SetActualTime( val );
			}

			switch( pPropData->m_valuetype )
			{
			case CX2ViewerParticleEditor::VT_RGBA:
				{
					CDXUTEditBox* pEditR = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_R );
					CDXUTEditBox* pEditG = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_G );
					CDXUTEditBox* pEditB = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_B );
					CDXUTEditBox* pEditA = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_A );

					CMinMax<D3DXCOLOR> val;
					CMinMax<float> valr = refParticleEditor.ParseCMinMaxFloat( pEditR->GetText() );
					CMinMax<float> valg = refParticleEditor.ParseCMinMaxFloat( pEditG->GetText() );
					CMinMax<float> valb = refParticleEditor.ParseCMinMaxFloat( pEditB->GetText() );
					CMinMax<float> vala = refParticleEditor.ParseCMinMaxFloat( pEditA->GetText() );

					val.m_Min = D3DXCOLOR(valr.m_Min, valg.m_Min, valb.m_Min, vala.m_Min);
					val.m_Max = D3DXCOLOR(valr.m_Max, valg.m_Max, valb.m_Max, vala.m_Max);

					refParticleEditor.SetRGBAValue( pEvent, pEvent->GetEventType(), val );
 
				} break;
			case CX2ViewerParticleEditor::VT_XYZ:
				{
					CDXUTEditBox* pEditX = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_X );
					CDXUTEditBox* pEditY = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_Y );
					CDXUTEditBox* pEditZ = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_Z );

					CMinMax<D3DXVECTOR3> val;
					val = refParticleEditor.ParseCMinMaxD3DVECTOR3( pEditX->GetText(), pEditY->GetText(), pEditZ->GetText() );

					refParticleEditor.SetXYZValue( pEvent, pEvent->GetEventType(), val );
				} break;
			case CX2ViewerParticleEditor::VT_XY:
				{
					CDXUTEditBox* pEditX = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_X );
					CDXUTEditBox* pEditY = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_Y );
					CMinMax<D3DXVECTOR2> val;
					val = refParticleEditor.ParseCMinMaxD3DVECTOR2( pEditX->GetText(), pEditY->GetText() );

					refParticleEditor.SetXYValue( pEvent, pEvent->GetEventType(), val );
				} break;
			case CX2ViewerParticleEditor::VT_STRING:
				{
					CDXUTEditBox* pSingleEdit = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_SINGLE );
					refParticleEditor.SetStringValue( pEvent, pEvent->GetEventType(), pSingleEdit->GetText() );

				} break;
			case CX2ViewerParticleEditor::VT_FLOAT:
				{
					CDXUTEditBox* pSingleEdit = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_SINGLE );
					CMinMax<float> val = refParticleEditor.ParseCMinMaxFloat( pSingleEdit->GetText() );
					refParticleEditor.SetFloatValue( pEvent, pEvent->GetEventType(), val );
				} break;			
			}

			SiSelf()->RefreshEventList();

			pParticleObj->ReplayPreviewParticle();
			
		} break;
	case UI_BUT_PARTICLE_EDITOR_EVENT_NEW:
		{
			//////////////////////////////////////////////////////////////////////////
			if( pCustomListBox->GetSelectedItem() == NULL )
				return;

			CKTDGParticleSystem::CParticleEvent* pEvent = NULL;
			CX2ViewerParticleEditor::PropertyData* pPropData = NULL;
			CKTDGParticleSystem::CParticleEventSequence* pSeq = 
				const_cast<CKTDGParticleSystem::CParticleEventSequence*>(
				pParticleObj->GetCustomParticleSystem()->GetTempletSequencesByName( pCustomListBox->GetSelectedItem()->strText ) );
			if( pSeq == NULL )
				return;
		
			// 여기도 Const_cast...
			// 새로 만들기
			if( pEventTempletBox->GetSelectedItem() != NULL )
			{
				pPropData = (CX2ViewerParticleEditor::PropertyData*)pEventTempletBox->GetSelectedItem()->pData;
				pEvent = refParticleEditor.EventFactory( (CKTDGParticleSystem::EVENT_TYPE)pPropData->m_Type );
			}
			else if( pParticleEventListBox->GetSelectedItem() != NULL )
			{				
				CKTDGParticleSystem::CParticleEvent* pOrgEvent = (CKTDGParticleSystem::CParticleEvent*)pParticleEventListBox->GetSelectedItem()->pData;
				pPropData = refParticleEditor.GetEventProperties( pOrgEvent->GetEventType() );
				pEvent = refParticleEditor.EventFactory( pOrgEvent->GetEventType() );
			}
			else
			{
				return;
			}
				

			vector<CKTDGParticleSystem::CParticleEvent*>* pVecEventList = pSeq->GetEventList();
			pVecEventList->push_back(pEvent);

			if( pPropData == NULL || pEvent == NULL )
				return;

			// 값 설정 시작
			CDXUTCheckBox* pCheckFade = (CDXUTCheckBox*)SiGetParticleEditorDlg( UI_CHECK_PARTICLE_EDITOR_EVENT_FADE );
			CDXUTEditBox* pEditTime1 = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_TIME1 );
			CDXUTEditBox* pEditTime2 = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_TIME2 );
			pEvent->SetFade( pCheckFade->GetChecked() );
			if( pCheckFade->GetChecked() )
			{
				CMinMax<float> val;
				val.m_Min = (float)_wtof( pEditTime1->GetText() );
				val.m_Max = (float)_wtof( pEditTime2->GetText() );
				pEvent->SetActualTime( val );
			}
			else
			{
				CMinMax<float> val;
				val.m_Min = (float)_wtof( pEditTime1->GetText() );
				val.m_Max = val.m_Min;
				pEvent->SetActualTime( val );
			}

			switch( pPropData->m_valuetype )
			{
			case CX2ViewerParticleEditor::VT_RGBA:
				{
					CDXUTEditBox* pEditR = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_R );
					CDXUTEditBox* pEditG = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_G );
					CDXUTEditBox* pEditB = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_B );
					CDXUTEditBox* pEditA = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_A );

					CMinMax<D3DXCOLOR> val;
					CMinMax<float> valr = refParticleEditor.ParseCMinMaxFloat( pEditR->GetText() );
					CMinMax<float> valg = refParticleEditor.ParseCMinMaxFloat( pEditG->GetText() );
					CMinMax<float> valb = refParticleEditor.ParseCMinMaxFloat( pEditB->GetText() );
					CMinMax<float> vala = refParticleEditor.ParseCMinMaxFloat( pEditA->GetText() );

					val.m_Min = D3DXCOLOR(valr.m_Min, valg.m_Min, valb.m_Min, vala.m_Min);
					val.m_Max = D3DXCOLOR(valr.m_Max, valg.m_Max, valb.m_Max, vala.m_Max);

					refParticleEditor.SetRGBAValue( pEvent, pEvent->GetEventType(), val );

				} break;
			case CX2ViewerParticleEditor::VT_XYZ:
				{
					CDXUTEditBox* pEditX = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_X );
					CDXUTEditBox* pEditY = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_Y );
					CDXUTEditBox* pEditZ = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_Z );

					CMinMax<D3DXVECTOR3> val;
					val = refParticleEditor.ParseCMinMaxD3DVECTOR3( pEditX->GetText(), pEditY->GetText(), pEditZ->GetText() );

					refParticleEditor.SetXYZValue( pEvent, pEvent->GetEventType(), val );
				} break;
			case CX2ViewerParticleEditor::VT_XY:
				{
					CDXUTEditBox* pEditX = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_X );
					CDXUTEditBox* pEditY = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_Y );
					CMinMax<D3DXVECTOR2> val;
					val = refParticleEditor.ParseCMinMaxD3DVECTOR2( pEditX->GetText(), pEditY->GetText() );

					refParticleEditor.SetXYValue( pEvent, pEvent->GetEventType(), val );
				} break;
			case CX2ViewerParticleEditor::VT_STRING:
				{
					CDXUTEditBox* pSingleEdit = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_SINGLE );
					refParticleEditor.SetStringValue( pEvent, pEvent->GetEventType(), pSingleEdit->GetText() );

				} break;
			case CX2ViewerParticleEditor::VT_FLOAT:
				{
					CDXUTEditBox* pSingleEdit = (CDXUTEditBox*)SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_SINGLE );
					CMinMax<float> val = refParticleEditor.ParseCMinMaxFloat( pSingleEdit->GetText() );
					refParticleEditor.SetFloatValue( pEvent, pEvent->GetEventType(), val );
				} break;			
			}

			SiSelf()->RefreshEventList();

			pParticleObj->ReplayPreviewParticle();

		} break;
	case UI_LIST_PARTICLE_EDITOR_MODEL_LIST:
		{

			if( nEvent == EVENT_LISTBOX_SELECTION )
			{
				pTempletListBox->m_nSelected = -1;
				pCustomListBox->m_nSelected = -1;
				pEventTempletBox->m_nSelected = -1;
				pParticleEventListBox->m_nSelected = -1;
				pEmitterPropertiesListBox->m_nSelected = -1;

				pParticleEventListBox->RemoveAllItems();

				if( pModelViewListBox->GetSelectedItem() == NULL )
					break;
				CX2ViewerParticle::ParticleEffectData* pData = (CX2ViewerParticle::ParticleEffectData*)pModelViewListBox->GetSelectedItem()->pData;
				if( pData == NULL )
					return;

				if( pParticleObj != NULL)
				{
					pParticleObj->SetPreviewParticle( pData->m_SequenceName, pData->m_bIsTemplet );
				}	
			}
			
			
		} break;
	case UI_BUT_PARTICLE_EDITOR_MODEL_ADD:
		{
			CX2ViewerParticle::ParticleEffectData* pData = new CX2ViewerParticle::ParticleEffectData();
			
			if( pCustomListBox->GetSelectedItem() != NULL )
			{
				// 새로 만든 거 :$
				pData->m_SequenceName = pCustomListBox->GetSelectedItem()->strText;
				pData->m_bIsTemplet = false;
			}
			else if( pTempletListBox->GetSelectedItem() != NULL )
			{
				// 원래 있는 거
				pData->m_SequenceName = pTempletListBox->GetSelectedItem()->strText;
				pData->m_bIsTemplet = true;
			}
			///
			WCHAR buf[256];
			int i = 0;
			wstring newname;
			do 
			{
				wsprintf(buf, L"%s_%02d", pData->m_SequenceName.c_str(), i);
				i++;
				pData->m_Name = buf;
			} while(pParticleObj->GetParticleEffectDataByName( pData->m_Name ));

			pParticleObj->AddParticleEffectData( pData );
			pModelViewListBox->AddItem( pData->m_Name.c_str(), (void*)pData );			

		} break;
	case UI_BUT_PARTICLE_EDITOR_MODEL_DELETE:
		{
			if( pModelViewListBox->GetSelectedItem() == NULL )
				return;
			CX2ViewerParticle::ParticleEffectData* pData = (CX2ViewerParticle::ParticleEffectData*)pModelViewListBox->GetSelectedItem()->pData;
			if( pData == NULL )
				return;

			pParticleObj->DeleteParticleEffectData( pData->m_Name );
			pModelViewListBox->RemoveItem( pModelViewListBox->GetSelectedIndex() );
		} break;

	case UI_COMBO_PARTICLE_EDITOR_PLAY_TYPE:
		{
			CDXUTComboBox* pCombo = (CDXUTComboBox*) pControl;
			if( nEvent == EVENT_COMBOBOX_SELECTION_CHANGED )
			{
				//**
				WCHAR* wszPlayType = pCombo->GetSelectedItem()->strText;
				if( wcscmp( wszPlayType, L"ONE" ) == 0 )
				{
					pParticleObj->SetPreviewPlayMode( false );
				}
				else if( wcscmp( wszPlayType, L"LOOP" ) == 0 )
				{
					pParticleObj->SetPreviewPlayMode( true );
				}
			}
            
		} break;
	case UI_BUT_PARTICLE_EDITOR_PLAY:
		{
			pParticleObj->ReplayPreviewParticle();
		} break;
	
	case UI_SLIDE_PARTICLE_EDITOR_SPEED:
		{			
			CDXUTSlider* pSlider = (CDXUTSlider*) pControl;
			int nValue = pSlider->GetValue();

			pParticleObj->SetPreviewPlaySpeed( nValue/100.0f );
		} break;

	case UI_BUT_PARTICLE_EDITOR_EXIT:
		{
			CX2ViewerSkinMesh*	pSkinMesh	= (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );

			if(pSkinMesh != NULL)
			{
				if( pSkinMesh->GetXSkinAnim() != NULL )
					pSkinMesh->GetXSkinAnim()->SetShowObject(true);
				if( NULL != pSkinMesh->GetWeaponXSkinAnim() )
					pSkinMesh->GetWeaponXSkinAnim()->SetShowObject(true);
			}

			pParticleObj->SetMode( CX2ViewerParticle::PM_NORMAL );

			// UI_BUT_PARTICLE_PARTICLEEDITOR에서 뺐던 벡터들을 원상복구 시킨다.
			// 다이얼로그 셋을 추가하려면 여기에도 추가해야 함.
			SiSelf()->SetParticleEditButOnOff( false );
			SiSelf()->m_vecDialog.clear();
			SiSelf()->m_vecDialog.push_back( &(SiSelf()->m_BaseOption) );
			SiSelf()->m_vecDialog.push_back( &(SiSelf()->m_Unit) );
			SiSelf()->m_vecDialog.push_back( &(SiSelf()->m_Mesh) );
			SiSelf()->m_vecDialog.push_back( &(SiSelf()->m_RenderParam) );
			SiSelf()->m_vecDialog.push_back( &(SiSelf()->m_ParticleBasic) );

			// 바깥쪽 리스트 업데이트
			CDXUTListBox* pListBox_Outside = (CDXUTListBox*)SiGetParticleDlg(UI_LIST_PARTICLE_LIST);
			pListBox_Outside->RemoveAllItems();
			vector<CX2ViewerParticle::ParticleEffectData*> vecParticleEffect = pParticleObj->GetParticleEffectData();
			for( vector<CX2ViewerParticle::ParticleEffectData*>::iterator it=vecParticleEffect.begin(); it != vecParticleEffect.end(); it++ )
			{
				CX2ViewerParticle::ParticleEffectData* pData = *it;
				if( pData == NULL )
					continue;
				pListBox_Outside->AddItem( pData->m_Name.c_str(), (void*)pData );
			}

		} break;
	default:
		break;
	}

}


void CX2ViewerUI::Init()
{
	//UI의 기본크기 기준은 1024/768 로 한다.
	float fScaleX = 0.0f;
	float fScaleY = 0.0f;

	RECT rt;
	SetRect( &rt, 0, 0, 0, 0 );

	if( m_bIsInit == false )
	{
		fScaleX = 1.0f;
		fScaleY = 1.0f;

		m_bIsInit = true;
	}
	else
	{
		GetClientRect( g_pKTDXApp->GetHWND(), &rt );

		fScaleX = rt.right/1024.0f;
		fScaleY = rt.bottom/768.0f;
	}

	KLuaManager	luaManager(g_pKTDXApp->GetLuaBinder()->GetLuaState(), 0, true);
	int			nX, nY, nW, nH;
	nX = nY = nW = nH = 0;

	KGCMassFileManager::CMassFile::MASSFILE_MEMBERFILEINFO_POINTER Info;
	Info = g_pKTDXApp->GetDeviceManager()->GetMassFileManager()->LoadDataFile( L"ViewerUI.lua" );
	if( Info == NULL )
	{
		ERRORMSG( L"ViewerUI.lua 파일열기 실패" );
		return;
	}

	if( luaManager.DoMemory( Info->pRealData, Info->size ) == false )
	{
		ERRORMSG( L"ViewerUI.lua 파싱 실패" );
		return;
	}

	if( rt.right == 0 && rt.bottom == 0 )
	{
		rt.right	= 1024;
		rt.bottom	= 768;
	}
	m_Unit.SetLocation( rt.right - 450, 0 );
	m_Mesh.SetLocation( rt.right - 450, 0 );
	m_RenderParam.SetLocation( (rt.right-RP_SIZE_X)/2, (rt.bottom-RP_SIZE_Y-100) );

	CDXUTControl*	pControl;
	D3DXVECTOR3		vTemp;
	WCHAR strNum[128] = L"";
	//CHECK_GRID
	GET_LUA_POS( CHECK_GRID, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_CHECK_GRID );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//CHECK_WIREFRAME
	GET_LUA_POS( CHECK_WIREFRAME, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_CHECK_WIREFRAME );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//BUT_RESET
	GET_LUA_POS( BUT_RESET, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_BUT_RESET );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//BUT_UI_INIT
	GET_LUA_POS( BUT_UI_INIT, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_BUT_UI_INIT );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_CAMERAMODE
	GET_LUA_POS( STATIC_CAMERA_MODE, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_STATIC_CAMERAMODE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//RADIO_CAMERA_NORMAL
	GET_LUA_POS( RADIO_CAMERA_NORMAL, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_RADIO_CAMERA_NORMAL );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//RADIO_CAMERA_NAVIGATION
	GET_LUA_POS( RADIO_CAMERA_NAVIGATION, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_RADIO_CAMERA_NAVIGATION );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//BUT_CAMERA_RESET
	GET_LUA_POS( BUT_CAMERA_RESET, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_BUT_CAMERA_RESET );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_BGSET
	GET_LUA_POS( STATIC_BG_COLOR, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_STATIC_BGSET );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//EDIT_BG_A
	GET_LUA_POS( EDIT_BG_A, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_EDIT_BG_A );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_BG_A
	GET_LUA_POS( STATIC_BG_A, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_STATIC_BG_A );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//EDIT_BG_R
	GET_LUA_POS( EDIT_BG_R, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_EDIT_BG_R );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_BG_R
	GET_LUA_POS( STATIC_BG_R, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_STATIC_BG_R );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//EDIT_BG_G
	GET_LUA_POS( EDIT_BG_G, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_EDIT_BG_G );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_BG_G
	GET_LUA_POS( STATIC_BG_G, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_STATIC_BG_G );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//EDIT_BG_B
	GET_LUA_POS( EDIT_BG_B, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_EDIT_BG_B );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_BG_B
	GET_LUA_POS( STATIC_BG_B, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_STATIC_BG_B );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//초기 텍스트 셋팅
	m_BaseOption.GetEditBox( UI_EDIT_BG_A )->SetText( L"50" );
	m_BaseOption.GetEditBox( UI_EDIT_BG_R )->SetText( L"50" );
	m_BaseOption.GetEditBox( UI_EDIT_BG_G )->SetText( L"50" );
	m_BaseOption.GetEditBox( UI_EDIT_BG_B )->SetText( L"50" );

	//STATIC_WORLD_MESH
	GET_LUA_POS( STATIC_WORLD_MESH, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_STATIC_WORLD_MESH );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//BUT_WORLD_MESH
	GET_LUA_POS( BUT_WORLD_MESH, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_BUT_WORLD_MESH );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );



	//BUT_WORLD_MESH_RESET
	GET_LUA_POS( BUT_WORLD_MESH_RESET, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_BUT_WORLD_MESH_RESET );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_WORLD_MESH_X
	GET_LUA_POS( STATIC_WORLD_MESH_X, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_STATIC_WORLD_MESH_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_WORLD_MESH_Y
	GET_LUA_POS( STATIC_WORLD_MESH_Y, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_STATIC_WORLD_MESH_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_WORLD_MESH_Z
	GET_LUA_POS( STATIC_WORLD_MESH_Z, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_STATIC_WORLD_MESH_Z );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//EDIT_WORLD_MESH_X
	GET_LUA_POS( EDIT_WORLD_MESH_X, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_EDIT_WORLD_MESH_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTEditBox*)pControl)->SetText( L"" );

	//EDIT_WORLD_MESH_Y
	GET_LUA_POS( EDIT_WORLD_MESH_Y, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_EDIT_WORLD_MESH_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTEditBox*)pControl)->SetText( L"" );

	//EDIT_WORLD_MESH_Z
	GET_LUA_POS( EDIT_WORLD_MESH_Z, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_EDIT_WORLD_MESH_Z );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTEditBox*)pControl)->SetText( L"" );

	//BUT_EFFECT_SET
	GET_LUA_POS( BUT_EFFECT_SET, nX, nY, nW, nH );
	pControl = m_BaseOption.GetControl( UI_BUT_EFFECT_SET );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	// World Mesh Init
	CX2ViewerWorldMesh* pWorld = (CX2ViewerWorldMesh*)SiGetObject( OS_WORLD_MESH );
	swprintf( strNum, L"%.2f", pWorld->GetMatrix()->GetXPos() );
	m_BaseOption.GetEditBox( UI_EDIT_WORLD_MESH_X )->SetText( strNum );
	swprintf( strNum, L"%.2f", pWorld->GetMatrix()->GetYPos() );
	m_BaseOption.GetEditBox( UI_EDIT_WORLD_MESH_Y )->SetText( strNum );
	swprintf( strNum, L"%.2f", pWorld->GetMatrix()->GetZPos() );
	m_BaseOption.GetEditBox( UI_EDIT_WORLD_MESH_Z )->SetText( strNum );

	//STATIC_SCALE
	GET_LUA_POS( STATIC_SCALE, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_STATIC_SCALE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_SCALE_X
	GET_LUA_POS( STATIC_SCALE_X, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_STATIC_SCALE_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_SCALE_X
	GET_LUA_POS( EDIT_SCALE_X, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_EDIT_SCALE_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_SCALE_Y
	GET_LUA_POS( STATIC_SCALE_Y, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_STATIC_SCALE_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_SCALE_Y
	GET_LUA_POS( EDIT_SCALE_Y, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_EDIT_SCALE_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_SCALE_Z
	GET_LUA_POS( STATIC_SCALE_Z, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_STATIC_SCALE_Z );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_SCALE_Z
	GET_LUA_POS( EDIT_SCALE_Z, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_EDIT_SCALE_Z );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	vTemp = ((CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH ))->GetScale();    
	CDXUTEditBox* pEditBox = NULL;
	swprintf( strNum, L"%.2f", vTemp.x );
	pEditBox = m_Unit.GetEditBox( UI_EDIT_SCALE_X );
	pEditBox->SetText( strNum );
	swprintf( strNum, L"%.2f", vTemp.y );
	pEditBox = m_Unit.GetEditBox( UI_EDIT_SCALE_Y );
	pEditBox->SetText( strNum );
	swprintf( strNum, L"%.2f", vTemp.z );
	pEditBox = m_Unit.GetEditBox( UI_EDIT_SCALE_Z );
	pEditBox->SetText( strNum );

	//STATIC_LIGHT_POS
	GET_LUA_POS( STATIC_LIGHT_POS, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_STATIC_LIGHT_POS );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_LIGHT_POS_X
	GET_LUA_POS( STATIC_LIGHT_POS_X, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_STATIC_LIGHT_POS_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_SCALE_X
	GET_LUA_POS( EDIT_LIGHT_POS_X, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_EDIT_LIGHT_POS_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_LIGHT_POS_Y
	GET_LUA_POS( STATIC_LIGHT_POS_Y, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_STATIC_LIGHT_POS_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_LIGHT_POS_Y
	GET_LUA_POS( EDIT_LIGHT_POS_Y, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_EDIT_LIGHT_POS_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_LIGHT_POS_Z
	GET_LUA_POS( STATIC_LIGHT_POS_Z, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_STATIC_LIGHT_POS_Z );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_LIGHT_POS_Z
	GET_LUA_POS( EDIT_LIGHT_POS_Z, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_EDIT_LIGHT_POS_Z );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//CHECK_LIGHT_ONOFF
	GET_LUA_POS( CHECK_LIGHT_ONOFF, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_CHECK_LIGHT_ONOFF );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	vTemp = ((CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH ))->GetLightPos();
	pEditBox = NULL;
	swprintf( strNum, L"%.2f", vTemp.x );
	pEditBox = m_Unit.GetEditBox( UI_EDIT_LIGHT_POS_X );
	pEditBox->SetText( strNum );
	swprintf( strNum, L"%.2f", vTemp.y );
	pEditBox = m_Unit.GetEditBox( UI_EDIT_LIGHT_POS_Y );
	pEditBox->SetText( strNum );
	swprintf( strNum, L"%.2f", vTemp.z );
	pEditBox = m_Unit.GetEditBox( UI_EDIT_LIGHT_POS_Z );
	pEditBox->SetText( strNum );

	//STATIC_OBJECT : OBJECT LIST
	GET_LUA_POS( STATIC_OBJECT, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_STATIC_OBJECT );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//LIST_OBJECT
	GET_LUA_POS( LIST_OBJECT, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_LIST_OBJECT );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_ANIMATION : ANIMATION LIST
	GET_LUA_POS( STATIC_ANIMATION, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_STATIC_ANIMATION );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//LIST_ANIMATION
	GET_LUA_POS( LIST_ANIMATION, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_LIST_ANIMATION );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

    //LIST_OBJECT
    GET_LUA_POS( LIST_BONE, nX, nY, nW, nH );
    pControl = m_Unit.GetControl( UI_LIST_BONE );
    pControl->SetLocation( nX, nY );
    pControl->SetSize( nW, nH );

	//EDIT_ANIM_NUM
	GET_LUA_POS( EDIT_ANIM_NUM, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_EDIT_ANIM_NUM );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	swprintf( strNum, L"%d", m_Unit.GetListBox( UI_LIST_ANIMATION )->GetSize() );
	((CDXUTEditBox*)pControl)->SetText( strNum );

	//EDIT_ANIM_NAME
	GET_LUA_POS( EDIT_ANIM_NAME, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_EDIT_ANIM_NAME );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//BUT_ANIM_NAME_CHANGE
	GET_LUA_POS( BUT_ANIM_NAME_CHANGE, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_BUT_ANIM_NAME_CHANGE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	
	//BUT_PLAY_ONOFF
	GET_LUA_POS( BUT_PLAY_ONOFF, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_BUT_PLAY_ONOFF );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_ANIM_SPEED
	GET_LUA_POS( STATIC_ANIM_SPEED, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_STATIC_ANIM_SPEED );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//SLIDE_ANIM_SPEED
	GET_LUA_POS( SLIDE_ANIM_SPEED, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_SLIDE_ANIM_SPEED );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_ANIM_FRAME
	GET_LUA_POS( STATIC_ANIM_FRAME, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_STATIC_ANIM_FRAME );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//CHECK_MOTION_ONOFF
	GET_LUA_POS( CHECK_MOTION_ONOFF, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_CHECK_MOTION_ONOFF );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTCheckBox*)pControl)->SetChecked( ((CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH ))->GetMotionOnOff() );

	//BUT_RENDER_PARAM
	GET_LUA_POS( BUT_RENDER_PARAM, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_BUT_RENDER_PARAM );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_PLAY_TYPE
	GET_LUA_POS( STATIC_PLAY_TYPE, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_STATIC_PLAY_TYPE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//COMBO_PLAY_TYPE
	GET_LUA_POS( COMBO_PLAY_TYPE, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_COMBO_PLAY_TYPE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	CDXUTComboBox* pComboBox = (CDXUTComboBox*)pControl;
	pComboBox->RemoveAllItems();
	pComboBox->AddItem( L"ONE", NULL );
	pComboBox->AddItem( L"LOOP", NULL );
	pComboBox->SetDropHeight( 50 );
	switch( ((CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH ))->GetPlayType() )
	{
	case CKTDGXSkinAnim::XAP_ONE_WAIT:
		pComboBox->SetSelectedByText( L"ONE" );
		break;

	case CKTDGXSkinAnim::XAP_LOOP:
		pComboBox->SetSelectedByText( L"LOOP" );
		break;
	}

	//CHECK_BOUNDING
	GET_LUA_POS( CHECK_BOUNDING, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_CHECK_BOUNDING );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTCheckBox*)pControl)->SetChecked( ((CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH ))->GetBounding() );

	//CHECK_ATTACK_BOX
	GET_LUA_POS( CHECK_ATTACK_BOX, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_CHECK_ATTACK_BOX );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTCheckBox*)pControl)->SetChecked( ((CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH ))->GetShowAttackBox() );

	
	//EDIT_FRAME_TIME_INC
	GET_LUA_POS( EDIT_FRAME_TIME_INC, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_EDIT_FRAME_TIME_INC );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTEditBox*)pControl)->SetTextFloatArray( &m_fAnimTimeInc, 1 );


	//BUT_PREV_FRAME
	GET_LUA_POS( BUT_PREV_FRAME, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_BUT_PREV_FRAME );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//BUT_NEXT_FRAME
	GET_LUA_POS( BUT_NEXT_FRAME, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_BUT_NEXT_FRAME );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );


	//BUT_ATTACH_WEAPON
	GET_LUA_POS( BUT_ATTACH_WEAPON, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_BUT_ATTACH_WEAPON );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//EDIT_ATTACH_WEAPON_BONE_NAME
	GET_LUA_POS( EDIT_ATTACH_WEAPON_BONE_NAME, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_EDIT_ATTACH_WEAPON_BONE_NAME );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_ATTACH_WEAPON_BONE_NAME
	GET_LUA_POS( STATIC_ATTACH_WEAPON_BONE_NAME, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_STATIC_ATTACH_WEAPON_BONE_NAME );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//EDIT_WEAPON_ROT_X
	GET_LUA_POS( EDIT_WEAPON_ROT_X, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_EDIT_WEAPON_ROT_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_WEAPON_ROT_X
	GET_LUA_POS( STATIC_WEAPON_ROT_X, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_STATIC_WEAPON_ROT_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );


	//EDIT_WEAPON_ROT_Y
	GET_LUA_POS( EDIT_WEAPON_ROT_Y, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_EDIT_WEAPON_ROT_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_WEAPON_ROT_Y
	GET_LUA_POS( STATIC_WEAPON_ROT_Y, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_STATIC_WEAPON_ROT_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );


	//EDIT_WEAPON_ROT_Z
	GET_LUA_POS( EDIT_WEAPON_ROT_Z, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_EDIT_WEAPON_ROT_Z );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_WEAPON_ROT_Z
	GET_LUA_POS( STATIC_WEAPON_ROT_Z, nX, nY, nW, nH );
	pControl = m_Unit.GetControl( UI_STATIC_WEAPON_ROT_Z );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

    //BUT_ATTACH_ACCESSORY
    GET_LUA_POS( BUT_ATTACH_ACCESSORY, nX, nY, nW, nH );
    pControl = m_Unit.GetControl( UI_BUT_ATTACH_ACCESSORY );
    pControl->SetLocation( nX, nY );
    pControl->SetSize( nW, nH );

    //EDIT_ATTACH_ACCESSORY_BONE_NAME
    GET_LUA_POS( EDIT_ATTACH_ACCESSORY_BONE_NAME, nX, nY, nW, nH );
    pControl = m_Unit.GetControl( UI_EDIT_ATTACH_ACCESSORY_BONE_NAME );
    pControl->SetLocation( nX, nY );
    pControl->SetSize( nW, nH );

    //STATIC_ATTACH_ACCESSORY_BONE_NAME
    GET_LUA_POS( STATIC_ATTACH_ACCESSORY_BONE_NAME, nX, nY, nW, nH );
    pControl = m_Unit.GetControl( UI_STATIC_ATTACH_ACCESSORY_BONE_NAME );
    pControl->SetLocation( nX, nY );
    pControl->SetSize( nW, nH );

    //EDIT_ACCESSORY_TRANS_X
    GET_LUA_POS( EDIT_ACCESSORY_TRANS_X, nX, nY, nW, nH );
    pControl = m_Unit.GetControl( UI_EDIT_ACCESSORY_TRANS_X );
    pControl->SetLocation( nX, nY );
    pControl->SetSize( nW, nH );

    //STATIC_ACCESSORY_TRANS_X
    GET_LUA_POS( STATIC_ACCESSORY_TRANS_X, nX, nY, nW, nH );
    pControl = m_Unit.GetControl( UI_STATIC_ACCESSORY_TRANS_X );
    pControl->SetLocation( nX, nY );
    pControl->SetSize( nW, nH );


    //EDIT_ACCESSORY_TRANS_Y
    GET_LUA_POS( EDIT_ACCESSORY_TRANS_Y, nX, nY, nW, nH );
    pControl = m_Unit.GetControl( UI_EDIT_ACCESSORY_TRANS_Y );
    pControl->SetLocation( nX, nY );
    pControl->SetSize( nW, nH );

    //STATIC_ACCESSORY_TRANS_Y
    GET_LUA_POS( STATIC_ACCESSORY_TRANS_Y, nX, nY, nW, nH );
    pControl = m_Unit.GetControl( UI_STATIC_ACCESSORY_TRANS_Y );
    pControl->SetLocation( nX, nY );
    pControl->SetSize( nW, nH );


    //EDIT_ACCESSORY_TRANS_Z
    GET_LUA_POS( EDIT_ACCESSORY_TRANS_Z, nX, nY, nW, nH );
    pControl = m_Unit.GetControl( UI_EDIT_ACCESSORY_TRANS_Z );
    pControl->SetLocation( nX, nY );
    pControl->SetSize( nW, nH );

    //STATIC_ACCESSORY_TRANS_Z
    GET_LUA_POS( STATIC_ACCESSORY_TRANS_Z, nX, nY, nW, nH );
    pControl = m_Unit.GetControl( UI_STATIC_ACCESSORY_TRANS_Z );
    pControl->SetLocation( nX, nY );
    pControl->SetSize( nW, nH );
    


    GET_LUA_POS( STATIC_ACCESSORY_SCALE, nX, nY, nW, nH );
    pControl = m_Unit.GetControl( UI_STATIC_ACCESSORY_SCALE );
    pControl->SetLocation( nX, nY );
    pControl->SetSize( nW, nH );

    GET_LUA_POS( STATIC_ACCESSORY_ROTATE, nX, nY, nW, nH );
    pControl = m_Unit.GetControl( UI_STATIC_ACCESSORY_ROTATE );
    pControl->SetLocation( nX, nY );
    pControl->SetSize( nW, nH );


    GET_LUA_POS( EDIT_ACCESSORY_SCALE_X, nX, nY, nW, nH );
    pControl = m_Unit.GetControl( UI_EDIT_ACCESSORY_SCALE_X );
    pControl->SetLocation( nX, nY );
    pControl->SetSize( nW, nH );
    GET_LUA_POS( EDIT_ACCESSORY_SCALE_Y, nX, nY, nW, nH );
    pControl = m_Unit.GetControl( UI_EDIT_ACCESSORY_SCALE_Y );
    pControl->SetLocation( nX, nY );
    pControl->SetSize( nW, nH );
    GET_LUA_POS( EDIT_ACCESSORY_SCALE_Z, nX, nY, nW, nH );
    pControl = m_Unit.GetControl( UI_EDIT_ACCESSORY_SCALE_Z );
    pControl->SetLocation( nX, nY );
    pControl->SetSize( nW, nH );
    GET_LUA_POS( EDIT_ACCESSORY_ROTATE_X, nX, nY, nW, nH );
    pControl = m_Unit.GetControl( UI_EDIT_ACCESSORY_ROTATE_X );
    pControl->SetLocation( nX, nY );
    pControl->SetSize( nW, nH );
    GET_LUA_POS( EDIT_ACCESSORY_ROTATE_Y, nX, nY, nW, nH );
    pControl = m_Unit.GetControl( UI_EDIT_ACCESSORY_ROTATE_Y );
    pControl->SetLocation( nX, nY );
    pControl->SetSize( nW, nH );
    GET_LUA_POS( EDIT_ACCESSORY_ROTATE_Z, nX, nY, nW, nH );
    pControl = m_Unit.GetControl( UI_EDIT_ACCESSORY_ROTATE_Z );
    pControl->SetLocation( nX, nY );
    pControl->SetSize( nW, nH );



	//////////////////////////////////////////////////////////////////////////
	// Mesh Seting
	//STATIC_MESH_SCALE
	GET_LUA_POS( STATIC_MESH_SCALE, nX, nY, nW, nH );
	pControl = m_Mesh.GetControl( UI_STATIC_MESH_SCALE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_MESH_SCALE_X
	GET_LUA_POS( STATIC_MESH_SCALE_X, nX, nY, nW, nH );
	pControl = m_Mesh.GetControl( UI_STATIC_MESH_SCALE_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_MESH_SCALE_X
	GET_LUA_POS( EDIT_MESH_SCALE_X, nX, nY, nW, nH );
	pControl = m_Mesh.GetControl( UI_EDIT_MESH_SCALE_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_MESH_SCALE_Y
	GET_LUA_POS( STATIC_MESH_SCALE_Y, nX, nY, nW, nH );
	pControl = m_Mesh.GetControl( UI_STATIC_MESH_SCALE_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_MESH_SCALE_Y
	GET_LUA_POS( EDIT_SCALE_Y, nX, nY, nW, nH );
	pControl = m_Mesh.GetControl( UI_EDIT_MESH_SCALE_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_MESH_SCALE_Z
	GET_LUA_POS( STATIC_MESH_SCALE_Z, nX, nY, nW, nH );
	pControl = m_Mesh.GetControl( UI_STATIC_MESH_SCALE_Z );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_MESH_SCALE_Z
	GET_LUA_POS( EDIT_MESH_SCALE_Z, nX, nY, nW, nH );
	pControl = m_Mesh.GetControl( UI_EDIT_MESH_SCALE_Z );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	vTemp = ((CX2ViewerMesh*)SiGetObject( OS_MESH ))->GetScale();
	pEditBox = NULL;
	swprintf( strNum, L"%.2f", vTemp.x );
	pEditBox = m_Mesh.GetEditBox( UI_EDIT_MESH_SCALE_X );
	pEditBox->SetText( strNum );
	swprintf( strNum, L"%.2f", vTemp.y );
	pEditBox = m_Mesh.GetEditBox( UI_EDIT_MESH_SCALE_Y );
	pEditBox->SetText( strNum );
	swprintf( strNum, L"%.2f", vTemp.z );
	pEditBox = m_Mesh.GetEditBox( UI_EDIT_MESH_SCALE_Z );
	pEditBox->SetText( strNum );

	//STATIC_MESH_LIGHT_POS
	GET_LUA_POS( STATIC_MESH_LIGHT_POS, nX, nY, nW, nH );
	pControl = m_Mesh.GetControl( UI_STATIC_MESH_LIGHT_POS );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_MESH_LIGHT_POS_X
	GET_LUA_POS( STATIC_MESH_LIGHT_POS_X, nX, nY, nW, nH );
	pControl = m_Mesh.GetControl( UI_STATIC_MESH_LIGHT_POS_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_MESH_SCALE_X
	GET_LUA_POS( EDIT_MESH_LIGHT_POS_X, nX, nY, nW, nH );
	pControl = m_Mesh.GetControl( UI_EDIT_MESH_LIGHT_POS_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_MESH_LIGHT_POS_Y
	GET_LUA_POS( STATIC_MESH_LIGHT_POS_Y, nX, nY, nW, nH );
	pControl = m_Mesh.GetControl( UI_STATIC_MESH_LIGHT_POS_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_MESH_LIGHT_POS_Y
	GET_LUA_POS( EDIT_MESH_LIGHT_POS_Y, nX, nY, nW, nH );
	pControl = m_Mesh.GetControl( UI_EDIT_MESH_LIGHT_POS_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_MESH_LIGHT_POS_Z
	GET_LUA_POS( STATICMESH__LIGHT_POS_Z, nX, nY, nW, nH );
	pControl = m_Mesh.GetControl( UI_STATIC_MESH_LIGHT_POS_Z );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_MESH_LIGHT_POS_Z
	GET_LUA_POS( EDIT_MESH_LIGHT_POS_Z, nX, nY, nW, nH );
	pControl = m_Mesh.GetControl( UI_EDIT_MESH_LIGHT_POS_Z );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//CHECK_MESH_LIGHT_ONOFF
	GET_LUA_POS( CHECK_MESH_LIGHT_ONOFF, nX, nY, nW, nH );
	pControl = m_Mesh.GetControl( UI_CHECK_MESH_LIGHT_ONOFF );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	vTemp = ((CX2ViewerMesh*)SiGetObject( OS_MESH ))->GetLightPos();
	pEditBox = NULL;
	swprintf( strNum, L"%.2f", vTemp.x );
	pEditBox = m_Mesh.GetEditBox( UI_EDIT_MESH_LIGHT_POS_X );
	pEditBox->SetText( strNum );
	swprintf( strNum, L"%.2f", vTemp.y );
	pEditBox = m_Mesh.GetEditBox( UI_EDIT_MESH_LIGHT_POS_Y );
	pEditBox->SetText( strNum );
	swprintf( strNum, L"%.2f", vTemp.z );
	pEditBox = m_Mesh.GetEditBox( UI_EDIT_MESH_LIGHT_POS_Z );
	pEditBox->SetText( strNum );

	//BUT_MESH_RENDER_PARAM
	GET_LUA_POS( BUT_MESH_RENDER_PARAM, nX, nY, nW, nH );
	pControl = m_Mesh.GetControl( UI_BUT_MESH_RENDER_PARAM );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	// Mesh Seting End.!
	//////////////////////////////////////////////////////////////////////////

	//////////////////////////////////////////////////////////////////////////
	// RENDER PARAM SETING
	//STATIC_RENDERTYPE
	GET_LUA_POS( STATIC_RENDERTYPE, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_RENDERTYPE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//COMBO_RENDERTYPE
	GET_LUA_POS( COMBO_RENDERTYPE, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_COMBO_RENDERTYPE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_CARTOON_TEX_TYPE
	GET_LUA_POS( STATIC_CARTOON_TEX_TYPE, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_CARTOON_TEX_TYPE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//COMBO_CARTOON_TEX_TYPE
	GET_LUA_POS( COMBO_CARTOON_TEX_TYPE, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_COMBO_CARTOON_TEX_TYPE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_OUTLINE_WIDE
	GET_LUA_POS( STATIC_OUTLINE_WIDE, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_OUTLINE_WIDE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//EDIT_OUTLINE_WIDE
	GET_LUA_POS( EDIT_OUTLINE_WIDE, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_OUTLINE_WIDE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTEditBox*)pControl)->SetText( L"" );

	//STATIC_OUTLINE_COLOR
	GET_LUA_POS( STATIC_OUTLINE_COLOR, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_OUTLINE_COLOR );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_OUTLINE_COLOR_A
	GET_LUA_POS( STATIC_OUTLINE_COLOR_A, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_OUTLINE_COLOR_A );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_OUTLINE_COLOR_R
	GET_LUA_POS( STATIC_OUTLINE_COLOR_R, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_OUTLINE_COLOR_R );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_OUTLINE_COLOR_G
	GET_LUA_POS( STATIC_OUTLINE_COLOR_G, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_OUTLINE_COLOR_G );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_OUTLINE_COLOR_B
	GET_LUA_POS( STATIC_OUTLINE_COLOR_B, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_OUTLINE_COLOR_B );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//EDIT_OUTLINE_COLOR_A
	GET_LUA_POS( EDIT_OUTLINE_COLOR_A, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_OUTLINE_COLOR_A );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTEditBox*)pControl)->SetText( L"" );
	//EDIT_OUTLINE_COLOR_R
	GET_LUA_POS( EDIT_OUTLINE_COLOR_R, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_OUTLINE_COLOR_R );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTEditBox*)pControl)->SetText( L"" );
	//EDIT_OUTLINE_COLOR_G
	GET_LUA_POS( EDIT_OUTLINE_COLOR_G, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_OUTLINE_COLOR_G );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTEditBox*)pControl)->SetText( L"" );
	//EDIT_OUTLINE_COLOR_B
	GET_LUA_POS( EDIT_OUTLINE_COLOR_B, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_OUTLINE_COLOR_B );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTEditBox*)pControl)->SetText( L"" );

	//STATIC_COLOR
	GET_LUA_POS( STATIC_COLOR, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_COLOR );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_COLOR_A
	GET_LUA_POS( STATIC_COLOR_A, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_COLOR_A );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_COLOR_R
	GET_LUA_POS( STATIC_COLOR_R, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_COLOR_R );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_COLOR_G
	GET_LUA_POS( STATIC_COLOR_G, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_COLOR_G );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_COLOR_B
	GET_LUA_POS( STATIC_COLOR_B, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_COLOR_B );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//EDIT_COLOR_A
	GET_LUA_POS( EDIT_COLOR_A, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_COLOR_A );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTEditBox*)pControl)->SetText( L"" );
	//EDIT_COLOR_R
	GET_LUA_POS( EDIT_COLOR_R, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_COLOR_R );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTEditBox*)pControl)->SetText( L"" );
	//EDIT_COLOR_G
	GET_LUA_POS( EDIT_COLOR_G, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_COLOR_G );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTEditBox*)pControl)->SetText( L"" );
	//EDIT_COLOR_B
	GET_LUA_POS( EDIT_COLOR_B, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_COLOR_B );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTEditBox*)pControl)->SetText( L"" );

	//STATIC_LIGHTFLOW_WIDE
	GET_LUA_POS( STATIC_LIGHTFLOW_WIDE, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_LIGHTFLOW_WIDE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_LIGHTFLOW_WIDE
	GET_LUA_POS( EDIT_LIGHTFLOW_WIDE, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_LIGHTFLOW_WIDE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTEditBox*)pControl)->SetText( L"" );

	//STATIC_LIGHTFLOW_IMPACT
	GET_LUA_POS( STATIC_LIGHTFLOW_IMPACT, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_LIGHTFLOW_IMPACT );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_LIGHTFLOW_IMPACT_MIN
	GET_LUA_POS( STATIC_LIGHTFLOW_IMPACT_MIN, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_LIGHTFLOW_IMPACT_MIN );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_LIGHTFLOW_IMPACT_MAX
	GET_LUA_POS( STATIC_LIGHTFLOW_IMPACT_MAX, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_LIGHTFLOW_IMPACT_MAX );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_LIGHTFLOW_IMPACT_ANIMTIME
	GET_LUA_POS( STATIC_LIGHTFLOW_IMPACT_ANIMTIME, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_LIGHTFLOW_IMPACT_ANIMTIME );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_LIGHTFLOW_IMPACT_MIN
	GET_LUA_POS( EDIT_LIGHTFLOW_IMPACT_MIN, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_LIGHTFLOW_IMPACT_MIN );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTEditBox*)pControl)->SetText( L"" );
	//EDIT_LIGHTFLOW_IMPACT_MAX
	GET_LUA_POS( EDIT_LIGHTFLOW_IMPACT_MAX, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_LIGHTFLOW_IMPACT_MAX );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTEditBox*)pControl)->SetText( L"" );
	//EDIT_LIGHTFLOW_IMPACT_ANIMTIME
	GET_LUA_POS( EDIT_LIGHTFLOW_IMPACT_ANIMTIME, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_LIGHTFLOW_IMPACT_ANIMTIME );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTEditBox*)pControl)->SetText( L"" );

	//STATIC_LIGHTFLOW_POINT
	GET_LUA_POS( STATIC_LIGHTFLOW_POINT, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_LIGHTFLOW_POINT );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_LIGHTFLOW_POINT_X
	GET_LUA_POS( STATIC_LIGHTFLOW_POINT_X, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_LIGHTFLOW_POINT_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_LIGHTFLOW_POINT_Y
	GET_LUA_POS( STATIC_LIGHTFLOW_POINT_Y, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_LIGHTFLOW_POINT_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_LIGHTFLOW_POINT_Z
	GET_LUA_POS( STATIC_LIGHTFLOW_POINT_Z, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_LIGHTFLOW_POINT_Z );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_LIGHTFLOW_POINT_X
	GET_LUA_POS( EDIT_LIGHTFLOW_POINT_X, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_LIGHTFLOW_POINT_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTEditBox*)pControl)->SetText( L"" );
	//EDIT_LIGHTFLOW_POINT_Y
	GET_LUA_POS( EDIT_LIGHTFLOW_POINT_Y, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_LIGHTFLOW_POINT_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTEditBox*)pControl)->SetText( L"" );
	//EDIT_LIGHTFLOW_POINT_Z
	GET_LUA_POS( EDIT_LIGHTFLOW_POINT_Z, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_LIGHTFLOW_POINT_Z );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	((CDXUTEditBox*)pControl)->SetText( L"" );

	//STATIC_TEXOFFSET_STAGE0
	GET_LUA_POS( STATIC_TEXOFFSET_STAGE0, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_TEXOFFSET_STAGE0 );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_TEXOFFSET_STAGE0_X
	GET_LUA_POS( STATIC_TEXOFFSET_STAGE0_X, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_TEXOFFSET_STAGE0_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_TEXOFFSET_STAGE0_Y
	GET_LUA_POS( STATIC_TEXOFFSET_STAGE0_Y, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_TEXOFFSET_STAGE0_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_TEXOFFSET_STAGE0_MIN
	GET_LUA_POS( STATIC_TEXOFFSET_STAGE0_MIN, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_TEXOFFSET_STAGE0_MIN );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_TEXOFFSET_STAGE0_MAX
	GET_LUA_POS( STATIC_TEXOFFSET_STAGE0_MAX, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_TEXOFFSET_STAGE0_MAX );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_TEXOFFSET_STAGE0_ANIMTIME
	GET_LUA_POS( STATIC_TEXOFFSET_STAGE0_ANIMTIME, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_TEXOFFSET_STAGE0_ANIMTIME );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_TEXOFFSET_STAGE0_MIN_X
	GET_LUA_POS( EDIT_TEXOFFSET_STAGE0_MIN_X, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_TEXOFFSET_STAGE0_MIN_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_TEXOFFSET_STAGE0_MIN_Y
	GET_LUA_POS( EDIT_TEXOFFSET_STAGE0_MIN_Y, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_TEXOFFSET_STAGE0_MIN_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_TEXOFFSET_STAGE0_MAX_X
	GET_LUA_POS( EDIT_TEXOFFSET_STAGE0_MAX_X, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_TEXOFFSET_STAGE0_MAX_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_TEXOFFSET_STAGE0_MAX_Y
	GET_LUA_POS( EDIT_TEXOFFSET_STAGE0_MAX_Y, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_TEXOFFSET_STAGE0_MAX_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_TEXOFFSET_STAGE0_ANIMTIME
	GET_LUA_POS( EDIT_TEXOFFSET_STAGE0_ANIMTIME, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_TEXOFFSET_STAGE0_ANIMTIME );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_TEXOFFSET_STAGE1
	GET_LUA_POS( STATIC_TEXOFFSET_STAGE1, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_TEXOFFSET_STAGE1 );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_TEXOFFSET_STAGE1_X
	GET_LUA_POS( STATIC_TEXOFFSET_STAGE1_X, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_TEXOFFSET_STAGE1_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_TEXOFFSET_STAGE1_Y
	GET_LUA_POS( STATIC_TEXOFFSET_STAGE1_Y, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_TEXOFFSET_STAGE1_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_TEXOFFSET_STAGE1_MIN
	GET_LUA_POS( STATIC_TEXOFFSET_STAGE1_MIN, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_TEXOFFSET_STAGE1_MIN );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_TEXOFFSET_STAGE1_MAX
	GET_LUA_POS( STATIC_TEXOFFSET_STAGE1_MAX, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_TEXOFFSET_STAGE1_MAX );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_TEXOFFSET_STAGE1_ANIMTIME
	GET_LUA_POS( STATIC_TEXOFFSET_STAGE1_ANIMTIME, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_TEXOFFSET_STAGE1_ANIMTIME );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_TEXOFFSET_STAGE1_MIN_X
	GET_LUA_POS( EDIT_TEXOFFSET_STAGE1_MIN_X, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_TEXOFFSET_STAGE1_MIN_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_TEXOFFSET_STAGE1_MIN_Y
	GET_LUA_POS( EDIT_TEXOFFSET_STAGE1_MIN_Y, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_TEXOFFSET_STAGE1_MIN_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_TEXOFFSET_STAGE1_MAX_X
	GET_LUA_POS( EDIT_TEXOFFSET_STAGE1_MAX_X, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_TEXOFFSET_STAGE1_MAX_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_TEXOFFSET_STAGE1_MAX_Y
	GET_LUA_POS( EDIT_TEXOFFSET_STAGE1_MAX_Y, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_TEXOFFSET_STAGE1_MAX_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_TEXOFFSET_STAGE1_ANIMTIME
	GET_LUA_POS( EDIT_TEXOFFSET_STAGE1_ANIMTIME, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_TEXOFFSET_STAGE1_ANIMTIME );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_TEXOFFSET_STAGE2
	GET_LUA_POS( STATIC_TEXOFFSET_STAGE2, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_TEXOFFSET_STAGE2 );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_TEXOFFSET_STAGE2_X
	GET_LUA_POS( STATIC_TEXOFFSET_STAGE2_X, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_TEXOFFSET_STAGE2_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_TEXOFFSET_STAGE2_Y
	GET_LUA_POS( STATIC_TEXOFFSET_STAGE2_Y, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_TEXOFFSET_STAGE2_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_TEXOFFSET_STAGE2_MIN
	GET_LUA_POS( STATIC_TEXOFFSET_STAGE2_MIN, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_TEXOFFSET_STAGE2_MIN );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_TEXOFFSET_STAGE2_MAX
	GET_LUA_POS( STATIC_TEXOFFSET_STAGE2_MAX, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_TEXOFFSET_STAGE2_MAX );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//STATIC_TEXOFFSET_STAGE2_ANIMTIME
	GET_LUA_POS( STATIC_TEXOFFSET_STAGE2_ANIMTIME, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_TEXOFFSET_STAGE2_ANIMTIME );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_TEXOFFSET_STAGE2_MIN_X
	GET_LUA_POS( EDIT_TEXOFFSET_STAGE2_MIN_X, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_TEXOFFSET_STAGE2_MIN_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_TEXOFFSET_STAGE2_MIN_Y
	GET_LUA_POS( EDIT_TEXOFFSET_STAGE2_MIN_Y, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_TEXOFFSET_STAGE2_MIN_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_TEXOFFSET_STAGE2_MAX_X
	GET_LUA_POS( EDIT_TEXOFFSET_STAGE2_MAX_X, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_TEXOFFSET_STAGE2_MAX_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_TEXOFFSET_STAGE2_MAX_Y
	GET_LUA_POS( EDIT_TEXOFFSET_STAGE2_MAX_Y, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_TEXOFFSET_STAGE2_MAX_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//EDIT_TEXOFFSET_STAGE2_ANIMTIME
	GET_LUA_POS( EDIT_TEXOFFSET_STAGE2_ANIMTIME, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_EDIT_TEXOFFSET_STAGE2_ANIMTIME );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_ALPHA_BLEND
	GET_LUA_POS( STATIC_ALPHA_BLEND, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_ALPHA_BLEND );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//COMBO_ALPHA_BLEND
	GET_LUA_POS( COMBO_ALPHA_BLEND, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_COMBO_ALPHA_BLEND );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_ZENABLE
	GET_LUA_POS( STATIC_ZENABLE, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_ZENABLE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//COMBO_ZENABLE
	GET_LUA_POS( COMBO_ZENABLE, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_COMBO_ZENABLE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_CULLMODE
	GET_LUA_POS( STATIC_CULLMODE, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_CULLMODE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//COMBO_CULLMODE
	GET_LUA_POS( COMBO_CULLMODE, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_COMBO_CULLMODE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_SRCBLEND
	GET_LUA_POS( STATIC_SRCBLEND, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_SRCBLEND );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//COMBO_SRCBLEND
	GET_LUA_POS( COMBO_SRCBLEND, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_COMBO_SRCBLEND );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//STATIC_DESTBLEND
	GET_LUA_POS( STATIC_DESTBLEND, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_STATIC_DESTBLEND );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//COMBO_DESTBLEND
	GET_LUA_POS( COMBO_DESTBLEND, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_COMBO_DESTBLEND );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//BUT_PARAM_OK
	GET_LUA_POS( BUT_PARAM_OK, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_BUT_PARAM_OK );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	//BUT_PARAM_CANCEL
	GET_LUA_POS( BUT_PARAM_CANCEL, nX, nY, nW, nH );
	pControl = m_RenderParam.GetControl( UI_BUT_PARAM_CANCEL );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );    

	//////////////////////////////////////////////////////////////////////////
	// Particle : 모델뷰 화면에서 나타나는 파티클 관련 UI
	GET_LUA_POS( LIST_PARTICLE_LIST, nX, nY, nW, nH );
	pControl = m_ParticleBasic.GetControl( UI_LIST_PARTICLE_LIST );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	
	GET_LUA_POS( BUT_PARTICLE_DELETE, nX, nY, nW, nH );
	pControl = m_ParticleBasic.GetControl( UI_BUT_PARTICLE_DELETE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( STATIC_PARTICLE_TIME, nX, nY, nW, nH );
	pControl = m_ParticleBasic.GetControl( UI_STATIC_PARTICLE_TIME );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	
	GET_LUA_POS( EDIT_PARTICLE_TIME, nX, nY, nW, nH );
	pControl = m_ParticleBasic.GetControl( UI_EDIT_PARTICLE_TIME );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( BUT_PARTICLE_BONESET, nX, nY, nW, nH );
	pControl = m_ParticleBasic.GetControl( UI_BUT_PARTICLE_BONESET );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( BUT_PARTICLE_BONECLEAR, nX, nY, nW, nH );
	pControl = m_ParticleBasic.GetControl( UI_BUT_PARTICLE_BONECLEAR );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( CHECK_PARTICLE_TRACE, nX, nY, nW, nH );
	pControl = m_ParticleBasic.GetControl( UI_CHECK_PARTICLE_TRACE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( STATIC_PARTICLE_OFFSET, nX, nY, nW, nH );
	pControl = m_ParticleBasic.GetControl( UI_STATIC_PARTICLE_OFFSET );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( EDIT_PARTICLE_OFFSET_X, nX, nY, nW, nH );
	pControl = m_ParticleBasic.GetControl( UI_EDIT_PARTICLE_OFFSET_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( EDIT_PARTICLE_OFFSET_Y, nX, nY, nW, nH );
	pControl = m_ParticleBasic.GetControl( UI_EDIT_PARTICLE_OFFSET_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( EDIT_PARTICLE_OFFSET_Z, nX, nY, nW, nH );
	pControl = m_ParticleBasic.GetControl( UI_EDIT_PARTICLE_OFFSET_Z );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( CHECK_PARTICLE_LANDPOS, nX, nY, nW, nH );
	pControl = m_ParticleBasic.GetControl( UI_CHECK_PARTICLE_LANDPOS );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( STATIC_PARTICLE_ROT, nX, nY, nW, nH );
	pControl = m_ParticleBasic.GetControl( UI_STATIC_PARTICLE_ROT );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );


	GET_LUA_POS( EDIT_PARTICLE_ROT_X, nX, nY, nW, nH );
	pControl = m_ParticleBasic.GetControl( UI_EDIT_PARTICLE_ROT_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );


	GET_LUA_POS( EDIT_PARTICLE_ROT_Y, nX, nY, nW, nH );
	pControl = m_ParticleBasic.GetControl( UI_EDIT_PARTICLE_ROT_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );


	GET_LUA_POS( EDIT_PARTICLE_ROT_Z, nX, nY, nW, nH );
	pControl = m_ParticleBasic.GetControl( UI_EDIT_PARTICLE_ROT_Z );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( CHECK_PARTICLE_APPUNITROT, nX, nY, nW, nH );
	pControl = m_ParticleBasic.GetControl( UI_CHECK_PARTICLE_APPUNITROT );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( BUT_PARTICLE_SAVESEQUENCE, nX, nY, nW, nH );
	pControl = m_ParticleBasic.GetControl( UI_BUT_PARTICLE_SAVESEQUENCE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );	

	GET_LUA_POS( BUT_PARTICLE_PARTICLEEDITOR, nX, nY, nW, nH );
	pControl = m_ParticleBasic.GetControl( UI_BUT_PARTICLE_PARTICLEEDITOR );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	//////////////////////////////////////////////////////////////////////////
	// Particle Editor UI
	GET_LUA_POS( LIST_PARTICLE_EDITOR_MYPARTICLE, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_LIST_PARTICLE_EDITOR_MYPARTICLE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	
	GET_LUA_POS( BUT_PARTICLE_EDITOR_MYPARTICLE_LOAD, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_BUT_PARTICLE_EDITOR_MYPARTICLE_LOAD );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( BUT_PARTICLE_EDITOR_MYPARTICLE_DELETE, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_BUT_PARTICLE_EDITOR_MYPARTICLE_DELETE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( BUT_PARTICLE_EDITOR_MYPARTICLE_SAVE, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_BUT_PARTICLE_EDITOR_MYPARTICLE_SAVE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( LIST_PARTICLE_EDITOR_PARTICLETEMPLET, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_LIST_PARTICLE_EDITOR_PARTICLETEMPLET );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
		
	GET_LUA_POS( BUT_PARTICLE_EDITOR_PARTICLETEMPLET_COPY, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_BUT_PARTICLE_EDITOR_PARTICLETEMPLET_COPY );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( BUT_PARTICLE_EDITOR_PARTICLETEMPLET_RELOAD, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_BUT_PARTICLE_EDITOR_PARTICLETEMPLET_RELOAD );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );	

	GET_LUA_POS( STATIC_PARTICLE_EDITOR_EMITTERATTRIBUTE, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_STATIC_PARTICLE_EDITOR_EMITTERATTRIBUTE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( LIST_PARTICLE_EDITOR_EMITTERATTRIBUTE, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_LIST_PARTICLE_EDITOR_EMITTERATTRIBUTE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	
	GET_LUA_POS( STATIC_PARTICLE_EDITOR_EMITTERATTRIBUTE_VALUE, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_STATIC_PARTICLE_EDITOR_EMITTERATTRIBUTE_VALUE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	
	GET_LUA_POS( EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_SINGLE, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_SINGLE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	
	GET_LUA_POS( EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_X, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_X );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );
	

	GET_LUA_POS( EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_Y, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_Y );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_Z, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_Z );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( COMBO_PARTICLE_EDITOR_EMITTERATTRIBUTE, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_COMBO_PARTICLE_EDITOR_EMITTERATTRIBUTE );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );

// 	GET_LUA_POS( BUT_PARTICLE_EDITOR_EMITTERATTRIBUTE_DEFAULT, nX, nY, nW, nH );
// 	pControl = m_ParticleEditor.GetControl( UI_BUT_PARTICLE_EDITOR_EMITTERATTRIBUTE_DEFAULT );
// 	pControl->SetLocation( nX, nY );
// 	pControl->SetSize( nW, nH );

	GET_LUA_POS( BUT_PARTICLE_EDITOR_EMITTERATTRIBUTE_APPLY, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_BUT_PARTICLE_EDITOR_EMITTERATTRIBUTE_APPLY );
	pControl->SetLocation( nX, nY );
	pControl->SetSize( nW, nH );


	GET_LUA_POS( LIST_PARTICLE_EDITOR_EVENT, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_LIST_PARTICLE_EDITOR_EVENT );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( BUT_PARTICLE_EDITOR_EVENT_DELETE, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_BUT_PARTICLE_EDITOR_EVENT_DELETE );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( STATIC_PARTICLE_EDITOR_EVENT_TYPE, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_STATIC_PARTICLE_EDITOR_EVENT_TYPE );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( LIST_PARTICLE_EDITOR_EVENT_TYPE, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_LIST_PARTICLE_EDITOR_EVENT_TYPE );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( STATIC_PARTICLE_EDITOR_EVENT_TIME, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_STATIC_PARTICLE_EDITOR_EVENT_TIME );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( CHECK_PARTICLE_EDITOR_EVENT_FADE, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_CHECK_PARTICLE_EDITOR_EVENT_FADE );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( STATIC_PARTICLE_EDITOR_EVENT_FROM, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_STATIC_PARTICLE_EDITOR_EVENT_FROM );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( STATIC_PARTICLE_EDITOR_EVENT_TO, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_STATIC_PARTICLE_EDITOR_EVENT_TO );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( EDIT_PARTICLE_EDITOR_EVENT_TIME1, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_EDIT_PARTICLE_EDITOR_EVENT_TIME1 );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( EDIT_PARTICLE_EDITOR_EVENT_TIME2, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_EDIT_PARTICLE_EDITOR_EVENT_TIME2 );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( STATIC_PARTICLE_EDITOR_EVENT_VALUE, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_STATIC_PARTICLE_EDITOR_EVENT_VALUE );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( EDIT_PARTICLE_EDITOR_EVENT_SINGLE, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_EDIT_PARTICLE_EDITOR_EVENT_SINGLE );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( EDIT_PARTICLE_EDITOR_EVENT_X, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_EDIT_PARTICLE_EDITOR_EVENT_X );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( EDIT_PARTICLE_EDITOR_EVENT_Y, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_EDIT_PARTICLE_EDITOR_EVENT_Y );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( EDIT_PARTICLE_EDITOR_EVENT_Z, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_EDIT_PARTICLE_EDITOR_EVENT_Z );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( COMBO_PARTICLE_EDITOR_EVENT, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_COMBO_PARTICLE_EDITOR_EVENT );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( EDIT_PARTICLE_EDITOR_EVENT_R, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_EDIT_PARTICLE_EDITOR_EVENT_R );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( EDIT_PARTICLE_EDITOR_EVENT_G, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_EDIT_PARTICLE_EDITOR_EVENT_G );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( EDIT_PARTICLE_EDITOR_EVENT_B, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_EDIT_PARTICLE_EDITOR_EVENT_B );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( EDIT_PARTICLE_EDITOR_EVENT_A, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_EDIT_PARTICLE_EDITOR_EVENT_A );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( BUT_PARTICLE_EDITOR_EVENT_APPLY, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_BUT_PARTICLE_EDITOR_EVENT_APPLY );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( BUT_PARTICLE_EDITOR_EVENT_NEW, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_BUT_PARTICLE_EDITOR_EVENT_NEW );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( LIST_PARTICLE_EDITOR_MODEL_LIST, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_LIST_PARTICLE_EDITOR_MODEL_LIST );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( BUT_PARTICLE_EDITOR_MODEL_ADD, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_BUT_PARTICLE_EDITOR_MODEL_ADD );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( BUT_PARTICLE_EDITOR_MODEL_DELETE, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_BUT_PARTICLE_EDITOR_MODEL_DELETE );
	pControl->SetLocation( rt.right - 1024 + nX, nY );
	pControl->SetSize( nW, nH );
	
	GET_LUA_POS( STATIC_PARTICLE_EDITOR_PLAY_TYPE, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_STATIC_PARTICLE_EDITOR_PLAY_TYPE );
	pControl->SetLocation( rt.right - 1024 + nX, rt.bottom -768 + nY );
	pControl->SetSize( nW, nH );
	GET_LUA_POS( COMBO_PARTICLE_EDITOR_PLAY_TYPE, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_COMBO_PARTICLE_EDITOR_PLAY_TYPE );
	pControl->SetLocation( rt.right - 1024 + nX, rt.bottom -768 + nY );
	pControl->SetSize( nW, nH );
	GET_LUA_POS( BUT_PARTICLE_EDITOR_PLAY, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_BUT_PARTICLE_EDITOR_PLAY );
	pControl->SetLocation( rt.right - 1024 + nX, rt.bottom -768 + nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( STATIC_PARTICLE_EDITOR_SPEED, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_STATIC_PARTICLE_EDITOR_SPEED );
	pControl->SetLocation( rt.right - 1024 + nX, rt.bottom -768 + nY );
	pControl->SetSize( nW, nH );
	
	GET_LUA_POS( SLIDE_PARTICLE_EDITOR_SPEED, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_SLIDE_PARTICLE_EDITOR_SPEED );
	pControl->SetLocation( rt.right - 1024 + nX, rt.bottom -768 + nY );
	pControl->SetSize( nW, nH );
	
	GET_LUA_POS( STATIC_PARTICLE_EDITOR_TIME, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_STATIC_PARTICLE_EDITOR_TIME );
	pControl->SetLocation( rt.right - 1024 + nX, rt.bottom -768 + nY );
	pControl->SetSize( nW, nH );

	GET_LUA_POS( BUT_PARTICLE_EDITOR_EXIT, nX, nY, nW, nH );
	pControl = m_ParticleEditor.GetControl( UI_BUT_PARTICLE_EDITOR_EXIT );
	pControl->SetLocation( rt.right - 1024 + nX, rt.bottom -768 + nY );
	pControl->SetSize( nW, nH );

	//////////////////////////////////////////////////////////////////////////
	
	m_Param.SetParamDlg( &m_RenderParam );

	SiMain()->SetSelectedAnimIndex( -1 );
}

void CX2ViewerUI::DropFile( std::wstring fileName, std::wstring dir )
{
	CX2ViewerSkinMesh*	pSkinMesh	= (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
	CX2ViewerMesh*		pMesh		= (CX2ViewerMesh*)SiGetObject( OS_MESH );

	if( pSkinMesh == NULL && pMesh == NULL )
	{
		WARNINGMSG( L"Skin Mesh/Mesh 가져오기 실패(CX2ViewerUI::DropFile():538) 안돼.." );
		return;
	}

	// 해당 파일이 있는 폴더를 Data Dir에 추가
	std::string strDir;
	ConvertWCHARToChar( strDir, dir.c_str() );
	g_pKTDXApp->GetDeviceManager()->GetMassFileManager()->AddDataDirectory( strDir, true );

	switch( pSkinMesh->InsertSkinMesh( fileName, dir ) )
	{
	case CX2ViewerSkinMesh::SOT_NONE:
		//WARNINGMSG( L"SKIN MESH 로딩 실패.(CX2ViewerUI::DropFile():545)" )
		return;

	case CX2ViewerSkinMesh::SOT_SKINMESH:
		{
			m_Unit.GetListBox( UI_LIST_ANIMATION )->RemoveAllItems();
			m_Unit.GetListBox( UI_LIST_OBJECT )->RemoveAllItems();
            m_Unit.GetListBox( UI_LIST_BONE )->RemoveAllItems();

			std::vector<std::wstring> vecAnimNameList;
            //
            pSkinMesh->SetAttachPoint(L"");
            SiSelf()->m_vecFrameNameList.clear();
			pSkinMesh->GetAnimNameList( vecAnimNameList );
            pSkinMesh->GetFrameNameList( m_vecFrameNameList );
            
            sort(m_vecFrameNameList.begin(), m_vecFrameNameList.end());

			int nAnimMax = (int)vecAnimNameList.size();
            int nFrameMax = (int)m_vecFrameNameList.size();

			//////////////////////////////////////////////////////////////////////////
			//UI 셋팅부분

			//애니메이션 이름 리스트
			for( int i = 0; i < nAnimMax; ++i )
			{
				m_Unit.GetListBox( UI_LIST_ANIMATION )->AddItem( vecAnimNameList[i].c_str(), (LPVOID)(size_t)0 );
			}

            //애니메이션 이름 리스트
            for( int i = 0; i < nFrameMax; ++i )
            {
                LPCSTR			szName;
                std::wstring	wstrName;
                WCHAR			wszName[128] = L"";

                szName = m_vecFrameNameList[i]->Name;

                MultiByteToWideChar( CP_ACP, 0, szName, -1, wszName, MAX_PATH);

                wstrName = wszName;
                m_Unit.GetListBox( UI_LIST_BONE )->AddItem( wstrName.c_str(), (LPVOID)(size_t)0 );
            }

			Init();

			//애니메이션 개수
			WCHAR wstrNum[10] = L"";
			swprintf( wstrNum, L"%d", nAnimMax );
			m_Unit.GetEditBox( UI_EDIT_ANIM_NUM )->SetText( wstrNum );

			SetUnitOnOff( true );

			//애니메이션 속도 초기화
			InitAnimSpeed();

			pMesh->Reset();
			SetMeshOnOff( false );
			//m_Param.Reset();
			m_MeshSel = MS_SKIN_MESH;

			SetRenderParamOnOff( false );

			CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );
			m_Param.GetRenderParam( pSkinMesh->GetRenderParam(), pSkinMesh->GetImpactData(), *(pSkinMesh->GetTexStageData()) );

			CDXUTListBox* pListBox = (CDXUTListBox*)SiGetUnitDlg( UI_LIST_ANIMATION );
			pListBox->SelectItem( 0 );

			SiMain()->SetAnimFileName( fileName.c_str() );
			SiMain()->SetAnimDirName( dir.c_str() );
		}
		break;

	case CX2ViewerSkinMesh::SOT_MESH:
		{
			m_Unit.GetListBox( UI_LIST_OBJECT )->AddItem( fileName.c_str(), (LPVOID)(size_t)0 );
		}
		break;

	case CX2ViewerSkinMesh::SOT_NOT_ADDMESH:
		{
			pMesh->SetMesh( fileName.c_str(), dir.c_str() );
			SetMeshOnOff( true );

			SetUnitOnOff( false );
			pSkinMesh->Reset();
			//m_Param.Reset();
			m_MeshSel = MS_MESH;

			SetRenderParamOnOff( false );

			CX2ViewerMesh*	pMesh = (CX2ViewerMesh*)SiGetObject( OS_MESH );
			m_Param.GetRenderParam( pMesh->GetRenderParam(), pMesh->GetImpactData(), *(pMesh->GetTexStageData()) );
            
		}
		break;
	}

	SetEffectButOnOff( true );
}

void CX2ViewerUI::DrawAnimFrame()
{
	CX2ViewerSkinMesh*	pSkinMesh = (CX2ViewerSkinMesh*)SiGetObject( OS_SKIN_MESH );

	float fNow, fMax;
	fNow = fMax = 0.0f;
	pSkinMesh->GetAnimTime( fNow, fMax );

	//Static 문구 출력
	CDXUTStatic* pStatic = (CDXUTStatic*)SiGetUnitDlg( UI_STATIC_ANIM_FRAME );
	WCHAR wszSpeed[128] = L"";
	swprintf( wszSpeed, L"Frame : %d / %d\n %.3f / %.3f", (int)(fNow/0.033333f), (int)(fMax/0.033333f), fNow, fMax );
	pStatic->SetText( wszSpeed );
}

// 파티클 에디터에서 해당 파티클의 작동 시간
void CX2ViewerUI::DrawParticleTime()
{
	CX2ViewerParticle* pParticleObj = (CX2ViewerParticle*)(SiGetObject( OS_PARTICLE ));
	CDXUTStatic* pStatic = (CDXUTStatic*)SiGetParticleEditorDlg( UI_STATIC_PARTICLE_EDITOR_TIME );
	
	if( pParticleObj != NULL )
	{
		WCHAR wszSpeed[64];
		swprintf( wszSpeed, L"%.3f", pParticleObj->GetPreviewPlayTime() );
		pStatic->SetText( wszSpeed );
	}
}

void CX2ViewerUI::InitAnimSpeed()
{
	CDXUTSlider* pSlider = (CDXUTSlider*)SiGetUnitDlg( UI_SLIDE_ANIM_SPEED );
	pSlider->SetValue( 100 );

	//Static 문구 출력
	CDXUTStatic* pStatic = (CDXUTStatic*)SiGetUnitDlg( UI_STATIC_ANIM_SPEED );
	WCHAR wszSpeed[128] = L"";
	swprintf( wszSpeed, L"speed : %d", 100 );
	pStatic->SetText( wszSpeed );
}

// 파티클 에디터 초기화 관련 코드. 할게 많아서 따로 뺌.
void CX2ViewerUI::InitParticleEditor()
{
	CX2ViewerParticle* pParticleObj = (CX2ViewerParticle*)(SiGetObject( OS_PARTICLE ));
	CDXUTListBox* pCustomListBox = (CDXUTListBox*) SiGetParticleEditorDlg(UI_LIST_PARTICLE_EDITOR_MYPARTICLE);
	CDXUTListBox* pTempletListBox = (CDXUTListBox*) SiGetParticleEditorDlg(UI_LIST_PARTICLE_EDITOR_PARTICLETEMPLET);
	CDXUTListBox* pModelParticleListBox = (CDXUTListBox*) SiGetParticleEditorDlg(UI_LIST_PARTICLE_EDITOR_MODEL_LIST);
	CDXUTListBox* pEventListBox = (CDXUTListBox*) SiGetParticleEditorDlg(UI_LIST_PARTICLE_EDITOR_EVENT);
	
	CDXUTListBox* pEmitterPropertiesListBox = (CDXUTListBox*) SiGetParticleEditorDlg(UI_LIST_PARTICLE_EDITOR_EMITTERATTRIBUTE);
	CDXUTListBox* pEventTypeListBox = (CDXUTListBox*) SiGetParticleEditorDlg(UI_LIST_PARTICLE_EDITOR_EVENT_TYPE);

	// list update
	{
		pCustomListBox->RemoveAllItems();
		const map<wstring, CKTDGParticleSystem::CParticleEventSequence*>& templetSeq = pParticleObj->GetCustomParticleSystem()->GetTempletSequences();
		map<wstring, CKTDGParticleSystem::CParticleEventSequence*>::const_iterator it;
		for( it=templetSeq.begin(); it != templetSeq.end(); it++ )
		{
			wstring& wstrName = (wstring)it->first;
			pCustomListBox->AddItem( wstrName.c_str(), NULL );
		}
	}

	//////////////////////////////////////////////////////////////////////////
	{
		pTempletListBox->RemoveAllItems();
		const map<wstring, CKTDGParticleSystem::CParticleEventSequence*>& templetSeq = pParticleObj->GetParticleSystem()->GetTempletSequences();
		map<wstring, CKTDGParticleSystem::CParticleEventSequence*>::const_iterator it;
		for( it=templetSeq.begin(); it != templetSeq.end(); it++ )
		{
			wstring& wstrName = (wstring)it->first;
			pTempletListBox->AddItem( wstrName.c_str(), NULL );
		}

	}
	
	//////////////////////////////////////////////////////////////////////////
	{
		pModelParticleListBox->RemoveAllItems();
		
		vector<CX2ViewerParticle::ParticleEffectData*> vecParticleEffect = pParticleObj->GetParticleEffectData();
		for( vector<CX2ViewerParticle::ParticleEffectData*>::iterator it=vecParticleEffect.begin(); it != vecParticleEffect.end(); it++ )
		{
			CX2ViewerParticle::ParticleEffectData* pData = *it;
			if( pData == NULL )
				continue;
			pModelParticleListBox->AddItem( pData->m_Name.c_str(), NULL );
		}
	}
	//////////////////////////////////////////////////////////////////////////
	pEventListBox->RemoveAllItems();
	//////////////////////////////////////////////////////////////////////////
	pEmitterPropertiesListBox->RemoveAllItems();
	
	for( int i = 0; i < CX2ViewerParticleEditor::EP_END; ++i )
	{
		CX2ViewerParticleEditor::PropertyData* refData = pParticleObj->GetParticleEditor().GetEmitterProperties( (CX2ViewerParticleEditor::EMITTER_PROPERTIES)i );
		pEmitterPropertiesListBox->AddItem( refData->m_name.c_str(), (void*)refData );
	}
	//////////////////////////////////////////////////////////////////////////
	pEventTypeListBox->RemoveAllItems();
	for( int i = 0; i < CKTDGParticleSystem::ET_END; ++i )
	{
		CX2ViewerParticleEditor::PropertyData* refData = pParticleObj->GetParticleEditor().GetEventProperties( (CKTDGParticleSystem::EVENT_TYPE)i );
		if( refData->m_name == L"" ) continue;
		pEventTypeListBox->AddItem( refData->m_name.c_str(), (void*)refData );
	}
	
	CDXUTComboBox* pComboBox = (CDXUTComboBox*)m_ParticleEditor.GetControl( UI_COMBO_PARTICLE_EDITOR_PLAY_TYPE );
	pComboBox->RemoveAllItems();
	pComboBox->AddItem( L"ONE", NULL );
	pComboBox->AddItem( L"LOOP", NULL );
	if( pParticleObj->GetPreviewPlayMode() )
	{
		pComboBox->SetSelectedByText( L"LOOP" );
	}
	else
	{
		pComboBox->SetSelectedByText( L"ONE" );
	}

	CDXUTSlider* pSlider = (CDXUTSlider*)SiGetParticleEditorDlg( UI_SLIDE_PARTICLE_EDITOR_SPEED );
	pSlider->SetValue( 100 );
	pParticleObj->SetPreviewPlaySpeed( 1 );

	SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_SINGLE )->SetVisible( false );
	SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_X )->SetVisible( false );
	SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_Y )->SetVisible( false );
	SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EMITTERATTRIBUTE_Z )->SetVisible( false );
	SiGetParticleEditorDlg( UI_COMBO_PARTICLE_EDITOR_EMITTERATTRIBUTE )->SetVisible( false );

	SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_SINGLE )->SetVisible( false );
	SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_X )->SetVisible( false );
	SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_Y )->SetVisible( false );
	SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_Z )->SetVisible( false );
	SiGetParticleEditorDlg( UI_COMBO_PARTICLE_EDITOR_EVENT )->SetVisible( false );
	SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_R )->SetVisible( false );
	SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_G )->SetVisible( false );
	SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_B )->SetVisible( false );
	SiGetParticleEditorDlg( UI_EDIT_PARTICLE_EDITOR_EVENT_A )->SetVisible( false );

}



// 이벤트 리스트 갱신
void CX2ViewerUI::RefreshEventList()
{

	CX2ViewerParticle* pParticleObj = (CX2ViewerParticle*)(SiGetObject( OS_PARTICLE ));
	CX2ViewerParticleEditor& refParticleEditor = pParticleObj->GetParticleEditor();
	CDXUTListBox* pCustomListBox = (CDXUTListBox*) SiGetParticleEditorDlg(UI_LIST_PARTICLE_EDITOR_MYPARTICLE);
	
	CDXUTListBox* pParticleEventListBox = (CDXUTListBox*) SiGetParticleEditorDlg(UI_LIST_PARTICLE_EDITOR_EVENT);


	pParticleEventListBox->RemoveAllItems();

	if( pCustomListBox->GetSelectedItem() == NULL )
		return;

	wstring name(pCustomListBox->GetSelectedItem()->strText);
	// 취급주의 : const_cast
	CKTDGParticleSystem::CParticleEventSequence* pTempletSeq = 
		const_cast<CKTDGParticleSystem::CParticleEventSequence*>(pParticleObj->GetCustomParticleSystem()->GetTempletSequencesByName( name ));

	if( pTempletSeq == NULL )
		return;

	vector<CKTDGParticleSystem::CParticleEvent*>* pvecEvent = pTempletSeq->GetEventList();

	for( vector<CKTDGParticleSystem::CParticleEvent*>::iterator it = pvecEvent->begin(); it < pvecEvent->end(); ++it )
	{
		CKTDGParticleSystem::CParticleEvent* pEvent = *it;
		if( pEvent == NULL )
			continue;

		wstringstream wstrm;
		CX2ViewerParticleEditor::PropertyData* pPropertyData = refParticleEditor.GetEventProperties( pEvent->GetEventType() );
		if( pPropertyData == NULL )
			continue;
		wstrm << pPropertyData->m_name;
		wstrm << L" (" << refParticleEditor.GetEventTimeString( pEvent, pTempletSeq->GetLifeTime().m_Max ) << L")";
		pParticleEventListBox->AddItem( wstrm.str().c_str(), (void*)pEvent );

	}

}