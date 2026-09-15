\# ChoDogyu Save / Load Framework



Unity 프로젝트에서 런타임 저장 데이터를 직렬화하고 파일 시스템에 안전하게 저장 및 복원하기 위한 범용 Save / Load Framework입니다.



게임별 SaveData와 게임 규칙은 사용하는 프로젝트가 소유하고, Framework는 저장 데이터를 직렬화하여 Primary / Backup 파일로 관리하고 복원하는 책임에 집중합니다.



특정 게임이나 장르에 종속되지 않으며 독립적인 Unity Package Manager 패키지로 사용할 수 있도록 구성했습니다.



\---



\## 주요 기능



\- Generic SaveData 저장 및 불러오기

\- `Save<T>()`

\- `Load<T>()`

\- `Exists()`

\- `Delete()`

\- `SaveSlot` 기반 저장 슬롯 관리

\- Serializer 추상화

\- `JsonUtility` 기반 기본 Serializer

\- Storage 추상화

\- 로컬 파일 기반 `FileSaveStorage`

\- Temp 파일 기반 Safe Write

\- Primary / Backup 이중 저장

\- Primary 손상 또는 읽기 실패 시 Backup 복구

\- 저장 파일이 없는 상태를 정상적인 `NotFound` 결과로 표현

\- Result 기반 오류 처리

\- 명확한 Save Framework 오류 코드



\---



\## 요구 사항



\- Unity 6.3 이상

\- ChoDogyu Core 1.0.0



개발 및 검증 환경:



```text

Unity 6.3 LTS

6000.3.9f1

```



이 패키지는 `CDG.Core.Results`의 `Result`, `Result<T>`, `ResultError`를 사용하므로 ChoDogyu Core가 필요합니다.



Data Framework, Object Pooling 또는 다른 CDG 패키지는 필요하지 않습니다.



\---



\## 설치



ChoDogyu Core를 먼저 설치한 뒤 Save / Load Framework를 설치합니다.



\### 1. Core 설치



Unity Package Manager에서 다음 Git URL을 설치합니다.



```text

https://github.com/ChoDoGyu/ChoDogyuCore.git?path=/com.chodogyu.core#v1.0.0

```



\### 2. Save / Load Framework 설치



```text

https://github.com/ChoDoGyu/ChoDogyuSave.git?path=/com.chodogyu.save#v1.0.0

```



Unity에서 다음 경로로 이동합니다.



```text

Window

→ Package Management

→ Package Manager

→ Install package from git URL...

```



위 Git URL을 입력하면 패키지를 설치할 수 있습니다.



\---



\## 기본 사용



저장 데이터는 사용하는 게임에서 직접 정의합니다.



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



Save Framework는 특정 SaveData 타입을 요구하지 않습니다.



기본 `JsonUtilitySaveSerializer`를 사용하는 경우 Unity의 직렬화 규칙을 따르는 타입을 사용해야 합니다.



\---



\## SaveService 생성



기본 구성에서는 `FileSaveStorage`와 `JsonUtilitySaveSerializer`를 사용합니다.



```csharp

using CDG.Save;

using CDG.Save.Serialization;

using CDG.Save.Storage;



FileSaveStorage storage = new FileSaveStorage();

JsonUtilitySaveSerializer serializer = new JsonUtilitySaveSerializer();



SaveService saveService =

&#x20;   new SaveService(storage, serializer);

```



`SaveService`는 Singleton을 제공하지 않습니다.



Storage와 Serializer를 생성하고 관리하는 책임은 사용하는 프로젝트가 가집니다.



\---



\## 저장



```csharp

using CDG.Core.Results;

using CDG.Save;



SaveSlot slot = new SaveSlot("player-main");



PlayerSaveData data = new PlayerSaveData

{

&#x20;   PlayerName = "Player",

&#x20;   Level = 12,

&#x20;   Gold = 1500

};



Result result =

&#x20;   saveService.Save(slot, data);



if (result.IsFailure)

{

&#x20;   UnityEngine.Debug.LogError(

&#x20;       $"{result.Error.Code}: {result.Error.Message}");

}

```



저장 시 데이터는 한 번만 직렬화됩니다.



동일한 직렬화 결과가 다음 두 파일에 기록됩니다.



```text

Primary

Backup

```



Backup은 이전 버전의 저장 기록이 아니라 현재 저장 데이터를 위한 안전 복사본입니다.



\---



\## 불러오기



