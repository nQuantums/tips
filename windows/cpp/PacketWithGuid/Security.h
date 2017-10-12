#pragma once
#include "Config.h"
#include <Windows.h>
#include <Sddl.h>
#include <string>
#include <initializer_list>

struct SecurityDescriptor {
	PSECURITY_DESCRIPTOR pSd;

	SecurityDescriptor() {
		this->pSd = NULL;
	}
	~SecurityDescriptor() {
		if (this->pSd) {
			delete this->pSd;
		}
	}

	void SetDacl(PACL acl) {
		if (!this->pSd) {
			PSECURITY_DESCRIPTOR psd;
			psd = new char[SECURITY_DESCRIPTOR_MIN_LENGTH];
			if (!InitializeSecurityDescriptor(psd, SECURITY_DESCRIPTOR_REVISION)) {
				delete psd;
				throw Exception("Failed to InitializeSecurityDescriptor.");
			}
			this->pSd = psd;
		}

		if (!::SetSecurityDescriptorDacl(this->pSd, TRUE, acl, FALSE)) {
			throw Exception("Failed to SetSecurityDescriptorDacl.");
		}
	}

	operator PSECURITY_DESCRIPTOR() const {
		return this->pSd;
	}
};

struct Sid {
	enum class Authority {
		NullSid,
		WorldSid,
		LocalSid,
		CreatorSid,
		NonUnique,
		ResourceManager,
	};

	PSID pSid;
	int DeallocateType;

	Sid() {
		this->pSid = NULL;
		this->DeallocateType = 0;
	}
	Sid(Authority authority, int subAuthorityCount = 0, DWORD rid0 = 0, DWORD rid1 = 0, DWORD rid2 = 0, DWORD rid3 = 0, DWORD rid4 = 0, DWORD rid5 = 0, DWORD rid6 = 0, DWORD rid7 = 0) {
		Create(authority, subAuthorityCount, rid0, rid1, rid2, rid3, rid4, rid5, rid6, rid7);
	}
	Sid(const wchar_t* accountName, std::wstring* domainName = NULL) {
		Create(accountName, domainName);
	}
	~Sid() {
		switch (this->DeallocateType) {
		case 1:
			if (this->pSid) {
				::FreeSid(this->pSid);
			}
			break;
		case 2:
			if (this->pSid) {
				delete this->pSid;
			}
			break;
		}
	}

	void Create(Authority authority, int subAuthorityCount = 0, DWORD rid0 = 0, DWORD rid1 = 0, DWORD rid2 = 0, DWORD rid3 = 0, DWORD rid4 = 0, DWORD rid5 = 0, DWORD rid6 = 0, DWORD rid7 = 0) {
		SID_IDENTIFIER_AUTHORITY sidauth;
		switch (authority) {
		case Authority::NullSid:
			sidauth = SECURITY_NULL_SID_AUTHORITY;
			break;
		case Authority::WorldSid:
			sidauth = SECURITY_WORLD_SID_AUTHORITY;
			break;
		case Authority::LocalSid:
			sidauth = SECURITY_LOCAL_SID_AUTHORITY;
			break;
		case Authority::CreatorSid:
			sidauth = SECURITY_CREATOR_SID_AUTHORITY;
			break;
		case Authority::NonUnique:
			sidauth = SECURITY_NON_UNIQUE_AUTHORITY;
			break;
		case Authority::ResourceManager:
			sidauth = SECURITY_RESOURCE_MANAGER_AUTHORITY;
			break;
		default:
			throw Exception("Unknown authority.");
		}

		PSID psid;
		if (!::AllocateAndInitializeSid(&sidauth, subAuthorityCount, rid0, rid1, rid2, rid3, rid4, rid5, rid6, rid7, &psid)) {
			throw Exception("Failed to AllocateAndInitializeSid.");
		}

		this->pSid = psid;
		this->DeallocateType = 1;
	}
	void Create(const wchar_t* accountName, std::wstring* domainName = NULL) {
		DWORD sidSize = 0;
		SID_NAME_USE snu;
		BOOL ret;
		DWORD domainNameBufferLength;

		ret = ::LookupAccountNameW(
			NULL,
			accountName,
			NULL,
			&sidSize,
			NULL,
			&domainNameBufferLength,
			&snu);
		if (!ret && ::GetLastError() != ERROR_INSUFFICIENT_BUFFER) {
			throw Exception("Failed to LookupAccountNameW.");
		}

		PSID psid;
		psid = (PSID)new char[sidSize];

		std::wstring domainNameBuffer;
		domainNameBuffer.resize(domainNameBufferLength - 1);

		ret = ::LookupAccountNameW(
			NULL,
			accountName,
			psid,
			&sidSize,
			&domainNameBuffer[0],
			&domainNameBufferLength,
			&snu);
		if (!ret) {
			delete psid;
			throw Exception("Failed to LookupAccountNameW.");
		}

		if (domainName) {
			*domainName = std::move(domainNameBuffer);
		}

		this->pSid = psid;
		this->DeallocateType = 2;
	}

