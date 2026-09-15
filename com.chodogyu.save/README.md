# ChoDogyu Save / Load Framework

Unity 프로젝트에서 런타임 저장 데이터를 직렬화하고 파일 시스템에 안전하게 저장 및 복원하기 위한 범용 Save / Load Framework입니다.

게임별 SaveData와 게임 규칙은 사용하는 프로젝트가 소유하고, Framework는 저장 데이터를 직렬화하여 Primary / Backup 파일로 관리하고 복원하는 책임에 집중합니다.

특정 게임이나 장르에 종속되지 않으며 독립적인 Unity Package Manager 패키지로 사용할 수 있도록 구성했습니다.

---

## 주요 기능

- Generic SaveData 저장 및 불러오기
- `Save<T>()`
- `Load<T>()`
- `Exists()`
- `Delete()`
- `SaveSlot` 기반 저장 슬롯 관리
- Serializer 추상화
- `JsonUtility` 기반 기본 Serializer
- Storage 추상화
- 로컬 파일 기반 `FileSaveStorage`
- Temp 파일 기반 Safe Write
- Primary / Backup 이중 저장
- Primary 손상 또는 읽기 실패 시 Backup 복구
- 저장 파일이 없는 상태를 정상적인 `NotFound` 결과로 표현
- Result 기반 오류 처리
- 명확한 Save Framework 오류 코드

---

## 요구 사항

- Unity 6.3 이상
- ChoDogyu Core 1.0.0

개발 및 검증 환경:

```text
Unity 6.3 LTS
6000.3.9f1
```

이 패키지는 `CDG.Core.Results`의 `Result`, `Result<T>`, `ResultError`를 사용하므로 ChoDogyu Core가 필요합니다.

Data Framework, Object Pooling 또는 다른 CDG 패키지는 필요하지 않습니다.

---

## 설치

ChoDogyu Core를 먼저 설치한 뒤 Save / Load Framework를 설치합니다.

### 1. Core 설치

```text
https://github.com/ChoDoGyu/ChoDogyuCore.git?path=/com.chodogyu.core#v1.0.0
```

### 2. Save / Load Framework 설치

```text
https://github.com/ChoDoGyu/ChoDogyuSave.git?path=/com.chodogyu.save#v1.0.0
```

Unity에서:

```text
Window
→ Package Management
→ Package Manager
→ Install package from git URL...
```

Core를 먼저 설치한 뒤 Save / Load Framework를 설치합니다.

---

## 기본 데이터 정의

저장 데이터는 사용하는 게임에서 직접 정의합니다.

```csharp
using System;

[Serializable]
public sealed class PlayerSaveData
{
    public string PlayerName;
    public int Level;
    public int Gold;
}
```

Save Framework는 특정 SaveData 타입을 요구하지 않습니다.

기본 `JsonUtilitySaveSerializer`를 사용하는 경우 Unity의 직렬화 규칙을 따르는 타입을 사용해야 합니다.

---

## SaveService 생성

```csharp
using CDG.Save;
using CDG.Save.Serialization;
using CDG.Save.Storage;

FileSaveStorage storage = new FileSaveStorage();
JsonUtilitySaveSerializer serializer = new JsonUtilitySaveSerializer();
SaveService saveService = new SaveService(storage, serializer);
```

`SaveService`는 Singleton을 제공하지 않습니다.

Storage와 Serializer를 생성하고 관리하는 책임은 사용하는 프로젝트가 가집니다.

---

## 저장

```csharp
using CDG.Core.Results;
using CDG.Save;

SaveSlot slot = new SaveSlot("player-main");

PlayerSaveData data = new PlayerSaveData
{
    PlayerName = "Player",
    Level = 12,
    Gold = 1500
};

Result result = saveService.Save(slot, data);

if (result.IsFailure)
{
    UnityEngine.Debug.LogError($"{result.Error.Code}: {result.Error.Message}");
}
```

저장 시 데이터는 한 번만 직렬화됩니다.

동일한 직렬화 결과가 다음 두 파일에 기록됩니다.

```text
Primary
Backup
```

Backup은 이전 버전의 저장 기록이 아니라 현재 저장 데이터를 위한 안전 복사본입니다.

---

## 불러오기

```csharp
Result<LoadResult<PlayerSaveData>> result = saveService.Load<PlayerSaveData>(slot);

if (result.IsFailure)
{
    UnityEngine.Debug.LogError($"{result.Error.Code}: {result.Error.Message}");
    return;
}

LoadResult<PlayerSaveData> loadResult = result.Value;

if (!loadResult.IsFound)
{
    UnityEngine.Debug.Log("저장 데이터가 없습니다.");
    return;
}

PlayerSaveData data = loadResult.Value;
```