```csharp

Result<LoadResult<PlayerSaveData>> result =

&#x20;   saveService.Load<PlayerSaveData>(slot);



if (result.IsFailure)

{

&#x20;   UnityEngine.Debug.LogError(

&#x20;       $"{result.Error.Code}: {result.Error.Message}");



&#x20;   return;

}



LoadResult<PlayerSaveData> loadResult =

&#x20;   result.Value;



if (!loadResult.IsFound)

{

&#x20;   UnityEngine.Debug.Log(

&#x20;       "저장 데이터가 없습니다.");



&#x20;   return;

}



PlayerSaveData data =

&#x20;   loadResult.Value;

```



`Load<T>()`는 저장 파일이 존재하지 않는 상황을 실패로 처리하지 않습니다.



두 저장 파일이 모두 없는 경우:



```text

Result = Success

Status = NotFound

IsFound = false

```



를 반환합니다.



첫 실행이나 아직 저장하지 않은 슬롯도 정상적인 상태로 처리할 수 있습니다.



\---



\## Load 상태



`LoadResult<T>.Status`는 다음 세 상태를 가집니다.



```text

NotFound

LoadedFromPrimary

LoadedFromBackup

```



\### LoadedFromPrimary



Primary 저장 파일을 정상적으로 읽고 복원한 경우입니다.



\### LoadedFromBackup



Primary를 사용할 수 없어 Backup 파일에서 복원한 경우입니다.



다음 상황에서 Backup 복구를 시도합니다.



```text

Primary 파일 없음

Primary 읽기 실패

Primary 역직렬화 실패

```



Backup에서 정상적으로 데이터를 복원하더라도 Framework가 Primary 파일을 자동으로 다시 생성하거나 수정하지는 않습니다.



\### NotFound



Primary와 Backup이 모두 존재하지 않는 경우입니다.



오류가 아니라 정상적인 Load 결과입니다.



\---



\## 저장 데이터 존재 확인



```csharp

Result<bool> result =

&#x20;   saveService.Exists(slot);



if (result.IsSuccess \&\&

&#x20;   result.Value)

{

&#x20;   UnityEngine.Debug.Log(

&#x20;       "저장 파일이 존재합니다.");

}

```



`Exists()`는 다음 중 하나라도 존재하면 `true`를 반환합니다.



```text

Primary

Backup

```



파일의 실제 데이터가 정상적으로 역직렬화 가능한지는 검사하지 않습니다.



즉 `Exists()`는 파일 존재 여부만 담당하며 실제 복원 가능 여부는 `Load<T>()`가 판단합니다.



\---



\## 삭제



```csharp

Result result =

&#x20;   saveService.Delete(slot);



if (result.IsFailure)

{

&#x20;   UnityEngine.Debug.LogError(

&#x20;       $"{result.Error.Code}: {result.Error.Message}");

}

```



삭제 순서:



```text

Primary

→ Backup

```



Primary 삭제가 실패하면 Backup 삭제를 진행하지 않습니다.



두 파일 삭제를 하나의 원자적 파일 시스템 작업으로 보장하지는 않습니다.



\---



\## SaveSlot



Save Framework는 직접 파일 경로를 Public API로 전달하지 않고 `SaveSlot`을 사용합니다.



```csharp

SaveSlot slot =

&#x20;   new SaveSlot("player-main");

```



기본 슬롯도 제공합니다.



```csharp

SaveSlot.Default

```



기본 슬롯 이름:



```text

default

```



\---



\## SaveSlot 이름 규칙



사용 가능한 문자:



```text

a-z

0-9

\-

\_

```



길이:



```text

1 \~ 64

```



유효한 예:



```text

default

player

player-1

slot\_01

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



대문자, 공백, 마침표, 경로 구분자 및 기타 문장부호는 허용하지 않습니다.



잘못된 Slot 이름은 Result 실패가 아니라 잘못된 API 사용으로 간주하며 `ArgumentException`이 발생합니다.



\---



\## 기본 저장 경로



`FileSaveStorage`의 기본 저장 루트:



```text

Application.persistentDataPath/Saves

```



파일 형식:



```text

Primary

<slot>.save



Backup

<slot>.save.bak



Temp

<slot>.save.tmp

```



예:



```text

default.save

default.save.bak

default.save.tmp

```



Temp 파일은 Storage 내부 구현 세부사항이며 Public API에서는 직접 다루지 않습니다.



\---



\## 사용자 지정 저장 경로



필요한 경우 `FileSaveStorage` 생성자에 다른 루트 경로를 전달할 수 있습니다.



```csharp

