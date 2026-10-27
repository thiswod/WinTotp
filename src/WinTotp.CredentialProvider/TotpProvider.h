#pragma once
#include "common.h"
#include "TotpCredential.h"

class TotpProvider : public ICredentialProvider, public ICredentialProviderSetUserArray
{
public:
    TotpProvider();
    virtual ~TotpProvider();

    IFACEMETHODIMP_(ULONG) AddRef() override;
    IFACEMETHODIMP_(ULONG) Release() override;
    IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv) override;

    IFACEMETHODIMP SetUsageScenario(CREDENTIAL_PROVIDER_USAGE_SCENARIO cpus, DWORD dwFlags) override;
    IFACEMETHODIMP SetSerialization(const CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION* pcpcs) override;
    IFACEMETHODIMP Advise(ICredentialProviderEvents* pcpe, UINT_PTR upAdviseContext) override;
    IFACEMETHODIMP UnAdvise() override;
    IFACEMETHODIMP GetFieldDescriptorCount(DWORD* pdwCount) override;
    IFACEMETHODIMP GetFieldDescriptorAt(DWORD dwIndex, CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR** ppcpfd) override;
    IFACEMETHODIMP GetCredentialCount(
        DWORD* pdwCount,
        DWORD* pdwDefault,
        BOOL* pbAutoLogonWithDefault) override;
    IFACEMETHODIMP GetCredentialAt(DWORD dwIndex, ICredentialProviderCredential** ppcpc) override;

    IFACEMETHODIMP SetUserArray(ICredentialProviderUserArray* users) override;

    friend HRESULT TotpProvider_CreateInstance(REFIID riid, void** ppv);

private:
    LONG _refs = 1;
    CREDENTIAL_PROVIDER_USAGE_SCENARIO _cpus = CPUS_INVALID;
    TotpCredential* _credential = nullptr;
    ICredentialProviderUserArray* _users = nullptr;
    bool _configOk = false;
    TotpCpConfig _cfg{};
};
