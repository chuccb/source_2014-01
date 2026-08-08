#pragma once

#include <windows.h>
#include <stdio.h>
#include <tchar.h>
#include <string.h>
#include <d3dx9math.h>
#include <vector>
#include <set>
#include <map>
#include <hash_map>
#include <algorithm>
#include <boost/static_assert.hpp>
#include <boost/foreach.hpp>

using   namespace std;

#define ASSERT(exp)

#include <lua.hpp>
#include <KLuaManager.h>
#include <KLuaBinder.h>

inline void TableBind( KLuaManager* pLuaManager, KLuabinder* pLuaBinder )
{
	bool	retVal = true;
	int		index = 1;
	string	buffer;

	retVal = pLuaManager->GetValue(index,buffer);
	while( retVal == true )
	{
		HRESULT hr = pLuaBinder->DoString( buffer.c_str() );
		if( hr != S_OK )
			return;

		retVal = pLuaManager->GetValue(index,buffer);
		index++;
	}
}

#define ARRAY_SIZE(a)       (sizeof(a)/sizeof((a)[0]))

extern  void        _ErrorLogMsg( int errnum, const char* pszMsg, const char* pszFile, const char* pszFunction, int line );
extern  void        _ErrorLogMsg( int errnum, const wchar_t* pwszMsg, const char* pszFile, const char* pszFunction, int line );

#define ErrorLogMsg( errorEnum, errorMsg )		\
{ \
    _ErrorLogMsg( errorEnum, errorMsg, __FILE__, __FUNCTION__, __LINE__ ); \
}

#define XEM_ERROR144    144       // Item.lua 파싱 오류
#define XEM_ERROR145    145       // SetItem.lua 파싱 오류
#define XEM_ERROR146    146       // 툴 오류

#define X2OPTIMIZE_ITEM_TEMPLET_PREPROCESSING
//#define ADD_ITEM_TEMPLET_ITEM