	std::wstring ToString() const {
		LPWSTR p;
		if (!::ConvertSidToStringSid(this->pSid, &p)) {
			throw Exception("Failed to ConvertSidToStringSid.");
		}
		std::wstring s = p;
		::LocalFree(p);
		return std::move(s);
	}

	operator PSID() const {
		return this->pSid;
	}
};

struct Acl {
	PACL pAcl;

	Acl() {
		this->pAcl = NULL;
	}
	Acl(size_t size) {
		Create(size);
	}
	Acl(std::initializer_list<PSID> sids) {
		Create(sids);
	}
	~Acl() {
		if (this->pAcl) {
			delete this->pAcl;
		}
	}

	void Create(size_t size) {
		this->pAcl = (PACL)new char[size];
		::InitializeAcl(this->pAcl, (DWORD)size, ACL_REVISION);
	}
	void Create(std::initializer_list<PSID> sids) {
		size_t size = AclSize(sids);
		this->pAcl = (PACL)new char[size];
		::InitializeAcl(this->pAcl, (DWORD)size, ACL_REVISION);
	}

	void AddAccessAllowedAce(DWORD accessMask, PSID sid) {
		if (!::AddAccessAllowedAceEx(this->pAcl, ACL_REVISION, CONTAINER_INHERIT_ACE | OBJECT_INHERIT_ACE, accessMask, sid)) {
			throw Exception("Failed to AddAccessAllowedAceEx.");
		}
	}
	void AddAccessDeniedAce(DWORD accessMask, PSID sid) {
		if (!::AddAccessDeniedAceEx(this->pAcl, ACL_REVISION, CONTAINER_INHERIT_ACE | OBJECT_INHERIT_ACE, accessMask, sid)) {
			throw Exception("Failed to AddAccessDeniedAceEx.");
		}
	}

	operator PACL() const {
		return this->pAcl;
	}

	static DWORD AclSize(const PSID* sids, int count) {
		DWORD aclSize = 0;
		for (int i = 0; i < count; i++) {
			aclSize += ::GetLengthSid(sids[i]);
			aclSize += sizeof(ACCESS_ALLOWED_ACE) - sizeof(DWORD);
		}
		aclSize += sizeof(ACL);
		return aclSize;
	}
	static DWORD AclSize(std::initializer_list<PSID> sids) {
		DWORD aclSize = 0;
		for (auto sid : sids) {
			aclSize += ::GetLengthSid(sid);
			aclSize += sizeof(ACCESS_ALLOWED_ACE) - sizeof(DWORD);
		}
		aclSize += sizeof(ACL);
		return aclSize;
	}
};

struct SecurityAttributes : SECURITY_ATTRIBUTES {
	SecurityAttributes() {
		this->nLength = sizeof(SECURITY_ATTRIBUTES);
		this->lpSecurityDescriptor = NULL;
		this->bInheritHandle = FALSE;
	}
	SecurityAttributes(PSECURITY_DESCRIPTOR sd) {
		this->nLength = sizeof(SECURITY_ATTRIBUTES);
		this->lpSecurityDescriptor = sd;
		this->bInheritHandle = FALSE;
	}
};
