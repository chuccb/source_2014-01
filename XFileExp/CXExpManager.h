#include "CXExpFrame.h"
//#include "XExportUtil.h"



class XExpManager
{
public:
    XExpManager();
    ~XExpManager();


// Method
private:
    HRESULT DoExport(const TCHAR *aFileName = NULL);

public:
    HRESULT XExport(const TCHAR *aName, ExpInterface *aExpInterface,Interface *aInterface, BOOL suppressPrompts, DWORD options);
    HRESULT XMeshConvert(const TCHAR *aName);

// Attribute
private:
    FILE *m_pFile;
    const TCHAR *m_pFileName;

    Interface*	m_pInterface;    

    CXExpFrame *m_frameRoot;

    LPDIRECTXFILE m_pxofapi;
    LPDIRECTXFILEDATA m_pRootData;
    
    _PS m_ps;   

};