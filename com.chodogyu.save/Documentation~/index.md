\# ChoDogyu Save / Load Framework Documentation



\## 1. 개요



ChoDogyu Save / Load Framework는 Unity 프로젝트에서 런타임 상태를 SaveData로 변환한 뒤 직렬화하여 영속적으로 저장하고 다시 복원하기 위한 범용 패키지입니다.



전체 기본 흐름은 다음과 같습니다.



```text

Game Runtime State

→ Game SaveData

→ SaveService

→ ISaveSerializer

→ byte\[]

→ ISaveStorage

→ Primary / Backup

```



Load는 반대 방향으로 진행됩니다.



```text

Primary / Backup

→ ISaveStorage

→ byte\[]

→ ISaveSerializer

→ Game SaveData

→ Game Runtime State

```



Framework는 게임 상태 자체를 소유하지 않습니다.



게임은 자신의 런타임 상태에서 SaveData를 만들고, Load된 SaveData를 실제 게임 상태에 다시 적용합니다.



\---



\## 2. Framework 책임



Save Framework가 담당하는 범위:



```text

SaveData 직렬화

SaveData 역직렬화

Save

Load

Exists

Delete

SaveSlot 관리

Storage 추상화

Serializer 추상화

파일 저장

Safe Write

Primary / Backup

Backup Recovery

저장 오류 표현

```



Framework가 담당하지 않는 범위:



```text

게임 상태 소유

게임 규칙

기본 SaveData 자동 생성

게임 도메인 유효성 검증

자동 Saveable 탐색

자동 저장

Quit / Pause Hook

Version Migration

Encryption

Compression

Cloud Save

PlayerPrefs 저장

비동기 Save / Load

Backup History

Editor Tool

```



\---



\## 3. Game State와 SaveData



Framework에 게임 내부 객체 전체를 직접 맡기는 구조를 전제로 하지 않습니다.



예를 들어 게임에는 다음 상태가 있을 수 있습니다.



```text

Player

Inventory

Quest System

Equipment

World State

```



게임은 필요한 값만 별도의 SaveData로 구성합니다.



```csharp

using System;



\[Serializable]

public sealed class PlayerSaveData

{

&#x20;   public string PlayerName;

&#x20;   public int Level;

&#x20;   public int Gold;

}

```



흐름:



```text

Game State

→ SaveData 생성

→ Save Framework

```



Load 후:



```text

Save Framework

→ SaveData

→ Game State에 적용

```



SaveData 설계와 적용 방식은 게임의 책임입니다.



\---



\## 4. 기본 구조



주요 구성 요소:



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



`SaveService`는 구체적인 JSON 형식이나 파일 시스템 구현에 직접 의존하지 않고 인터페이스를 통해 기능을 조정합니다.



\---



\## 5. SaveService



`SaveService`는 Framework의 주요 진입점입니다.



생성:



```csharp

FileSaveStorage storage =

&#x20;   new FileSaveStorage();



JsonUtilitySaveSerializer serializer =

&#x20;   new JsonUtilitySaveSerializer();



SaveService service =

&#x20;   new SaveService(

&#x20;       storage,

&#x20;       serializer);

```



생성자에 전달하는 두 요소:



```text

ISaveStorage

ISaveSerializer

```



는 null일 수 없습니다.



null을 전달하면 `ArgumentNullException`이 발생합니다.



\---



\## 6. Singleton을 제공하지 않는 이유



`SaveService`는 Singleton이 아닙니다.



Framework가 전역 생명주기와 접근 방식을 강제하지 않기 위해서입니다.



사용 프로젝트에서 원하는 방식으로 관리할 수 있습니다.



예:



```text

GameBootstrap

Service Container

GameManager

Composition Root

직접 생성

```



Framework는 특정 DI Container나 Service Locator에도 의존하지 않습니다.



\---



\## 7. Save



기본 API:



```csharp

Result Save<T>(

&#x20;   SaveSlot slot,

&#x20;   T data);

```



흐름:



