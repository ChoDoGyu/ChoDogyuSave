# ChoDogyu Save / Load Framework Documentation

## 1. 개요

ChoDogyu Save / Load Framework는 Unity 프로젝트에서 런타임 상태를 SaveData로 변환한 뒤 직렬화하여 영속적으로 저장하고 다시 복원하기 위한 패키지입니다.

전체 기본 흐름:

```text
Game Runtime State
→ Game SaveData
→ SaveService
→ ISaveSerializer
→ byte[]
→ ISaveStorage
→ Primary / Backup
```

Load:

```text
Primary / Backup
→ ISaveStorage
→ byte[]
→ ISaveSerializer
→ Game SaveData
→ Game Runtime State
```

Framework는 게임 상태 자체를 소유하지 않습니다.

게임은 자신의 런타임 상태에서 SaveData를 만들고, Load된 SaveData를 실제 게임 상태에 다시 적용합니다.

---

## 2. 책임 범위

Save Framework가 담당합니다.

```text
SaveData 직렬화
SaveData 역직렬화
Save
Load
Exists
Delete
SaveSlot
Storage 추상화
Serializer 추상화
File Storage
Safe Write
Primary / Backup
Backup Recovery
저장 오류 표현
```

담당하지 않습니다.

```text
게임 상태 소유
게임 규칙
기본 SaveData 자동 생성
게임 도메인 Validation
자동 Saveable 탐색
Autosave
Quit / Pause Hook
Version Migration
Encryption
Compression
Cloud Save
PlayerPrefs Storage
Async Save / Load
Backup History
Editor Tool
```

---

## 3. Game State와 SaveData

게임 내부 Runtime 객체와 저장용 데이터는 분리할 수 있습니다.

예:

```text
Player
Inventory
Quest
Equipment
World State
```

필요한 값만 SaveData로 구성합니다.

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

흐름:

```text
Game State
→ SaveData 생성
→ Save Framework
```

Load 이후:

```text
Save Framework
→ SaveData
→ Game State 적용
```

---

## 4. 기본 구조

```text
SaveService
├─ ISaveSerializer
└─ ISaveStorage
```

기본 구현:

```text
ISaveSerializer
└─ JsonUtilitySaveSerializer

ISaveStorage
└─ FileSaveStorage
```

`SaveService`는 구체적인 JSON 형식이나 저장 매체 구현에 직접 의존하지 않습니다.

---

## 5. SaveService 생성

```csharp
FileSaveStorage storage = new FileSaveStorage();
JsonUtilitySaveSerializer serializer = new JsonUtilitySaveSerializer();
SaveService service = new SaveService(storage, serializer);
```

생성자에 전달하는 `ISaveStorage`, `ISaveSerializer`는 null일 수 없습니다.

null이면 `ArgumentNullException`이 발생합니다.

`SaveService`는 Singleton을 제공하지 않습니다.

---

## 6. Save

API:

```csharp
Result Save<T>(SaveSlot slot, T data);
```

흐름:

```text
SaveData
→ Serialize 1회
→ byte[]
→ Primary Write
→ Backup Write
```

Serializer는 한 Save 작업에서 한 번만 호출됩니다.

Primary와 Backup에는 동일한 byte 배열이 전달됩니다.

---

## 7. Save 실패

Serialize 실패:

```text
Serialize 실패
→ Failure
→ Storage Write 없음
```

Primary 실패:

```text
Primary Write 실패
→ StorageWriteFailed
→ Backup Write 없음
```

Primary 성공 후 Backup 실패:

```text
BackupWriteFailed
```

이 경우 Primary에는 이미 새로운 데이터가 기록되었을 수 있습니다.

---

## 8. Load

API:

```csharp
Result<LoadResult<T>> Load<T>(SaveSlot slot);
```

Primary를 먼저 사용합니다.

```text
Primary Read
→ Deserialize
→ 성공
→ LoadedFromPrimary
```

Primary를 사용할 수 없다면 Backup Recovery를 시도합니다.

---

## 9. LoadResult<T>

Load는 단순히 `T`만 반환하지 않습니다.

```text
Result<LoadResult<T>>
```

외부 `Result`는 Load 작업 자체의 성공 / 실패를 표현합니다.

`LoadResult<T>`는 다음을 표현합니다.

```text
데이터 존재 여부
Primary / Backup 구분
실제 SaveData
```

---

## 10. SaveLoadStatus

```text
NotFound
LoadedFromPrimary
LoadedFromBackup
```