`Load<T>()`는 저장 파일이 존재하지 않는 상황을 실패로 처리하지 않습니다.

두 저장 파일이 모두 없는 경우:

```text
Result = Success
Status = NotFound
IsFound = false
```

를 반환합니다.

---

## Load 상태

`LoadResult<T>.Status`:

```text
NotFound
LoadedFromPrimary
LoadedFromBackup
```

### LoadedFromPrimary

Primary 저장 파일에서 정상적으로 복원한 경우입니다.

### LoadedFromBackup

Primary를 사용할 수 없어 Backup 파일에서 복원한 경우입니다.

다음 상황에서 Backup 복구를 시도합니다.

```text
Primary 파일 없음
Primary 읽기 실패
Primary 역직렬화 실패
```

Backup에서 정상적으로 복원하더라도 Framework가 Primary 파일을 자동으로 다시 생성하거나 수정하지는 않습니다.

### NotFound

Primary와 Backup이 모두 존재하지 않는 경우입니다.

오류가 아니라 정상적인 Load 결과입니다.

---

## 저장 데이터 존재 확인

```csharp
Result<bool> result = saveService.Exists(slot);

if (result.IsSuccess && result.Value)
{
    UnityEngine.Debug.Log("저장 파일이 존재합니다.");
}
```

`Exists()`는 Primary 또는 Backup 중 하나라도 존재하면 `true`를 반환합니다.

파일의 실제 데이터가 정상적으로 역직렬화 가능한지는 검사하지 않습니다.

---

## 삭제

```csharp
Result result = saveService.Delete(slot);

if (result.IsFailure)
{
    UnityEngine.Debug.LogError($"{result.Error.Code}: {result.Error.Message}");
}
```

삭제 순서:

```text
Primary
→ Backup
```

Primary 삭제가 실패하면 Backup 삭제를 진행하지 않습니다.

---

## SaveSlot

```csharp
SaveSlot slot = new SaveSlot("player-main");
```

기본 슬롯:

```csharp
SaveSlot.Default
```

기본 슬롯 이름:

```text
default
```

---

## SaveSlot 이름 규칙

사용 가능한 문자:

```text
a-z
0-9
-
_
```

길이:

```text
1 ~ 64
```

유효한 예:

```text
default
player
player-1
slot_01
chapter2
```

유효하지 않은 예:

```text
Player
player.main
player main
../player
플레이어
slot/01
```

잘못된 Slot 이름은 Result 실패가 아니라 API 사용 오류로 간주하며 `ArgumentException`이 발생합니다.

---

## 기본 저장 경로

`FileSaveStorage` 기본 저장 루트:

```text
Application.persistentDataPath/Saves
```

파일:

```text
Primary
<slot>.save

Backup
<slot>.save.bak

Temp
<slot>.save.tmp
```

---

## 사용자 지정 저장 경로

```csharp
FileSaveStorage storage = new FileSaveStorage(customPath);
```

전달된 경로는 내부에서 절대 경로로 정규화됩니다.

null, 빈 문자열 또는 공백만 있는 경로는 허용하지 않습니다.

---

## Safe Write

기본 흐름:

```text
직렬화 데이터
→ Temp 파일 기록
→ 기록 성공
→ Primary 또는 Backup 파일 교체
```

새 대상 파일이면 Temp 파일을 이동하고, 기존 대상 파일이면 Temp 파일을 이용해 교체합니다.

저장 과정에서 실패하면 남아 있는 Temp 파일 삭제를 시도합니다.

모든 운영체제와 파일 시스템에서 완전한 Transaction 또는 절대적 Atomicity를 보장하는 기능은 아닙니다.

---

## Primary와 Backup

```text
SaveData
↓
Serialize
↓
byte[]
├─ Primary
└─ Backup
```

정상적인 Save 직후 Primary와 Backup은 같은 데이터를 가집니다.

Backup은 이전 Save 상태를 보관하는 History 시스템이 아닙니다.

```text
Save A

Primary = A
Backup  = A

Save B

Primary = B
Backup  = B
```

---

## Backup 저장 실패

Primary 저장이 성공한 뒤 Backup 저장이 실패하면:

```text
save.backup_write_failed
```

를 반환합니다.

이 경우 Save 전체 Result는 실패이지만 Primary에는 이미 새로운 데이터가 기록되었을 수 있습니다.

---

## Backup 복구

```text
Primary
↓
정상
→ LoadedFromPrimary
```

Primary를 사용할 수 없다면:

```text
Primary 실패
↓
Backup
↓
정상
→ LoadedFromBackup
```

Primary와 Backup 모두 정상적으로 복원할 수 없다면 Load는 실패합니다.

Backup Load 성공 후 Primary를 자동 복구하지는 않습니다.