```text

SaveData

→ Serialize 1회

→ byte\[]

→ Primary Write

→ Backup Write

```



Serializer는 한 Save 작업에서 한 번만 호출됩니다.



Primary와 Backup에는 동일한 직렬화 결과가 전달됩니다.



\---



\## 8. Save 실패 흐름



직렬화가 실패하면 Storage에는 접근하지 않습니다.



```text

Serialize 실패

→ Failure

→ Storage Write 없음

```



Primary 저장이 실패하면 Backup 저장을 시도하지 않습니다.



```text

Primary Write 실패

→ StorageWriteFailed

→ Backup Write 없음

```



Primary 저장은 성공했지만 Backup 저장이 실패하면:



```text

BackupWriteFailed

```



를 반환합니다.



이 시점에는 Primary 파일에 이미 새로운 데이터가 저장되었을 수 있습니다.



\---



\## 9. Load



기본 API:



```csharp

Result<LoadResult<T>> Load<T>(

&#x20;   SaveSlot slot);

```



Primary를 먼저 확인합니다.



```text

Primary Read

→ Deserialize

→ 성공

→ LoadedFromPrimary

```



Primary를 정상적으로 사용할 수 없다면 Backup으로 복구를 시도합니다.



\---



\## 10. LoadResult<T>



Load의 결과는 단순히 `T`만 반환하지 않습니다.



```text

Result<LoadResult<T>>

```



구조를 사용합니다.



외부 `Result`:



```text

Load 작업 자체의 성공 / 실패

```



내부 `LoadResult<T>`:



```text

데이터 존재 여부

어디에서 Load되었는지

실제 값

```



을 표현합니다.



\---



\## 11. SaveLoadStatus



Load 상태:



```text

NotFound

LoadedFromPrimary

LoadedFromBackup

```



\### NotFound



저장 데이터가 존재하지 않습니다.



\### LoadedFromPrimary



Primary 파일에서 정상적으로 복원했습니다.



\### LoadedFromBackup



Primary를 사용할 수 없어 Backup 파일에서 복원했습니다.



\---



\## 12. NotFound는 오류가 아님



다음 상태:



```text

Primary 없음

Backup 없음

```



은 실패가 아닙니다.



반환:



```text

Result

→ Success



LoadResult.Status

→ NotFound



LoadResult.IsFound

→ false

```



첫 실행 또는 아직 Save하지 않은 슬롯은 정상적인 게임 상태일 수 있기 때문입니다.



Framework는 이 상황에서 기본 SaveData를 자동 생성하지 않습니다.



\---



\## 13. LoadResult Value



데이터가 존재하는 경우:



```csharp

if (loadResult.IsFound)

{

&#x20;   PlayerSaveData data =

&#x20;       loadResult.Value;

}

```



`NotFound` 상태에서 `Value`에 접근하면 예외가 발생합니다.



호출자는 `IsFound` 또는 `Status`를 먼저 확인해야 합니다.



\---



\## 14. Primary 우선 정책



Load는 항상 Primary를 먼저 확인합니다.



```text

Primary 정상

→ Backup 읽지 않음

→ LoadedFromPrimary

```



Backup은 정상적인 Primary가 존재할 때 항상 같이 읽는 파일이 아닙니다.



복구용 안전 복사본입니다.



\---



\## 15. Backup Recovery 조건



다음 경우 Backup을 시도합니다.



```text

Primary 파일 없음

Primary 파일 Read 실패

Primary Deserialize 실패

```



Backup이 정상이라면:



```text

Result.Success

LoadResult.LoadedFromBackup

```



를 반환합니다.



\---



\## 16. Backup Load 후 Primary 자동 복구



Backup에서 Load에 성공해도 Framework는 Primary를 자동으로 다시 저장하지 않습니다.



즉:



```text

Primary 손상

→ Backup Load 성공

```



이후에도 Primary 파일은 기존 상태로 남을 수 있습니다.



복구 성공을 감지한 게임이 필요에 따라 다시 Save할 수 있습니다.



