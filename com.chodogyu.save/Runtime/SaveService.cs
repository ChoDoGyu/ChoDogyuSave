using System;
using CDG.Core.Results;
using CDG.Save.Serialization;
using CDG.Save.Storage;

namespace CDG.Save
{
    /// <summary>
    /// 저장 데이터의 직렬화와 Storage 접근을 조정하는 Save / Load 서비스입니다.
    /// 실제 파일 형식과 저장 매체 구현은 각각 Serializer와 Storage에 위임합니다.
    /// </summary>
    public sealed class SaveService
    {
        private readonly ISaveStorage storage;
        private readonly ISaveSerializer serializer;

        /// <summary>
        /// 지정한 Storage와 Serializer를 사용하여 SaveService를 생성합니다.
        /// </summary>
        /// <param name="storage">직렬화된 데이터를 저장하고 읽을 Storage입니다.</param>
        /// <param name="serializer">저장 데이터를 직렬화하고 역직렬화할 Serializer입니다.</param>
        /// <exception cref="ArgumentNullException">
        /// Storage 또는 Serializer가 null인 경우 발생합니다.
        /// </exception>
        public SaveService(ISaveStorage storage, ISaveSerializer serializer)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <summary>
        /// 지정한 저장 데이터를 직렬화하여 기본 저장 파일과 백업 저장 파일에 기록합니다.
        /// 데이터는 한 번만 직렬화되며 동일한 직렬화 결과를 두 저장 파일에 사용합니다.
        /// </summary>
        /// <typeparam name="T">저장할 데이터의 타입입니다.</typeparam>
        /// <param name="slot">데이터를 기록할 저장 슬롯입니다.</param>
        /// <param name="data">저장할 데이터입니다.</param>
        /// <returns>직렬화 및 저장 작업의 성공 또는 실패 결과입니다.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="slot"/>이 null인 경우 발생합니다.
        /// </exception>
        public Result Save<T>(SaveSlot slot, T data)
        {
            ValidateSlot(slot);

            Result<byte[]> serializationResult = serializer.Serialize(data);

            if (serializationResult.IsFailure)
            {
                return Result.Failure(serializationResult.Error);
            }

            return WriteCopies(slot, serializationResult.Value);
        }

        /// <summary>
        /// 지정한 저장 슬롯에서 데이터를 불러옵니다.
        /// 기본 저장 파일을 우선 사용하며 기본 파일이 없으면 백업 저장 파일을 확인합니다.
        /// 두 파일 모두 존재하지 않는 경우 실패가 아닌 NotFound 결과를 반환합니다.
        /// </summary>
        /// <typeparam name="T">불러올 저장 데이터의 타입입니다.</typeparam>
        /// <param name="slot">데이터를 불러올 저장 슬롯입니다.</param>
        /// <returns>불러오기 상태와 데이터를 포함하는 성공 결과 또는 실패 결과입니다.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="slot"/>이 null인 경우 발생합니다.
        /// </exception>
        public Result<LoadResult<T>> Load<T>(SaveSlot slot)
        {
            ValidateSlot(slot);

            Result<byte[]> primaryReadResult = storage.Read(slot, SaveStorageCopy.Primary);

            if (primaryReadResult.IsSuccess)
            {
                Result<T> primaryDeserializeResult = serializer.Deserialize<T>(primaryReadResult.Value);

                if (primaryDeserializeResult.IsFailure)
                {
                    return Result<LoadResult<T>>.Failure(primaryDeserializeResult.Error);
                }

                return Result<LoadResult<T>>.Success(
                    LoadResult<T>.FromPrimary(primaryDeserializeResult.Value));
            }

            if (primaryReadResult.Error.Code != SaveErrorCodes.StorageNotFound)
            {
                return Result<LoadResult<T>>.Failure(primaryReadResult.Error);
            }

            Result<byte[]> backupReadResult = storage.Read(slot, SaveStorageCopy.Backup);

            if (backupReadResult.IsFailure)
            {
                if (backupReadResult.Error.Code == SaveErrorCodes.StorageNotFound)
                {
                    return Result<LoadResult<T>>.Success(LoadResult<T>.NotFound());
                }

                return Result<LoadResult<T>>.Failure(backupReadResult.Error);
            }

            Result<T> backupDeserializeResult = serializer.Deserialize<T>(backupReadResult.Value);

            if (backupDeserializeResult.IsFailure)
            {
                return Result<LoadResult<T>>.Failure(backupDeserializeResult.Error);
            }

            return Result<LoadResult<T>>.Success(
                LoadResult<T>.FromBackup(backupDeserializeResult.Value));
        }

        internal Result WriteCopies(SaveSlot slot, byte[] data)
        {
            Result primaryResult = storage.Write(slot, SaveStorageCopy.Primary, data);

            if (primaryResult.IsFailure)
            {
                return primaryResult;
            }

            Result backupResult = storage.Write(slot, SaveStorageCopy.Backup, data);

            if (backupResult.IsFailure)
            {
                return Result.Failure(new ResultError(
                    SaveErrorCodes.BackupWriteFailed,
                    $"기본 저장 파일은 기록되었지만 백업 저장 파일 기록에 실패했습니다. {backupResult.Error.Message}"));
            }

            return Result.Success();
        }

        private static void ValidateSlot(SaveSlot slot)
        {
            if (slot == null)
            {
                throw new ArgumentNullException(nameof(slot));
            }
        }
    }
}