### NotFound

저장 데이터가 존재하지 않는 정상 상태입니다.

### LoadedFromPrimary

Primary 파일에서 복원했습니다.

### LoadedFromBackup

Backup 파일에서 복원했습니다.

---

## 11. NotFound 정책

```text
Primary 없음
Backup 없음
```

이면:

```text
Result = Success
Status = NotFound
IsFound = false
```

입니다.

첫 실행이나 아직 저장하지 않은 슬롯은 정상 상태일 수 있기 때문입니다.

Framework는 기본 SaveData를 자동 생성하지 않습니다.

---

## 12. LoadResult Value

```csharp
if (loadResult.IsFound)
{
    PlayerSaveData data = loadResult.Value;
}
```

`NotFound` 상태에서 `Value`에 접근하면 `InvalidOperationException`이 발생합니다.

---

## 13. Primary 우선 정책

```text
Primary 정상
→ Backup 읽지 않음
→ LoadedFromPrimary
```

Backup은 정상 Load에서 항상 같이 읽는 파일이 아닙니다.

복구용 안전 복사본입니다.

---

## 14. Backup Recovery 조건

다음 경우 Backup을 확인합니다.

```text
Primary 파일 없음
Primary Read 실패
Primary Deserialize 실패
```

Backup이 정상이라면:

```text
Result.Success
LoadedFromBackup
```

을 반환합니다.

---

## 15. Primary 자동 복구

Backup Load에 성공해도 Primary를 자동 Repair하지 않습니다.

```text
Primary 손상
→ Backup Load 성공
→ Primary 그대로 유지
```

필요하다면 게임이 이후 명시적으로 다시 Save합니다.

---

## 16. CorruptedData

파일은 존재하지만 정상 복구가 불가능한 경우:

```text
save.corrupted_data
```

를 반환합니다.

대표 상황:

```text
Primary Deserialize 실패
Backup 없음
```

또는:

```text
Primary Deserialize 실패
Backup Deserialize 실패
```

손상 파일을 자동 삭제하거나 기본 데이터로 덮어쓰지 않습니다.

---

## 17. Storage I/O 오류

I/O 오류와 데이터 손상을 구분합니다.

```text
Primary Read 실패
Backup 정상
→ LoadedFromBackup
```

```text
Primary Read 실패
Backup 없음
→ Primary Read Failure
```

```text
Primary Read 실패
Backup Read 실패
→ Backup Read Failure
```

---

## 18. SaveSlot

Public Save API는 Raw Path 대신 `SaveSlot`을 사용합니다.

```csharp
SaveSlot slot = new SaveSlot("player-main");
```

---

## 19. SaveSlot.Default

```csharp
SaveSlot.Default
```

기본 이름:

```text
default
```

---

## 20. SaveSlot 규칙

허용 문자:

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

유효:

```text
default
player
player-1
slot_01
save2
```

유효하지 않음:

```text
Player
PLAYER
player.main
player main
player/save
../save
플레이어
```

---

## 21. 잘못된 SaveSlot

잘못된 이름:

```text
ArgumentException
```

null `SaveSlot`:

```text
ArgumentNullException
```

---

## 22. SaveSlot Equality

SaveSlot은 이름을 기준으로 비교합니다.

```text
StringComparison.Ordinal
```

슬롯 규칙이 소문자 ASCII만 허용하므로 자동 대소문자 정규화는 하지 않습니다.

---

## 23. ISaveSerializer

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

Serializer는 다음을 알지 못합니다.

```text
SaveSlot
파일 경로
Primary
Backup
Temp
저장 디렉터리
```

---

## 24. JsonUtilitySaveSerializer

기본 Serializer:

```text
JsonUtilitySaveSerializer
```

Unity `JsonUtility`를 사용합니다.

인코딩:

```text
UTF-8
BOM 없음
잘못된 byte sequence 검증
```

---

## 25. JsonUtility 직렬화 규칙

```csharp
[Serializable]
public sealed class PlayerSaveData
{
    public string Name;
    public int Level;
}
```

기본 Serializer를 사용할 경우 Unity `JsonUtility` 직렬화 규칙을 따라야 합니다.

다른 규칙이 필요하면 별도 `ISaveSerializer` 구현을 사용할 수 있습니다.

---

## 26. Serializer Invalid Data

Serialize null:

```text
save.invalid_data
```

Deserialize:

```text
null
빈 byte[]
```

역시:

```text
save.invalid_data
```

입니다.