Framework가 자동 Repair를 수행하지 않는 이유는 Load 작업이 사용자의 명시적인 저장 상태를 임의로 변경하지 않도록 하기 위함입니다.



\---



\## 17. CorruptedData



실제 파일 데이터가 존재하지만 정상적으로 복원할 수 없고 Backup에서도 복구할 수 없는 경우:



```text

save.corrupted\_data

```



를 반환합니다.



예:



```text

Primary Deserialize 실패

Backup 없음

```



또는:



```text

Primary Deserialize 실패

Backup Deserialize 실패

```



Framework는 손상된 파일을 자동 삭제하거나 새로운 기본 SaveData로 덮어쓰지 않습니다.



\---



\## 18. Storage I/O 오류



파일 자체를 읽을 수 없는 문제와 데이터 손상은 구분합니다.



예:



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



파일 I/O 오류를 모두 `CorruptedData`로 바꾸지는 않습니다.



\---



\## 19. SaveSlot



Public Save API는 Raw File Path 대신 `SaveSlot`을 사용합니다.



```csharp

SaveSlot slot =

&#x20;   new SaveSlot("player-main");

```



Framework가 경로 형식을 직접 외부 API에 노출하지 않기 위한 구조입니다.



\---



\## 20. SaveSlot.Default



기본 슬롯:



```csharp

SaveSlot.Default

```



이름:



```text

default

```



단일 저장 슬롯만 필요한 프로젝트에서도 별도의 이름을 정하지 않고 사용할 수 있습니다.



\---



\## 21. SaveSlot 규칙



허용 문자:



```text

a-z

0-9

\-

\_

```



허용 길이:



```text

1 \~ 64

```



유효한 예:



```text

default

player

player-1

slot\_01

save2

```



유효하지 않은 예:



```text

Player

PLAYER

player.main

player main

player/save

../save

플레이어

```



\---



\## 22. 잘못된 SaveSlot



잘못된 Slot 이름은 외부 환경에서 발생한 저장 실패가 아니라 API 사용 오류입니다.



따라서:



```text

ArgumentException

```



을 사용합니다.



null `SaveSlot`을 Public API에 전달하는 경우:



```text

ArgumentNullException

```



이 발생합니다.



\---



\## 23. SaveSlot Equality



SaveSlot은 문자열 이름을 기준으로 비교합니다.



비교 방식:



```text

Ordinal

```



입니다.



Slot 규칙 자체가 소문자 ASCII만 허용하므로 대소문자 정규화 과정은 존재하지 않습니다.



\---



\## 24. ISaveSerializer



Serializer의 책임:



```text

T

↕

byte\[]

```



입니다.



인터페이스:



```csharp

public interface ISaveSerializer

{

&#x20;   Result<byte\[]> Serialize<T>(

&#x20;       T data);



&#x20;   Result<T> Deserialize<T>(

&#x20;       byte\[] data);

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



\---



\## 25. JsonUtilitySaveSerializer



기본 Serializer:



```text

JsonUtilitySaveSerializer

```



Unity의 `JsonUtility`를 사용합니다.



기본 인코딩:



```text

UTF-8

BOM 없음

잘못된 byte sequence 검증

```



을 사용합니다.



\---



\## 26. JsonUtility 직렬화 규칙



`JsonUtilitySaveSerializer`를 사용하는 SaveData는 Unity의 `JsonUtility` 직렬화 규칙을 따라야 합니다.



예:



```csharp

\[Serializable]

public sealed class PlayerSaveData

{

&#x20;   public string Name;

&#x20;   public int Level;

}

```



Unity가 직렬화할 수 없는 구조를 사용하는 경우 프로젝트에서 다른 `ISaveSerializer` 구현을 사용할 수 있습니다.



\---



\## 27. Serializer Invalid Data



Serialize에 null 데이터를 전달하면:



```text

save.invalid\_data

```



를 반환합니다.



Deserialize 입력이:



```text

null

빈 byte\[]

```



인 경우에도:



```text

save.invalid\_data

