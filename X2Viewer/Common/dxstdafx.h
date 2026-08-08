//--------------------------------------------------------------------------------------
// File: DxStdAfx.h
//
// Desc: Standard includes and precompiled headers for DXUT
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------
#pragma once
#ifndef DXSDK_STDAFX_H
#define DXSDK_STDAFX_H

#pragma warning(disable:4267)
#include "KTDX.h"

#define ERRORMSG(str)	MessageBox( NULL, str, L"error", MB_ICONERROR | MB_OK); \
						DestroyWindow( g_pKTDXApp->GetHWND() );
#define WARNINGMSG(str)	MessageBox( NULL, str, L"error", MB_ICONQUESTION | MB_OK);


struct IMPACT_DATA
{
	float	fMin;
	float	fMax;
	float	fAnimTime;

	IMPACT_DATA()
	{
		fMin		= 0.0f;
		fMax		= 0.0f;
		fAnimTime	= 0.0f;
	}
};

struct TEX_STAGE_DATA
{
	D3DXVECTOR2	vMin;
	D3DXVECTOR2	vMax;
	float		fAnimTime;
	float		fNowAnimTime;

	TEX_STAGE_DATA()
	{
		vMin.x = 0.0f;
		vMin.y = 0.0f;
		vMax.x = 0.0f;
		vMax.y = 0.0f;
		fAnimTime = 0.0f;
		fNowAnimTime = 0.0f;
	}
};

// x2viewer
#include "../X2ViewerObject.h"
#include "../X2ViewerGrid.h"
#include "../X2ViewerCamera.h"
#include "../X2ViewerMesh.h"
#include "../X2ViewerSkinMesh.h"
#include "../X2ViewerParticleEditor.h"
#include "../X2ViewerParticle.h"
#include "../X2ViewerParam.h"
#include "../X2ViewerFileOS.h"
#include "../X2ViewerUI.h"
#include "../X2ViewerWorldMesh.h"
#include "../X2ViewerMain.h"

extern CDXUTDialogResourceManager g_DialogResourceManager;

extern ID3DXFont*              g_pFont;
extern ID3DXSprite*            g_pTextSprite;

#endif // !defined(DXSDK_STDAFX_H)