---

## 27. Serialization 오류

직렬화 실패:

```text
save.serialization_failed
```

역직렬화 실패:

```text
save.deserialization_failed
```

---

## 28. 사용자 정의 Serializer

`ISaveSerializer`를 구현하여 다른 포맷을 사용할 수 있습니다.

예:

```text
다른 JSON Library
MessagePack
Custom Binary Format
플랫폼별 Serialization
```

v1에서는 `JsonUtilitySaveSerializer`만 기본 제공합니다.

---

## 29. ISaveStorage

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

Storage는 SaveData 타입이나 JSON 구조를 알지 못합니다.

---

## 30. SaveStorageCopy

공개 저장 복사본:

```text
Primary
Backup
```

Temp는 Public Storage 종류가 아닙니다.

---

## 31. FileSaveStorage

기본 Storage:

```text
FileSaveStorage
```

기본 Root:

```text
Application.persistentDataPath/Saves
```

---

## 32. 파일 이름

Primary:

```text
<slot>.save
```

Backup:

```text
<slot>.save.bak
```

Temp:

```text
<slot>.save.tmp
```

예:

```text
player-main.save
player-main.save.bak
player-main.save.tmp
```

---

## 33. 사용자 지정 Root

```csharp
FileSaveStorage storage = new FileSaveStorage(customPath);
```

경로는 절대 경로로 정규화됩니다.

허용하지 않음:

```text
null
""
"   "
```

---

## 34. Safe Write

최종 파일에 직접 쓰지 않고 Temp에 먼저 기록합니다.

```text
byte[]
→ Temp
→ Temp Write 성공
→ Target 교체
```

신규 파일:

```text
Temp
→ Move
→ Target
```

기존 파일:

```text
Temp
→ Replace
→ Target
```

---

## 35. Safe Write 실패

실패 시:

```text
save.storage_write_failed
```

를 반환하고 Temp 삭제를 시도합니다.

모든 환경에서 Temp 파일 미잔존을 절대 보장하지는 않습니다.

---

## 36. Atomicity 범위

Safe Write는 완전한 Database Transaction이 아닙니다.

다음을 보장하지 않습니다.

```text
ACID
모든 OS에서의 절대 Atomicity
전원 차단 상황의 완전 복구
모든 FileSystem에서 동일한 Replace Semantics
```

---

## 37. Primary와 Backup

```text
SaveData
↓
Serialize
↓
byte[]
├─ Primary
└─ Backup
```

정상 Save 직후 같은 데이터를 가집니다.

---

## 38. Backup은 History가 아님

```text
Save A
Primary = A
Backup  = A
```

이후:

```text
Save B
Primary = B
Backup  = B
```

입니다.

다음이 아닙니다.

```text
Primary = B
Backup  = A
```

---

## 39. Backup Write Failure

```text
Primary Write 성공
→ Backup Write 실패
→ save.backup_write_failed
```

전체 Save Result는 실패이지만 Primary에는 최신 데이터가 존재할 수 있습니다.

---

## 40. Exists

API:

```csharp
Result<bool> Exists(SaveSlot slot);
```

흐름:

```text
Primary 존재
→ true
→ Backup 확인 안 함
```

Primary가 없다면 Backup을 확인합니다.

---

## 41. Exists 의미

Exists는 파일 존재 여부만 확인합니다.

다음은 확인하지 않습니다.

```text
JSON 정상 여부
Deserialize 가능 여부
게임 규칙상 유효 여부
```

실제 복원 가능 여부는 `Load<T>()`가 판단합니다.

---

## 42. Delete

API:

```csharp
Result Delete(SaveSlot slot);
```

순서:

```text
Primary
→ Backup
```

Primary 삭제가 실패하면 Backup은 삭제하지 않습니다.

---

## 43. 존재하지 않는 파일 Delete

대상 파일이 없는 상태는 Delete 실패가 아닙니다.

이미 삭제된 슬롯을 다시 Delete해도 정상 성공할 수 있습니다.

---

## 44. 부분 Delete 실패

```text
Primary Delete 성공
Backup Delete 실패
```

이면 전체 Delete 결과는 Failure입니다.

Backup은 남아 있을 수 있습니다.

---

## 45. 오류 코드

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

---

## 46. save.invalid_data

입력 자체가 유효하지 않은 경우입니다.

```text
Serialize null
Deserialize null
Deserialize empty bytes
```

---

## 47. save.serialization_failed