```



를 반환합니다.



\---



\## 28. Serialization Failure



직렬화 과정에서 데이터를 변환할 수 없는 경우:



```text

save.serialization\_failed

```



역직렬화할 수 없는 경우:



```text

save.deserialization\_failed

```



를 사용합니다.



\---



\## 29. 사용자 정의 Serializer



프로젝트가 다른 포맷을 사용하려면 `ISaveSerializer`를 구현할 수 있습니다.



예:



```text

다른 JSON 라이브러리

MessagePack

자체 Binary Format

플랫폼별 Serialization

```



Framework v1은 이러한 구현을 기본 제공하지 않습니다.



SaveService 자체는 구체적인 포맷을 알지 못합니다.



\---



\## 30. ISaveStorage



Storage의 책임:



```text

byte\[]

↕

Persistence Medium

```



입니다.



인터페이스:



```csharp

public interface ISaveStorage

{

&#x20;   Result Write(

&#x20;       SaveSlot slot,

&#x20;       SaveStorageCopy copy,

&#x20;       byte\[] data);



&#x20;   Result<byte\[]> Read(

&#x20;       SaveSlot slot,

&#x20;       SaveStorageCopy copy);



&#x20;   Result<bool> Exists(

&#x20;       SaveSlot slot,

&#x20;       SaveStorageCopy copy);



&#x20;   Result Delete(

&#x20;       SaveSlot slot,

&#x20;       SaveStorageCopy copy);

}

```



Storage는 SaveData의 타입이나 JSON 구조를 알지 못합니다.



\---



\## 31. SaveStorageCopy



Storage가 다루는 공개 저장 복사본:



```text

Primary

Backup

```



두 종류입니다.



Temp는 포함하지 않습니다.



Temp 파일은 `FileSaveStorage` 내부 구현 세부사항입니다.



\---



\## 32. FileSaveStorage



기본 Storage:



```text

FileSaveStorage

```



파일 시스템에 데이터를 저장합니다.



기본 Root:



```text

Application.persistentDataPath/Saves

```



입니다.



\---



\## 33. 파일 이름



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



\---



\## 34. 사용자 지정 Root



기본 경로 대신 다른 Root를 전달할 수 있습니다.



```csharp

FileSaveStorage storage =

&#x20;   new FileSaveStorage(customPath);

```



전달된 경로는 절대 경로로 정규화됩니다.



다음 값은 허용하지 않습니다.



```text

null

""

"   "

```



\---



\## 35. Safe Write



파일 저장 시 최종 대상 파일에 직접 데이터를 쓰지 않습니다.



기본 흐름:



```text

byte\[]

→ Temp 파일

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



방식을 사용합니다.



\---



\## 36. Safe Write 실패



Safe Write 과정에서 예외가 발생하면:



```text

save.storage\_write\_failed

```



를 반환합니다.



그리고 남아 있는 Temp 파일 삭제를 시도합니다.



Temp Cleanup 역시 실패할 수 있으므로 Framework가 모든 환경에서 Temp 파일 잔존이 절대 발생하지 않는다고 보장하지는 않습니다.



\---



\## 37. Atomicity 범위



Safe Write는 최종 파일에 직접 데이터를 덮어쓰는 것보다 안전한 방식을 제공하기 위한 기능입니다.



하지만 다음을 보장하는 시스템은 아닙니다.



```text

Database Transaction

완전한 ACID

모든 운영체제의 절대적 Atomicity

전원 차단 상황의 완전 복구

모든 FileSystem의 동일한 Replace Semantics

```



문서에서는 이를 완전한 원자적 저장이라고 표현하지 않습니다.



\---



\## 38. Primary와 Backup



한 번의 Save에서 Serialize는 한 번만 실행됩니다.



```text

SaveData

↓

Serialize

↓

byte\[]

├─ Primary

└─ Backup

```



두 파일은 정상적인 Save 직후 같은 저장 내용을 가집니다.



\---



\## 39. Backup은 Save History가 아님



Backup은 직전 버전의 Save를 보관하기 위한 파일이 아닙니다.



예:



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