FileSaveStorage storage =

&#x20;   new FileSaveStorage(customPath);

```



전달된 경로는 내부에서 절대 경로로 정규화됩니다.



null, 빈 문자열 또는 공백만 있는 경로는 허용하지 않습니다.



\---



\## Safe Write



`FileSaveStorage`는 실제 대상 파일에 데이터를 바로 기록하지 않습니다.



기본 흐름:



```text

직렬화 데이터

→ Temp 파일 기록

→ 기록 성공

→ Primary 또는 Backup 파일 교체

```



새 대상 파일이면 Temp 파일을 이동하고, 기존 대상 파일이면 Temp 파일을 이용해 교체합니다.



저장 과정에서 실패하면 남아 있는 Temp 파일 삭제를 시도합니다.



이 방식은 실제 저장 파일을 직접 덮어쓰는 방식보다 안전한 저장 흐름을 제공하기 위한 것입니다.



모든 운영체제와 파일 시스템에서 완전한 트랜잭션 또는 절대적인 원자성을 보장하는 기능은 아닙니다.



\---



\## Primary와 Backup



Save Framework는 한 번의 Save에서 데이터를 한 번만 직렬화합니다.



```text

SaveData

↓

Serialize

↓

byte\[]

├─ Primary

└─ Backup

```



따라서 정상적인 Save 직후 Primary와 Backup은 같은 데이터를 가집니다.



Backup은 이전 Save 상태를 보관하는 History 시스템이 아닙니다.



예:



```text

첫 번째 Save



Primary = A

Backup  = A

```



이후 다시 저장하면:



```text

두 번째 Save



Primary = B

Backup  = B

```



가 됩니다.



\---



\## Backup 저장 실패



Primary 저장이 성공한 뒤 Backup 저장이 실패하면:



```text

BackupWriteFailed

```



를 반환합니다.



이 경우 Save 전체 Result는 실패이지만 Primary에는 이미 새로운 데이터가 기록되었을 수 있습니다.



따라서 Save 작업은 Primary와 Backup을 하나의 완전한 트랜잭션으로 보장하지 않습니다.



\---



\## Backup 복구



Load 시 Primary를 우선 사용합니다.



```text

Primary

↓

정상

→ LoadedFromPrimary

```



Primary를 사용할 수 없다면 Backup을 확인합니다.



```text

Primary 실패

↓

Backup

↓

정상

→ LoadedFromBackup

```



Primary와 Backup 모두 정상적으로 복원할 수 없다면 Load는 실패합니다.



손상된 데이터로 판단되는 경우:



```text

save.corrupted\_data

```



오류를 반환합니다.



Backup Load 성공 후 Primary를 자동 복구하지는 않습니다.



\---



\## Serializer



Serializer는 저장 데이터 타입과 바이트 배열 사이의 변환만 담당합니다.



```text

T

↕

byte\[]

```



Public 인터페이스:



```csharp

ISaveSerializer

```



기본 구현:



```csharp

JsonUtilitySaveSerializer

```



Serializer는 다음 정보를 알지 못합니다.



```text

파일 경로

SaveSlot

Primary / Backup

저장 매체

```



\---



\## 사용자 정의 Serializer



다른 직렬화 방식을 사용하려면 `ISaveSerializer`를 구현할 수 있습니다.



```csharp

public interface ISaveSerializer

{

&#x20;   Result<byte\[]> Serialize<T>(T data);



&#x20;   Result<T> Deserialize<T>(

&#x20;       byte\[] data);

}

```



예를 들어 프로젝트 요구에 따라 별도의 JSON 라이브러리나 자체 데이터 포맷을 구현할 수 있습니다.



Save Framework v1에는 `JsonUtilitySaveSerializer`만 기본 제공됩니다.



\---



\## Storage



Storage는 직렬화가 끝난 `byte\[]`를 실제 저장 매체에 기록하고 읽는 역할만 담당합니다.



```text

byte\[]

↕

Persistence Medium

```



Public 인터페이스:



```csharp

ISaveStorage

```



기본 구현:



```csharp

FileSaveStorage

```



Storage는 다음 정보를 알지 못합니다.



```text

SaveData 타입

게임 규칙

JSON 구조

데이터 의미

