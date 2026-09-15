\# ChoDogyu Save / Load Framework



Unity 프로젝트에서 런타임 상태를 직렬화하여 안전하게 저장하고 복원하기 위한 범용 Save / Load Framework입니다.



게임별 SaveData와 게임 상태 적용 책임은 사용하는 프로젝트가 담당하고, Framework는 Serializer와 Storage를 통해 저장 및 복원 흐름을 제공합니다.



Primary / Backup 저장, Temp 기반 Safe Write, Backup Recovery, 명확한 Result 기반 오류 처리를 중심으로 설계했습니다.



특정 게임이나 장르에 종속되지 않으며 Unity Package Manager를 통해 독립적으로 설치할 수 있습니다.



\---



\## 주요 기능



\- Generic SaveData 저장 및 불러오기

\- `Save<T>()`

\- `Load<T>()`

\- `Exists()`

\- `Delete()`

\- `SaveSlot` 기반 슬롯 관리

\- `LoadResult<T>` 기반 Load 상태 표현

\- Serializer 추상화

\- `JsonUtilitySaveSerializer`

\- Storage 추상화

\- `FileSaveStorage`

\- Temp 기반 Safe Write

\- Primary / Backup 이중 저장

\- Primary 손상 또는 읽기 실패 시 Backup Recovery

\- 저장 데이터가 없는 상태를 정상적인 `NotFound`로 표현

\- Result 기반 오류 처리

\- 게임 도메인과 저장 기술 책임 분리



\---



\## 저장소 구조



```text

ChoDogyuSave/

├─ SaveDevelopment/

│  └─ 패키지 개발 및 검증용 Unity 프로젝트

│

├─ com.chodogyu.save/

│  ├─ Runtime/

│  │  ├─ Serialization/

│  │  └─ Storage/

│  ├─ Tests/

│  ├─ Documentation\~/

│  ├─ package.json

│  ├─ README.md

│  └─ CHANGELOG.md

│

├─ .gitattributes

├─ .gitignore

└─ README.md

```



\### SaveDevelopment



Save / Load Framework의 개발, 테스트 및 통합 검증을 위한 Unity 프로젝트입니다.



실제 UPM 배포 대상에는 포함되지 않습니다.



\### com.chodogyu.save



실제로 배포하는 Unity Package Manager 패키지입니다.



다른 Unity 프로젝트에서는 이 폴더를 Git UPM 패키지로 설치하여 사용할 수 있습니다.



\---



\## 요구 사항



\- Unity 6.3 이상

\- ChoDogyu Core 1.0.0



개발 및 검증 환경:



```text

Unity 6.3 LTS

6000.3.9f1

```



Save Framework는 `CDG.Core.Results`의 Result 계열 타입을 사용합니다.



따라서 ChoDogyu Core를 먼저 설치해야 합니다.



다음 패키지는 필수 의존성이 아닙니다.



```text

ChoDogyu Data Framework

ChoDogyu Object Pooling

ChoDogyu General Editor Tools

```



\---



\## 설치



\### 1. ChoDogyu Core 설치



```text

https://github.com/ChoDoGyu/ChoDogyuCore.git?path=/com.chodogyu.core#v1.0.0

```



\### 2. ChoDogyu Save / Load Framework 설치



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



\---



\## 기본 구조



Save Framework의 핵심 구조:



```text

Game SaveData

↓

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



`SaveService`는 Serializer와 Storage 구현을 생성자에서 전달받습니다.



Singleton이나 Service Locator를 강제하지 않습니다.



\---



\## 기본 SaveData



SaveData는 사용하는 게임에서 직접 정의합니다.



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



Framework는 특정 게임 상태 구조를 요구하지 않습니다.



\---



\## Save



```csharp

using CDG.Core.Results;

using CDG.Save;

using CDG.Save.Serialization;

using CDG.Save.Storage;



FileSaveStorage storage =

&#x20;   new FileSaveStorage();



JsonUtilitySaveSerializer serializer =

&#x20;   new JsonUtilitySaveSerializer();



SaveService saveService =

&#x20;   new SaveService(storage, serializer);



SaveSlot slot =

&#x20;   new SaveSlot("player-main");



PlayerSaveData data =

&#x20;   new PlayerSaveData

&#x20;   {

&#x20;       PlayerName = "Player",

&#x20;       Level = 10,

&#x20;       Gold = 1000

&#x20;   };



Result result =

&#x20;   saveService.Save(slot, data);

```



Save 시 데이터는 한 번만 직렬화됩니다.



```text

SaveData

↓

Serialize

↓

byte\[]

├─ Primary

└─ Backup

```



Primary와 Backup은 정상 Save 직후 동일한 데이터를 가집니다.



\---



\## Load



```csharp

Result<LoadResult<PlayerSaveData>> result =

&#x20;   saveService.Load<PlayerSaveData>(slot);

```



Load 상태:



```text

NotFound