다음 구조가 아닙니다.



```text

Primary = B

Backup  = A

```



Save History 기능은 v1 범위에 포함하지 않습니다.



\---



\## 40. Backup Write Failure



흐름:



```text

Primary Write 성공

→ Backup Write 실패

```



이면:



```text

save.backup\_write\_failed

```



를 반환합니다.



Save Result는 Failure지만 Primary에는 이미 새 데이터가 존재할 수 있습니다.



따라서 Primary와 Backup Write 전체를 하나의 Transaction으로 보장하지 않습니다.



\---



\## 41. Exists



API:



```csharp

Result<bool> Exists(

&#x20;   SaveSlot slot);

```



Primary를 먼저 확인합니다.



```text

Primary 존재

→ true

→ Backup 확인 안 함

```



Primary가 없다면:



```text

Backup 확인

```



을 진행합니다.



\---



\## 42. Exists의 의미



Exists는:



> 저장 파일이 존재하는가?



만 확인합니다.



다음을 확인하지 않습니다.



```text

JSON이 정상인가

Deserialize 가능한가

게임 규칙상 유효한가

```



따라서 손상된 파일도 파일 자체가 존재하면 `true`일 수 있습니다.



실제 복원 가능성은 `Load<T>()`가 판단합니다.



\---



\## 43. Delete



API:



```csharp

Result Delete(

&#x20;   SaveSlot slot);

```



삭제 순서:



```text

Primary

→ Backup

```



Primary 삭제가 실패하면 Backup은 삭제하지 않습니다.



\---



\## 44. 존재하지 않는 파일 Delete



`FileSaveStorage`에서 대상 파일이 존재하지 않는 상태는 Delete 실패로 처리하지 않습니다.



즉 이미 삭제된 슬롯을 다시 Delete하는 작업도 정상적으로 성공할 수 있습니다.



\---



\## 45. 부분 Delete 실패



예:



```text

Primary Delete 성공

Backup Delete 실패

```



이면 전체 `Delete()` 결과는 Failure입니다.



이 경우 Backup은 여전히 남아 있을 수 있습니다.



Delete 역시 두 파일을 하나의 원자적 파일 시스템 Transaction으로 보장하지 않습니다.



\---



\## 46. 오류 코드



Save Framework v1 오류 코드:



```text

save.invalid\_data

save.serialization\_failed

save.deserialization\_failed

save.storage\_not\_found

save.storage\_read\_failed

save.storage\_write\_failed

save.backup\_write\_failed

save.storage\_delete\_failed

save.corrupted\_data

```



\---



\## 47. save.invalid\_data



직렬화 또는 역직렬화 입력 자체가 유효하지 않은 경우입니다.



예:



```text

Serialize null

Deserialize null

Deserialize empty bytes

```



\---



\## 48. save.serialization\_failed



SaveData를 `byte\[]`로 변환하지 못했습니다.



발생 위치:



```text

Serializer

```



\---



\## 49. save.deserialization\_failed



`byte\[]`를 요청한 SaveData 타입으로 복원하지 못했습니다.



발생 위치:



```text

Serializer

```



SaveService는 Primary Deserialize 실패 이후 Backup Recovery를 시도할 수 있습니다.



\---



\## 50. save.storage\_not\_found



Storage 계층에서 요청한 파일 자체를 찾지 못했습니다.



이 오류는 Storage 수준의 결과입니다.



SaveService에서는:



```text

Primary NotFound

Backup NotFound

```



을 정상적인:



```text

LoadResult.NotFound

```



로 변환합니다.



\---



\## 51. save.storage\_read\_failed



저장 파일을 읽거나 존재 여부를 확인하는 과정에서 파일 시스템 오류가 발생했습니다.



단순히 파일이 없는 상태와 구분합니다.



\---



\## 52. save.storage\_write\_failed



Primary 또는 일반 Storage 파일 Write 과정이 실패했습니다.



SaveService의 Primary Write 실패는 이 오류를 그대로 반환합니다.



\---



\## 53. save.backup\_write\_failed



