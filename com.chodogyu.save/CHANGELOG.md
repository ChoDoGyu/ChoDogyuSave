\# Changelog



ChoDogyu Save / Load Framework 패키지의 주요 변경 사항을 기록합니다.



\## \[1.0.0] - 2026-09-16



첫 정식 배포 버전입니다.



\### Added



\#### SaveService



\- Generic SaveData를 위한 `Save<T>()` API 추가

\- `Load<T>()` API 추가

\- `Exists()` API 추가

\- `Delete()` API 추가

\- Storage와 Serializer 생성자 주입 지원

\- Singleton 및 Service Locator 비사용

\- SaveData를 한 번만 직렬화한 뒤 동일한 데이터를 Primary와 Backup에 기록

\- Primary 저장 실패 시 Backup 저장 중단

\- Primary 저장 성공 후 Backup 저장 실패 시 `BackupWriteFailed` 반환

\- Primary 우선 Load

\- Primary 파일이 없을 경우 Backup Load

\- Primary 읽기 실패 시 Backup Recovery

\- Primary 역직렬화 실패 시 Backup Recovery

\- Backup 성공 시 `LoadedFromBackup` 상태 반환

\- Backup Load 성공 후 Primary 자동 복구 미수행

\- Primary와 Backup이 모두 없을 경우 정상적인 `NotFound` 결과 반환



\#### SaveSlot



\- 문자열 기반 저장 슬롯 추가

\- `SaveSlot.Default` 제공

\- 기본 슬롯 이름 `default`

\- 슬롯 이름 길이 1\~64 제한

\- 소문자 ASCII `a-z` 허용

\- 숫자 `0-9` 허용

\- `-` 및 `\_` 허용

\- 대문자 차단

\- 공백 차단

\- 마침표 차단

\- 경로 구분자 차단

\- 경로 탐색 문자열 차단

\- 한글 및 기타 문장부호 차단

\- Ordinal 기반 Equality 및 HashCode 제공

\- 잘못된 슬롯 이름에 `ArgumentException` 사용



\#### Load Result



\- `LoadResult<T>` 추가

\- `SaveLoadStatus` 추가

\- `NotFound` 상태 제공

\- `LoadedFromPrimary` 상태 제공

\- `LoadedFromBackup` 상태 제공

\- `IsFound` 제공

\- `NotFound` 상태에서 `Value` 접근 차단

\- 정상 Load 값으로 null 허용



\#### Serialization



\- `ISaveSerializer` 추상화 추가

\- Serializer의 책임을 `T ↔ byte\[]` 변환으로 제한

\- 파일 경로 및 Storage 책임과 분리

\- `JsonUtilitySaveSerializer` 기본 구현 추가

\- Unity `JsonUtility` 기반 JSON 직렬화

\- UTF-8 인코딩 사용

\- UTF-8 BOM 미사용

\- 잘못된 UTF-8 데이터 검증

\- null 직렬화 입력 검증

\- null 및 빈 역직렬화 데이터 검증

\- 직렬화 실패 Result 처리

\- 역직렬화 실패 Result 처리



\#### Storage



\- `ISaveStorage` 추상화 추가

\- `Write()` 제공

\- `Read()` 제공

\- `Exists()` 제공

\- `Delete()` 제공

\- `SaveStorageCopy` 추가

\- `Primary` 저장 파일 종류 제공

\- `Backup` 저장 파일 종류 제공

\- Temp 파일을 Public Storage 종류에서 제외

\- `FileSaveStorage` 기본 구현 추가



\#### File Storage



\- 기본 저장 경로 `Application.persistentDataPath/Saves`

\- 사용자 지정 저장 루트 경로 지원

\- 사용자 지정 경로 절대 경로 정규화

\- Primary 확장자 `.save`

\- Backup 확장자 `.save.bak`

\- Temp 확장자 `.save.tmp`

\- 저장 디렉터리 자동 생성

\- 파일 Read 지원

\- 파일 Exists 지원

\- 파일 Delete 지원

\- 존재하지 않는 파일 Delete를 성공으로 처리

\- 존재하지 않는 파일 Read 시 `StorageNotFound` 반환

\- 예상 파일 경로에 디렉터리가 존재하는 비정상 상태 검증



\#### Safe Write



\- 실제 대상 파일 직접 덮어쓰기 대신 Temp 우선 기록 방식 추가

\- 신규 저장 파일에서 Temp → Target 이동

\- 기존 저장 파일에서 Temp 기반 파일 교체

\- 저장 실패 시 Temp 파일 정리 시도

\- Primary와 Backup 모두 Safe Write 적용

\- Safe Write 후 Temp 파일 미잔존 검증

\- 완전한 파일 시스템 Transaction 또는 모든 플랫폼의 절대적 Atomicity를 보장하지 않는 정책 적용



\#### Primary / Backup