SaveData를 `byte[]`로 변환하지 못한 경우입니다.

---

## 48. save.deserialization_failed

`byte[]`를 요청한 SaveData 타입으로 복원하지 못한 경우입니다.

SaveService는 Primary Deserialize 실패 후 Backup Recovery를 시도할 수 있습니다.

---

## 49. save.storage_not_found

Storage 계층에서 파일을 찾지 못한 상태입니다.

SaveService에서는:

```text
Primary NotFound
Backup NotFound
```

을:

```text
LoadResult.NotFound
```

로 변환합니다.

---

## 50. save.storage_read_failed

파일 Read 또는 존재 여부 확인 중 File System 오류가 발생한 경우입니다.

---

## 51. save.storage_write_failed

Primary 또는 일반 Storage Write 작업이 실패한 경우입니다.

---

## 52. save.backup_write_failed

Primary 기록은 성공했지만 Backup 기록에 실패한 경우입니다.

Primary에는 최신 데이터가 존재할 수 있습니다.

---

## 53. save.storage_delete_failed

Storage Delete 과정에서 오류가 발생한 경우입니다.

대상 파일이 단순히 없는 상태는 이 오류가 아닙니다.

---

## 54. save.corrupted_data

파일은 존재하지만 정상적인 복구가 불가능한 경우입니다.

대표 상황:

```text
Primary Deserialize 실패
Backup 없음
```

또는:

```text
Primary Deserialize 실패
Backup Deserialize 실패
```

---

## 55. Data Framework와의 분리

Save Framework는 Data Framework에 의존하지 않습니다.

예:

```csharp
public string EquippedWeaponId;
```

저장 값:

```text
sword_iron
```

Save Framework는 이 값을 단순 문자열로 저장하고 복원합니다.

Load 이후 실제 Data 조회는 Game 또는 Data Layer가 담당합니다.

---

## 56. Data와 Save

Data:

```text
아이템 기본 공격력
스킬 기본 쿨타임
퀘스트 정의
몬스터 기본 능력치
```

Save:

```text
보유 아이템
현재 골드
현재 퀘스트 진행도
현재 장착 장비 ID
클리어 상태
```

```text
Data = 무엇인가를 정의하는 값
Save = 현재 플레이 상태를 기록하는 값
```

---

## 57. 동기 API

v1 API는 동기 방식입니다.

```text
Save
Load
Exists
Delete
```

현재 Thread에서 완료될 때까지 실행됩니다.

다음과 같은 사용은 피해야 합니다.

```text
Update()에서 매 Frame Save
매 Frame Load
매 Frame Exists
매우 큰 데이터의 빈번한 저장
```

---

## 58. 저장 시점

Framework가 Autosave 시점을 결정하지 않습니다.

게임이 직접 선택합니다.

예:

```text
스테이지 종료
설정 변경 완료
체크포인트
Save 버튼
중요 진행 상태 변경
```

---

## 59. 게임 도메인 Validation

Framework는 저장 기술의 성공 여부를 판단합니다.

예를 들어 다음 값이 게임 규칙상 유효한지는 판단하지 않습니다.

```text
Level = -50
Gold = -999999
없는 Item ID
진행 불가능한 Quest State
```

---

## 60. 기본 데이터 생성

저장 파일이 없다고 자동으로 다음을 만들지 않습니다.

```text
Default Player
Default Inventory
Default Progress
```

게임이 `NotFound`를 확인하고 초기 상태를 생성합니다.

---

## 61. Migration

v1에서는 다음 기능을 제공하지 않습니다.

```text
Version Field 자동 관리
Old Save Upgrade
Schema Migration
Migration Pipeline
Backward Compatibility Layer
```

---

## 62. Encryption / Compression

v1 기본 제공 범위가 아닙니다.

```text
Encryption
Compression
Obfuscation
Checksum
Authentication
```

필요한 경우 Serializer 또는 Storage 경계를 통해 확장할 수 있습니다.

---

## 63. Cloud Storage

기본 Storage는 로컬 파일 시스템입니다.

Cloud Save Provider 구현은 포함하지 않습니다.

---

## 64. PlayerPrefs

PlayerPrefs Storage는 기본 제공하지 않습니다.

기본 구현은 파일 기반 `FileSaveStorage`입니다.

---

## 65. WebGL

v1에서는 WebGL 전용 Storage 정책이나 호환 계층을 제공하지 않습니다.

필요한 경우 별도 `ISaveStorage` 구현이 필요합니다.

---