Primary는 정상적으로 기록했지만 Backup 기록에 실패했습니다.



Primary에는 최신 데이터가 존재할 수 있다는 점이 중요합니다.



\---



\## 54. save.storage\_delete\_failed



Storage 파일 삭제 과정에서 오류가 발생했습니다.



Delete 대상 파일이 단순히 존재하지 않는 상태는 이 오류가 아닙니다.



\---



\## 55. save.corrupted\_data



파일 데이터가 존재하지만 정상적인 복구가 불가능하다고 SaveService가 판단한 경우입니다.



대표 예:



```text

Primary Deserialize 실패

Backup 없음

```



또는:



```text

Primary Deserialize 실패

Backup Deserialize 실패

```



입니다.



\---



\## 56. Data Framework와의 분리



Save Framework는 ChoDogyu Data Framework에 의존하지 않습니다.



예를 들어 SaveData에:



```csharp

public string EquippedWeaponId;

```



를 저장할 수 있습니다.



값:



```text

sword\_iron

```



을 Save Framework는 단순 문자열로 저장하고 복원합니다.



Load 이후:



```text

sword\_iron

→ 실제 ItemData 조회

```



는 게임 또는 Data Layer의 책임입니다.



\---



\## 57. Data와 Save의 역할



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



정리:



```text

Data

= 무엇인가를 정의하는 값



Save

= 현재 플레이 상태를 기록하는 값

```



두 Framework가 함께 사용될 수 있지만 서로 필수 의존하지 않습니다.



\---



\## 58. 동기 API



Save Framework v1 API는 동기 방식입니다.



```text

Save

Load

Exists

Delete

```



호출은 현재 Thread에서 완료될 때까지 실행됩니다.



따라서 다음과 같은 사용은 권장하지 않습니다.



```text

Update()에서 매 Frame Save

매 Frame Exists

매 Frame Load

매우 큰 데이터의 빈번한 저장

```



게임의 적절한 저장 시점에서 호출해야 합니다.



\---



\## 59. 저장 시점



Framework는 자동 저장 시점을 결정하지 않습니다.



게임에서 다음과 같은 지점을 선택할 수 있습니다.



```text

스테이지 종료

설정 변경 완료

체크포인트

명시적 Save 버튼

중요 진행 상태 변경

```



`OnApplicationQuit`, `OnApplicationPause` 등에 자동으로 연결하지 않습니다.



필요하다면 사용하는 게임이 직접 연결합니다.



\---



\## 60. 게임 도메인 Validation



Save Framework가 확인하는 것은 저장 및 복원 가능 여부입니다.



예를 들어 Load된 데이터가:



```text

Level = -50

Gold = -999999

없는 Item ID

진행 불가능한 Quest State

```



인지 판단하지 않습니다.



이러한 의미적 Validation은 게임 도메인의 책임입니다.



\---



\## 61. 기본 데이터 생성



저장 파일이 없다고 Framework가 자동으로 다음을 만들지 않습니다.



```text

Default Player

Default Inventory

Default Progress

```



Load가 `NotFound`를 반환하면 게임이 필요에 따라 초기 상태를 생성합니다.



\---



\## 62. Migration



v1에서는 Save Version과 Migration 기능을 제공하지 않습니다.



다음 기능은 포함하지 않습니다.



```text

Version Field 자동 관리

Old Save Upgrade

Schema Migration

Migration Pipeline

Backward Compatibility Layer

```



필요한 프로젝트는 SaveData 내부 Version 관리 또는 별도 Game Layer를 통해 구현할 수 있습니다.



\---



\## 63. Encryption과 Compression



v1에서는 기본 제공하지 않습니다.



```text

Encryption

Compression

Obfuscation

Checksum

Authentication

```



등이 필요한 경우 Serializer 또는 별도의 Storage 구현 계층에서 확장할 수 있습니다.



\---



\## 64. Cloud Storage



v1 기본 Storage는 로컬 파일 시스템입니다.



Cloud Save는 제공하지 않습니다.