```



\---



\## 사용자 정의 Storage



다른 저장 매체를 사용하려면 `ISaveStorage`를 구현할 수 있습니다.



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



예를 들어 별도의 플랫폼 저장소를 위한 구현을 추가할 수 있습니다.



Save Framework v1에는 로컬 파일 기반 `FileSaveStorage`만 기본 제공됩니다.



\---



\## 오류 코드



Save Framework에서 사용하는 오류 코드:



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



\### save.invalid\_data



직렬화 또는 저장에 사용할 데이터가 유효하지 않은 경우입니다.



\### save.serialization\_failed



SaveData를 바이트 데이터로 직렬화하지 못한 경우입니다.



\### save.deserialization\_failed



바이트 데이터를 지정한 SaveData 타입으로 복원하지 못한 경우입니다.



\### save.storage\_not\_found



Storage 계층에서 요청한 저장 파일을 찾지 못한 경우입니다.



`SaveService.Load<T>()`는 Primary와 Backup 모두 없는 상황을 이 오류 그대로 노출하지 않고 정상적인 `NotFound` 결과로 변환합니다.



\### save.storage\_read\_failed



저장 파일을 읽거나 존재 여부를 확인하는 과정에서 오류가 발생한 경우입니다.



\### save.storage\_write\_failed



Primary 저장 파일을 기록하는 과정에서 오류가 발생한 경우입니다.



\### save.backup\_write\_failed



Primary 저장은 성공했지만 Backup 저장에 실패한 경우입니다.



\### save.storage\_delete\_failed



저장 파일을 삭제하는 과정에서 오류가 발생한 경우입니다.



\### save.corrupted\_data



Primary 데이터를 정상적으로 복원할 수 없고 사용할 수 있는 Backup 데이터도 없는 등 저장 데이터를 정상적으로 복구할 수 없는 경우입니다.



\---



\## 책임 범위



Save Framework가 담당하는 범위:



```text

SaveData 직렬화

SaveData 역직렬화

Save / Load

Exists / Delete

SaveSlot

파일 Storage

Temp 기반 Safe Write

Primary / Backup 관리

Backup Recovery

저장 오류 표현

```



Save Framework가 담당하지 않는 범위:



```text

게임 상태 자체의 소유

게임 규칙

기본 SaveData 자동 생성

게임 데이터 의미 검증

Data Framework Asset 조회

ID를 실제 게임 데이터로 변환

자동 Saveable 탐색

자동 저장

Quit / Pause 자동 저장 Hook

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



게임은 자신의 런타임 상태로부터 SaveData를 생성하고, Load된 SaveData를 다시 게임 상태에 적용하는 책임을 가집니다.



\---



\## Data Framework와의 관계



Save Framework는 ChoDogyu Data Framework에 의존하지 않습니다.



예를 들어 저장 데이터에:



```text

item\_sword

quest\_main\_01

```



같은 ID를 문자열로 저장할 수 있습니다.



Load 후 해당 ID를 실제 Data Framework Asset 또는 게임 데이터로 해석하는 작업은 사용하는 프로젝트가 담당합니다.



구조:



```text

Save Framework

→ ID 값 저장 및 복원



Game / Data Layer

→ ID를 실제 데이터로 해석

```



이를 통해 Save Framework가 특정 데이터 관리 방식에 종속되지 않도록 유지합니다.



\---



\## 테스트



Unity Test Framework 기반으로 Runtime 기능을 검증했습니다.



v1.0 기준:



```text

Runtime

105 Passed



Failed

0

```



주요 검증 범위:



```text

SaveSlot 유효성

LoadResult

오류 코드

JsonUtility 직렬화 / 역직렬화

UTF-8 Round Trip

Storage 경로 생성

파일 Write / Read / Exists / Delete

Safe Write

Temp 파일 정리

Primary / Backup 저장

SaveService Save

SaveService Load

NotFound

Backup Recovery

CorruptedData

Exists

Delete

실제 FileSaveStorage 통합

실제 JsonUtilitySaveSerializer 통합

재저장

Slot 격리

End-to-End Save / Load

```



\---



\## UPM 설치 검증



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



개발 프로젝트 또는 로컬 패키지 경로에 의존하지 않는 Git 기반 독립 설치를 검증했습니다.



\---



\## 버전



현재 패키지 버전:



```text

v1.0.0

```



변경 사항은 `CHANGELOG.md`에서 확인할 수 있습니다.



더 자세한 설계 및 사용 규칙은 다음 문서에서 확인할 수 있습니다.



```text

Documentation\~/index.md

```