\- 하나의 저장 슬롯에 Primary 및 Backup 복사본 제공

\- 동일한 직렬화 결과를 Primary와 Backup에 기록

\- Backup을 이전 Save History가 아닌 현재 데이터의 안전 복사본으로 정의

\- Primary 우선 사용

\- Primary 손상 시 Backup Recovery

\- Primary 유실 시 Backup Recovery

\- Primary I/O 실패 시 Backup Recovery

\- Backup 복구 성공 여부를 `LoadResult<T>` 상태로 구분

\- Backup 자동 History 관리 미지원

\- Backup Load 후 Primary 자동 Repair 미지원



\#### Corruption Handling



\- Primary 역직렬화 실패 후 Backup 복구 지원

\- Primary와 Backup 모두 역직렬화 실패 시 `CorruptedData`

\- Primary 손상 및 Backup 부재 시 `CorruptedData`

\- Storage I/O 오류와 데이터 손상 오류 구분

\- 손상된 데이터를 자동 삭제하거나 초기화하지 않음

\- 게임 도메인 수준의 SaveData 의미 검증을 Framework에서 수행하지 않음



\#### Exists / Delete



\- Primary가 존재하면 Backup 조회 없이 `Exists()` 성공

\- Primary가 없으면 Backup 존재 여부 확인

\- Primary 또는 Backup 중 하나라도 존재하면 `true`

\- 파일 내용의 역직렬화 가능 여부와 존재 여부 책임 분리

\- Primary → Backup 순서 Delete

\- Primary Delete 실패 시 Backup Delete 중단

\- Backup Delete 실패 시 실패 Result 반환

\- Primary와 Backup Delete를 원자적 작업으로 보장하지 않음



\#### Error Codes



\- `save.invalid\_data`

\- `save.serialization\_failed`

\- `save.deserialization\_failed`

\- `save.storage\_not\_found`

\- `save.storage\_read\_failed`

\- `save.storage\_write\_failed`

\- `save.backup\_write\_failed`

\- `save.storage\_delete\_failed`

\- `save.corrupted\_data`



\#### Package Structure



\- Runtime Assembly `CDG.Save`

\- Root Namespace `CDG.Save`

\- Serialization Namespace `CDG.Save.Serialization`

\- Storage Namespace `CDG.Save.Storage`

\- ChoDogyu Core만 필수 참조

\- ChoDogyu Data Framework 비의존

\- ChoDogyu Object Pooling 비의존

\- ChoDogyu Editor Tools 비의존

\- Editor Assembly 미포함

\- Samples 미포함

\- 특정 게임 및 장르 코드 비포함



\#### Tests



\- Runtime Test 105개 통과

\- SaveSlot 규칙 검증

\- LoadResult 상태 검증

\- JsonUtility 직렬화 및 역직렬화 검증

\- UTF-8 Round Trip 검증

\- FileSaveStorage 경로 검증

\- 파일 Write / Read / Exists / Delete 검증

\- Safe Write 검증

\- Temp 파일 정리 검증

\- Primary / Backup 저장 검증

\- SaveService Save 검증

\- SaveService Load 검증

\- NotFound 검증

\- Backup Recovery 검증

\- CorruptedData 검증

\- Exists / Delete 통합 검증

\- 실제 `JsonUtilitySaveSerializer` 및 `FileSaveStorage` 통합 검증

\- 재저장 시 최신 데이터 교체 검증

\- 서로 다른 SaveSlot 데이터 격리 검증

\- End-to-End Save / Load 검증



\#### UPM Verification



\- 완전히 새로운 Unity 6.3 프로젝트에서 Git UPM 설치 검증

\- ChoDogyu Core v1.0.0 선설치 검증

\- ChoDogyu Save / Load Framework Git 설치 검증

\- `CDG.Save` Public API 참조 검증

\- `FileSaveStorage` 생성 검증

\- `JsonUtilitySaveSerializer` 생성 검증

\- 실제 Save 성공 검증

\- 실제 Load 성공 검증

\- `LoadedFromPrimary` 상태 검증

\- 저장 데이터 값 복원 검증

\- Unity 종료 및 재실행 후 기존 저장 데이터 Load 검증

\- 개발 프로젝트 및 로컬 패키지 경로 비의존 검증



\#### Documentation



\- 패키지 README 추가

\- CHANGELOG 추가

\- 상세 Documentation 추가

\- 설치 방법 문서화

\- Save / Load / Exists / Delete 사용법 문서화

\- SaveSlot 규칙 문서화

\- Primary / Backup 정책 문서화

\- Safe Write 정책 문서화

\- Backup Recovery 정책 문서화

\- 오류 코드 문서화

\- Serializer 및 Storage 확장 지점 문서화

\- Save Framework 책임 범위 문서화

\- Data Framework와의 책임 분리 문서화