`ISaveStorage` 추상화를 통해 다른 저장 매체를 구현할 수 있지만 Framework v1에서 구체적인 Cloud Provider 구현은 포함하지 않습니다.



\---



\## 65. PlayerPrefs



PlayerPrefs 기반 Storage는 기본 제공하지 않습니다.



Save Framework의 기본 구현은 파일 기반입니다.



작은 설정 값과 게임 SaveData의 책임을 불필요하게 혼합하지 않기 위한 범위 결정입니다.



\---



\## 66. WebGL



v1에서는 WebGL 전용 Storage 정책이나 파일 시스템 호환 계층을 제공하지 않습니다.



플랫폼별 저장 제약이 필요한 경우 해당 플랫폼에 맞는 `ISaveStorage` 구현을 별도로 제공해야 합니다.



\---



\## 67. 사용자 정의 Storage



`ISaveStorage`를 구현하면 다른 저장 매체를 사용할 수 있습니다.



예:



```text

플랫폼별 Native Storage

Remote Storage Adapter

Memory Storage

테스트용 Fake Storage

커스텀 File System

```



SaveService는 구체적인 Storage 종류를 알 필요가 없습니다.



\---



\## 68. 확장 책임



Framework 확장은 두 주요 경계로 나뉩니다.



직렬화 형식 변경:



```text

ISaveSerializer

```



저장 매체 변경:



```text

ISaveStorage

```



게임별 SaveData 변경은 Framework 확장이 아니라 사용하는 게임의 책임입니다.



\---



\## 69. Namespace



기본 Namespace:



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



입니다.



\---



\## 70. 패키지 의존성



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



Save Framework는 독립적으로 설치할 수 있습니다.



\---



\## 71. 설치



먼저 Core를 설치합니다.



```text

https://github.com/ChoDoGyu/ChoDogyuCore.git?path=/com.chodogyu.core#v1.0.0

```



그다음 Save Framework를 설치합니다.



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



\---



\## 72. v1.0 범위



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



\---



\## 73. 테스트



Unity Test Framework 기반 Runtime Test:



```text

105 Passed

0 Failed

```



주요 검증:



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



\---



\## 74. 실제 구현 통합 테스트



Fake만 사용한 단위 테스트 외에 실제:



```text

SaveService

JsonUtilitySaveSerializer

FileSaveStorage

```



조합도 검증했습니다.



흐름:



```text

SaveData

→ JSON

→ UTF-8 byte\[]

→ File

→ byte\[]

→ JSON

→ SaveData

```



Round Trip을 확인했습니다.



\---



\## 75. Backup 통합 검증



실제 파일을 사용하여:



```text

Primary 삭제

→ Backup Load 성공

```



및:



```text

Primary 손상

→ Backup Load 성공

```



을 검증했습니다.



양쪽 모두 손상된 경우:



```text

save.corrupted\_data

```



를 반환하는 것도 확인했습니다.



\---



\## 76. Delete 통합 검증



실제 Save 이후:



```text

Exists = true

→ Delete

→ Primary 없음

→ Backup 없음

→ Exists = false

→ Load = NotFound

```



흐름을 검증했습니다.



\---



\## 77. UPM 설치 검증



완전히 새로운 Unity 6.3 프로젝트에서 Git UPM 설치를 검증했습니다.



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



Unity를 종료한 뒤 다시 실행하여 기존 저장 파일 Load도 확인했습니다.



\---



\## 78. 검증 환경



개발 및 검증:



```text

Unity 6.3 LTS

6000.3.9f1

```



패키지 버전:



```text

1.0.0

```



\---



\## 79. 설계 요약



Save Framework v1의 핵심 구조:



```text

Typed SaveData

\+

Serializer

\+

Storage

\+

Safe Write

\+

Single Backup

\+

Clear Errors

```



Framework는 게임 상태의 의미를 알지 못합니다.



저장과 복원의 기술적 책임에 집중하고, 게임 도메인과 파일 시스템 구현을 명확히 분리하는 것을 기본 원칙으로 합니다.