LoadedFromPrimary

LoadedFromBackup

```



Primary를 먼저 사용하고, 사용할 수 없는 경우 Backup Recovery를 시도합니다.



\---



\## NotFound



다음 상태:



```text

Primary 없음

Backup 없음

```



은 오류가 아닙니다.



```text

Result = Success

LoadResult.Status = NotFound

LoadResult.IsFound = false

```



로 반환합니다.



Framework는 기본 SaveData를 자동 생성하지 않습니다.



\---



\## Backup Recovery



Backup Recovery는 다음 상황에서 시도합니다.



```text

Primary 파일 없음

Primary Read 실패

Primary Deserialize 실패

```



Backup이 정상이라면:



```text

LoadedFromBackup

```



을 반환합니다.



Backup에서 복원에 성공하더라도 Primary를 자동으로 다시 저장하거나 수정하지는 않습니다.



\---



\## SaveSlot



저장 슬롯 이름 예:



```text

default

player

player-main

slot\_01

```



허용 문자:



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



잘못된 슬롯 이름은 `ArgumentException`으로 처리합니다.



기본 슬롯:



```csharp

SaveSlot.Default

```



\---



\## 기본 저장 경로



`FileSaveStorage` 기본 저장 위치:



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



\---



\## Safe Write



파일에 직접 데이터를 덮어쓰기 전에 Temp 파일에 먼저 기록합니다.



```text

byte\[]

→ Temp

→ 기록 성공

→ Target 교체

```



신규 파일은 Move, 기존 파일은 Replace 방식을 사용합니다.



이 기능은 직접 덮어쓰기보다 안전한 저장 흐름을 제공하기 위한 것이며 모든 플랫폼과 파일 시스템에서 완전한 Transaction 또는 절대적 Atomicity를 보장하지는 않습니다.



\---



\## Serializer



Serializer 책임:



```text

T

↕

byte\[]

```



기본 구현:



```text

JsonUtilitySaveSerializer

```



Unity `JsonUtility`와 UTF-8을 사용합니다.



다른 직렬화 방식이 필요하다면:



```text

ISaveSerializer

```



를 구현하여 교체할 수 있습니다.



\---



\## Storage



Storage 책임:



```text

byte\[]

↕

Persistence Medium

```



기본 구현:



```text

FileSaveStorage

```



다른 저장 매체가 필요하다면:



```text

ISaveStorage

```



를 구현할 수 있습니다.



\---



\## Error Codes



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



Storage 수준에서 파일이 존재하지 않는 경우 `save.storage\_not\_found`를 사용합니다.



`SaveService.Load<T>()`에서는 Primary와 Backup이 모두 존재하지 않는 경우 이를 정상적인 `NotFound` 상태로 변환합니다.



\---



\## Data Framework와의 관계



Save Framework는 ChoDogyu Data Framework에 의존하지 않습니다.



예를 들어:



```text

equippedWeaponId = "sword\_iron"

```



처럼 ID만 SaveData에 저장할 수 있습니다.



Load 후:



```text

sword\_iron

→ 실제 ItemData 조회

```



는 사용하는 게임이나 Data Layer가 담당합니다.



정리:



```text

Data

= 게임 데이터를 정의



Save

= 현재 플레이 상태를 기록

```



\---



\## 책임 범위



포함:



```text

Save

Load

Exists

Delete

SaveSlot

Serializer

Storage

File Storage

Safe Write

Primary / Backup

Backup Recovery

Result 기반 오류 처리

```



포함하지 않음:



```text

Autosave

Async Save / Load

Cloud Save

Encryption

Compression

Migration

Save Version 관리

PlayerPrefs Storage

Binary Serializer 기본 구현

Editor Tool

Automatic Saveable Discovery

Quit / Pause Hook

Backup History

Primary Auto Repair

게임 도메인 Validation

```



\---



\## 테스트



Unity Test Framework 기반으로 Runtime 기능을 검증했습니다.



```text

Runtime

105 Passed



Failed

0

```



주요 검증 범위:



```text

SaveSlot

LoadResult

Serialization

UTF-8

File Storage

Safe Write

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



\## UPM 설치 검증



완전히 새로운 Unity 6.3 프로젝트에서 다음 흐름을 검증했습니다.



```text

ChoDogyu Core v1.0.0 Git 설치

→ 성공



ChoDogyu Save / Load Framework Git 설치

→ 성공



CDG.Save Public API 참조

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



개발 프로젝트 또는 로컬 패키지 경로에 의존하지 않는 Git UPM 독립 설치를 확인했습니다.



\---



\## 버전



현재 패키지 버전:



```text

v1.0.0

```



주요 변경 사항:



```text

com.chodogyu.save/CHANGELOG.md

```



패키지 사용법:



```text

com.chodogyu.save/README.md

```



세부 설계 및 사용 규칙:



```text

com.chodogyu.save/Documentation\~/index.md

```