## 66. 사용자 정의 Storage

`ISaveStorage` 구현 예:

```text
Native Platform Storage
Remote Storage Adapter
Memory Storage
Test Fake Storage
Custom File System
```

SaveService는 구체적인 Storage 종류를 알 필요가 없습니다.

---

## 67. 확장 경계

직렬화 형식 변경:

```text
ISaveSerializer
```

저장 매체 변경:

```text
ISaveStorage
```

게임별 SaveData 구조는 Framework가 아닌 게임이 관리합니다.

---

## 68. Namespace

```text
CDG.Save
```

Serialization:

```text
CDG.Save.Serialization
```

Storage:

```text
CDG.Save.Storage
```

Assembly:

```text
CDG.Save
```

필수 Assembly 참조:

```text
CDG.Core
```

---

## 69. 패키지 의존성

필수:

```text
ChoDogyu Core 1.0.0
```

필수 아님:

```text
ChoDogyu Data
ChoDogyu Pooling
ChoDogyu Editor Tools
```

---

## 70. 설치

Core:

```text
https://github.com/ChoDoGyu/ChoDogyuCore.git?path=/com.chodogyu.core#v1.0.0
```

Save:

```text
https://github.com/ChoDoGyu/ChoDogyuSave.git?path=/com.chodogyu.save#v1.0.0
```

Unity:

```text
Window
→ Package Management
→ Package Manager
→ Install package from git URL...
```

---

## 71. v1.0 범위

포함:

```text
Save<T>
Load<T>
Exists
Delete
SaveSlot
LoadResult<T>
SaveLoadStatus
ISaveSerializer
JsonUtilitySaveSerializer
ISaveStorage
FileSaveStorage
Primary
Backup
Safe Write
Backup Recovery
Result 기반 오류 처리
```

포함하지 않음:

```text
Async
Autosave
Cloud
Encryption
Compression
Migration
Version Management
PlayerPrefs Storage
Binary Serializer
Editor Tool
Automatic Saveable Discovery
Quit / Pause Hook
Backup History
Primary Auto Repair
Game Domain Validation
```

---

## 72. 테스트

Unity Test Framework 기반 Runtime Tests:

```text
105 Passed
0 Failed
```

검증 범위:

```text
SaveSlot
LoadResult
SaveLoadStatus
SaveErrorCodes
JsonUtility Serialization
UTF-8
File Path
File Read
File Write
File Exists
File Delete
Safe Write
Temp Cleanup
Primary / Backup
Save
Load
NotFound
Backup Recovery
CorruptedData
Exists
Delete
Overwrite
Slot Isolation
End-to-End
```

---

## 73. 실제 구현 통합 테스트

실제 다음 조합을 사용했습니다.

```text
SaveService
JsonUtilitySaveSerializer
FileSaveStorage
```

Round Trip:

```text
SaveData
→ JSON
→ UTF-8 byte[]
→ File
→ byte[]
→ JSON
→ SaveData
```

를 검증했습니다.

---

## 74. Backup 통합 검증

```text
Primary 삭제
→ Backup Load 성공
```

```text
Primary 손상
→ Backup Load 성공
```

양쪽 모두 손상된 경우:

```text
save.corrupted_data
```

를 확인했습니다.

---

## 75. Delete 통합 검증

```text
Save
→ Exists = true
→ Delete
→ Primary 없음
→ Backup 없음
→ Exists = false
→ Load = NotFound
```

흐름을 검증했습니다.

---

## 76. UPM 설치 검증

완전히 새로운 Unity 6.3 프로젝트에서 실제 Git UPM 설치를 검증했습니다.

```text
Core v1.0.0 설치
→ 성공

Save Framework 설치
→ 성공

CDG.Save 참조
→ 성공

Save
→ 성공

Load
→ 성공

LoadedFromPrimary
→ 확인
```

Unity 종료 후 다시 실행하여 기존 Save Load도 확인했습니다.

---

## 77. 검증 환경

```text
Unity 6.3 LTS
6000.3.9f1
```

패키지 버전:

```text
1.0.0
```

---

## 78. 설계 요약

Save Framework v1의 핵심 구조:

```text
Typed SaveData
+
Serializer
+
Storage
+
Safe Write
+
Single Backup
+
Clear Errors
```

Framework는 게임 상태의 의미를 알지 못합니다.

저장과 복원의 기술적 책임에 집중하고, 게임 도메인과 저장 매체 책임을 분리하는 것을 기본 원칙으로 합니다.