---

## Serializer

Serializer 책임:

```text
T
↕
byte[]
```

인터페이스:

```csharp
public interface ISaveSerializer
{
    Result<byte[]> Serialize<T>(T data);
    Result<T> Deserialize<T>(byte[] data);
}
```

기본 구현:

```text
JsonUtilitySaveSerializer
```

Serializer는 파일 경로, SaveSlot, Primary / Backup 또는 저장 매체를 알지 못합니다.

---

## Storage

Storage 책임:

```text
byte[]
↕
Persistence Medium
```

인터페이스:

```csharp
public interface ISaveStorage
{
    Result Write(SaveSlot slot, SaveStorageCopy copy, byte[] data);
    Result<byte[]> Read(SaveSlot slot, SaveStorageCopy copy);
    Result<bool> Exists(SaveSlot slot, SaveStorageCopy copy);
    Result Delete(SaveSlot slot, SaveStorageCopy copy);
}
```

기본 구현:

```text
FileSaveStorage
```

Storage는 SaveData 타입, 게임 규칙 또는 JSON 구조를 알지 못합니다.

---

## 오류 코드

```text
save.invalid_data
save.serialization_failed
save.deserialization_failed
save.storage_not_found
save.storage_read_failed
save.storage_write_failed
save.backup_write_failed
save.storage_delete_failed
save.corrupted_data
```

### save.invalid_data

입력 데이터 자체가 유효하지 않은 경우입니다.

### save.serialization_failed

SaveData를 바이트 배열로 직렬화하지 못한 경우입니다.

### save.deserialization_failed

바이트 배열을 SaveData 타입으로 복원하지 못한 경우입니다.

### save.storage_not_found

Storage 계층에서 요청한 저장 파일을 찾지 못한 경우입니다.

### save.storage_read_failed

파일 읽기 또는 존재 여부 확인 중 오류가 발생한 경우입니다.

### save.storage_write_failed

Primary 또는 Storage 파일 기록에 실패한 경우입니다.

### save.backup_write_failed

Primary는 기록되었지만 Backup 기록에 실패한 경우입니다.

### save.storage_delete_failed

파일 삭제에 실패한 경우입니다.

### save.corrupted_data

Primary 데이터를 정상적으로 복원할 수 없고 사용할 수 있는 Backup도 없는 경우입니다.

---

## 책임 범위

Save Framework가 담당하는 범위:

```text
SaveData 직렬화
SaveData 역직렬화
Save / Load
Exists / Delete
SaveSlot
파일 Storage
Safe Write
Primary / Backup
Backup Recovery
저장 오류 표현
```

담당하지 않는 범위:

```text
게임 상태 자체의 소유
게임 규칙
기본 SaveData 자동 생성
게임 데이터 의미 검증
자동 Saveable 탐색
자동 저장
Quit / Pause Hook
Save Version
Migration
Encryption
Compression
Cloud Save
PlayerPrefs 저장
Binary Serializer 기본 제공
Async Save / Load
Backup History
Editor Tool
```

---

## Data Framework와의 관계

Save Framework는 ChoDogyu Data Framework에 의존하지 않습니다.

예:

```text
item_sword
quest_main_01
```

같은 ID를 문자열로 저장할 수 있습니다.

Load 이후 ID를 실제 Data Framework Asset 또는 게임 데이터로 해석하는 작업은 사용하는 프로젝트가 담당합니다.

---

## 테스트

Unity Test Framework 기반 Runtime 테스트:

```text
105 Passed
0 Failed
```

주요 검증 범위:

```text
SaveSlot
LoadResult
오류 코드
JsonUtility 직렬화 / 역직렬화
UTF-8 Round Trip
Storage 경로
Write / Read / Exists / Delete
Safe Write
Temp 파일 정리
Primary / Backup
Save
Load
NotFound
Backup Recovery
CorruptedData
Overwrite
Slot Isolation
End-to-End
```

---

## UPM 설치 검증

완전히 새로운 Unity 6.3 프로젝트에서 다음 흐름을 검증했습니다.

```text
ChoDogyu Core v1.0.0 Git 설치
→ 성공

ChoDogyu Save / Load Framework Git 설치
→ 성공

CDG.Save Public API 참조
→ 성공

FileSaveStorage 생성
→ 성공

JsonUtilitySaveSerializer 생성
→ 성공

Save
→ 성공

Load
→ 성공

LoadedFromPrimary
→ 확인

Unity 종료 및 재실행
→ 정상

기존 저장 데이터 Load
→ 성공
```

---

## 버전

현재 패키지 버전:

```text
v1.0.0
```

변경 사항:

```text
CHANGELOG.md
```

상세 문서:

```text
Documentation~/index.md
```